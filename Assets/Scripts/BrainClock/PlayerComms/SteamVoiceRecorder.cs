using Steamworks;
using System.IO;
using UnityEngine;
using static UI.ConfirmationPanel;

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
        private bool VoiceRecordEnabled = false;

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

        }

        public void Initialize(bool startVoiceCapture)
        {
            IsReady = false;

            // Do we have anyone to send data to?
            audioStreamReceivers = AudioReceiver.GetComponents<IAudioStreamReceiver>();
            if (audioStreamReceivers != null && audioStreamReceivers.Length > 0)
                IsReady = true;

            // Create a new Stream for voice capture
            voiceStream = new MemoryStream();

            VoiceRecordEnabled = startVoiceCapture;
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
