using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HLProject.Managers;

namespace HLProject
{
    [RequireComponent(typeof(AudioSource))]
    public class OnCollideSound : MonoBehaviour
    {
        [SerializeField] List<AssetReference> randomSoundsToPlay;

        AudioSource aSrc;
        AsyncOperationHandle<IList<AudioClip>> randomSounds;

        void Awake()
        {
            aSrc = GetComponent<AudioSource>();

            randomSounds = Addressables.LoadAssetsAsync<AudioClip>(randomSoundsToPlay, null, Addressables.MergeMode.Union);
            randomSounds.Completed += OnSoundsLoadComplete;
        }

        void OnEnable() => AudioManager.INS.RegisterAudioSource(aSrc, AudioManager.AudioSourceTarget.Other);
        void OnDisable() => AudioManager.INS.UnRegisterAudioSource(aSrc, AudioManager.AudioSourceTarget.Other);

        void OnSoundsLoadComplete(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Couldn't load Random Sounds: {0}", operation.OperationException);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!randomSounds.IsDone) return;
            aSrc.PlayOneShot(randomSounds.Result[Random.Range(0, randomSounds.Result.Count)]);
        }
    }
}
