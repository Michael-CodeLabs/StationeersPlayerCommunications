using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;

// Some code from https://github.com/Facepunch/Facepunch.Steamworks/issues/261

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Class responsible from enabling Steam recording and managing the voice data
    /// </summary>
    public class VoiceRecorder : MonoBehaviour
    {

        // Stream to store the audio data
        private MemoryStream voiceStream;

        // Stationeers AppId
        private uint AppId = 544550U;

        public bool EnableOnStart = true;

        public bool InitializeSteam = false;

        [SerializeField]
        private bool _WasRecording;

        // Is SteamClient ready
        public bool IsReady
        {
            get
            {
                return SteamClient.IsValid;
            }
        }

        // Is Steam currently recording
        public bool IsRecording
        {
            get
            {
                return SteamUser.VoiceRecord;
            }
        }

        public bool VoiceRecord
        {
            get
            {
                return SteamUser.VoiceRecord;
            }
            set
            {
                SteamUser.VoiceRecord = value;
            }
        }


        [Header("Testing")]
        public VoicePlayback playBack;


        // Start is called before the first frame update
        void Start()
        {
            if (!SteamClient.IsValid)
            {
                if (InitializeSteam)
                {
                    // Make sure Steam is intialized to operate Asynchronously
                    SteamClient.Init(AppId, true);
                }
                else
                {
                    Debug.Log("Steam has not been initialized.");
                    return;
                }
            }

            // Save previous recording state
            _WasRecording = SteamUser.VoiceRecord;

            voiceStream = new MemoryStream();

            // Enable recording?
            if (EnableOnStart)
                SteamUser.VoiceRecord = true;

            // Adding custom message type
            MessageFactoryInjector.InjectCustomMessageType(typeof(VoiceMessage));

            Debug.Log($"VoiceRecorder.Start({SteamUser.VoiceRecord})");
        }

        // Client call 
        void FixedUpdate()
        {
            if (!SteamClient.IsValid)
                return;

            if (SteamUser.HasVoiceData)
            {
                // Read as a compressed stream
                int compressedRead = SteamUser.ReadVoiceData(voiceStream);

                // convert to bytes and reset the buffer
                voiceStream.Position = 0;
                var bytes = new System.ArraySegment<byte>(voiceStream.GetBuffer(), 0, compressedRead);
                Debug.Log($"Captured {bytes.Count} bytes from audio voice");

                // Send to playback for testing
                if (playBack != null)
                    playBack.SendVoiceRecording(bytes.Array, compressedRead);

                // Send audio only if we have a human character spawned                
                if (NetworkManager.IsActive && InventoryManager.ParentHuman != null)
                    OnVoiceRecording(bytes.Array, compressedRead);

            }
            else
            {
                Debug.Log("No voice data");
            }
        }

        public void OnVoiceRecording(byte[] data, int Length)
        {
            VoiceMessage voiceMessage = new VoiceMessage();
            long referenceId = InventoryManager.ParentHuman.ReferenceId;
            voiceMessage.HumanId = InventoryManager.ParentHuman.ReferenceId;
            voiceMessage.Length = Length;
            voiceMessage.Message = data;
            if (NetworkManager.IsClient)
                NetworkClient.SendToServer<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic);
            else if (NetworkManager.IsServer)
            {
                voiceMessage.PrintDebug();
                NetworkServer.SendToClients<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic, -1L);
            }
            else
                voiceMessage.PrintDebug();
        }

        public void OnDestroy()
        {
            if (!SteamClient.IsValid)
                return;

            // Restore previous recording state
            SteamUser.VoiceRecord = _WasRecording;
        }
    }
}
