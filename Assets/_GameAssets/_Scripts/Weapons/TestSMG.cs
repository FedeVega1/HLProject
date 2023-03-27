using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSMG : BaseClientWeapon
{
    [SerializeField] Animator weaponAnim;

    public override void Fire(Vector3 destination, bool didHit, int ammo)
    {
        if (!isDrawn) return;
        GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
        Bullet bullet = bulletObject.GetComponent<Bullet>();

        bullet.Init(bulletData.initialSpeed, didHit);
        bullet.TravelTo(destination);
        bullet.MyTransform.parent = null;
    }

    public override void EmptyFire() { }


    public override void AltFire(Vector3 destination, bool didHit)
    {
        if (!isDrawn) return;

    }

    public override void Scope()
    {
        if (!isDrawn) return;

    }

    public override void Reload() { }

    public override void DrawWeapon()
    {
        gameObject.SetActive(true);
        isDrawn = true;
    }

    public override void HolsterWeapon()
    {
        gameObject.SetActive(false);
        isDrawn = false;
    }

    public override void DropProp()
    {
        Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
        Destroy(gameObject);
    }

    public override Transform GetVirtualPivot() => virtualBulletPivot;
    public override Transform GetWorldPivot() => worldBulletPivot;

    public override void CheckPlayerMovement(bool isMoving, bool isRunning) { }
}
