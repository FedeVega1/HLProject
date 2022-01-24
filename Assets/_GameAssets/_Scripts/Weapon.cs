using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public enum BulletType { RayCast, Physics }

public class Weapon : CachedNetTransform
{
    [SerializeField] Transform firePivot;
    [SerializeField] LayerMask weaponLayerMask;

    double fireTime;
    Ray weaponRay;
    RaycastHit rayHit;

    Player playerScript;
    WeaponData weaponData;
    BulletData bulletData;

    public void Init(WeaponData data)
    {
        weaponData = data;
        bulletData = data.bulletData;

        weaponRay = new Ray(firePivot.position, firePivot.forward);
    }

    [Server]
    public void Fire()
    {
        if (isClient)
        {
            Instantiate(bulletData.bulletPrefab, firePivot.position, firePivot.rotation);
        }

        if (NetworkTime.time < fireTime) return;

        weaponRay.direction = firePivot.forward; 

        if (Physics.Raycast(weaponRay, out rayHit, bulletData.maxTravelDistance))
        {
            float height = MyTransform.position.y;
            float angle = MyTransform.eulerAngles.x;
            float speed = bulletData.initialSpeed;

            float root = Mathf.Sqrt(Mathf.Pow(speed * Mathf.Sin(angle), 2) + 2 * Physics.gravity.y * height);
            float range = speed * Mathf.Cos(angle) * (speed * Mathf.Sin(angle) + root) / Physics.gravity.y;
            //float range = Mathf.Pow(bulletData.initialSpeed, 2) * Mathf.Sin(2 * angle) / Physics.gravity.y;
            print(range);
        }

        fireTime = NetworkTime.time * weaponData.rateOfFire;
    }

    public float GetRateOfFire() => weaponData.rateOfFire;
}
