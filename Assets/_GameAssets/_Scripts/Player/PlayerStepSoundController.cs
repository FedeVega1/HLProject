using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace HLProject
{
    public class PlayerStepSoundController : MonoBehaviour
    {
        [SerializeField] AudioSource setpsSource;

        static AsyncOperationHandle<IList<AudioClip>> baseStepHandle, woodStepHandle, chainStepHandle, metalStepHandle, concreteStepHandle, dirtStepHandle, fleshStepHandle;
        MaterialType currentFloorType;

        void Awake()
        {
            if (!baseStepHandle.IsValid())
            {
                baseStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "tile1", "tile2", "tile3", "tile4", "tile5", "tile6", "tile7", "tile8", "tile9", "tile10", "tile11" }, null, Addressables.MergeMode.Union);
                baseStepHandle.Completed += OnSoundsLoaded;
            }

            if (!woodStepHandle.IsValid())
            {
                woodStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "wood1", "wood2", "wood3", "wood4", "wood5", "wood6", "wood7", "wood8", "wood9", "wood10", "wood11" }, null, Addressables.MergeMode.Union);
                woodStepHandle.Completed += OnSoundsLoaded;
            }

            if (!chainStepHandle.IsValid())
            {
                chainStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "chainlink1", "chainlink2", "chainlink3", "chainlink4", "chainlink5", "chainlink6", "chainlink7", "chainlink8", "chainlink9", "chainlink10", "chainlink11" }, null, Addressables.MergeMode.Union);
                chainStepHandle.Completed += OnSoundsLoaded;
            }

            if (!metalStepHandle.IsValid())
            {
                metalStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "metal1", "metal2", "metal3", "metal4", "metal5", "metal6", "metal7", "metal8", "metal9", "metal10", "metal11" }, null, Addressables.MergeMode.Union);
                metalStepHandle.Completed += OnSoundsLoaded;
            }

            if (!concreteStepHandle.IsValid())
            {
                concreteStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "concrete1", "concrete2", "concrete3", "concrete4", "concrete5", "concrete6", "concrete7", "concrete8", "concrete9", "concrete10", "concrete11" }, null, Addressables.MergeMode.Union);
                concreteStepHandle.Completed += OnSoundsLoaded;
            }

            if (!dirtStepHandle.IsValid())
            {
                dirtStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "dirt1", "dirt2", "dirt3", "dirt4", "dirt5", "dirt6", "dirt7", "dirt8", "dirt9", "dirt10", "dirt11" }, null, Addressables.MergeMode.Union);
                dirtStepHandle.Completed += OnSoundsLoaded;
            }

            if (!fleshStepHandle.IsValid())
            {
                fleshStepHandle = Addressables.LoadAssetsAsync<AudioClip>(new List<string> { "flesh1", "flesh2", "flesh3", "flesh4", "flesh5", "flesh6", "flesh7", "flesh8", "flesh9", "flesh10", "flesh11" }, null, Addressables.MergeMode.Union);
                fleshStepHandle.Completed += OnSoundsLoaded;
            }
        }

        void OnSoundsLoaded(AsyncOperationHandle<IList<AudioClip>> operation)
        {
            if (operation.Status == AsyncOperationStatus.Failed)
                Debug.LogErrorFormat("Failed to load step sounds for: {0}", operation.DebugName);
        }

        public void SetCurrentFloorMaterial(MaterialType currentType) => currentFloorType = currentType;
        public void PlayStepSound() => setpsSource.PlayOneShot(currentFloorType switch
        {
            MaterialType.Wood => woodStepHandle.Result[Random.Range(0, woodStepHandle.Result.Count)],
            MaterialType.Chain => chainStepHandle.Result[Random.Range(0, chainStepHandle.Result.Count)],
            MaterialType.Metal => metalStepHandle.Result[Random.Range(0, metalStepHandle.Result.Count)],
            MaterialType.Concrete => concreteStepHandle.Result[Random.Range(0, concreteStepHandle.Result.Count)],
            MaterialType.Dirt => dirtStepHandle.Result[Random.Range(0, dirtStepHandle.Result.Count)],
            MaterialType.Flesh => fleshStepHandle.Result[Random.Range(0, fleshStepHandle.Result.Count)],
            _ => baseStepHandle.Result[Random.Range(0, baseStepHandle.Result.Count)],
        });

        public void ClearStepSounds()
        {
            Addressables.Release(baseStepHandle);
            Addressables.Release(woodStepHandle);
            Addressables.Release(chainStepHandle);
            Addressables.Release(metalStepHandle);
            Addressables.Release(concreteStepHandle);
            Addressables.Release(dirtStepHandle);
            Addressables.Release(fleshStepHandle);
        }
    }
}
