using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Scriptables;

namespace HLProject.Weapons
{
    public class Shotgun : BaseClientWeapon
    {
        [SerializeField] Transform bulletEffectPivot;
        [SerializeField] Animator[] weaponAnimators;
        [SerializeField] Rigidbody bulletCasingPrefab;

        Animator weaponAnim;

        int delayTweenID = -1;
        bool lastWalkCheck, lastRunningCheck, isFiring;
        float randomInspectTime, movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;

        AsyncOperationHandle<IList<AudioClip>> virtualShootSoundsHandle, reloadSoundsHandle, shotgunCockSoundHandle, inspectionSoundsHandle;
        AsyncOperationHandle<AudioClip> lowAmmoSoundHandle, zoomInSoundHandle, zoomOutSoundHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Shotgun", "FireSound" }, null, Addressables.MergeMode.Intersection);
            virtualShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            reloadSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/Shotgun", "ReloadSound" }, null, Addressables.MergeMode.Intersection);
            reloadSoundsHandle.Completed += OnWeaponSoundsComplete;

            shotgunCockSoundHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "shotgun_cock_back1", "shotgun_cock_back2", "shotgun_cock_back3", "shotgun_cock_forward1", "shotgun_cock_forward2", "shotgun_cock_forward3" }, 
                null, Addressables.MergeMode.Union);
            shotgunCockSoundHandle.Completed += OnWeaponSoundsComplete;

            inspectionSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "shotgun_bolt_back", "shotgun_bolt_forward" }, null, Addressables.MergeMode.Union);
            inspectionSoundsHandle.Completed += OnWeaponSoundsComplete;

            zoomInSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_in");
            zoomInSoundHandle.Completed += OnWeaponSoundComplete;

            zoomOutSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_out");
            zoomOutSoundHandle.Completed += OnWeaponSoundComplete;

            lowAmmoSoundHandle = Addressables.LoadAssetAsync<AudioClip>("lowammo");
            lowAmmoSoundHandle.Completed += OnWeaponSoundComplete;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Addressables.Release(virtualShootSoundsHandle);
            Addressables.Release(shotgunCockSoundHandle);
            Addressables.Release(reloadSoundsHandle);
            Addressables.Release(lowAmmoSoundHandle);
            Addressables.Release(zoomInSoundHandle);
            Addressables.Release(zoomOutSoundHandle);
            Addressables.Release(inspectionSoundsHandle);
        }

        void OnWeaponSoundComplete(AsyncOperationHandle<AudioClip> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Weapon Sound: {0}", operation.OperationException);
        }

        public override void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
        {
            base.Init(isServer, wData, data, propPrefab);
            weaponAnim = weaponAnimators[isServer ? 0 : 1];
        }

        protected override void OnEnable()
        {
            base.OnEnable();
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
                    virtualMovementSource.PlayOneShot(weaponSprintSoundsHandle.Result[Random.Range(0, weaponSprintSoundsHandle.Result.Count)]);
                    movementSoundTime = Time.time + .4f;
                    return;
                }

                //clipToPlay = random <= 50 ? weaponMovSounds[Random.Range(0, weaponMovSounds.Length)] : weaponWalkSounds[Random.Range(0, weaponWalkSounds.Length)];
                virtualMovementSource.PlayOneShot(weaponWalkSoundsHandle.Result[Random.Range(0, weaponWalkSoundsHandle.Result.Count)]);
                movementSoundTime = Time.time + .5f;
            }

            if (weaponAnim == null || Time.time < randomInspectTime) return;
            RandomIdleAnim();
        }

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn || doingScopeAnim) return;
            base.Fire(destination, didHit, ammo);

            if (isFiring) return;
            if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

            weaponAnim.SetTrigger("Fire");
            weaponAnim.SetInteger("RandomFire", 0);

            if (currentPlayerTeam > 1)
            {
                LeanTween.delayedCall(onScopeAim ? .68f : .75f, () => virtualAudioSource.PlayOneShot(shotgunCockSoundHandle.Result[Random.Range(0, 3)]));
                LeanTween.delayedCall(onScopeAim ? .83f : .9f, () => virtualAudioSource.PlayOneShot(shotgunCockSoundHandle.Result[Random.Range(3, 6)]));
            }

            virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[Random.Range(0, virtualShootSoundsHandle.Result.Count)]);

            if (ammo < 3)
                virtualAudioSource.PlayOneShot(lowAmmoSoundHandle.Result);

            Invoke(nameof(SpawnCasing), .04f);

            weaponAnim.ResetTrigger("Walk");
            weaponAnim.ResetTrigger("Idle");
            weaponAnim.SetBool("IsWalking", false);
            weaponAnim.SetBool("IsFiring", true);
            isFiring = true;

            if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
            float time = currentPlayerTeam > 1 ? weaponData.weaponAnimsTiming.shotgunPumpFireMaxDelay : weaponData.weaponAnimsTiming.fireMaxDelay;
            delayTweenID = LeanTween.delayedCall(time, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void ScopeIn()
        {
            base.ScopeIn();
            weaponAnim.SetBool("OnScope", true);
            virtualAudioSource.PlayOneShot(zoomInSoundHandle.Result);
        }

        public override void ScopeOut()
        {
            base.ScopeOut();
            weaponAnim.SetBool("OnScope", false);
            virtualAudioSource.PlayOneShot(zoomOutSoundHandle.Result);
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void EmptyFire()
        {
            if (!isDrawn) return;
        }

        public override void AltFire(Vector3 destination, bool didHit, int ammo) { }

        public override void Reload(int bulletsToReload)
        {
            if (bulletsToReload < weaponData.bulletsPerMag)
                weaponAnim.SetBool("EmptyGun", false);

                weaponAnim.SetTrigger("Reload");
            weaponAnim.SetBool("IsReloading", true);

            LeanTween.cancel(gameObject);
            StartCoroutine(HandleReloadSounds(bulletsToReload));
            float time = weaponData.weaponAnimsTiming.initReload + bulletsToReload;
            LeanTween.delayedCall(time, () => weaponAnim.SetBool("IsReloading", false));
            if (bulletsToReload >= weaponData.bulletsPerMag)
            {
                weaponAnim.SetBool("EmptyGun", true);
                LeanTween.delayedCall(time += 1.5f, () => weaponAnim.SetBool("EmptyGun", false));
                LeanTween.delayedCall(time, () => virtualAudioSource.PlayOneShot(shotgunCockSoundHandle.Result[Random.Range(0, 3)]));
                LeanTween.delayedCall(time + .2f, () => virtualAudioSource.PlayOneShot(shotgunCockSoundHandle.Result[Random.Range(3, 6)]));
            }
        }

        void SpawnCasing()
        {
            Rigidbody rb = Instantiate(bulletCasingPrefab, bulletEffectPivot.position, bulletEffectPivot.rotation);
            Debug.DrawRay(bulletEffectPivot.position, bulletEffectPivot.right, Color.red, 2);

            rb.AddForce(bulletEffectPivot.right * 4, ForceMode.Impulse);
            rb.AddTorque(Utilities.RandomVector3(Vector3.one, -1, 1) * 25);
        }

        public override void OnAltMode(bool toggle, WeaponData newData) { }
        public override void DrawWeapon()
        {
            base.DrawWeapon();
            weaponAnim.SetBool("CanPump", currentPlayerTeam > 1);
            weaponAnim.SetTrigger("Draw");

            virtualAudioSource.PlayOneShot(deploySound);
        }

        public override void HolsterWeapon()
        {
            base.HolsterWeapon();
            weaponAnim.SetTrigger("Holster");
        }

        void RandomIdleAnim()
        {
            if (onScopeAim) return;
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

        IEnumerator HandleReloadSounds(int bulletsToReload)
        {
            yield return new WaitForSeconds(weaponData.weaponAnimsTiming.initReload);

            WaitForSeconds initBulletWait = new WaitForSeconds(.21f);
            WaitForSeconds endBulletWait = new WaitForSeconds(.79f);
            for (int i = 0; i < bulletsToReload; i++)
            {
                yield return initBulletWait;
                virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[Random.Range(0, reloadSoundsHandle.Result.Count)]);
                yield return endBulletWait;
            }
        }

        IEnumerator HanldeInspectionSound(int randomIdle)
        {
            if (randomIdle != 1) yield break;
            yield return new WaitForSeconds(1.42f);
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[0]);

            yield return new WaitForSeconds(3f); // 4.42f
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[1]);
        }
    }
}
