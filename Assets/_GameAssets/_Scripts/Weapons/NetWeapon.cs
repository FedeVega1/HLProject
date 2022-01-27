using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetWeapon : CachedNetTransform
{
    [SyncVar] bool serverInitialized;

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
        RpcSyncWeaponData(weaponData.GetInstanceID());

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
            print($"Server Fire! Range[] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
            Debug.DrawLine(weaponRay.origin, rayHit.point, Color.green, 5);
            RpcFireWeapon(rayHit.point);
        }
        else
        {
            RpcFireWeapon(weaponRay.origin + (weaponRay.direction * bulletData.maxTravelDistance));
            Debug.DrawRay(weaponRay.origin, weaponRay.direction, Color.red, 2);
        }

        fireTime = NetworkTime.time + weaponData.rateOfFire;
    }

    [Server]
    void AltFire()
    {

    }

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
        RpcSyncWeaponData(weaponData.GetInstanceID());
    }


    [ClientRpc(includeOwner = false)]
    public void RpcInitClientWeapon() => InitClientWeapon();

    void InitClientWeapon()
    {
        if (!serverInitialized || hasAuthority) return;

        clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<IWeapon>();
        if (clientWeapon != null)
        {
            clientWeapon.Init(true, bulletData);
        }
        else
        {
            GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
            clientWeapon = nullWGO.AddComponent<NullWeapon>();
            clientWeapon.Init(true, bulletData);
            Debug.LogError($"Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
        }
    }

    [ClientRpc(includeOwner = false)]
    public void RpcFireWeapon(Vector3 destination)
    {
        clientWeapon.Fire(destination);
    }

    [ClientRpc]
    void RpcSyncWeaponData(int weaponID)
    {
        if (weaponData == null) weaponData = (WeaponData)Resources.InstanceIDToObject(weaponID);
        if (bulletData == null) bulletData = weaponData.bulletData;

        if (serverInitialized && clientWeapon == null) InitClientWeapon();
    }

    public WeaponData GetWeaponData() => weaponData;
}
