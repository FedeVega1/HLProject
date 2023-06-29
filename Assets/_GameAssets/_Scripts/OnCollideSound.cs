using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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
