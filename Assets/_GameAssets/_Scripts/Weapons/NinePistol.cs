using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NinePistol : CachedTransform, IWeapon
{
    [SerializeField] GameObject[] viewModels;
    [SerializeField] Transform virtualBulletPivot, worldBulletPivot, magazinePivot, bulletEffectPivot;
    [SerializeField] Animator[] weaponAnimators;
    [SerializeField] GameObject pistolMagazine;

    bool isServer, isDrawn;
    BulletData bulletData;
    WeaponData weaponData;
    GameObject weaponPropPrefab;
    Animator weaponAnim;

    float randomInspectTime;
    bool lastWalkCheck, lastRunningCheck, lastBullet;

    public void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
    {
        bulletData = data;
        weaponData = wData;

        weaponPropPrefab = propPrefab;
        ToggleAllViewModels(false);

        viewModels[isServer ? 0 : 1].SetActive(true);
        this.isServer = isServer;
        gameObject.SetActive(false);

        weaponAnim = weaponAnimators[isServer ? 0 : 1];
    }

    void OnEnable()
    {
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public void ToggleAllViewModels(bool toggle)
    {
        int size = viewModels.Length;
        for (byte i = 0; i < size; i++) viewModels[i].SetActive(toggle);
    }

    void Update()
    {
        if (weaponAnim == null || Time.time < randomInspectTime) return;
        weaponAnim.SetInteger("RandomIdle", Random.Range(0, 2));
        weaponAnim.SetTrigger("RandomInspect");
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public void Fire(Vector3 destination, bool didHit, bool lastBullet = false)
    {
        if (!isDrawn) return;
        GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
        Bullet bullet = bulletObject.GetComponent<Bullet>();

        bullet.Init(bulletData.initialSpeed, didHit);
        bullet.TravelTo(destination);
        bullet.MyTransform.parent = null;

        if (this.lastBullet != lastBullet) weaponAnim.SetBool("EmptyGun", lastBullet);
        this.lastBullet = lastBullet;

        weaponAnim.SetTrigger("Fire");
        weaponAnim.SetInteger("RandomFire", lastBullet ? 4 : Random.Range(0, 4));

        weaponAnim.ResetTrigger("Walk");
        weaponAnim.SetBool("IsFiring", true);

        LeanTween.cancel(gameObject);
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.fire, () => weaponAnim.SetBool("IsFiring", false));
    }

    public void AltFire(Vector3 destination, bool didHit)
    {
        if (!isDrawn) return;
    }

    public void Scope()
    {
        if (!isDrawn) return;
    }

    public void Reload() 
    {
        lastBullet = false;
        weaponAnim.SetBool("EmptyGun", false);

        weaponAnim.SetTrigger("Reload");
        weaponAnim.SetBool("IsReloading", true);

        LeanTween.cancel(gameObject);
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () => weaponAnim.SetBool("IsReloading", false));
        LeanTween.delayedCall(.67f, SpawnMagazine);
    }

    void SpawnMagazine()
    {
        Rigidbody rb = Instantiate(pistolMagazine, magazinePivot.position, magazinePivot.rotation).GetComponent<Rigidbody>();
        rb.AddForce(Vector3.down * 10, ForceMode.Impulse);
    }

    public void DrawWeapon()
    {
        gameObject.SetActive(true);
        weaponAnim.SetTrigger("Draw");
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.draw, () => isDrawn = true);
    }

    public void HolsterWeapon()
    {
        weaponAnim.SetTrigger("Holster");
        isDrawn = false;
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.holster, () => gameObject.SetActive(false));
    }
    public void DropProp()
    {
        Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
        Destroy(gameObject);
    }

    public Transform GetVirtualPivot() => virtualBulletPivot;
    public Transform GetWorldPivot() => worldBulletPivot;

    public void CheckPlayerMovement(bool isMoving, bool isRunning)
    {
        if (isMoving != lastWalkCheck)
        {
            if (isMoving) weaponAnim.SetTrigger("Walk");
            else weaponAnim.SetTrigger("Idle");
        }

        if (isRunning != lastRunningCheck) weaponAnim.SetBool("Sprint", isRunning);

        lastWalkCheck = isMoving;
        lastRunningCheck = isRunning;
    }

    public void OnCameraMovement(Vector2 axis)
    {

    }
}
