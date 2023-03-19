using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetWeapon : CachedNetTransform
{
    public const int FallOffQuality = 5;

    public WeaponType WType => weaponData.weaponType;

    NetworkVariable<bool> serverInitialized = new NetworkVariable<bool>();

    public bool IsDroppingWeapons { get; private set; }

    bool clientWeaponInit;
    int bulletsInMag, mags;
    double fireTime;
    RaycastHit rayHit;

    WeaponData weaponData;

    Player owningPlayer;
    IWeapon clientWeapon;
    LayerMask weaponLayerMask;
    BulletData bulletData;
    Transform firePivot;

    protected override void OnClientSpawn()
    {
        if (IsOwner) return;
        RequestWeaponData_ServerRpc();
    }

    void Start() => weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");

    public void Init(WeaponData data, Player owner)
    {
        weaponData = data;
        SyncWeaponData_ClientRpc(weaponData.name);

        bulletData = data.bulletData;
        owningPlayer = owner;

        mags = weaponData.mags;
        bulletsInMag = weaponData.bulletsPerMag;

        GameObject weaponObj = Instantiate(weaponData.clientPrefab, MyTransform);
        IWeapon cWeapon = weaponObj.GetComponent<IWeapon>();
        if (cWeapon != null)
        {
            cWeapon.ToggleAllViewModels(false);
            firePivot = cWeapon.GetVirtualPivot();
            firePivot.SetParent(weaponObj.transform);
        }
        else
        {
            firePivot = MyTransform;
        }

        serverInitialized.Value = true;
        InitClientWeapon_ClientRpc(SendRpcToEveryoneExceptPlayer);
    }

    #region Server

    void Fire_Server()
    {
        if (NetTime < fireTime) return;

        MyTransform.eulerAngles = new Vector3(owningPlayer.GetPlayerCameraXAxis_Server(), MyTransform.eulerAngles.y, MyTransform.eulerAngles.z);

        switch (bulletData.type)
        {
            case BulletType.RayCast:
                ShootRayCastBullet_Server();
                break;

            case BulletType.Physics:
                GameObject granade = Instantiate(bulletData.bulletPrefab, MyTransform.position + MyTransform.forward * 1.5f, MyTransform.rotation);
                Bullet granadeBulletScript = granade.GetComponent<Bullet>();
                granadeBulletScript.Init(bulletData.initialSpeed, true);
                granadeBulletScript.PhysicsTravelTo(true, firePivot.forward, bulletData.radius, false, bulletData.timeToExplode);
                granadeBulletScript.OnExplode += OnBulletExplode_Server;
                granade.GetComponent<NetworkObject>().Spawn();
                //NetworkServer.Spawn(granade);
                break;
        }

        fireTime = NetTime + weaponData.weaponAnimsTiming.fire;
    }

    void OnBulletExplode_Server(List<HitBox> hitBoxList, Vector3 finalPos, Quaternion finalRot)
    {
        int size = hitBoxList.Count;
        for (int i = 0; i < size; i++)
        {
            float distance = Vector3.Distance(finalPos, hitBoxList[i].MyTransform.position);
            float damageFalloff = Mathf.Clamp(bulletData.radius - distance, 0, bulletData.radius) / bulletData.radius;
            hitBoxList[i].GetCharacterScript().TakeDamage_Server(bulletData.damage * damageFalloff, bulletData.damageType);
        }
        BulletExplosion_ClientRpc(finalPos, finalRot);
    }

    void ShootRayCastBullet_Server()
    {
        Ray weaponRay = new Ray(firePivot.position, firePivot.forward);

        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
        {
            Vector3 hitPos = rayHit.point;

            bool fallOffCheck = CheckBulletFallOff(ref weaponRay, ref rayHit, out float distance);
            if (!fallOffCheck) hitPos = rayHit.point;
            FireWeapon_ClientRpc(hitPos, true, SendRpcToEveryoneExceptPlayer);

            if (fallOffCheck)
            {
                HitBox hitBox = rayHit.transform.GetComponent<HitBox>();
                if (hitBox != null)
                {
                    StartCoroutine(ApplyDistanceToDamage_Server(hitBox, rayHit.distance));
                    Debug.DrawLine(weaponRay.origin, hitPos, Color.green, 5);
                    return;
                }
            }

            if (rayHit.collider != null) Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
            //else RpcFireWeapon(weaponRay.origin + (weaponRay.direction * distance), false);
        }

        Vector3 fallOff = Vector3.down * bulletData.fallOff;
        FireWeapon_ClientRpc(weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), false, SendRpcToEveryoneExceptPlayer);

        Debug.DrawRay(weaponRay.origin, weaponRay.direction + fallOff, Color.red, 2);
        Debug.DrawLine(weaponRay.origin, weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), Color.red, 2);
    }

    bool CheckBulletFallOff(ref Ray weaponRay, ref RaycastHit rayHit, out float distance)
    {
        float fallStep = bulletData.maxTravelDistance / (float) FallOffQuality;
        distance = bulletData.maxTravelDistance - fallStep;
        float lastDistance = distance;
        Vector3 lastHit = rayHit.point;

        for (int i = 0; i < FallOffQuality; i++)
        {
            if (Physics.Raycast(weaponRay, out rayHit, distance, weaponLayerMask))
            {
                Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", distance, rayHit.point, rayHit.collider.name);
                Debug.DrawLine(weaponRay.origin, rayHit.point, Color.yellow, 5);

                lastDistance = distance;
                //distance -= fallStep;
                weaponRay.direction += Vector3.down * bulletData.fallOff;
                continue;
            }

            distance = lastDistance;
            rayHit.point = lastHit;
            return false;
        }

        return true;
    }

    void AltFire_Server()
    {

    }

    void Reload_Server()
    {
        if (bulletsInMag >= weaponData.bulletsPerMag || mags <= 0) return;
        clientWeapon.Reload();
        StartCoroutine(ReloadRoutine_Server());
    }

    public void DropClientWeaponAndDestroy_Server()
    {
        IsDroppingWeapons = true;
        DropWeapon_ClientRpc();
    }

    IEnumerator ApplyDistanceToDamage_Server(HitBox hitBoxToHit, float distance)
    {
        yield return new WaitForSeconds(distance / bulletData.initialSpeed);
        hitBoxToHit.GetCharacterScript().TakeDamage_Server(bulletData.damage, DamageType.Bullet);
    }

    #endregion

    #region Commands

    [ServerRpc]
    public void RequestFire_ServerRpc()
    {
        Fire_Server();
    }

    [ServerRpc]
    public void RequestAltFire_ServerRpc()
    {
        AltFire_Server();
    }

    [ServerRpc]
    public void RequestReload_ServerRpc()
    {
        Reload_Server();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestWeaponData_ServerRpc()
    {
        SyncWeaponData_ClientRpc(weaponData.name);
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerDroppedWeapon_ServerRpc()
    {
        if (!IsDroppingWeapons) return;
        NetObject.Despawn(true);
        //NetworkServer.Destroy(gameObject);
    }

    #endregion

    #region RPCs

    [ClientRpc]
    public void BulletExplosion_ClientRpc(Vector3 pos, Quaternion rot)
    {
        GameObject granade = Instantiate(bulletData.bulletPrefab, pos, rot);
        Bullet granadeBulletScript = granade.GetComponent<Bullet>();
        granadeBulletScript.Init(0, true);
        granadeBulletScript.PhysicsTravelTo(false, Vector3.zero, bulletData.radius, false, 0);
    }

    [ClientRpc]
    public void ToggleClientWeapon_ClientRpc(bool toggle, ClientRpcParams rpcParams)
    {
        StartCoroutine(WaitForServerAndClientInitialization_Client(true, true, () => {
            if (toggle) clientWeapon.DrawWeapon();
            else clientWeapon.HolsterWeapon();
        }));
    }

    [ClientRpc]
    public void InitClientWeapon_ClientRpc(ClientRpcParams rpcParams)
    {
        print("INIT CLIENT WEAPON");
        InitClientWeapon_Client();
    }

    [ClientRpc]
    public void FireWeapon_ClientRpc(Vector3 destination, bool didHit, ClientRpcParams rpcParams)
    {
        clientWeapon.Fire(destination, didHit);
    }

    [ClientRpc]
    void SyncWeaponData_ClientRpc(string weaponAssetName)//(int weaponID)
    {
        if (weaponData == null) weaponData = Resources.Load<WeaponData>($"Weapons/{weaponAssetName}");
        if (bulletData == null) bulletData = weaponData.bulletData;

        if (serverInitialized.Value && clientWeapon == null) InitClientWeapon_Client();
    }

    [ClientRpc]
    public void DropWeapon_ClientRpc()
    {
        if (IsOwner)
        {
            OnPlayerDroppedWeapon_ServerRpc();
            return;
        }

            StartCoroutine(WaitForServerAndClientInitialization_Client(true, true, () => {
            clientWeapon.DropProp();
            OnPlayerDroppedWeapon_ServerRpc();
        }));
    }

    #endregion

    #region Coroutines

    IEnumerator ReloadRoutine_Server()
    {
        yield return new WaitForSeconds(2);
        bulletsInMag = weaponData.bulletsPerMag;
        mags--;
    }

    IEnumerator WaitForServerAndClientInitialization_Client(bool waitForClient, bool waitForServer, System.Action OnServerAndClientInitialized)
    {
        int debug = 0;
        while ((!serverInitialized.Value && waitForServer) || (!clientWeaponInit && waitForClient))
        {
            Debug.LogFormat("Waiting for client Initialization {0}", debug++);
            yield return new WaitForSeconds(.1f);
        }
        OnServerAndClientInitialized?.Invoke();
    }

    #endregion

    void InitClientWeapon_Client()
    {
        if (IsOwner) return;
        StartCoroutine(WaitForServerAndClientInitialization_Client(false, true, () =>
        {
            clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<IWeapon>();
            if (clientWeapon != null)
            {
                clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
            }
            else
            {
                GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
                clientWeapon = nullWGO.AddComponent<NullWeapon>();
                clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
                Debug.LogError($"Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
            }

            clientWeaponInit = true;
        }));
    }

    public WeaponData GetWeaponData() => weaponData;
}
