using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class RevolverWeapon : BaseClientWeapon
    {
        [SerializeField] Animator weaponAnim;
        [SerializeField] List<AssetReference> reloadSounds, inspectionSounds;
        [SerializeField] Transform bulletCasingPivot;
        [SerializeField] Rigidbody bulletCasingPrefab;
        [SerializeField] Vector3 sprintOffset;

        bool lastWalkCheck, lastRunningCheck, isFiring;
        int delayTweenID = -1;
        float randomInspectTime, movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;
        Vector3 lastGunPos;

        AsyncOperationHandle<IList<AudioClip>> virtualShootSoundsHandle, reloadSoundsHandle, inspectionSoundsHandle;
        AsyncOperationHandle<AudioClip> lowAmmoSoundHandle, zoomInSoundHandle, zoomOutSoundHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/357", "FireSound" }, null, Addressables.MergeMode.Intersection);
            virtualShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            reloadSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(reloadSounds, null, Addressables.MergeMode.Union);
            reloadSoundsHandle.Completed += OnWeaponSoundsComplete;

            lowAmmoSoundHandle = Addressables.LoadAssetAsync<AudioClip>("lowammo");
            lowAmmoSoundHandle.Completed += OnWeaponSoundComplete;

            zoomInSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_in");
            zoomInSoundHandle.Completed += OnWeaponSoundComplete;

            zoomOutSoundHandle = Addressables.LoadAssetAsync<AudioClip>("ironsights_out");
            zoomOutSoundHandle.Completed += OnWeaponSoundComplete;

            inspectionSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(inspectionSounds, null, Addressables.MergeMode.Union);
            inspectionSoundsHandle.Completed += OnWeaponSoundsComplete;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            Addressables.Release(virtualShootSoundsHandle);
            Addressables.Release(reloadSoundsHandle);
            Addressables.Release(lowAmmoSoundHandle);
            Addressables.Release(inspectionSoundsHandle);
        }

        void OnWeaponSoundComplete(AsyncOperationHandle<AudioClip> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Weapon Sound: {0}", operation.OperationException);
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

            handleInspectionSoundsRoutine = StartCoroutine(HanldeInspectionSound(randomIdle));
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void Fire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn || doingScopeAnim) return;
            base.Fire(destination, didHit, ammo);

            weaponAnim.SetTrigger("Fire");

            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () =>
            {
                virtualAudioSource.pitch = Random.Range(.85f, .95f);
                virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[Random.Range(0, virtualShootSoundsHandle.Result.Count)]);
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
            virtualAudioSource.pitch = 1;
            virtualAudioSource.PlayOneShot(zoomInSoundHandle.Result);
        }

        public override void ScopeOut()
        {
            base.ScopeOut();
            weaponAnim.SetBool("OnScope", false);
            virtualAudioSource.pitch = 1;
            virtualAudioSource.PlayOneShot(zoomOutSoundHandle.Result);
            randomInspectTime = Time.time + Random.Range(20f, 40f);
        }

        public override void AltFire(Vector3 destination, bool didHit) { }

        public override void Reload()
        {
            weaponAnim.SetTrigger("Reload");
            weaponAnim.SetBool("IsReloading", true);

            LeanTween.delayedCall(.54f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[0]));
            LeanTween.delayedCall(1.67f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[1]));

            LeanTween.delayedCall(1.75f, () =>
            {
                virtualAudioSource.pitch = 1;
                virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[2]);
                SpawnBullets();
            });

            LeanTween.delayedCall(3.58f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[3]));
            LeanTween.delayedCall(4.41f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[4]));

            LeanTween.cancel(gameObject);
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.reload, () => weaponAnim.SetBool("IsReloading", false));
        }

        public override void DrawWeapon()
        {
            base.DrawWeapon();
            weaponAnim.SetTrigger("Draw");
            virtualAudioSource.pitch = 1;
            LeanTween.delayedCall(.5f, () => virtualAudioSource.PlayOneShot(deploySound));
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

            if (isRunning != lastRunningCheck)
            {
                if (isRunning)
                {
                    lastGunPos = viewModels[(int) currentActiveViewModel].transform.localPosition;
                    LeanTween.moveLocal(viewModels[(int) currentActiveViewModel], sprintOffset, .2f);
                }
                else
                {
                    //viewModels[(int) currentActiveViewModel].transform.localPosition = lastGunPos;
                    LeanTween.moveLocal(viewModels[(int) currentActiveViewModel], lastGunPos, .2f);
                }

                weaponAnim.SetBool("Sprint", isRunning);
            }

            lastWalkCheck = isMoving;
            lastRunningCheck = isRunning;

            if (lastRunningCheck && onScopeAim) ScopeOut();

            if ((lastWalkCheck || lastRunningCheck) && handleInspectionSoundsRoutine != null)
                StopCoroutine(handleInspectionSoundsRoutine);
        }

        IEnumerator HanldeInspectionSound(int randomIdle)
        {
            if (randomIdle == 1)
            {
                yield return new WaitForSeconds(.65f);
                virtualAudioSource.pitch = 1;
                virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[0]);
                yield break;
            }

            yield return new WaitForSeconds(.96f);
            virtualAudioSource.pitch = 1;
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[1]);

            yield return new WaitForSeconds(1.14f);
            virtualAudioSource.PlayOneShot(inspectionSoundsHandle.Result[2]);
        }

        void SpawnBullets()
        {
            for (int i = 0; i < weaponData.bulletsPerMag; i++)
            {
                Quaternion angle = Quaternion.AngleAxis((360f / (float) -weaponData.bulletsPerMag) * i, Vector3.forward);
                Rigidbody bullet = Instantiate(bulletCasingPrefab, bulletCasingPivot.position + (angle * bulletCasingPivot.up * .05f), bulletCasingPivot.rotation);
                bullet.AddForce(bullet.transform.forward * 2, ForceMode.Impulse);
                bullet.AddTorque(bullet.transform.forward * 15, ForceMode.Impulse);
            }
        }
    }
}
