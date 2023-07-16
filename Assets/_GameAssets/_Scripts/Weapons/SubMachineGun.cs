using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class SubMachineGun : BaseClientWeapon
    {
        [SerializeField] Transform magazinePivot, bulletEffectPivot;
        [SerializeField] Animator[] weaponAnimators;
        [SerializeField] Rigidbody pistolMagazinePrefab, pistolBulletCasingPrefab;

        Animator weaponAnim;

        int delayTweenID = -1;
        bool lastWalkCheck, lastRunningCheck, isFiring;
        float randomInspectTime, movementSoundTime;
        Coroutine handleInspectionSoundsRoutine;

        AsyncOperationHandle<IList<AudioClip>> virtualShootSoundsHandle, reloadSoundsHandle;
        AsyncOperationHandle<AudioClip> lowAmmoSoundHandle, zoomInSoundHandle, zoomOutSoundHandle;

        protected override void LoadAssets()
        {
            base.LoadAssets();

            virtualShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "smg1_fire1", "smg1_fire2", "smg1_fire3" }, null, Addressables.MergeMode.Union);
            virtualShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            //worldShootSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "WeaponSounds/PistolWorld", "FireSound" }, null, Addressables.MergeMode.Intersection);
            //worldShootSoundsHandle.Completed += OnWeaponSoundsComplete;

            reloadSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "smg1_reload" }, null, Addressables.MergeMode.Union);
            reloadSoundsHandle.Completed += OnWeaponSoundsComplete;

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
            //Addressables.Release(worldShootSoundsHandle);
            Addressables.Release(reloadSoundsHandle);
            Addressables.Release(lowAmmoSoundHandle);
            Addressables.Release(zoomInSoundHandle);
            Addressables.Release(zoomOutSoundHandle);
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

            bool onLastBullet = ammo == 1;

            if (handleInspectionSoundsRoutine != null) StopCoroutine(handleInspectionSoundsRoutine);

            weaponAnim.SetTrigger("Fire");
            weaponAnim.SetInteger("RandomFire", Random.Range(0, 4));

            virtualAudioSource.PlayOneShot(virtualShootSoundsHandle.Result[Random.Range(0, virtualShootSoundsHandle.Result.Count)]);

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

            /*weaponAnim.SetTrigger("Fire");
            weaponAnim.SetInteger("RandomFire", 4);*/
        }

        public override void AltFire(Vector3 destination, bool didHit) { }

        public override void Reload(int bulletsToReload)
        {
            LeanTween.delayedCall(.67f, SpawnMagazine);
            /*if (currentActiveViewModel == ActiveViewModel.World)
            {
                worldAudioSource.PlayOneShot(reloadSoundsHandle.Result[4]);
                return;
            }*/

            virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[0]);

            /*LeanTween.delayedCall(.375f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[1]));
            LeanTween.delayedCall(1, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[2]));
            LeanTween.delayedCall(1.54f, () => virtualAudioSource.PlayOneShot(reloadSoundsHandle.Result[3]));*/

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
            //Vector3 cross = Vector3.Cross(-MyTransform.forward, MyTransform.up * Random.Range(.5f, 1f));
            float y = Random.Range(.5f, .65f);
            Vector3 cross = Vector3.Normalize(new Vector3(1 - y, y, 0));
            Debug.DrawRay(bulletEffectPivot.position, cross, Color.red, 2);
            rb.AddForce(cross * 6, ForceMode.Impulse);
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
