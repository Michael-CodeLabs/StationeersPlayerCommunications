using Assets.Scripts.Sound;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class VoiceDataToAudioClip : MonoBehaviour
    {
        public AudioSource audioSource;

        private MemoryStream uncompressedStream;
        private MemoryStream compressedStream;

        private float[] audioclipBuffer;
        private int audioclipBufferSize;

        private int playbackBuffer;
        private int dataPosition;
        private int dataReceived;

        public bool IsReady = false;

        // Start is called before the first frame update
        void Start()
        {
            uncompressedStream = new MemoryStream();
            compressedStream = new MemoryStream();

            int optimalRate = (int)SteamUser.OptimalSampleRate;

            audioclipBufferSize = optimalRate * 5;
            audioclipBuffer = new float[audioclipBufferSize];

            // Here optimalRate * 2 seems to be what fixes the playback issues
            audioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("External"));

            audioSource.clip = AudioClip.Create("HumanVoiceData", (int)optimalRate * 2, 1, (int)optimalRate, true, OnAudioRead, null);
            audioSource.loop = true;
            audioSource.Play();

            // Attach to the right Audio mixer
            // Too Early to do it here for now, 
            //audioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("Interface"));
            Debug.Log($"VoiceDataToAudioClip Started.");

            IsReady = true;
        }

        public void SendVoiceRecording(byte[] compressed, int length)
        {
            // Run once
            if (audioSource == null || !IsReady)
                return;

            Debug.Log($"VoiceDataToAudioClip Received {length} bytes");
            compressedStream.Position = 0;
            compressedStream.Write(compressed, 0, length);
            compressedStream.Position = 0;

            uncompressedStream.Position = 0;
            int uncompressedWritten = SteamUser.DecompressVoice(compressedStream, length, uncompressedStream);
            // Would be nice to use the byte[] variant here, but it doesn't seem to work, it generates no audio

            byte[] outputBuffer = uncompressedStream.GetBuffer();
            WriteToClip(outputBuffer, uncompressedWritten);
        }

        private void WriteToClip(byte[] uncompressed, int iSize)
        {
            for (int i = 0; i < iSize; i += 2)
            {
                // insert converted float to buffer
                float converted = (short)(uncompressed[i] | uncompressed[i + 1] << 8) / 32767.0f;
                audioclipBuffer[dataReceived] = converted;

                // buffer loop
                dataReceived = (dataReceived + 1) % audioclipBufferSize;

                playbackBuffer++;
            }
        }

        private void OnAudioRead(float[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                // start with silence
                data[i] = 0;

                // do I  have anything to play?
                if (playbackBuffer > 0)
                {
                    // current data position playing
                    dataPosition = (dataPosition + 1) % audioclipBufferSize;

                    data[i] = audioclipBuffer[dataPosition];

                    playbackBuffer--;
                }
            }

        }


        private void OnDestroy()
        {
            // If we are registered to the voicedatamanager, remove ourselfs from the list 
            VoiceDataManager.Instance?.RemoveHumanAudioSource(this);
        }

    }
}
