using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HLProject
{
    struct AudioSourceHandler
    {
        public AudioSource ASrc { get; private set; }
        public float MaxVolume { get; private set; }

        public AudioSourceHandler(AudioSource _aSrc)
        {
            ASrc = _aSrc;

            if (ASrc == null)
            {
                MaxVolume = -1;
                return;
            }

            MaxVolume = ASrc.volume;
        }
    }

    public class AudioManager : MonoBehaviour
    {
        [System.Flags]
        public enum AudioSourceTarget { LocalPlayer = 0b1, CurrentWeapon = 0b10, Other = 0b100 }

        public static AudioManager INS;

        List<AudioSourceHandler> localPlayerSources, otherAudioSources, currentWeaponSources;

        float _MasterVolume = 1;

        float _GlobalVolume = 1;
        public float GlobalVolume
        {
            get { return _GlobalVolume; }
            set { _GlobalVolume = Mathf.Clamp01(value * _MasterVolume); SyncVolumeValue(AudioSourceTarget.LocalPlayer | AudioSourceTarget.CurrentWeapon | AudioSourceTarget.Other); }
        }

        float _LocalPlayerVolume = 1;
        public float LocalPlayerVolume
        {
            get { return _LocalPlayerVolume; }
            set { _LocalPlayerVolume = Mathf.Clamp01(value); SyncVolumeValue(AudioSourceTarget.LocalPlayer); }
        }

        float _CurrentWeaponVolume = 1;
        public float CurrentWeaponVolume
        {
            get { return _CurrentWeaponVolume; }
            set { _CurrentWeaponVolume = Mathf.Clamp01(value); SyncVolumeValue(AudioSourceTarget.CurrentWeapon); }
        }

        float _OtherVolume = 1;
        public float OtherVolume
        {
            get { return _OtherVolume; }
            set { _OtherVolume = Mathf.Clamp01(value); SyncVolumeValue(AudioSourceTarget.Other); }
        }

        void Awake()
        {
            if (INS != null) 
            {
                Destroy(gameObject);
                return;
            }

            localPlayerSources = new List<AudioSourceHandler>();
            currentWeaponSources = new List<AudioSourceHandler>();
            otherAudioSources = new List<AudioSourceHandler>();

            _MasterVolume = GameManager.INS.AudioOptions.MasterVolume;
            GlobalVolume = GameManager.INS.AudioOptions.SFXVolume;

            INS = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterAudioSource(AudioSource aSrc, AudioSourceTarget target)
        {
            if (aSrc == null) return;
            AudioSourceHandler handler = new AudioSourceHandler(aSrc);

            (target switch
            {
                AudioSourceTarget.LocalPlayer => localPlayerSources,
                AudioSourceTarget.CurrentWeapon => currentWeaponSources,
                _ => otherAudioSources,
            }).Add(handler);

            aSrc.volume = handler.MaxVolume * GlobalVolume;
        }

        public void UnRegisterAudioSource(AudioSource aSrc, AudioSourceTarget target)
        {
            List<AudioSourceHandler> targetList = (target switch
            {
                AudioSourceTarget.LocalPlayer => localPlayerSources,
                AudioSourceTarget.CurrentWeapon => otherAudioSources,
                _ => currentWeaponSources,
            });

            targetList.Remove(targetList.Find((handler) => handler.ASrc == aSrc));
        }

        public void SyncVolumeValue(AudioSourceTarget targets)
        {
            if ((targets & AudioSourceTarget.LocalPlayer) == AudioSourceTarget.LocalPlayer)
            {
                int size = localPlayerSources.Count;
                for (int i = 0; i < size; i++)
                {
                    if (localPlayerSources[i].ASrc == null) continue;
                    localPlayerSources[i].ASrc.volume = localPlayerSources[i].MaxVolume * LocalPlayerVolume * GlobalVolume;
                    //Debug.LogFormat("LocalPlayer ASrc {0}: {1}", localPlayerSources[i].ASrc.name, localPlayerSources[i].ASrc.volume);
                }
            }

            if ((targets & AudioSourceTarget.Other) == AudioSourceTarget.Other)
            {
                int size = otherAudioSources.Count;
                for (int i = 0; i < size; i++)
                {
                    if (otherAudioSources[i].ASrc == null) continue;
                    otherAudioSources[i].ASrc.volume = otherAudioSources[i].MaxVolume * OtherVolume * GlobalVolume;
                    //Debug.LogFormat("Other ASrc {0}: {1}", otherAudioSources[i].ASrc.name, otherAudioSources[i].ASrc.volume);
                }
            }

            if ((targets & AudioSourceTarget.CurrentWeapon) == AudioSourceTarget.CurrentWeapon)
            {
                int size = currentWeaponSources.Count;
                for (int i = 0; i < size; i++)
                {
                    if (currentWeaponSources[i].ASrc == null) continue;
                    currentWeaponSources[i].ASrc.volume = currentWeaponSources[i].MaxVolume * CurrentWeaponVolume * GlobalVolume;
                    //Debug.LogFormat("CurrentWeapon ASrc {0}: {1}", currentWeaponSources[i].ASrc.name, currentWeaponSources[i].ASrc.volume);
                }
            }
        }

        public void UpdateMasterVolume(float newValue)
        {
            _MasterVolume = newValue;
        }

        public void UpdateSFXVolume(float newValue)
        {
            GlobalVolume = newValue;
        }
    }
}
