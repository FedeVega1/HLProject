using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Scriptables;

namespace HLProject.Weapons
{
    public class Grenade : BaseClientWeapon
    {
        [SerializeField] Animator[] weaponAnimators;

        Animator weaponAnim;

        int delayTweenID = -1;
        bool lastWalkCheck, lastRunningCheck, isFiring;
        float movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;

        AsyncOperationHandle<IList<AudioClip>> virtualShootSoundsHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "pin_pull", "grenade_throw" }, null, Addressables.MergeMode.Union);
            virtualShootSoundsHandle.Completed += OnWeaponSoundsComplete;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Addressables.Release(virtualShootSoundsHandle);
        }

        public override void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
        {
            base.Init(isServer, wData, data, propPrefab);
            weaponAnim = weaponAnimators[isServer ? 0 : 1];
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

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn) return;

            if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

            weaponAnim.SetTrigger("Fire");

            virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[1]);

            weaponAnim.ResetTrigger("Walk");
            weaponAnim.ResetTrigger("Idle");
            weaponAnim.SetBool("IsWalking", false);
            weaponAnim.SetBool("IsFiring", true);
            weaponAnim.SetBool("IsEmpty", true);
            isFiring = true;

            if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
            delayTweenID = LeanTween.delayedCall(weaponData.weaponAnimsTiming.fireMaxDelay, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
        }

        public override void EmptyFire() { }
        public override void AltFire(Vector3 destination, bool didHit, int ammo) { }
        public override void ScopeIn() { }
        public override void ScopeOut() { }
        public override void OnAltMode(bool toggle, WeaponData newData) { }

        public override void Reload(int bulletsToReload) 
        {
            weaponAnim.SetTrigger("Reload");
            weaponAnim.SetBool("IsReloading", true);
            weaponAnim.SetBool("IsEmpty", false);
            virtualAudioSource.PlayOneShot(deploySound);

            LeanTween.cancel(gameObject);
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () =>
            {
                weaponAnim.SetBool("IsReloading", false);
            });
        }

        public override void DrawWeapon()
        {
            base.DrawWeapon();
            weaponAnim.SetTrigger("Draw");
            virtualAudioSource.PlayOneShot(deploySound);
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

            if ((lastWalkCheck || lastRunningCheck) && handleInspectionSoundsRoutine != null)
                StopCoroutine(handleInspectionSoundsRoutine);
        }
    }
}
