using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOptions : CachedRectTransform
{
    [SerializeField] Toggle backgroundToggle, fullscreenToggle;
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

        backgroundToggle.isOn = GameManager.INS.videoOptions.EnableMainMenuBackgrounds;
        fullscreenToggle.isOn = GameManager.INS.videoOptions.CurrentFullScreenMode == FullScreenMode.FullScreenWindow;
        resolutionDropdown.value = GameManager.INS.videoOptions.CurrentResolution;
        qualityPresetDropdown.value = GameManager.INS.videoOptions.CurrentQualityPreset;
    }

    public void Toggle(bool toggle)
    {
        MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
    }

    public void OnBackgroundToggle()
    {
        GameManager.INS.videoOptions.EnableMainMenuBackgrounds = backgroundToggle.isOn;
    }

    public void OnFullscreenToggle()
    {
        GameManager.INS.videoOptions.CurrentFullScreenMode = fullscreenToggle.isOn ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
    }

    public void OnResolutionDropdownValue()
    {
        GameManager.INS.videoOptions.CurrentResolution = resolutionDropdown.value;
    }

    public void OnQualityPresetDropdownValue()
    {
        GameManager.INS.videoOptions.CurrentQualityPreset = qualityPresetDropdown.value;
    }

    void GetResolutionStrings(ref List<string> resStrings)
    {
        Resolution[] resolutions = Screen.resolutions;
        int size = resolutions.Length;

        for (int i = 0; i < size; i++)
            resStrings.Add(resolutions[i].ToString());
    }
}
