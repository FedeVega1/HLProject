using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class Crowbar : BaseClientWeapon
    {
        [SerializeField] Animator weaponAnim;
        [SerializeField] List<AssetReference> inspectionSounds;

        bool lastWalkCheck, lastRunningCheck, isFiring;
        int delayTweenID = -1;
        float randomInspectTime, movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;

        AsyncOperationHandle<IList<AudioClip>> virtualSwingSoundsHandle, inspectionSoundsHandle, virtualHitSoundsHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualSwingSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Crowbar", "SwingSound" }, null, Addressables.MergeMode.Intersection);
            virtualSwingSoundsHandle.Completed += OnWeaponSoundsComplete;

            virtualHitSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Crowbar", "FireSound" }, null, Addressables.MergeMode.Intersection);
            virtualSwingSoundsHandle.Completed += OnWeaponSoundsComplete;

            inspectionSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(inspectionSounds, null, Addressables.MergeMode.Union);
            inspectionSoundsHandle.Completed += OnWeaponSoundsComplete;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Addressables.Release(inspectionSoundsHandle);
        }

        protected override void Update()
        {
            if (!isDrawn) return;
            base.Update();

            if (lastWalkCheck && Time.time >= movementSoundTime)
            {
                if (lastRunningCheck)
                {
                    virtualMovementSource.PlayOneShot(weaponSprintSoundsHandle.Result[Random.Range(0, weaponSprintSoundsHandle.Result.Count)]);
                    movementSoundTime = Time.time + .4f;
                    return;
                }

                virtualMovementSource.PlayOneShot(weaponWalkSoundsHandle.Result[Random.Range(0, weaponWalkSoundsHandle.Result.Count)]);
                movementSoundTime = Time.time + .5f;
            }

            if (weaponAnim == null || Time.time < randomInspectTime) return;
            RandomIdleAnim();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        void RandomIdleAnim()
        {
            if (onScopeAim) return;
            weaponAnim.SetTrigger("InspectIdle");

            if (handleInspectionSoundsRoutine != null)
                StopCoroutine(handleInspectionSoundsRoutine);

            handleInspectionSoundsRoutine = StartCoroutine(HanldeInspectionSound());
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void MeleeSwing(bool didHit, bool playerHit, bool killHit)
        {
            if (!isDrawn) return;

            weaponAnim.SetTrigger("Fire");
            int randomFire = didHit ? (killHit ? 3 : Random.Range(0, 3)) : Random.Range(4, 6);
            weaponAnim.SetInteger("RandomFire", randomFire);

            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () =>
            {
                virtualAudioSource.pitch = Random.Range(.85f, .95f);
                AudioClip soundToPlay;

                if (!didHit) soundToPlay = virtualSwingSoundsHandle.Result[Random.Range(0, virtualSwingSoundsHandle.Result.Count)];
                else soundToPlay = virtualHitSoundsHandle.Result[Random.Range(0, virtualHitSoundsHandle.Result.Count)];
                virtualAudioSource.PlayOneShot(soundToPlay);
            });

            weaponAnim.ResetTrigger("Walk");
            weaponAnim.ResetTrigger("Idle");
            weaponAnim.SetBool("IsWalking", false);
            weaponAnim.SetBool("IsFiring", true);
            isFiring = true;

            if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
            delayTweenID = LeanTween.delayedCall(weaponData.weaponAnimsTiming.fireMaxDelay + .2f, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void EmptyFire() { }
        public override void ScopeIn() { }
        public override void ScopeOut() { }
        public override void AltFire(Vector3 destination, bool didHit) { }
        public override void Reload(int bulletsToReload) { }

        public override void DrawWeapon()
        {
            base.DrawWeapon();
            weaponAnim.SetTrigger("Draw");
            virtualAudioSource.pitch = 1;
            LeanTween.delayedCall(0, () => virtualAudioSource.PlayOneShot(deploySound));
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

            if ((lastWalkCheck || lastRunningCheck) && handleInspectionSoundsRoutine != null)
                StopCoroutine(handleInspectionSoundsRoutine);
        }

        IEnumerator HanldeInspectionSound()
        {
            virtualAudioSource.pitch = 1;
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[0]);

            yield return new WaitForSeconds(1.75f);
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[1]);

            yield return new WaitForSeconds(2.15f);
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[2]);
        }
    }
}
