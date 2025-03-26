using Assets.Scripts.Inventory;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Entities;
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
            Debug.Log($"AudioClipMessage.Process(hostId {hostId})");
            this.PrintDebug();

            // Note, on hosted sessions, we still have our own audio available
            if (NetworkManager.IsServer)
            {
                Debug.Log("+ this is the Server recieving AudioClip from a client");
                Debug.Log("+ re-sending AudioClip to clients now");
                //TODO send to all clients except the origin (to save bandwith).
                this.SendToClients();
                if (Application.platform != RuntimePlatform.WindowsServer)
                    SendAudioDataToManager();
            }

            // Note, we are getting audio from everyone, including ourselves
            if (NetworkManager.IsClient)
            {
                Debug.Log("+ this is a client recieving AudioClip from the server");
                Debug.Log("+ Sending AudioClip to the Audio Manager");
                SendAudioDataToManager();
            }

            // Ignore any other case
        }

        private void SendAudioDataToManager()
        {
            Debug.Log("AudioClipMessage.SendAudioDataToManager");

            if (PlayerCommunicationsManager.Instance) 
            {
                foreach(IAudioDataReceiver receiver in PlayerCommunicationsManager.Instance.GetComponents<IAudioDataReceiver>())
                {
                    receiver.ReceiveAudioData(referenceId, Message, Length, Volume, Flags);
                }

            }

        }


        public void PrintDebug()
        {
            Debug.Log($"AudioClipMessage.Debug - Id: {this.referenceId} {this.Message.Length} {this.Flags}");
        }
    }
}
