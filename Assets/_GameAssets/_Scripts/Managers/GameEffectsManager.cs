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

        MotionBlur mainMotionBlur;

        void Awake()
        {
            mainVolume.profile.TryGet<MotionBlur>(out mainMotionBlur);
            mainMotionBlur.intensity.value = GameManager.INS.VideoOptions.EnableMotionBlur ? 1 : 0;
        }
    }
}
