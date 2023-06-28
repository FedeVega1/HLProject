using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Experimental.AI;

public class NetWeapon : CachedNetTransform
{
    public const int FallOffQuality = 5;

    public WeaponType WType => weaponData.weaponType;

    [SyncVar] bool serverInitialized;

    public bool IsDroppingWeapons { get; private set; }

    bool IsMelee => weaponData.weaponType == WeaponType.Melee;

    bool clientWeaponInit, isReloading, onScope;
    int bulletsInMag, mags;
    double fireTime, scopeTime;
    RaycastHit rayHit;

    WeaponData weaponData;

    Player owningPlayer;
    BaseClientWeapon clientWeapon;
    LayerMask weaponLayerMask;
    BulletData bulletData;
    Transform firePivot;
    Collider[] raycastShootlastCollider;
    Coroutine meleeRoutine;

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

        raycastShootlastCollider = new Collider[10];

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
        if (owningPlayer.PlayerIsRunning() || NetworkTime.time < fireTime || NetworkTime.time < scopeTime || isReloading) return;

        if (IsMelee)
        {
            meleeRoutine = StartCoroutine(MeleeSwing());
            return;
        }

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

        fireTime = NetworkTime.time + weaponData.weaponAnimsTiming.fireMaxDelay;
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
                StartCoroutine(ApplyDistanceToDamage(hitPos, rayHit.distance));
                Debug.DrawLine(weaponRay.origin, hitPos, Color.green, 5);

                //HitBox hitBox = rayHit.transform.GetComponent<HitBox>();
                //if (hitBox != null)
                //{
                //    StartCoroutine(ApplyDistanceToDamage(hitBox, rayHit.distance));
                //    Debug.DrawLine(weaponRay.origin, hitPos, Color.green, 5);
                //    return;
                //}
            }

            if (rayHit.collider != null) Debug.LogFormat("Server Fire! Range[{0}] - HitPoint: {1}|{2}", bulletData.maxTravelDistance, rayHit.point, rayHit.collider.name);
            //else RpcFireWeapon(weaponRay.origin + (weaponRay.direction * distance), false);
        }

        bulletsInMag--;
        //if (bulletsInMag == 0) ScopeOut();

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

    [Server]
    void AltFire()
    {

    }

    [Server]
    void Reload()
    {
        if (IsMelee || isReloading || onScope || bulletsInMag >= weaponData.bulletsPerMag || mags <= 0) return;
        owningPlayer.TogglePlayerRunAbility(false);
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
    void ScopeIn()
    {
        if (IsMelee || owningPlayer.PlayerIsRunning() || isReloading || onScope) return;
        owningPlayer.TogglePlayerScopeStatus(true);
        onScope = true;
        scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomInSpeed;
    }

    [Server]
    void ScopeOut()
    {
        if (IsMelee || !onScope) return;
        owningPlayer.TogglePlayerScopeStatus(false);
        onScope = false;
        scopeTime = NetworkTime.time + weaponData.weaponAnimsTiming.zoomOutSpeed;
    }

    [Server]
    public void OnWeaponSwitch()
    {
        if (meleeRoutine != null)
        {
            StopCoroutine(meleeRoutine);
            meleeRoutine = null;
        }
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

    [Command]
    public void CmdRequestScopeIn() => ScopeIn();

    [Command]
    public void CmdRequestScopeOut() => ScopeOut();

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
    IEnumerator MeleeSwing()
    {
        yield return new WaitForSeconds(weaponData.weaponAnimsTiming.meleeHitboxIn);

        float hitboxTime = 0;
        while (hitboxTime < weaponData.weaponAnimsTiming.meleeHitboxOut)
        {
            int quantity = Physics.OverlapBoxNonAlloc(firePivot.position + firePivot.forward * .5f, new Vector3(.5f, .5f, .5f), raycastShootlastCollider, firePivot.rotation, weaponLayerMask);

            for (int i = 0; i < quantity; i++)
            {
                HitBox hitBoxToHit = raycastShootlastCollider[i].GetComponent<HitBox>();
                if (hitBoxToHit != null) continue;

                hitBoxToHit.GetCharacterScript().TakeDamage(weaponData.meleeDamage, DamageType.Base);
                meleeRoutine = null;
                yield break;
            }

            hitboxTime += Time.deltaTime;
            yield return null;
        }

        meleeRoutine = null;
    }

    [Server]
    IEnumerator ApplyDistanceToDamage(Vector3 hitOrigin, float distance)
    {
        yield return new WaitForSeconds((distance / bulletData.initialSpeed) + weaponData.weaponAnimsTiming.initFire);
        int quantity = Physics.OverlapSphereNonAlloc(hitOrigin, .1f, raycastShootlastCollider, weaponLayerMask);

        for (int i = 0; i < quantity; i++)
        {
            HitBox hitBoxToHit = raycastShootlastCollider[i].GetComponent<HitBox>();
            if (hitBoxToHit == null) continue;

            hitBoxToHit.GetCharacterScript().TakeDamage(bulletData.damage, DamageType.Bullet);
            yield break;
        }
    }

    [Server]
    IEnumerator ReloadRoutine()
    {
        yield return new WaitForSeconds(weaponData.weaponAnimsTiming.reload);
        bulletsInMag = weaponData.bulletsPerMag;
        mags--;
        isReloading = false;
        owningPlayer.TogglePlayerRunAbility(true);
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
