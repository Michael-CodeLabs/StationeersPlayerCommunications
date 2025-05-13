//using Assets.Scripts.Networking;
//using Objects.Structures;
//using UnityEngine;

//namespace BrainClock.PlayerComms
//{
//    public class MorsePlayMessage : ProcessedMessage<MorsePlayMessage>
//    {
//        public long ReferenceId { get; set; }
//        public bool IsBoosted { get; set; }

//        public MorsePlayMessage() { }

//        public MorsePlayMessage(long referenceId, bool isBoosted)
//        {
//            this.ReferenceId = referenceId;
//            this.IsBoosted = isBoosted;
//        }

//        public override void Serialize(RocketBinaryWriter writer)
//        {
//            writer.WriteInt64(ReferenceId);
//            writer.WriteBoolean(IsBoosted);
//        }

//        public override void Deserialize(RocketBinaryReader reader)
//        {
//            ReferenceId = reader.ReadInt64();
//            IsBoosted = reader.ReadBoolean();
//        }

//        public override void Process(long hostId)
//        {
//            base.Process(hostId);

//            // Skip execution if we're the host (only for clients)
//            if (NetworkManager.IsServer)
//                return;

//            foreach (Radio radio in Radio.AllRadios)
//            {
//                if (radio.ReferenceId == ReferenceId)
//                {
//                    radio._morseCode.PlayMorse(IsBoosted);
//                    break;
//                }
//            }
//        }
//    }
//}
