using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPistol : CachedTransform, IWeapon
{
    [SerializeField] GameObject[] viewModels;
    [SerializeField] Transform virtualBulletPivot, worldBulletPivot;
    [SerializeField] Animator weaponAnim;

    bool isServer, isDrawn;
    BulletData bulletData;
    GameObject weaponPropPrefab;

    public void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
    {
        bulletData = data;

        weaponPropPrefab = propPrefab;
        ToggleAllViewModels(false);

        viewModels[isServer ? 0 : 1].SetActive(true);
        this.isServer = isServer;
        gameObject.SetActive(false);
    }

    public void ToggleAllViewModels(bool toggle)
    {
        int size = viewModels.Length;
        for (byte i = 0; i < size; i++) viewModels[i].SetActive(toggle);
    }

    public void Fire(Vector3 destination, bool didHit, bool lastBullet = false)
    {
        if (!isDrawn) return;
        GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
        Bullet bullet = bulletObject.GetComponent<Bullet>();

        bullet.Init(bulletData.initialSpeed, didHit);
        bullet.TravelTo(destination);
        bullet.MyTransform.parent = null;
    }

    public void AltFire(Vector3 destination, bool didHit)
    {
        if (!isDrawn) return;

    }

    public void Scope()
    {
        if (!isDrawn) return;

    }

    public void Reload() { }

    public void DrawWeapon()
    {
        gameObject.SetActive(true);
        isDrawn = true;
    }

    public void HolsterWeapon()
    {
        isDrawn = false;
        gameObject.SetActive(false);
    }

    public void DropProp()
    {
        Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
        Destroy(gameObject);
    }

    public Transform GetVirtualPivot() => virtualBulletPivot;
    public Transform GetWorldPivot() => worldBulletPivot;

    public void CheckPlayerMovement(bool isMoving, bool isRunning) { }
    public void OnCameraMovement(Vector2 axis) { }
}
