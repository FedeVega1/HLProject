using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HLProject.Managers;

namespace HLProject.UI
{
    public class AudioOptions
    {
        float _MasterVolume = -1;
        public float MasterVolume
        {
            get
            {
                if (_MasterVolume == -1)
                {
                    if (PlayerPrefs.HasKey("MasterVolume"))
                    {
                        _MasterVolume = PlayerPrefs.GetFloat("MasterVolume");
                    }
                    else
                    {
                        _MasterVolume = 1;
                        AudioManager.INS.UpdateMasterVolume(_MasterVolume);
                        PlayerPrefs.SetFloat("MasterVolume", _MasterVolume);
                    }
                }

                return _MasterVolume;
            }

            set
            {
                _MasterVolume = value;
                AudioManager.INS.UpdateMasterVolume(_MasterVolume);
                PlayerPrefs.SetFloat("MasterVolume", _MasterVolume);
            }
        }

        float _SFXVolume = -1;
        public float SFXVolume
        {
            get
            {
                if (_SFXVolume == -1)
                {
                    if (PlayerPrefs.HasKey("SFXVolume"))
                    {
                        _SFXVolume = PlayerPrefs.GetFloat("SFXVolume");
                    }
                    else
                    {
                        _SFXVolume = 1;
                        AudioManager.INS.UpdateSFXVolume(_SFXVolume);
                        PlayerPrefs.SetFloat("SFXVolume", _SFXVolume);
                    }
                }

                return _SFXVolume;
            }

            set
            {
                _SFXVolume = value;
                AudioManager.INS.UpdateSFXVolume(_SFXVolume);
                PlayerPrefs.SetFloat("SFXVolume", _SFXVolume);
            }
        }

        float _MusicVolume = -1;
        public float MusicVolume
        {
            get
            {
                if (_MusicVolume == -1)
                {
                    if (PlayerPrefs.HasKey("MusicVolume"))
                    {
                        _MusicVolume = PlayerPrefs.GetFloat("MusicVolume");
                    }
                    else
                    {
                        _MusicVolume = 1;
                        PlayerPrefs.SetFloat("MusicVolume", _MusicVolume);
                    }
                }

                return _MusicVolume;
            }

            set
            {
                _MusicVolume = value;
                PlayerPrefs.SetFloat("MusicVolume", _MusicVolume);
            }
        }
    }

    public class UIAudioOptionsTab : CachedRectTransform, IOptionTab
    {
        [SerializeField] Slider masterVolumeSlider, sfxVolumeSLider, musicVolumeSlider;

        public string TabType { get; set; } = "Audio";

        public void Init()
        {
            masterVolumeSlider.value = GameManager.INS.AudioOptions.MasterVolume;
            sfxVolumeSLider.value = GameManager.INS.AudioOptions.SFXVolume;
            musicVolumeSlider.value = GameManager.INS.AudioOptions.MusicVolume;
        }

        public void UpdateMasterVolume(float newValue) => GameManager.INS.AudioOptions.MasterVolume = newValue;
        public void UpdateSFXVolume(float newValue) => GameManager.INS.AudioOptions.SFXVolume = newValue;
        public void UpdateMusicVolume(float newValue) => GameManager.INS.AudioOptions.MusicVolume = newValue;

        public void ToggleTab(bool toggle)
        {
            MyRectTransform.localScale = toggle ? Vector3.one : Vector3.zero;
        }
    }
}
