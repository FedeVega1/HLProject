using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetWeapon : CachedNetTransform
{
    public WeaponType WType => weaponData.weaponType;

    [SyncVar] bool serverInitialized;

    public bool IsDroppingWeapons { get; private set; }

    bool clientWeaponInit;
    double fireTime;
    RaycastHit rayHit;

    WeaponData weaponData;

    Player owningPlayer;
    IWeapon clientWeapon;
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

        serverInitialized = true;
        RpcInitClientWeapon();
    }

    #region Server

    [Server]
    void Fire()
    {
        if (NetworkTime.time < fireTime) return;

        MyTransform.eulerAngles = new Vector3(owningPlayer.GetPlayerCameraXAxis(), MyTransform.eulerAngles.y, MyTransform.eulerAngles.z);

        Ray weaponRay = new Ray(firePivot.position, firePivot.forward);
        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
        {
            //float height = MyTransform.position.y;
            //float angle = owningPlayer.GetPlayerCameraXAxis();
            //float sin = angle > 0 ? Mathf.Sin(angle) : 1, cos = angle > 0 ? Mathf.Cos(angle) : 1;
            //float speed = bulletData.initialSpeed;

            //float root = Mathf.Sqrt(Mathf.Pow(speed * sin, 2) + 2 * Physics.gravity.y * height);
            //float range = speed * cos * (speed * sin + root) / Physics.gravity.y;
            //float range = Mathf.Pow(bulletData.initialSpeed, 2) * Mathf.Sin(2 * angle) / Physics.gravity.y;
            Character chrScript = rayHit.transform.GetComponent<Character>();
            if (chrScript != null)
            {
                ApplyDistanceToDamage(chrScript, rayHit.distance);
                Debug.DrawLine(weaponRay.origin, rayHit.point, Color.green, 5);
            }
            else
            {
                Debug.DrawLine(weaponRay.origin, rayHit.point, Color.yellow, 5);
            }

            print($"Server Fire! Range[] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
            RpcFireWeapon(rayHit.point, true);
        }
        else
        {
            RpcFireWeapon(weaponRay.origin + (weaponRay.direction * bulletData.maxTravelDistance), false);
            Debug.DrawRay(weaponRay.origin, weaponRay.direction, Color.red, 2);
        }

        fireTime = NetworkTime.time + weaponData.rateOfFire;
    }

    [Server]
    void AltFire()
    {

    }

    [Server]
    public void DropClientWeaponAndDestroy()
    {
        IsDroppingWeapons = true;
        RpcDropWeapon();
    }

    [Server]
    IEnumerator ApplyDistanceToDamage(Character chrToHit, float distance)
    {
        yield return new WaitForSeconds(distance / bulletData.initialSpeed);
        chrToHit.TakeDamage(bulletData.damage, DamageType.Bullet);
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
        clientWeapon.Fire(destination, didHit);
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

    [Client]
    void InitClientWeapon()
    {
        if (hasAuthority) return;
        StartCoroutine(WaitForServerAndClientInitialization(false, true, () =>
        {
            clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<IWeapon>();
            if (clientWeapon != null)
            {
                clientWeapon.Init(true, bulletData, weaponData.propPrefab);
            }
            else
            {
                GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
                clientWeapon = nullWGO.AddComponent<NullWeapon>();
                clientWeapon.Init(true, bulletData, weaponData.propPrefab);
                Debug.LogError($"Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
            }

            clientWeaponInit = true;
        }));
    }

    [Client]
    IEnumerator WaitForServerAndClientInitialization(bool waitForClient, bool waitForServer, System.Action OnServerAndClientInitialized)
    {
        int debug = 0;
        while ((!serverInitialized && waitForServer) || (!clientWeaponInit && waitForClient))
        {
            print($"Waiting for client Initialization {debug++}");
            yield return new WaitForSeconds(.1f);
        }
        OnServerAndClientInitialized?.Invoke();
    }

    public WeaponData GetWeaponData() => weaponData;
}
