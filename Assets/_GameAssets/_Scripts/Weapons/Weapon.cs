using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum BulletType { RayCast, Physics }

public interface IWeapon
{
    public void Init(bool isServer, BulletData data);

    public void Fire(Vector3 destination);
    public void AltFire(Vector3 destination);
    public void Scope();

    public void ToggleAllViewModels(bool toggle);

    public Transform GetVirtualPivot();
    public Transform GetWorldPivot();
}

public class Weapon : CachedTransform
{
    double fireTime;
    RaycastHit rayHit;

    IWeapon clientWeapon;
    LayerMask weaponLayerMask;
    WeaponData weaponData;
    BulletData bulletData;
    Transform firePivot;
    NetWeapon netWeapon;

    void Start()
    {
        weaponLayerMask = LayerMask.GetMask("PlayerHitBoxes", "SceneObjects");
    }

    public void Init(WeaponData data, NetWeapon serverSideWeapon)
    {
        weaponData = data;
        bulletData = data.bulletData;
        netWeapon = serverSideWeapon;

        clientWeapon = Instantiate(weaponData.clientPrefab, MyTransform).GetComponent<IWeapon>();
        if (clientWeapon != null)
        {
            clientWeapon.Init(false, bulletData);
            firePivot = clientWeapon.GetVirtualPivot();
        }
        else
        {
            GameObject nullWGO = new GameObject($"{weaponData.weaponName} (NULL)");
            clientWeapon = nullWGO.AddComponent<NullWeapon>();
            clientWeapon.Init(false, bulletData);
            Debug.LogError($"Client weapon prefab does not have a IWeapon type component.\nSwitching to default weapon");
        }
    }

    public void Fire()
    {
        if (NetworkTime.time < fireTime) return;

        Ray weaponRay = new Ray(firePivot.position, firePivot.forward);

        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance, weaponLayerMask))
        {
            //float height = MyTransform.position.y;
            //float angle = MyTransform.eulerAngles.x;
            //float speed = bulletData.initialSpeed;

            //float root = Mathf.Sqrt(Mathf.Pow(speed * Mathf.Sin(angle), 2) + 2 * Physics.gravity.y * height);
            //float range = speed * Mathf.Cos(angle) * (speed * Mathf.Sin(angle) + root) / Physics.gravity.y;
            //float range = Mathf.Pow(bulletData.initialSpeed, 2) * Mathf.Sin(2 * angle) / Physics.gravity.y;
            print($"Player Fire! Range[] - HitPoint: {rayHit.point}|{rayHit.collider.name}");
            Debug.DrawLine(weaponRay.origin, rayHit.point, Color.cyan, 2);
        }
        else
        {
            Debug.DrawRay(weaponRay.origin, weaponRay.direction, Color.red, 2);
        }

        fireTime = NetworkTime.time * weaponData.rateOfFire;
        netWeapon.CmdRequestFire();
        clientWeapon.Fire(rayHit.point);
    }

    public void AltFire()
    {
        clientWeapon.AltFire(Vector3.zero);
    }

    public void Scope()
    {
        clientWeapon.Scope();
    }

    public float GetRateOfFire() => weaponData.rateOfFire;
}
