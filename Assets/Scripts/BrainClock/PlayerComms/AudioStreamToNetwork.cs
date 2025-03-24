using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using static UI.ConfirmationPanel;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// This class receives Audio data as byte array or a stream and sends it through 
    /// the Network. Because this is supposed to be Human generated voice, it will
    /// automatically attach the Human.referenceId as source for the audio being sent.
    /// </summary>
    public class AudioStreamToNetwork : MonoBehaviour, IAudioStreamReceiver
    {
        private Queue<byte> _audioBuffer;

        [Tooltip("Minimum buffer size before sending the capture audio stream")]
        public int MinimumMessageSize = 1;

        [Tooltip("Maximum buffer size of audio capture to transmit")]
        public int MaximumMessageSize = int.MaxValue;

        // List of additional stream receivers to send the data to.
        private static List<IAudioStreamReceiver> LocalEmitters = new List<IAudioStreamReceiver>();

        /// <summary>
        /// Create a FIFO queue to host the Audio data 
        /// </summary>
        private void Awake()
        {
            _audioBuffer = new Queue<byte>();
        }

        // Implement interface;
        public void ReceiveAudioStreamData(MemoryStream stream, int length){}

        /// <summary>
        /// Will receive local recording audio, queue, process it and send it to the newtork
        /// if the player is ready and able to speak.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            if (data == null || length <= 0)
                return;

            if (!NetworkManager.IsActive || InventoryManager.ParentHuman == null)
                return;

            if (!InventoryManager.ParentHuman.isActiveAndEnabled)
                return;

            if (InventoryManager.ParentHuman.IsUnresponsive || InventoryManager.ParentHuman.IsSleeping)
                return;

            // Enqueue new data into the buffer
            for (int i = 0; i < length; i++)
            {
                _audioBuffer.Enqueue(data[i]);
            }

            // Process full chunks
            while (_audioBuffer.Count >= MinimumMessageSize)
            {
                int chunkSize = Math.Min(MaximumMessageSize, _audioBuffer.Count);
                byte[] chunk = new byte[chunkSize];

                for (int i = 0; i < chunkSize; i++)
                {
                    chunk[i] = _audioBuffer.Dequeue(); // Efficient removal
                }

                ProcessAndSendAudioStream(chunk, chunk.Length);
            }

            // Additional AudioStreamReceivers can subscribe to this
            foreach (IAudioStreamReceiver local in LocalEmitters)
            {
                local.ReceiveAudioStreamData(data, length);
            }
        }

        /// <summary>
        /// Creates and sends a VoiceMessage based on the game's network role.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        public void ProcessAndSendAudioStream(byte[] data, int length) {

            Debug.Log($"Creating a VoiceMessage of {length} bytes");

            // TODO: MOVE THIS COMPARISON OUTSIDE OF THIS to optimize this func.
            Human human = InventoryManager.ParentHuman.AsHuman;
            if (human != null)
            {
                if (human.HasInternals && human.InternalsOn) { }
                try
                {
                    Debug.Log($"* Human {human.name} {human.CustomName} {human.ReferenceId}");
                    Debug.Log($"* helmet closed {(human.HasInternals && human.InternalsOn)}");
                    Debug.Log($"* breathing world {(human.WorldAtmosphere == human.BreathingAtmosphere)}");
                }
                catch (Exception) { }
            }

            VoiceMessage voiceMessage = new VoiceMessage(
                InventoryManager.ParentHuman.ReferenceId,
                data,
                length,
                1, // Min(externalatmos, internalatmos) so it doesn't propagate over void.
                (human.HasInternals && human.InternalsOn)
            );
            

            if (NetworkManager.IsClient)
            {
                Debug.Log("Client sending message to server");
                voiceMessage.SendToServer();
                //NetworkClient.SendToServer<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic);
            }
            else
            {
                if (!NetworkManager.IsServer)
                    return;
                Debug.Log("Server sending message to clients");
                voiceMessage.SendToClients();
                //NetworkServer.SendToClients<VoiceMessage>((MessageBase<VoiceMessage>)voiceMessage, NetworkChannel.GeneralTraffic, -1L);
            }
        }
    }
}
