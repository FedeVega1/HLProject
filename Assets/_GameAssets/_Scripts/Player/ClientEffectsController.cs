using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Mirror;
using HLProject.Managers;

namespace HLProject.Characters
{
    public class ClientEffectsController : CachedTransform
    {
        [SerializeField] AudioSource localPlayerSource, localPlayerPanSource;

        List<int> explosionTweenIDs;
        float originalMaxExposure;
        Volume playerLocalVolume;
        Exposure exposureComponent;
        MotionBlur motionBlurComponent;
        DepthOfField depthOfFieldComponent;
        Vignette vignetteComponent;

        Player localPlayer;

        AsyncOperationHandle<IList<AudioClip>> hearingLossSoundHandle, bulletFlyBySoundsHandle;

        public void Init(Player player, Volume localVolume)
        {
            explosionTweenIDs = new List<int>();

            playerLocalVolume = localVolume;
            playerLocalVolume.profile.TryGet<Exposure>(out exposureComponent);
            playerLocalVolume.profile.TryGet<MotionBlur>(out motionBlurComponent);
            playerLocalVolume.profile.TryGet<DepthOfField>(out depthOfFieldComponent);
            playerLocalVolume.profile.TryGet<Vignette>(out vignetteComponent);

            localPlayer = player;
        }

        public void LoadAssets()
        {
            hearingLossSoundHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "impairedhearing_1", "impairedhearing_2", "impairedhearing_3", "impairedhearing_4" }, null, Addressables.MergeMode.Union);
            hearingLossSoundHandle.Completed += OnSoundsLoaded;

            List<string> bulletFlybySoundsString = new List<string>();
            for (int i = 1; i < 23; i++) bulletFlybySoundsString.Add("bulletltor" + i.ToString("00"));

            bulletFlyBySoundsHandle = Addressables.LoadAssetsAsync<AudioClip>(bulletFlybySoundsString, null, Addressables.MergeMode.Union);
            bulletFlyBySoundsHandle.Completed += OnSoundsLoaded;
        }

        void OnSoundsLoaded(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Sounds for local player: {0}", operation.OperationException);
        }

        void OnEnable()
        {
            AudioManager.INS.RegisterAudioSource(localPlayerSource, AudioManager.AudioSourceTarget.LocalPlayer);
        }

        void OnDisable()
        {
            AudioManager.INS.UnRegisterAudioSource(localPlayerSource, AudioManager.AudioSourceTarget.LocalPlayer);
        }

        void Update()
        {
            if (localPlayer.OnShock)
            {
                double currentShockTime = 1 - (localPlayer.ShockTime - NetworkTime.time);
                exposureComponent.limitMax.value = 14f * (float) currentShockTime;
            }

            HandlePlayerSuppression();
        }

        void HandlePlayerSuppression()
        {
            if (localPlayer.SuppressionAmmount <= 0) return;
            vignetteComponent.intensity.value = localPlayer.SuppressionAmmount * .5f;

            float invert = 1 - localPlayer.SuppressionAmmount;

            AudioManager.INS.CurrentWeaponVolume = Mathf.Clamp(invert, .08f, 1);
            AudioManager.INS.OtherVolume = Mathf.Clamp(invert, .08f, 1);

            depthOfFieldComponent.nearFocusEnd.value = localPlayer.SuppressionAmmount * 10;
            depthOfFieldComponent.farFocusEnd.value = invert * 80000;
        }

        public void OnPlayerTakesDamage(DamageType type)
        {
            switch (type)
            {
                case DamageType.Shock:
                    originalMaxExposure = exposureComponent.limitMax.value;
                    exposureComponent.limitMax.value = 0;
                    break;

                case DamageType.Explosion:
                    localPlayerSource.panStereo = 1;
                    AudioClip clip = hearingLossSoundHandle.Result[Random.Range(0, hearingLossSoundHandle.Result.Count)];
                    if (!localPlayerSource.isPlaying)
                        localPlayerSource.PlayOneShot(clip);

                    int size = explosionTweenIDs.Count;
                    if (size > 0)
                    {
                        for (int i = 0; i < size; i++)
                            LeanTween.cancel(explosionTweenIDs[i]);
                    }

                    motionBlurComponent.active = true;
                    motionBlurComponent.intensity.value = 150;

                    depthOfFieldComponent.nearFocusEnd.value = 10;
                    depthOfFieldComponent.farFocusEnd.value = 0;

                    float weaponsStartingVolume = AudioManager.INS.CurrentWeaponVolume;
                    float otherStartingVolume = AudioManager.INS.CurrentWeaponVolume;

                    explosionTweenIDs.Add(LeanTween.value(weaponsStartingVolume, .02f, .2f).setOnUpdate((float x) => AudioManager.INS.CurrentWeaponVolume = x).uniqueId);
                    explosionTweenIDs.Add(LeanTween.value(.02f, weaponsStartingVolume, .7f).setOnUpdate((float x) => AudioManager.INS.CurrentWeaponVolume = x).setDelay(clip.length * .6f).uniqueId);

                    explosionTweenIDs.Add(LeanTween.value(otherStartingVolume, .02f, .2f).setOnUpdate((float x) => AudioManager.INS.OtherVolume = x).uniqueId);
                    explosionTweenIDs.Add(LeanTween.value(.02f, otherStartingVolume, .7f).setOnUpdate((float x) => AudioManager.INS.OtherVolume = x).setDelay(clip.length * .6f).uniqueId);

                    explosionTweenIDs.Add(LeanTween.value(1, 0, .55f).setOnUpdate((float x) => motionBlurComponent.intensity.value = x * 150f).setOnComplete(() => motionBlurComponent.active = false).uniqueId);
                    explosionTweenIDs.Add(LeanTween.value(0, 1, .2f).setOnUpdate((float x) =>
                    {
                        depthOfFieldComponent.nearFocusEnd.value = (1 - x) * 10;
                        depthOfFieldComponent.farFocusEnd.value = x * 80000;
                        print(depthOfFieldComponent.farFocusEnd.value);
                    }).setEaseOutQuad().setDelay(clip.length * .8f).uniqueId);
                    break;
            }
        }

        public void PlayBulletFlyBy(Vector3 origin)
        {
            localPlayerPanSource.panStereo = Mathf.Clamp(MyTransform.InverseTransformPoint(origin).x, -1, 1);
            localPlayerPanSource.PlayOneShot(bulletFlyBySoundsHandle.Result[Random.Range(0, bulletFlyBySoundsHandle.Result.Count)]);
        }
    }
}
