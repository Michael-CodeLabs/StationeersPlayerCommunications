using Steamworks;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{

    /// <summary>
    /// Records audio from Steam.
    /// Expects SteamClient to be initialized.
    /// </summary>
    public class SteamVoiceRecorder : MonoBehaviour
    {
        public enum VoiceCaptureMode
        {
            FixedUpdate,
            OnDemand
        }

        // Stream to store the audio data
        [Tooltip("Component that will receive the audio stream when available.")]
        [SerializeField]
        public GameObject AudioReceiver;
        private IAudioStreamReceiver[] audioStreamReceivers;

        public bool IsReady = false;
        public bool VoiceRecordEnabled = true;

        [Tooltip("Steam AppId you want to initialize")]
        public uint AppId = 0;

        // Stream to store the audio data
        [Tooltip("Audio capture method")]
        public VoiceCaptureMode voiceCaptureMode;

        [Tooltip("Use this only for local testing")]
        public bool UseStream = false;

        private MemoryStream voiceStream;

        private void Awake()
        {
            // Initialize Voice stream
            voiceStream = new MemoryStream();
            if (AudioReceiver == null)
                AudioReceiver = this.gameObject;
        }

        // Start is called before the first frame update
        void Start()
        {
            // Try to initialize Steam if not done already.
            if (!SteamClient.IsValid)
                SteamClient.Init(AppId, true);

            if (!SteamClient.IsValid)
                return;
            Debug.Log("SteamVoiceRecorder: Steam is valid");

            IsReady = true;

            // Do we have anyone to send data to?
            audioStreamReceivers = AudioReceiver.GetComponents<IAudioStreamReceiver>();
            if (audioStreamReceivers != null && audioStreamReceivers.Length > 0)
                IsReady = true;
            
            Debug.Log("PlayerCommunicationsManager.HandleWorldStarted() is ready");

            // Create a new Stream for voice capture
            voiceStream = new MemoryStream();

            SteamUser.VoiceRecord = VoiceRecordEnabled;
        }


        public void Shutdown()
        {
            SteamUser.VoiceRecord = false;
            voiceStream = null;
        }


        private void FixedUpdate()
        {
            if (voiceCaptureMode == VoiceCaptureMode.FixedUpdate)
            {
                ProcessCapture();
            }
        }

        public void ProcessCapture()
        {
            if (!IsReady || !VoiceRecordEnabled || voiceStream == null)
                return;

            int compressedRead = SteamUser.ReadVoiceData(voiceStream);
            voiceStream.Position = 0;
            if (compressedRead > 0)
            {   
                // Stream can be used when network is not involved (local to local).
                if (UseStream)
                    SendToAllReceivers(voiceStream, compressedRead);
                else
                {
                    var bytes = new System.ArraySegment<byte>(voiceStream.GetBuffer(), 0, compressedRead);
                    SendToAllReceivers(bytes.Array, compressedRead);
                }

            }
        }

        private void SendToAllReceivers(MemoryStream stream, int length)
        {
            foreach(IAudioStreamReceiver receiver in audioStreamReceivers)
            {
                receiver.ReceiveAudioStreamData(stream, length);
            }

        }
        private void SendToAllReceivers(byte[] data, int length)
        {
            if (audioStreamReceivers != null)
            {
                foreach (IAudioStreamReceiver receiver in audioStreamReceivers)
                {
                    receiver.ReceiveAudioStreamData(data, length);
                }
            }
        }


    }
}
