using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public abstract class BaseClientWeapon : CachedTransform
    {
        protected static AsyncOperationHandle<IList<AudioClip>> weaponWalkSoundsHandle, weaponSprintSoundsHandle, weaponMovSoundsHandle, weaponFireModeSwitchSoundsHandle;

        protected enum ActiveViewModel { World, Virtual }

        [SerializeField] protected GameObject[] viewModels;
        [SerializeField] protected Transform virtualBulletPivot, worldBulletPivot, weaponRootBone;
        [SerializeField] protected float weaponSwaySmooth = 5;
        [SerializeField] protected Vector3 weaponSwayAmmount;
        [SerializeField] protected AudioSource virtualAudioSource, worldAudioSource, virtualMovementSource;
        [SerializeField] protected AudioClip deploySound;
        [SerializeField] protected Vector3 aimPosition;
        [SerializeField] protected Quaternion aimRotation;

        public Transform WeaponRootBone => weaponRootBone;

        protected bool isServer, isDrawn, enableWeaponSway, onScopeAim, doingScopeAnim;
        protected ActiveViewModel currentActiveViewModel;
        protected int scopeID = -1, currentPlayerTeam;
        protected Vector3 defaultWeaponPos;
        protected Vector3 swayFactor;
        protected Quaternion defaultWeaponRotation, lastCameraRotation;

        protected GameObject weaponPropPrefab;
        protected BulletData bulletData;
        protected WeaponData weaponData;

        void Awake()
        {
            LoadAssets();
        }

        public virtual void Init(bool isServer, WeaponData wData, BulletData data, GameObject propPrefab)
        {
            bulletData = data;
            weaponData = wData;

            weaponPropPrefab = propPrefab;
            ToggleAllViewModels(false);

            currentActiveViewModel = isServer ? ActiveViewModel.World : ActiveViewModel.Virtual;
            viewModels[(int) currentActiveViewModel].SetActive(true);

            this.isServer = isServer;
            gameObject.SetActive(false);
        }

        protected virtual void LoadAssets()
        {
            if (!weaponWalkSoundsHandle.IsValid())
            {
                weaponWalkSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>("WeaponSounds/Movement/Walk", null);
                weaponWalkSoundsHandle.Completed += OnWeaponSoundsComplete;
            }

            if (!weaponSprintSoundsHandle.IsValid())
            {
                weaponSprintSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>("WeaponSounds/Movement/Sprint", null);
                weaponSprintSoundsHandle.Completed += OnWeaponSoundsComplete;
            }

            if (!weaponMovSoundsHandle.IsValid())
            {
                weaponMovSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>("WeaponSounds/Movement", null);
                weaponMovSoundsHandle.Completed += OnWeaponSoundsComplete;
            }

            if (!weaponFireModeSwitchSoundsHandle.IsValid())
            {
                weaponFireModeSwitchSoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "switch_single", "switch_burst" }, null, Addressables.MergeMode.Union);
                weaponFireModeSwitchSoundsHandle.Completed += OnWeaponSoundsComplete;
            }
        }

        protected void OnWeaponSoundsComplete(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Weapon Sounds: {0}", operation.OperationException);
        }

        protected virtual void OnEnable()
        {
            AudioManager.INS.RegisterAudioSource(virtualAudioSource, AudioManager.AudioSourceTarget.CurrentWeapon);
            AudioManager.INS.RegisterAudioSource(worldAudioSource, AudioManager.AudioSourceTarget.CurrentWeapon);
            AudioManager.INS.RegisterAudioSource(virtualMovementSource, AudioManager.AudioSourceTarget.CurrentWeapon);
        }

        protected virtual void OnDestroy()
        {
            AudioManager.INS.UnRegisterAudioSource(virtualAudioSource, AudioManager.AudioSourceTarget.CurrentWeapon);
            AudioManager.INS.UnRegisterAudioSource(worldAudioSource, AudioManager.AudioSourceTarget.CurrentWeapon);
            AudioManager.INS.UnRegisterAudioSource(virtualMovementSource, AudioManager.AudioSourceTarget.CurrentWeapon);

            if (weaponWalkSoundsHandle.IsValid()) Addressables.Release(weaponWalkSoundsHandle);
            if (weaponWalkSoundsHandle.IsValid()) Addressables.Release(weaponSprintSoundsHandle);
            if (weaponWalkSoundsHandle.IsValid()) Addressables.Release(weaponMovSoundsHandle);
        }

        protected virtual void Start()
        {
            defaultWeaponRotation = MyTransform.localRotation;
            defaultWeaponPos = MyTransform.localPosition;
            enableWeaponSway = true;
        }

        protected virtual void Update()
        {
            if (!isDrawn || !enableWeaponSway) return;

            swayFactor.x = Input.GetAxis("Mouse Y") * weaponSwayAmmount.x;
            swayFactor.y = -Input.GetAxis("Mouse X") * weaponSwayAmmount.y;
            swayFactor.z = -Input.GetAxis("Mouse X") * weaponSwayAmmount.z;

            Quaternion sway = Quaternion.Euler(defaultWeaponRotation.x + swayFactor.x, defaultWeaponRotation.y + swayFactor.y, defaultWeaponRotation.z + swayFactor.z);
            MyTransform.localRotation = Quaternion.Slerp(MyTransform.localRotation, sway, Time.deltaTime * weaponSwaySmooth);
        }

        public virtual void Fire(Vector3 destination, bool didHit, int ammo)
        {
            if (!isDrawn || doingScopeAnim) return;
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.initFire, () =>
            {
                GameObject bulletObject = Instantiate(bulletData.bulletPrefab, isServer ? worldBulletPivot : virtualBulletPivot);
                Bullet bullet = bulletObject.GetComponent<Bullet>();

                bullet.Init(bulletData.initialSpeed, didHit);
                bullet.TravelTo(destination);
                bullet.MyTransform.parent = null;
            });
        }

        public virtual void MeleeSwing(bool didHit, bool playerHit, bool killHit) { }

        public abstract void EmptyFire();

        public abstract void AltFire(Vector3 destination, bool didHit, int ammo);

        public abstract void OnAltMode(bool toggle, WeaponData newData);

        public virtual void ScopeIn()
        {
            if (!isDrawn || onScopeAim) return;
            enableWeaponSway = false;
            onScopeAim = true;

            Vector3 currentPos = MyTransform.localPosition;
            Quaternion currentRot = MyTransform.localRotation;

            if (scopeID != -1) LeanTween.cancel(scopeID);
            doingScopeAnim = true;
            scopeID = LeanTween.value(0, 1, weaponData.weaponAnimsTiming.zoomInSpeed).setOnUpdate((float x) =>
            {
                GameModeManager.INS.SetClientVCameraFOV(Mathf.Lerp(90, 60, x));
                MyTransform.localPosition = Vector3.Lerp(currentPos, aimPosition, x);
                MyTransform.localRotation = Quaternion.Lerp(currentRot, aimRotation, x);
            }).setOnComplete(() => { scopeID = -1; doingScopeAnim = false; }).uniqueId;
        }

        public virtual void ScopeOut()
        {
            if (!isDrawn || !onScopeAim) return;
            enableWeaponSway = true;
            onScopeAim = false;

            Vector3 currentPos = MyTransform.localPosition;
            Quaternion currentRot = MyTransform.localRotation;

            if (scopeID != -1) LeanTween.cancel(scopeID);
            doingScopeAnim = true;
            scopeID = LeanTween.value(0, 1, weaponData.weaponAnimsTiming.zoomOutSpeed).setOnUpdate((float x) =>
            {
                GameModeManager.INS.SetClientVCameraFOV(Mathf.Lerp(60, 90, x));
                MyTransform.localPosition = Vector3.Lerp(currentPos, defaultWeaponPos, x);
                MyTransform.localRotation = Quaternion.Lerp(currentRot, defaultWeaponRotation, x);
            }).setOnComplete(() => { scopeID = -1; doingScopeAnim = false; }).uniqueId;
        }

        public abstract void Reload(int bulletsToReload);

        public virtual void HolsterWeapon()
        {
            isDrawn = false;
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.holster, () => gameObject.SetActive(false));
        }

        public virtual void DrawWeapon()
        {
            gameObject.SetActive(true);
            LeanTween.delayedCall(weaponData.weaponAnimsTiming.draw, () => isDrawn = true);
        }

        public virtual void ToggleAllViewModels(bool toggle)
        {
            int size = viewModels.Length;
            for (byte i = 0; i < size; i++) viewModels[i].SetActive(toggle);
        }

        public virtual void DropProp()
        {
            Instantiate(weaponPropPrefab, MyTransform.position, MyTransform.rotation);
            Destroy(gameObject);
        }

        public virtual Transform GetVirtualPivot() => virtualBulletPivot;
        public virtual Transform GetWorldPivot() => worldBulletPivot;

        public abstract void CheckPlayerMovement(bool isMoving, bool isRunning);

        public void ToggleWeaponSway(bool toggle) => enableWeaponSway = toggle;

        public Animator GetCurrentViewmodelAnimator() => viewModels[(int) currentActiveViewModel].GetComponentInChildren<Animator>();

        public void SetPlayerTeam(int team) => currentPlayerTeam = team;

        public virtual void ChangeFireMode(int fireMode)
        {
            if (fireMode >= weaponFireModeSwitchSoundsHandle.Result.Count) return;
            virtualAudioSource.PlayOneShot(weaponFireModeSwitchSoundsHandle.Result[fireMode]);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (virtualBulletPivot == null) return;
            Debug.DrawRay(virtualBulletPivot.position, virtualBulletPivot.forward, Color.red, Time.deltaTime);
        }
#endif
    }
}
