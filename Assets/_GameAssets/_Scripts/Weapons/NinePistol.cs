using System.Collections;
using System.Collections.Generic;
using kcp2k;
using Mirror;
using UnityEngine;

public class NinePistol : BaseClientWeapon
{
    [SerializeField] Transform magazinePivot, bulletEffectPivot;
    [SerializeField] Animator[] weaponAnimators;
    [SerializeField] Rigidbody pistolMagazinePrefab, pistolBulletCasingPrefab;
    [SerializeField] AudioClip[] virtualShootSounds, worldShootSounds, reloadSounds;
    [SerializeField] AudioClip emptySound, deploySound, lowAmmoSound;

    Animator weaponAnim;

    float randomInspectTime, movementSoundTime;
    bool lastWalkCheck, lastRunningCheck, lastBullet;

    public override void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
    {
        base.Init(isServer, wData, data, propPrefab);
        weaponAnim = weaponAnimators[isServer ? 0 : 1];
    }

    void OnEnable()
    {
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    protected override void Update()
    {
        if (!isDrawn) return;
        base.Update();

        if (lastWalkCheck && Time.time >= movementSoundTime)
        {
            if (lastRunningCheck)
            {
                virtualMovementSource.PlayOneShot(weaponSprintSounds[Random.Range(0, weaponSprintSounds.Length)]);
                movementSoundTime = Time.time + .38f;

                return;
            }

            virtualMovementSource.PlayOneShot(weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)]);
            movementSoundTime = Time.time + .48f;
        }

        if (weaponAnim == null || Time.time < randomInspectTime) return;
        RandomIdleAnim();
    }

    public override void Fire(Vector3 destination, bool didHit, int ammo)
    {
        if (!isDrawn) return;
        GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
        Bullet bullet = bulletObject.GetComponent<Bullet>();

        bullet.Init(bulletData.initialSpeed, didHit);
        bullet.TravelTo(destination);
        bullet.MyTransform.parent = null;

        if (currentActiveViewModel == ActiveViewModel.World)
        {
            worldAudioSource.PlayOneShot(worldShootSounds[Random.Range(0, worldShootSounds.Length)]);
            return;
        }

        bool onLastBullet = ammo == 1;
        if (lastBullet != onLastBullet) weaponAnim.SetBool("EmptyGun", onLastBullet);
        lastBullet = onLastBullet;

        weaponAnim.SetTrigger("Fire");
        weaponAnim.SetInteger("RandomFire", lastBullet ? 4 : Random.Range(0, 4));

        virtualAudioSource.PlayOneShot(virtualShootSounds[Random.Range(0, virtualShootSounds.Length)]);

        if (ammo < 5)
            virtualAudioSource.PlayOneShot(lowAmmoSound);

        Invoke(nameof(SpawnCasing), .04f);

        weaponAnim.ResetTrigger("Walk");
        weaponAnim.SetBool("IsFiring", true);

        LeanTween.cancel(gameObject);
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.fire, () => weaponAnim.SetBool("IsFiring", false));
    }

    public override void EmptyFire()
    {
        if (!isDrawn) return;
        virtualAudioSource.PlayOneShot(emptySound);

        weaponAnim.SetTrigger("Fire");
        weaponAnim.SetInteger("RandomFire", 4);
    }

    public override void AltFire(Vector3 destination, bool didHit)
    {
        if (!isDrawn) return;
    }

    public override void Scope()
    {
        if (!isDrawn) return;
    }

    public override void Reload() 
    {
        LeanTween.delayedCall(.67f, SpawnMagazine);
        if (currentActiveViewModel == ActiveViewModel.World)
        {
            worldAudioSource.PlayOneShot(reloadSounds[4]);
            return;
        }

        virtualAudioSource.PlayOneShot(reloadSounds[0]);

        LeanTween.delayedCall(.375f, () =>
        {
            virtualAudioSource.PlayOneShot(reloadSounds[1]);
        });

        LeanTween.delayedCall(1, () =>
        {
            virtualAudioSource.PlayOneShot(reloadSounds[2]);
        });

        LeanTween.delayedCall(1.54f, () =>
        {
            virtualAudioSource.PlayOneShot(reloadSounds[3]);
        });

        lastBullet = false;
        weaponAnim.SetBool("EmptyGun", false);

        weaponAnim.SetTrigger("Reload");
        weaponAnim.SetBool("IsReloading", true);

        LeanTween.cancel(gameObject);
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () => weaponAnim.SetBool("IsReloading", false));
    }

    void SpawnMagazine()
    {
        Rigidbody rb = Instantiate(pistolMagazinePrefab, magazinePivot.position, magazinePivot.rotation);
        rb.AddForce(-MyTransform.up * 2, ForceMode.Impulse);
        rb.AddTorque(Utilities.RandomVector3(Vector3.one, -1, 1) * 2, ForceMode.Impulse);
    }

    void SpawnCasing()
    {
        Rigidbody rb = Instantiate(pistolBulletCasingPrefab, bulletEffectPivot.position, bulletEffectPivot.rotation);
        rb.AddForce(Vector3.Cross(-MyTransform.forward, MyTransform.up * Random.Range(.1f, .5f)) * 18, ForceMode.Impulse);
        rb.AddTorque(Utilities.RandomVector3(Vector3.one, -1, 1) * 25);
    }

    public override void DrawWeapon()
    {
        gameObject.SetActive(true);
        weaponAnim.SetTrigger("Draw");
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.draw, () => isDrawn = true);

        virtualAudioSource.PlayOneShot(deploySound);

        if (lastBullet) return;
        LeanTween.delayedCall(.58f, () =>
        {
            virtualAudioSource.PlayOneShot(reloadSounds[3]);
        });
    }

    public override void HolsterWeapon()
    {
        weaponAnim.SetTrigger("Holster");
        isDrawn = false;
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.holster, () => gameObject.SetActive(false));
    }
    public override void DropProp()
    {
        Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
        Destroy(gameObject);
    }

    void RandomIdleAnim()
    {
        int randomIdle = Random.Range(0, 2);
        weaponAnim.SetInteger("RandomIdle", randomIdle);
        weaponAnim.SetTrigger("InspectIdle");

        if (randomIdle == 1)
        {
            LeanTween.delayedCall(.875f, () =>
            {
                virtualAudioSource.PlayOneShot(reloadSounds[0]);
            });

            LeanTween.delayedCall(2.16f, () =>
            {
                virtualAudioSource.PlayOneShot(reloadSounds[3]);
            });
        }

        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override Transform GetVirtualPivot() => virtualBulletPivot;
    public override Transform GetWorldPivot() => worldBulletPivot;

    public override void CheckPlayerMovement(bool isMoving, bool isRunning)
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
}
