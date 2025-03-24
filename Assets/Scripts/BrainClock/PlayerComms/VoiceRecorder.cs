using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;

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

        public int ChunkSize = 512;

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
                    Debug.Log("SteamAPI initialized");
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

            // Send to playback for testing
            if (playBack != null)
                playBack.Initialize();


            // Enable recording?
            if (EnableOnStart)
                SteamUser.VoiceRecord = true;

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
                {
                    int offset = 0;
                    while (offset < compressedRead)
                    {
                        int bytesToSend = Math.Min(ChunkSize, compressedRead - offset);
                        byte[] chunk = new byte[bytesToSend];
                        Array.Copy(bytes.Array, offset, chunk, 0, bytesToSend);

                        OnVoiceRecording(chunk, bytesToSend);
                        offset += bytesToSend;
                    }

                }

            }
            else
            {
                Debug.Log("No voice data");
            }
        }

        public void OnVoiceRecording(byte[] data, int Length)
        {
            Debug.Log($"Creating a VoiceMessage of {Length} bytes");
            VoiceMessage voiceMessage = new VoiceMessage();
            voiceMessage.HumanId = InventoryManager.ParentHuman.ReferenceId;
            voiceMessage.Length = Length;
            voiceMessage.Message = data;
            //NetworkClient.SendToServer<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic);
            if (NetworkManager.IsClient)
            {
                Debug.Log("Client sending message to server");
                voiceMessage.SendToServer();
            }
            else {
                if (!NetworkManager.IsServer)
                    return;
                Debug.Log("Server sending message to clients");
                voiceMessage.SendToClients();
                //NetworkServer.SendToClients<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic, -1L);
            }
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
