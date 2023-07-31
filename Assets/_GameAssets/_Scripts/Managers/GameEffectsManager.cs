using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace HLProject.Managers
{
    public class GameEffectsManager : MonoBehaviour
    {
        [SerializeField] Volume mainVolume;
        [SerializeField] VolumeProfile defaultGlobalProfile;

        MotionBlur mainMotionBlur;
        RenderPipelineSettings pipelineSettings;

        void Awake()
        {
            HDRenderPipelineAsset renderAsset = GraphicsSettings.currentRenderPipeline as HDRenderPipelineAsset;
            pipelineSettings = renderAsset.currentPlatformRenderPipelineSettings;

            //mainVolume.profile.TryGet<MotionBlur>(out mainMotionBlur);
            defaultGlobalProfile.TryGet<MotionBlur>(out mainMotionBlur);
            mainMotionBlur.intensity.value = GameManager.INS.VideoOptions.EnableMotionBlur ? 1 : 0;
        }
    }
}
