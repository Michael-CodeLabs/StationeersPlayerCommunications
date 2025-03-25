using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// This class is responsible for sending live audio data through the network.
    /// 
    /// It will receive audio that can come from any source, and will send it through 
    /// the network if the audio source belongs to the current player (or doesn't have
    /// a reference Id).
    /// 
    /// This is ideally the right place to add the logic for when audio should be sent
    /// to reduce the data bandwith.
    /// 
    /// - Do not operate if we are a dedicated server (we don't need to play audio)
    /// - Will Initialize on OnWorldStart.
    /// - Will Deinitialize on OnWorldExit.
    /// - When receiving audio without a referenceId, it will send through the network 
    ///   as audio belonging to the player referenceId.
    /// </summary>
    public class AudioClipIntefaceNetwork : MonoBehaviour, IAudioDataReceiver
    {
        [Header("Network Packet")]
        [Tooltip("Minimum buffer size before sending the capture audio stream")]
        public int MinimumMessageSize = 1;

        [Tooltip("Maximum buffer size of audio capture to transmit")]
        public int MaximumMessageSize = 1000;

        /// <summary>
        /// For those cases where we should not run.
        /// </summary>
        private bool isReady = false;

        /// <summary>
        /// Byte queue for fast dequeing and sending data.
        /// </summary>
        private Queue<byte> _audioBuffer;

        private void Awake()
        {
            _audioBuffer = new Queue<byte>();
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("AudioClipIntefaceNetwork.Start()");

            // Note: We can remove this and feed the received data in the server as well, 
            // this will allow us to inject audio directly from a dedicated server
            // specially audio that doesn't not come from a player (e.g. a music device).
            // We can prevent loopback if we add extra referenceId checks.
            if (Application.platform == RuntimePlatform.WindowsServer)
                return;

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit += HandleWorldExit;

            isReady = false;         
        }

        /// <summary>
        /// Implementation of interface, use it to send our own audio data to the other peers.
        /// 
        /// - We will only process audio that doesn't have a referenceId. We will receive network audio
        ///   that we had previously sent and we don't want that to be processed again.
        /// </summary>
        /// <param name="referenceId"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <param name="volume"></param>
        /// <param name="flags"></param>
        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            if (!isReady)
                return;

            Debug.Log("AudioClipIntefaceNetwork.ReceiveAudioData()");

            if (!NetworkManager.IsActive)
                return;
            Debug.Log("AudioClipIntefaceNetwork.ReceiveAudioData() network is active");

            if (!InventoryManager.ParentHuman)
                return;
            Debug.Log("AudioClipIntefaceNetwork.ReceiveAudioData() parent human is around");

            long humanReferenceId = InventoryManager.ParentHuman.ReferenceId;

            // We can receive our own audio through the network, we don't want it to be sent again
            if (referenceId == humanReferenceId)
                return;

            // Ignore any audio that has a referenceId that is not ourselves, we don't manage.
            if (referenceId > 0 && referenceId != humanReferenceId)
                return;

            // Correctly associate the audio to our player, still allow 'unown sources' to go
            // through for when custom audio is implemented in the Dedicated Server.
            referenceId = (referenceId < 1) ? humanReferenceId : referenceId;

            // Enqueue new data into the buffer
            for (int i = 0; i < length; i++)
            {
                _audioBuffer.Enqueue(data[i]);
            }

            // Process audio into chunks for network-ready audio data.
            while (_audioBuffer.Count >= MinimumMessageSize)
            {
                int chunkSize = Math.Min(MaximumMessageSize, _audioBuffer.Count);
                byte[] chunk = new byte[chunkSize];

                for (int i = 0; i < chunkSize; i++)
                {
                    chunk[i] = _audioBuffer.Dequeue();
                }

                // We send the audio data split into different packages due to network data size limits.
                ProcessAndSendAudioStream(referenceId, chunk, chunk.Length, volume, flags);
            }
        }


        /// <summary>
        /// Creates and sends a VoiceMessage based on the game's network role.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        private void ProcessAndSendAudioStream(long referenceId, byte[] data, int length, float volume, int flags)
        {
            Debug.Log($"AudioClipInterfaceNetwork:ProcessAndSendAudioMessage of {length} bytes");

            // Create network message
            AudioClipMessage audioClipMessage = new AudioClipMessage(
                referenceId,
                data,
                length,
                volume,
                flags
            );

            // Send to one or Send to Many.
            if (NetworkManager.IsClient)
            {
                Debug.Log("AudioClipInterfaceNetwork:Client sending message to server");
                audioClipMessage.SendToServer();
            }
            if (NetworkManager.IsServer)
            {
                Debug.Log("AudioClipInterfaceNetwork:Server sending message to clients");
                audioClipMessage.SendToClients();
            }

            // Every other case ignored.
        }



        /// <summary>
        /// We need to check if we are in a networked game before we decide
        /// to process Audio.
        /// </summary>
        private void HandleWorldStarted()
        {
            Console.WriteLine("AudioClipIntefaceNetwork.HandleWOrldStart()");
            isReady = true;
        }

        /// <summary>
        /// Clean up after leaving a world.
        /// </summary>
        private void HandleWorldExit()
        {
            Console.WriteLine("AudioClipIntefaceNetwork.HandleWorldExit()");
            isReady = false;
            _audioBuffer = new Queue<byte>();
        }

        /// <summary>
        /// Clean up if we are being shut down.
        /// </summary>
        private void OnDestroy()
        {
            Console.WriteLine("AudioClipIntefaceNetwork.OnDestroy()");

            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit -= HandleWorldExit;
        }

    }
}
