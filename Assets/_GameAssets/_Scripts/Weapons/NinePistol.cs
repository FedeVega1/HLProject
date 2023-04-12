using System.Collections;
using System.Collections.Generic;
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

    int delayTweenID = -1;
    bool lastWalkCheck, lastRunningCheck, lastBullet, emptyGun, isFiring;
    float randomInspectTime, movementSoundTime;
    Coroutine handleInspectionSoundsRoutine;

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
            //int random = Random.Range(0, 101);
            //AudioClip clipToPlay;

            if (lastRunningCheck)
            {
                //clipToPlay = random <= 50 ? weaponMovSounds[Random.Range(0, weaponMovSounds.Length)] : weaponSprintSounds[Random.Range(0, weaponSprintSounds.Length)];
                virtualMovementSource.PlayOneShot(weaponSprintSounds[Random.Range(0, weaponSprintSounds.Length)]);
                movementSoundTime = Time.time + .4f;
                return;
            }

            //clipToPlay = random <= 50 ? weaponMovSounds[Random.Range(0, weaponMovSounds.Length)] : weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)];
            virtualMovementSource.PlayOneShot(weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)]);
            movementSoundTime = Time.time + .5f;
        }

        if (weaponAnim == null || Time.time < randomInspectTime) return;
        RandomIdleAnim();
    }

    public override void Fire(Vector3 destination, bool didHit, int ammo)
    {
        if (!isDrawn || doingScopeAnim) return;
        base.Fire(destination, didHit, ammo);

        if (currentActiveViewModel == ActiveViewModel.World)
        {
            worldAudioSource.PlayOneShot(worldShootSounds[Random.Range(0, worldShootSounds.Length)]);
            return;
        }

        bool onLastBullet = ammo == 1;
        emptyGun = ammo <= 0;
        if (lastBullet != onLastBullet) weaponAnim.SetBool("EmptyGun", onLastBullet);
        lastBullet = onLastBullet;

        if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

        weaponAnim.SetTrigger("Fire");
        weaponAnim.SetInteger("RandomFire", lastBullet ? 4 : Random.Range(0, 4));

        virtualAudioSource.PlayOneShot(virtualShootSounds[Random.Range(0, virtualShootSounds.Length)]);

        if (ammo < 5)
            virtualAudioSource.PlayOneShot(lowAmmoSound);

        Invoke(nameof(SpawnCasing), .04f);

        weaponAnim.ResetTrigger("Walk");
        weaponAnim.ResetTrigger("Idle");
        weaponAnim.SetBool("IsWalking", false);
        weaponAnim.SetBool("IsFiring", true);
        isFiring = true;

        if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
        delayTweenID = LeanTween.delayedCall(weaponData.weaponAnimsTiming.fireMaxDelay + .2f, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override void ScopeIn()
    {
        base.ScopeIn();
        weaponAnim.SetBool("OnScope", true);
    }

    public override void ScopeOut()
    {
        base.ScopeOut();
        weaponAnim.SetBool("OnScope", false);
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override void EmptyFire()
    {
        if (!isDrawn) return;
        virtualAudioSource.PlayOneShot(emptySound);

        weaponAnim.SetTrigger("Fire");
        weaponAnim.SetInteger("RandomFire", 4);
    }

    public override void AltFire(Vector3 destination, bool didHit) { }

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

        lastBullet = emptyGun = false;
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
        base.DrawWeapon();
        weaponAnim.SetTrigger("Draw");

        virtualAudioSource.PlayOneShot(deploySound);

        if (lastBullet) return;
        LeanTween.delayedCall(.58f, () =>
        {
            virtualAudioSource.PlayOneShot(reloadSounds[3]);
        });
    }

    public override void HolsterWeapon()
    {
        base.HolsterWeapon();
        weaponAnim.SetTrigger("Holster");
    }

    void RandomIdleAnim()
    {
        if (emptyGun || onScopeAim) return;
        int randomIdle = Random.Range(0, 2);
        weaponAnim.SetInteger("RandomIdle", randomIdle);
        weaponAnim.SetTrigger("InspectIdle");

        if (handleInspectionSoundsRoutine != null)
            StopCoroutine(handleInspectionSoundsRoutine);

        handleInspectionSoundsRoutine = StartCoroutine(HanldeInspectionSound(randomIdle));

        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override void CheckPlayerMovement(bool isMoving, bool isRunning)
    {
        if (isFiring) return;
        if (isMoving != lastWalkCheck)
        {
            if (isMoving)
            {
                weaponAnim.SetTrigger("Walk");
                weaponAnim.SetBool("IsWalking", true);
            }
            else
            {
                weaponAnim.SetTrigger("Idle");
                weaponAnim.SetBool("IsWalking", false);
                randomInspectTime = Time.time + Random.Range(20f, 40f);
            }
        }

        if (isRunning != lastRunningCheck) weaponAnim.SetBool("Sprint", isRunning);

        lastWalkCheck = isMoving;
        lastRunningCheck = isRunning;

        if (lastRunningCheck && onScopeAim) ScopeOut();

        if ((lastWalkCheck || lastRunningCheck) && handleInspectionSoundsRoutine != null) 
            StopCoroutine(handleInspectionSoundsRoutine);
    }

    IEnumerator HanldeInspectionSound(int randomIdle)
    {
        if (randomIdle != 1) yield break;
        yield return new WaitForSeconds(.875f);
        virtualAudioSource.PlayOneShot(reloadSounds[0]);

        yield return new WaitForSeconds(1.285f); // 2.16f
        virtualAudioSource.PlayOneShot(reloadSounds[3]);
    }
}
