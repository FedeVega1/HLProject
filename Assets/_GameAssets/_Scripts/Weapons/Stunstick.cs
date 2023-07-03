using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class Stunstick : BaseClientWeapon
    {
        [SerializeField] Animator weaponAnim;
        [SerializeField] List<AssetReference> inspectionSounds;

        bool lastWalkCheck, lastRunningCheck, isFiring;
        int delayTweenID = -1;
        float movementSoundTime;

        AsyncOperationHandle<IList<AudioClip>> virtualSwingSoundsHandle, virtualHitSoundsHandle, virtualHitFleshHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualSwingSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Stunstick", "SwingSound" }, null, Addressables.MergeMode.Intersection);
            virtualSwingSoundsHandle.Completed += OnWeaponSoundsComplete;

            virtualHitSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Stunstick", "FireSound" }, null, Addressables.MergeMode.Intersection);
            virtualHitSoundsHandle.Completed += OnWeaponSoundsComplete;

            virtualHitFleshHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Stunstick", "FleshHitSound" }, null, Addressables.MergeMode.Intersection);
            virtualHitFleshHandle.Completed += OnWeaponSoundsComplete;
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
        }

        public override void MeleeSwing(bool didHit, bool playerHit, bool killHit)
        {
            if (!isDrawn) return;

            weaponAnim.SetTrigger("Fire");
            int randomFire = didHit ? Random.Range(0, 3) : Random.Range(3, 6);
            weaponAnim.SetInteger("RandomFire", randomFire);

            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () =>
            {
                virtualAudioSource.pitch = Random.Range(.85f, .95f);
                AudioClip soundToPlay;

                if (!didHit) soundToPlay = virtualSwingSoundsHandle.Result[Random.Range(0, virtualSwingSoundsHandle.Result.Count)];
                else if (playerHit) soundToPlay = virtualHitFleshHandle.Result[Random.Range(0, virtualHitFleshHandle.Result.Count)];
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
        }

        public override void EmptyFire() { }
        public override void ScopeIn() { }
        public override void ScopeOut() { }
        public override void AltFire(Vector3 destination, bool didHit) { }
        public override void Reload() { }

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
                }
            }

            if (isRunning != lastRunningCheck) weaponAnim.SetBool("Sprint", isRunning);

            lastWalkCheck = isMoving;
            lastRunningCheck = isRunning;

            if (lastRunningCheck && onScopeAim) ScopeOut();
        }
    }
}
