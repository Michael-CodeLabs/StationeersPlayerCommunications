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
            ////Debug.log($"AudioClipMessage.Process(hostId {hostId})");
            //this.PrintDebug();

            // Note, on hosted sessions, we still have our own audio available
            if (NetworkManager.IsServer)
            {
                ////Debug.log("+ this is the Server recieving AudioClip from a client");
                ////Debug.log("+ re-sending AudioClip to clients now");
                //TODO send to all clients except the origin (to save bandwith).
                this.SendToClients();
                if (Application.platform != RuntimePlatform.WindowsServer)
                    SendAudioDataToManager();
            }

            // Note, we are getting audio from everyone, including ourselves
            if (NetworkManager.IsClient)
            {
                ////Debug.log("+ this is a client recieving AudioClip from the server");
                ////Debug.log("+ Sending AudioClip to the Audio Manager");
                SendAudioDataToManager();
            }

            // Ignore any other case
        }

        private void SendAudioDataToManager()
        {
            ////Debug.log("AudioClipMessage.SendAudioDataToManager");

            if (PlayerCommunicationsManager.Instance)
            {
                foreach (IAudioDataReceiver receiver in PlayerCommunicationsManager.Instance.GetComponents<IAudioDataReceiver>())
                {
                    receiver.ReceiveAudioData(referenceId, Message, Length, Volume, Flags);
                }

            }

        }


        public void PrintDebug()
        {
            //Debug.log($"AudioClipMessage.Debug - Id: {this.referenceId} {this.Message.Length} {this.Flags}");
        }
    }


}
