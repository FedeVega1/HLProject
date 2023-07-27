using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HLProject
{
    public class VideoOptions
    {
        int _EnableMainMenuBackgrounds = -1;
        public bool EnableMainMenuBackgrounds
        {
            get
            {
                if (_EnableMainMenuBackgrounds == -1)
                {
                    if (PlayerPrefs.HasKey("MainMenuBackgrounds"))
                    {
                        _EnableMainMenuBackgrounds = PlayerPrefs.GetInt("MainMenuBackgrounds");
                    }
                    else
                    {
                        _EnableMainMenuBackgrounds = 1;
                        PlayerPrefs.SetInt("MainMenuBackgrounds", _EnableMainMenuBackgrounds);
                    }
                }

                return _EnableMainMenuBackgrounds == 1;
            }

            set
            {
                _EnableMainMenuBackgrounds = value ? 1 : 0;
                PlayerPrefs.SetInt("MainMenuBackgrounds", _EnableMainMenuBackgrounds);
            }
        }

        int _FullScreenMode = -1;
        public FullScreenMode CurrentFullScreenMode
        {
            get
            {
                if (_FullScreenMode == -1)
                {
                    if (PlayerPrefs.HasKey("EnableFullscreen"))
                    {
                        _FullScreenMode = PlayerPrefs.GetInt("EnableFullscreen");
                    }
                    else
                    {
                        _FullScreenMode = 1;
                        PlayerPrefs.SetInt("EnableFullscreen", _FullScreenMode);
                    }
                }

                return (FullScreenMode) _FullScreenMode;
            }

            set
            {
                _FullScreenMode = (int) value;
                PlayerPrefs.SetInt("EnableFullscreen", _FullScreenMode);
                Screen.fullScreenMode = value;
            }
        }

        int _CurrentResolution = -1;
        public int CurrentResolution
        {
            get
            {
                if (_CurrentResolution == -1)
                {
                    if (PlayerPrefs.HasKey("CurrentResolution"))
                    {
                        _CurrentResolution = PlayerPrefs.GetInt("CurrentResolution");
                    }
                    else
                    {
                        _CurrentResolution = GetResolutionIndex(Screen.currentResolution);
                        PlayerPrefs.SetInt("CurrentResolution", _CurrentResolution);
                    }
                }

                return _CurrentResolution;
            }

            set
            {
                _CurrentResolution = value;
                PlayerPrefs.SetInt("CurrentResolution", _CurrentResolution);

                Resolution newRes = GetResolutionByIndex(_CurrentResolution);
                Screen.SetResolution(newRes.width, newRes.height, CurrentFullScreenMode, newRes.refreshRateRatio);
            }
        }

        int _CurrentQualityPreset = -1;
        public int CurrentQualityPreset
        {
            get
            {
                if (_CurrentQualityPreset == -1)
                {
                    if (PlayerPrefs.HasKey("CurrentQualityPreset"))
                    {
                        _CurrentQualityPreset = PlayerPrefs.GetInt("CurrentQualityPreset");
                    }
                    else
                    {
                        _CurrentQualityPreset = QualitySettings.GetQualityLevel();
                        PlayerPrefs.SetInt("CurrentQualityPreset", _CurrentQualityPreset);
                    }
                }

                return _CurrentQualityPreset;
            }

            set
            {
                _CurrentQualityPreset = value;
                PlayerPrefs.SetInt("CurrentQualityPreset", _CurrentQualityPreset);

                QualitySettings.SetQualityLevel(_CurrentQualityPreset, true);
            }
        }

        Resolution[] resArray = Screen.resolutions;

        int GetResolutionIndex(Resolution res)
        {
            int size = resArray.Length;

            for (int i = 0; i < size; i++)
            {
                if (resArray[i].width == res.width && resArray[i].height == res.height && resArray[i].refreshRateRatio.value == res.refreshRateRatio.value)
                    return i;
            }

            return 0;
        }

        int _EnableMotionBlur = -1;
        public bool EnableMotionBlur
        {
            get
            {
                if (_EnableMotionBlur == -1)
                {
                    if (PlayerPrefs.HasKey("EnableMotionBlur"))
                    {
                        _EnableMotionBlur = PlayerPrefs.GetInt("EnableMotionBlur");
                    }
                    else
                    {
                        _EnableMotionBlur = 1;
                        PlayerPrefs.SetInt("EnableMotionBlur", _EnableMotionBlur);
                    }
                }

                return _EnableMotionBlur == 1;
            }

            set
            {
                _EnableMotionBlur = value ? 1 : 0;
                PlayerPrefs.SetInt("EnableMotionBlur", _EnableMotionBlur);
            }
        }

        Resolution GetResolutionByIndex(int index) => resArray[index];
    }

    public class UIVideoOptionsTab : CachedRectTransform, IOptionTab
    {
        [SerializeField] Toggle backgroundToggle, fullscreenToggle, motionBlurToggle;
        [SerializeField] TMP_Dropdown resolutionDropdown, qualityPresetDropdown;
        public string TabType { get; set; } = "Video";

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
            motionBlurToggle.isOn = GameManager.INS.VideoOptions.EnableMotionBlur;
        }

        public void ToggleTab(bool toggle)
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
            GameManager.INS.VideoOptions.EnableMotionBlur = motionBlurToggle.isOn;
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
