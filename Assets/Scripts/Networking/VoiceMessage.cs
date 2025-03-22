using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts.Networking
{
    public class VoiceMessage : ProcessedMessage<VoiceMessage>
    {
        public long HumanId { get; set; }

        public int Length { get; set; }

        public byte[] Message { get; set; }

        public override void Process(long hostId)
        {
            this.PrintDebug();
            if (NetworkManager.IsServer)
                NetworkServer.SendToClients<VoiceMessage>((MessageBase<VoiceMessage>)this, NetworkChannel.GeneralTraffic, -1L);

            Human human = Assets.Scripts.Objects.Thing.Find<Human>(this.HumanId);
            if (!(bool)((UnityEngine.Object)human) || !(bool)((UnityEngine.Object)InventoryManager.Parent) || InventoryManager.Parent.ReferenceId == this.HumanId)
                return;

            // If not us, then send the audio?
            //human.SendAudioData(this audio data);
        }

        public override void Deserialize(RocketBinaryReader reader)
        {
            this.HumanId = reader.ReadInt64();
            this.Length = reader.ReadInt32();
            this.Message = reader.ReadBytes(Length);
        }

        public override void Serialize(RocketBinaryWriter writer)
        {
            writer.WriteInt64(this.HumanId);
            writer.WriteInt32(this.Length);
            writer.WriteBytes(this.Message);
        }

        public void PrintDebug()
        {
            Debug.Log($"from {this.HumanId}: {this.Message.Length}");
        }
    }
}