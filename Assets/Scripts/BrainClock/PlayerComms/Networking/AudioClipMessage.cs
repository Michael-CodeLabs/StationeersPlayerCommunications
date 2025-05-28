using Assets.Scripts.Networking;
using System;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class AudioClipMessage : ProcessedMessage<AudioClipMessage>
    {
        public long referenceId { get; set; }
        public int Length { get; set; }
        public byte[] Message { get; set; }
        public float Volume { get; set; }
        public int Flags { get; set; }

        [Flags]
        public enum AudioFlags
        {
            None = 0,
            VoiceWhisper = 1 << 0,
            VoiceNormal = 1 << 1,
            VoiceShout = 1 << 2,
        }

        public AudioClipMessage() { }

        public AudioClipMessage(long referenceId, byte[] Message, int Length, float Volume, int Flags)
        {
            this.referenceId = referenceId;
            this.Length = Length;
            this.Message = Message;
            this.Volume = Volume;
            this.Flags = Flags;
        }

        public override void Deserialize(RocketBinaryReader reader)
        {
            this.referenceId = reader.ReadInt64();
            this.Length = reader.ReadInt32();
            this.Message = reader.ReadBytes(Length);
            this.Volume = reader.ReadFloatHalf();
            this.Flags = reader.ReadInt32();
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            writer.WriteInt64(this.referenceId);
            writer.WriteInt32(this.Length);
            writer.WriteBytes(this.Message);
            writer.WriteFloatHalf(this.Volume);
            writer.WriteInt32(this.Flags);
        }

        public override void Process(long hostId)
        {
            base.Process(hostId);
            Debug.Log($"AudioClipMessage.Process(hostId={hostId}, referenceId={referenceId}, Flags={Flags})");

            if (NetworkManager.IsServer)
            {
                Debug.Log("Server received AudioClipMessage, forwarding to clients");
                this.SendToClients();
                SendAudioDataToManager(); // Process locally regardless of platform for testing
            }
            else
            {
                Debug.Log("Client received AudioClipMessage, sending to Audio Manager");
                SendAudioDataToManager();
            }
        }

        private void SendAudioDataToManager()
        {
            Debug.Log($"Sending audio to manager: referenceId={referenceId}, Length={Length}, Volume={Volume}, Flags={Flags}");
            if (PlayerCommunicationsManager.Instance)
            {
                foreach (IAudioDataReceiver receiver in PlayerCommunicationsManager.Instance.GetComponents<IAudioDataReceiver>())
                {
                    Debug.Log($"Forwarding to receiver: {receiver.GetType().Name}");
                    receiver.ReceiveAudioData(referenceId, Message, Length, Volume, Flags);
                }
            }
            else
            {
                Debug.LogError("PlayerCommunicationsManager.Instance is null");
            }
        }

        public void PrintDebug()
        {
            Debug.Log($"AudioClipMessage.Debug - Id: {this.referenceId} {this.Message.Length} {this.Flags}");
        }
    }
}