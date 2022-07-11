using UnityEngine;
using Mirror;
using Heliograph.Settings;
using Heliograph.Packets;

namespace Heliograph
{
    [AddComponentMenu("Audio/Heliograph/HelioRecorder")]
    public class HelioRecorder : NetworkBehaviour
    {
        [SerializeField] MicrophoneSettings settings;

        AudioClip clipToTransmit;
        int lastSampleOffset;
        HelioPlayer helioPlayer;

        public override void OnStartLocalPlayer()
        {
            //if (!isLocalPlayer) return;
            helioPlayer = GetComponent<HelioPlayer>();
            clipToTransmit = Microphone.Start(null, true, 10, MicrophoneSettings.Frequency);
        }

        void OnDisable()
        {
            if (!isLocalPlayer) return;
            Microphone.End(null);
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer) return;
            int currentMicSamplePosition = Microphone.GetPosition(null);
            int samplesToTransmit = GetSampleTransmissionCount(currentMicSamplePosition);

            if (samplesToTransmit > 0)
            {
                TransmitSamples(samplesToTransmit);
                lastSampleOffset = currentMicSamplePosition;
            }
        }

        private int GetSampleTransmissionCount(int currentMicrophoneSample)
        {
            int sampleTransmissionCount = currentMicrophoneSample - lastSampleOffset;
            if (sampleTransmissionCount < 0) sampleTransmissionCount = (clipToTransmit.samples - lastSampleOffset) + currentMicrophoneSample;
            return sampleTransmissionCount;
        }

        private void TransmitSamples(int sampleCountToTransmit)
        {
            float[] samplesToTransmit = new float[sampleCountToTransmit * clipToTransmit.channels];
            clipToTransmit.GetData(samplesToTransmit, lastSampleOffset);
            CmdSendAudio(new AudioPacket(samplesToTransmit));
        }

        [Command]
        public void CmdSendAudio(AudioPacket audio)
        {
            foreach (var connection in NetworkServer.connections)
            {
                if (connection.Value != connectionToClient)
                    RpcPlayAudio(connection.Value, audio);
            }
        }

        [TargetRpc]
        public void RpcPlayAudio(NetworkConnection target, AudioPacket audio) => helioPlayer.UpdateSoundSamples(audio);
    }
}
