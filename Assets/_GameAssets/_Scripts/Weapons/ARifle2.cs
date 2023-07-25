using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class ARifle2 : BaseClientWeapon
    {
        [SerializeField] Transform magazinePivot, bulletEffectPivot;
        [SerializeField] Animator[] weaponAnimators;
        [SerializeField] Rigidbody magazinePrefab, bulletCasingPrefab;

        Animator weaponAnim;

        int delayTweenID = -1;
        bool lastWalkCheck, lastRunningCheck, isFiring, onAltMode, lastBullet, midEmpty;
        float randomInspectTime, movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;

        AsyncOperationHandle<IList<AudioClip>> virtualShootSoundsHandle, reloadSoundsHandle;
        AsyncOperationHandle<AudioClip> lowAmmoSoundHandle, zoomInSoundHandle, zoomOutSoundHandle, emptyFireHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "ar2_fire1", "ar2_fire2", "ar2_fire3", "ar2_fire4", "ar2_charge", "ar2_secondary_fire" }, null, Addressables.MergeMode.Union);
            virtualShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            //worldShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/PistolWorld", "FireSound" }, null, Addressables.MergeMode.Intersection);
            //worldShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            reloadSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "ar2_reload", "ar2_magout", "ar2_magin" }, null, Addressables.MergeMode.Union);
            reloadSoundsHandle.Completed += OnWeaponSoundsComplete;

            zoomInSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_in");
            zoomInSoundHandle.Completed += OnWeaponSoundComplete;

            zoomOutSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_out");
            zoomOutSoundHandle.Completed += OnWeaponSoundComplete;

            lowAmmoSoundHandle = Addressables.LoadAssetAsync<AudioClip>("lowammo");
            lowAmmoSoundHandle.Completed += OnWeaponSoundComplete;

            emptyFireHandle = Addressables.LoadAssetAsync<AudioClip>("ar2_empty");
            emptyFireHandle.Completed += OnWeaponSoundComplete;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Addressables.Release(virtualShootSoundsHandle);
            //Addressables.Release(worldShootSoundsHandle);
            Addressables.Release(reloadSoundsHandle);
            Addressables.Release(lowAmmoSoundHandle);
            Addressables.Release(zoomInSoundHandle);
            Addressables.Release(zoomOutSoundHandle);
            Addressables.Release(emptyFireHandle);
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

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn || doingScopeAnim) return;
            base.Fire(destination, didHit, ammo);

            /*if (currentActiveViewModel == ActiveViewModel.World)
            {
                worldAudioSource.PlayOneShot(worldShootSoundsHandle.Result[Random.Range(0, worldShootSoundsHandle.Result.Count)]);
                return;
            }*/

            bool onLastBullet = ammo == 1, _midEmpty = ammo == 2;
            if (lastBullet != onLastBullet || midEmpty != _midEmpty) weaponAnim.SetBool("EmptyGun", onLastBullet || _midEmpty);
            lastBullet = onLastBullet;
            midEmpty = _midEmpty;

            if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

            weaponAnim.SetTrigger("Fire");

            if (midEmpty) weaponAnim.SetInteger("RandomFire", 6);
            else if (lastBullet) weaponAnim.SetInteger("RandomFire", 4);
            else weaponAnim.SetInteger("RandomFire", Random.Range(0, 4));

            virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[Random.Range(0, 3)]);

            if (ammo < 5)
                virtualAudioSource.PlayOneShot(lowAmmoSoundHandle.Result);

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

            virtualAudioSource.PlayOneShot(emptyFireHandle.Result);

            if (onAltMode) return;
            weaponAnim.SetTrigger("Fire");
            weaponAnim.SetInteger("RandomFire", 4);
        }

        public override void AltFire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn || doingScopeAnim) return;

            if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

            weaponAnim.SetTrigger("Fire");
            virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[4]);
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () => virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[5]));
            isFiring = true;

            if (delayTweenID != -1) LeanTween.cancel(delayTweenID);
            delayTweenID = LeanTween.delayedCall(weaponData.weaponAnimsTiming.fireMaxDelay + .2f, () => { weaponAnim.SetBool("IsFiring", false); isFiring = false; }).uniqueId;
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void OnAltMode(bool toggle, WeaponData newData)
        {
            onAltMode = toggle;
            weaponData = newData;
            virtualAudioSource.PlayOneShot(weaponFireModeSwitchSoundsHandle.Result[toggle ? 0 : 1]);
            weaponAnim.SetInteger("RandomFire", 5);
        }

        public override void Reload(int bulletsToReload)
        {
            if (onAltMode)
            {
                //virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[1]);
                //LeanTween.delayedCall(.17f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[2]));
            }
            else
            {
                LeanTween.delayedCall(1, SpawnMagazine);
                virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[0]);
                LeanTween.delayedCall(.5f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[1]));
                LeanTween.delayedCall(1.8f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[2]));
            }

            weaponAnim.SetBool("MidEmpty", midEmpty);
            lastBullet = midEmpty = false;

            weaponAnim.SetTrigger("Reload");
            weaponAnim.SetBool("IsReloading", true);

            LeanTween.cancel(gameObject);
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () => 
            {
                weaponAnim.SetBool("IsReloading", false);
                weaponAnim.SetBool("EmptyGun", false); 
                weaponAnim.SetBool("MidEmpty", false);
            });
        }

        void SpawnMagazine()
        {
            Rigidbody rb = Instantiate(magazinePrefab, magazinePivot.position, magazinePivot.rotation);
            rb.AddForce(-MyTransform.up * 2, ForceMode.Impulse);
            rb.AddTorque(Utilities.RandomVector3(Vector3.one, -1, 1) * 5, ForceMode.Impulse);
        }

        void SpawnCasing()
        {
            Rigidbody rb = Instantiate(bulletCasingPrefab, bulletEffectPivot.position, bulletEffectPivot.rotation);
            float y = Random.Range(.1f, .2f);
            Vector3 cross = new Vector3(1 - y, y);

            Debug.DrawRay(bulletEffectPivot.position, cross, Color.red, 2);
            rb.AddForce(MyTransform.TransformDirection(cross) * 6, ForceMode.Impulse);
            rb.AddTorque(Utilities.RandomVector3(Vector3.one, -1, 1) * 25);
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

        IEnumerator HanldeInspectionSound(int randomIdle)
        {
            if (randomIdle != 1) yield break;
            /*yield return new WaitForSeconds(.875f);
            virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[0]);

            yield return new WaitForSeconds(1.285f); // 2.16f
            virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[3]);*/
        }
    }
}
