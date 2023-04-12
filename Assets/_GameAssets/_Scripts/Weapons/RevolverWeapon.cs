using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevolverWeapon : BaseClientWeapon
{
    [SerializeField] Animator weaponAnim;

    bool lastWalkCheck, lastRunningCheck, isFiring;
    int delayTweenID = -1;
    float randomInspectTime, movementSoundTime;
    Coroutine handleInspectionSoundsRoutine;

    protected override void Update()
    {
        if (!isDrawn) return;
        base.Update();

        //if (lastWalkCheck && Time.time >= movementSoundTime)
        //{
        //    //int random = Random.Range(0, 101);
        //    //AudioClip clipToPlay;

        //    if (lastRunningCheck)
        //    {
        //        //clipToPlay = random <= 50 ? weaponMovSounds[Random.Range(0, weaponMovSounds.Length)] : weaponSprintSounds[Random.Range(0, weaponSprintSounds.Length)];
        //        virtualMovementSource.PlayOneShot(weaponSprintSounds[Random.Range(0, weaponSprintSounds.Length)]);
        //        movementSoundTime = Time.time + .4f;
        //        return;
        //    }

        //    //clipToPlay = random <= 50 ? weaponMovSounds[Random.Range(0, weaponMovSounds.Length)] : weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)];
        //    virtualMovementSource.PlayOneShot(weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)]);
        //    movementSoundTime = Time.time + .5f;
        //}

        if (weaponAnim == null || Time.time < randomInspectTime) return;
        RandomIdleAnim();
    }

    void OnEnable()
    {
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    void RandomIdleAnim()
    {
        if (onScopeAim) return;
        int randomIdle = Random.Range(0, 2);
        weaponAnim.SetInteger("RandomIdle", randomIdle);
        weaponAnim.SetTrigger("InspectIdle");

        if (handleInspectionSoundsRoutine != null)
            StopCoroutine(handleInspectionSoundsRoutine);

        //handleInspectionSoundsRoutine = StartCoroutine(HanldeInspectionSound(randomIdle));
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override void Fire(Vector3 destination, bool didHit, int ammo) 
    {
        if (!isDrawn || doingScopeAnim) return;
        base.Fire(destination, didHit, ammo);

        weaponAnim.SetTrigger("Fire");

        weaponAnim.ResetTrigger("Walk");
        weaponAnim.ResetTrigger("Idle");
        weaponAnim.SetBool("IsWalking", false);
        weaponAnim.SetBool("IsFiring", true);
        isFiring = true;

        if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
        delayTweenID = LeanTween.delayedCall(weaponData.weaponAnimsTiming.fireMaxDelay + .2f, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
        randomInspectTime = Time.time + Random.Range(20f, 40f);
    }

    public override void EmptyFire() 
    {
        if (!isDrawn) return;
        //virtualAudioSource.PlayOneShot(emptySound);
        //weaponAnim.SetTrigger("Fire");
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

    public override void AltFire(Vector3 destination, bool didHit) { }

    public override void Reload() 
    {
        weaponAnim.SetTrigger("Reload");
        weaponAnim.SetBool("IsReloading", true);

        LeanTween.cancel(gameObject);
        LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () => weaponAnim.SetBool("IsReloading", false));
    }

    public override void DrawWeapon()
    {
        base.DrawWeapon();
        weaponAnim.SetTrigger("Draw");
    }

    public override void HolsterWeapon()
    {
        base.HolsterWeapon();
        weaponAnim.SetTrigger("Holster");
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

        //if ((lastWalkCheck || lastRunningCheck) && handleInspectionSoundsRoutine != null)
        //    StopCoroutine(handleInspectionSoundsRoutine);
    }

    //IEnumerator HanldeInspectionSound(int randomIdle)
    //{
    //    if (randomIdle != 1) yield break;
    //    yield return new WaitForSeconds(.875f);
    //    virtualAudioSource.PlayOneShot(reloadSounds[0]);

    //    yield return new WaitForSeconds(1.285f); // 2.16f
    //    virtualAudioSource.PlayOneShot(reloadSounds[3]);
    //}
}
