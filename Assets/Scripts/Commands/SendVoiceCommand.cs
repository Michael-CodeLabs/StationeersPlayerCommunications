//using Assets.Scripts.Inventory;
//using Assets.Scripts.Networking;
//using System.Collections;
//using System.Collections.Generic;
//using System.Security.Cryptography;
//using UnityEngine;
//using UnityEngine.UIElements;
//using Util.Commands;
//using static UI.ConfirmationPanel;

//namespace BrainClock.PlayerComms
//{

//    public class SendVoiceCommand : CommandBase
//    {
//        public override string HelpText
//        {
//            get
//            {
//                return "Attempts to send a command as client or server";
//            }
//        }

//        public override string[] Arguments { get; }

//        public override bool IsLaunchCmd
//        {
//            get
//            {
//                return false;
//            }
//        }

//        public override string Execute(string[] args)
//        {
//            string result = "Message not sent";

//            AudioClipMessage voiceMessage = new AudioClipMessage();

//            long referenceId = InventoryManager.ParentHuman.ReferenceId;
//            voiceMessage.referenceId = InventoryManager.ParentHuman.ReferenceId;
//            voiceMessage.Length = 4;
//            byte[] byteArray = { 10, 20, 30, 40 };
//            voiceMessage.Message = byteArray;
//            voiceMessage.Volume = 1;
//            voiceMessage.Flags = 0;

//            if (NetworkManager.IsClient)
//            {
//                Debug.Log("Client sending message to server");
//                result = "Sending Message to server";
//                voiceMessage.SendToServer();
//            }
//            else
//            {
//                if (!NetworkManager.IsServer)
//                    return "Can't send message, not connected";
//                Debug.Log("Server sending message to clients");
//                result = "Sending Message to clients";
//                voiceMessage.SendToClients();
//            }

//            return result;
//        }
//    }
//}
