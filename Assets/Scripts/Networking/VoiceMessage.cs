using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using BrainClock.PlayerComms;
using System;
using UnityEngine;
using UnityEngine.Networking;
using Util.Commands;

namespace Assets.Scripts.Networking
{
    public class VoiceMessage : ProcessedMessage<VoiceMessage>
    {
        public long HumanId { get; set; }

        public int Length { get; set; }

        public byte[] Message { get; set; }

        public float VolumeMultiplier { get; set; }

        public bool HasHelmet { get; set; }

        public VoiceMessage() { }

        public VoiceMessage(long referenceId, byte[] Message, int Length, float VolumeMultiplier, bool HasHelmet) {
            this.HumanId = referenceId;
            this.Length = Length;
            this.Message = Message;
            this.VolumeMultiplier = VolumeMultiplier;
            this.HasHelmet = HasHelmet;
        }

        public override void Process(long hostId)
        {
            base.Process(hostId);
            Debug.Log($"VoiceMessage.Process(hostId {hostId})");
            this.PrintDebug();


            if (NetworkManager.IsServer)
            {
                Debug.Log("+ this is the Server recieving voice from client");
                Debug.Log("+ this is the Server sending voice to clients");
                this.SendToClients();
                /*
                foreach (Human human in Human.AllHumans)
                {
                    if (human.ReferenceId == HumanId)
                    {
                        Debug.Log($"* Human {human.name} {human.CustomName} {human.ReferenceId}");
                        Debug.Log($"* helmet closed {(human.HasInternals && human.InternalsOn)}");
                        Debug.Log($"* breathing world {(human.WorldAtmosphere == human.BreathingAtmosphere)}");
                    }
                }
                */

                if (Application.platform != RuntimePlatform.WindowsServer)
                {
                    Debug.Log("+ Message bytes sent to playback (Not Dedicated Server)");
                    //VoicePlayback.Instance.SendVoiceRecording(Message, Length);
                    //VoiceDataManager.Instance.SendVoiceRecording(HumanId, Message, Length);
                    if (PlayerCommunicationsManager.Instance?.networkStreamReceiver != null)
                        PlayerCommunicationsManager.Instance?.networkStreamReceiver.ReceiveVoiceRecording(HumanId, Message, Length, VolumeMultiplier, HasHelmet);
                }
            }
            else
            {
                if (NetworkManager.IsClient)
                {
                    Debug.Log("+ this is the Client recieving voice from server");
                    if (HumanId == InventoryManager.ParentHuman.ReferenceId && PlayerCommunicationsManager.Instance?.networkStreamReceiver?.ReceiveOwnAudio != true)
                    {
                        Debug.Log("+ Ignoring own VoiceMessage");
                    }
                    else
                    {
                        Debug.Log("+ Message bytes sent to playback");
                        //VoicePlayback.Instance.SendVoiceRecording(Message, Length);
                        //VoiceDataManager.Instance.SendVoiceRecording(HumanId, Message, Length);
                        if (PlayerCommunicationsManager.Instance?.networkStreamReceiver != null)
                            PlayerCommunicationsManager.Instance.networkStreamReceiver.ReceiveVoiceRecording(HumanId, Message, Length, VolumeMultiplier, HasHelmet);
                    }
                }
                else
                {
                    Debug.Log("+ I'm not an this is the Client recieving voice from server");
                }
            }
        }

        public override void Deserialize(RocketBinaryReader reader)
        {
            this.HumanId = reader.ReadInt64();
            this.VolumeMultiplier = reader.ReadFloatHalf();
            this.HasHelmet = reader.ReadBoolean();
            this.Length = reader.ReadInt32();
            this.Message = reader.ReadBytes(Length);
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            writer.WriteInt64(this.HumanId);
            writer.WriteFloatHalf(this.VolumeMultiplier);
            writer.WriteBoolean(this.HasHelmet);
            writer.WriteInt32(this.Length);
            writer.WriteBytes(this.Message);
        }

        public void PrintDebug()
        {
            Debug.Log($"VoiceMessage.Debug - VoiceMessage from HumanId {this.HumanId} of Length {this.Message.Length}");
        }
    }
}