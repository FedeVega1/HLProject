using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOptions : CachedRectTransform
{
    [SerializeField] Toggle backgroundToggle, fullscreenToggle;
    [SerializeField] TMP_Dropdown resolutionDropdown;

    public void Init()
    {
        Resolution[] resolutions = Screen.resolutions;
        List<string> resStrings = new List<string>();
        int size = resolutions.Length;
        
        for (int i = 0; i < size; i++)
            resStrings.Add(resolutions[i].ToString());
        
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resStrings);

        backgroundToggle.isOn = GameManager.INS.videoOptions.EnableMainMenuBackgrounds;
        fullscreenToggle.isOn = GameManager.INS.videoOptions.EnableFullScreen;
        resolutionDropdown.value = GameManager.INS.videoOptions.CurrentResolution;
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
        GameManager.INS.videoOptions.EnableFullScreen = fullscreenToggle.isOn;
    }

    public void OnResolutionDropdownValue()
    {
        GameManager.INS.videoOptions.CurrentResolution = resolutionDropdown.value;
    }
}
