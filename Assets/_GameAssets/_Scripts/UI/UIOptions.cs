using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject
{
    public class UIOptions : CachedRectTransform
    {
        [SerializeField] Toggle backgroundToggle, fullscreenToggle, motionBlurToggle;
        [SerializeField] TMP_Dropdown resolutionDropdown, qualityPresetDropdown;

        public void Init()
        {
            List<string> resStrings = new List<string>();
            GetResolutionStrings(ref resStrings);

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(resStrings);

            List<string> presetStrings = new List<string>(QualitySettings.names);
            qualityPresetDropdown.ClearOptions();
            qualityPresetDropdown.AddOptions(presetStrings);

            backgroundToggle.isOn = GameManager.INS.VideoOptions.EnableMainMenuBackgrounds;
            fullscreenToggle.isOn = GameManager.INS.VideoOptions.CurrentFullScreenMode == FullScreenMode.FullScreenWindow;
            resolutionDropdown.value = GameManager.INS.VideoOptions.CurrentResolution;
            qualityPresetDropdown.value = GameManager.INS.VideoOptions.CurrentQualityPreset;
            //motionBlurToggle
        }

        public void Toggle(bool toggle)
        {
            MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
        }

        public void OnBackgroundToggle()
        {
            GameManager.INS.VideoOptions.EnableMainMenuBackgrounds = backgroundToggle.isOn;
        }

        public void OnFullscreenToggle()
        {
            GameManager.INS.VideoOptions.CurrentFullScreenMode = fullscreenToggle.isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public void OnResolutionDropdownValue()
        {
            GameManager.INS.VideoOptions.CurrentResolution = resolutionDropdown.value;
        }

        public void OnQualityPresetDropdownValue()
        {
            GameManager.INS.VideoOptions.CurrentQualityPreset = qualityPresetDropdown.value;
        }

        public void OnMotionBlurToggle()
        {
            //motionBlurToggle
        }

        void GetResolutionStrings(ref List<string> resStrings)
        {
            Resolution[] resolutions = Screen.resolutions;
            int size = resolutions.Length;

            for (int i = 0; i < size; i++)
                resStrings.Add(resolutions[i].ToString());
        }
    }
}
