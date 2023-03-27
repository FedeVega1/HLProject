using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetWeapon : CachedNetTransform
{
    public const int FallOffQuality = 5;

    public WeaponType WType => weaponData.weaponType;

    [SyncVar] bool serverInitialized;

    public bool IsDroppingWeapons { get; private set; }

    bool clientWeaponInit, isReloading;
    int bulletsInMag, mags;
    double fireTime;
    RaycastHit rayHit;

    WeaponData weaponData;

    Player owningPlayer;
    BaseClientWeapon clientWeapon;
    LayerMask weaponLayerMask;
    BulletData bulletData;
    Transform firePivot;

    public override void OnStartClient()
    {
        if (hasAuthority) return;
        CmdRequestWeaponData();
    }

    void Start() => weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");

    public void Init(WeaponData data, Player owner)
    {
        weaponData = data;
        RpcSyncWeaponData(weaponData.name);

        bulletData = data.bulletData;
        owningPlayer = owner;

        mags = weaponData.mags;
        bulletsInMag = weaponData.bulletsPerMag;

        GameObject weaponObj = Instantiate(weaponData.clientPrefab, MyTransform);
        BaseClientWeapon cWeapon = weaponObj.GetComponent<BaseClientWeapon>();
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

        serverInitialized = true;
        RpcInitClientWeapon();
    }

    #region Server

    [Server]
    void Fire()
    {
        if (NetworkTime.time < fireTime || isReloading) return;
        if (bulletsInMag <= 0) return;

        MyTransform.eulerAngles = new Vector3(owningPlayer.GetPlayerCameraXAxis(), MyTransform.eulerAngles.y, MyTransform.eulerAngles.z);

        switch (bulletData.type)
        {
            case BulletType.RayCast:
                ShootRayCastBullet();
                break;

            case BulletType.Physics:
                GameObject granade = Instantiate(bulletData.bulletPrefab, MyTransform.position + MyTransform.forward * 1.5f, MyTransform.rotation);
                Bullet granadeBulletScript = granade.GetComponent<Bullet>();
                granadeBulletScript.Init(bulletData.initialSpeed, true);
                granadeBulletScript.PhysicsTravelTo(true, firePivot.forward, bulletData.radius, false, bulletData.timeToExplode);
                granadeBulletScript.OnExplode += OnBulletExplode;
                NetworkServer.Spawn(granade);
                break;
        }

        fireTime = NetworkTime.time + weaponData.weaponAnimsTiming.fire;
    }

    [Server]
    void OnBulletExplode(List<HitBox> hitBoxList, Vector3 finalPos, Quaternion finalRot)
    {
        int size = hitBoxList.Count;
        for (int i = 0; i < size; i++)
        {
            float distance = Vector3.Distance(finalPos, hitBoxList[i].MyTransform.position);
            float damageFalloff = Mathf.Clamp(bulletData.radius - distance, 0, bulletData.radius) / bulletData.radius;
            hitBoxList[i].GetCharacterScript().TakeDamage(bulletData.damage * damageFalloff, bulletData.damageType);
        }
        RpcBulletExplosion(finalPos, finalRot);
    }

    [Server]
    void ShootRayCastBullet()
    {
        Ray weaponRay = new Ray(firePivot.position, firePivot.forward);

        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
        {
            Vector3 hitPos = rayHit.point;

            bool fallOffCheck = CheckBulletFallOff(ref weaponRay, ref rayHit, out float distance);
            if (!fallOffCheck) hitPos = rayHit.point;
            RpcFireWeapon(hitPos, true);

            if (fallOffCheck)
            {
                HitBox hitBox = rayHit.transform.GetComponent<HitBox>();
                if (hitBox != null)
                {
                    StartCoroutine(ApplyDistanceToDamage(hitBox, rayHit.distance));
                    Debug.DrawLine(weaponRay.origin, hitPos, Color.green, 5);
                    return;
                }
            }

            if (rayHit.collider != null) Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
            //else RpcFireWeapon(weaponRay.origin + (weaponRay.direction * distance), false);
        }

        bulletsInMag--;
        Vector3 fallOff = Vector3.down * bulletData.fallOff;
        RpcFireWeapon(weaponRay.origin + ((weaponRay.direction + fallOff) * bulletData.maxTravelDistance), false);

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
                print($"Server Fire! Range[{distance}] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
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

    [Server]
    void AltFire()
    {

    }

    [Server]
    void Reload()
    {
        if (isReloading || bulletsInMag >= weaponData.bulletsPerMag || mags <= 0) return;
        isReloading = true;
        RpcReloadWeapon();
        StartCoroutine(ReloadRoutine());
    }

    [Server]
    public void DropClientWeaponAndDestroy()
    {
        IsDroppingWeapons = true;
        RpcDropWeapon();
    }

    [Server]
    IEnumerator ApplyDistanceToDamage(HitBox hitBoxToHit, float distance)
    {
        yield return new WaitForSeconds(distance / bulletData.initialSpeed);
        hitBoxToHit.GetCharacterScript().TakeDamage(bulletData.damage, DamageType.Bullet);
    }

    #endregion

    #region Commands

    [Command]
    public void CmdRequestFire()
    {
        Fire();
    }

    [Command]
    public void CmdRequestAltFire()
    {
        AltFire();
    }

    [Command]
    public void CmdRequestReload()
    {
        Reload();
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestWeaponData()
    {
        RpcSyncWeaponData(weaponData.name);
    }

    [Command(requiresAuthority = false)]
    public void CmdOnPlayerDroppedWeapon()
    {
        if (!IsDroppingWeapons) return;
        NetworkServer.Destroy(gameObject);
    }

    #endregion

    #region RPCs

    [ClientRpc]
    public void RpcBulletExplosion(Vector3 pos, Quaternion rot)
    {
        GameObject granade = Instantiate(bulletData.bulletPrefab, pos, rot);
        Bullet granadeBulletScript = granade.GetComponent<Bullet>();
        granadeBulletScript.Init(0, true);
        granadeBulletScript.PhysicsTravelTo(false, Vector3.zero, bulletData.radius, false, 0);
    }

    [ClientRpc(includeOwner = false)]
    public void RpcToggleClientWeapon(bool toggle)
    {
        StartCoroutine(WaitForServerAndClientInitialization(true, true, () => {
            if (toggle) clientWeapon.DrawWeapon();
            else clientWeapon.HolsterWeapon();
        }));
    }

    [ClientRpc(includeOwner = false)]
    public void RpcInitClientWeapon()
    {
        print("INIT CLIENT WEAPON");
        InitClientWeapon();
    }

    [ClientRpc(includeOwner = false)]
    public void RpcFireWeapon(Vector3 destination, bool didHit)
    {
        clientWeapon.Fire(destination, didHit, -1);
    }

    [ClientRpc(includeOwner = false)]
    public void RpcReloadWeapon()
    {
        clientWeapon.Reload();
    }

    [ClientRpc]
    void RpcSyncWeaponData(string weaponAssetName)//(int weaponID)
    {
        if (weaponData == null) weaponData = Resources.Load<WeaponData>($"Weapons/{weaponAssetName}");
        if (bulletData == null) bulletData = weaponData.bulletData;

        if (serverInitialized && clientWeapon == null) InitClientWeapon();
    }

    [ClientRpc]
    public void RpcDropWeapon()
    {
        if (hasAuthority)
        {
            CmdOnPlayerDroppedWeapon();
            return;
        }

            StartCoroutine(WaitForServerAndClientInitialization(true, true, () => {
            clientWeapon.DropProp();
            CmdOnPlayerDroppedWeapon();
        }));
    }

    #endregion

    #region Coroutines

    [Server]
    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(2);
        bulletsInMag = weaponData.bulletsPerMag;
        mags--;
        isReloading = false;
    }

    [Client]
    IEnumerator WaitForServerAndClientInitialization(bool waitForClient, bool waitForServer, System.Action OnServerAndClientInitialized)
    {
        int debug = 0;
        while ((!serverInitialized && waitForServer) || (!clientWeaponInit && waitForClient))
        {
            Debug.LogFormat("Waiting for client Initialization {0}", debug++);
            yield return new WaitForSeconds(.1f);
        }
        OnServerAndClientInitialized?.Invoke();
    }

    #endregion

    [Client]
    void InitClientWeapon()
    {
        if (hasAuthority) return;
        StartCoroutine(WaitForServerAndClientInitialization(false, true, () =>
        {
            clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<BaseClientWeapon>();
            if (clientWeapon != null)
            {
                clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
            }
            else
            {
                GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
                clientWeapon = nullWGO.AddComponent<NullWeapon>();
                clientWeapon.Init(true, weaponData, bulletData, weaponData.propPrefab);
                Debug.LogError("Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
            }

            clientWeaponInit = true;
        }));
    }

    public WeaponData GetWeaponData() => weaponData;
}
