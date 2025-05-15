//using Assets.Scripts.Networking;
//using System;
//using UnityEngine;

//public class MorsePlayMessage : ProcessedMessage<MorsePlayMessage>
//{
//    public long ReferenceId;
//    public bool IsBoosted;
//    public byte PlayCounter;

//    public MorsePlayMessage() { }

//    public MorsePlayMessage(long referenceId, bool isBoosted, byte playCounter)
//    {
//        ReferenceId = referenceId;
//        IsBoosted = isBoosted;
//        PlayCounter = playCounter;
//    }

//    public override void Deserialize(RocketBinaryReader reader)
//    {
//        ReferenceId = reader.ReadInt64();
//        IsBoosted = reader.ReadBoolean();
//        PlayCounter = reader.ReadByte();
//    }

//    public override void Serialize(RocketBinaryWriter writer)
//    {
//        writer.WriteInt64(ReferenceId);
//        writer.WriteBoolean(IsBoosted);
//        writer.WriteByte(PlayCounter);
//    }

//    public override void Process(long hostId)
//    {
//        // Server should re-broadcast to other clients
//        if (NetworkManager.IsServer)
//        {
//            this.SendToClients(); // send to all clients except sender
//        }

//        // On clients or dedicated server with audio output
//        //foreach (var radio in BrainClock.PlayerComms.Radio.AllRadios)
//        //{
//        //    if (radio.ReferenceId == this.ReferenceId)
//        //    {
//        //        radio.PlayMorseFromNetwork(IsBoosted, PlayCounter);
//        //        break;
//        //    }
//        //}
//    }
//}
