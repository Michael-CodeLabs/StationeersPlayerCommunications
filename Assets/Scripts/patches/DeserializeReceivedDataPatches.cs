
using Assets.Scripts;
using Assets.Scripts.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace BrainClock.PlayerComms
{

    [HarmonyPatch(typeof(NetworkBase), "DeserializeReceivedData")]
    public static class DeserializeReceivedDataPatch
    {
        static bool Prefix(long hostId, RocketBinaryReader reader)
        {
            // Read the message type
            Type type = reader.ReadMessageType();

            // Get the "Singleton" property using reflection
            PropertyInfo property = type.GetProperty("Singleton", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (property == null)
            {
                throw new NullReferenceException($"Failed to find 'Singleton' on type {type}");
            }

            // Get the property's getter method and invoke it
            MethodInfo getMethod = property.GetGetMethod();
            object instance = getMethod?.Invoke(null, null);

            // Ensure the instance implements IMessageSerialisable
            if (instance is not IMessageSerialisable messageSerialisable)
            {
                throw new InvalidCastException($"Type {instance?.GetType()} could not be cast to IMessageSerialisable");
            }

            // Deserialize the message, handling any stream errors
            try
            {
                messageSerialisable.Deserialize(reader);
            }
            catch (EndOfStreamException ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Message: ({instance.GetType()}) {instance}");
            }

            if (!(messageSerialisable is IMessageProcessable messageProcessable))
                return false; // Skip original method if it's not a valid message

            bool isServer = NetworkManager.IsServer;

            // Original allowed message types
            HashSet<Type> allowedOnClient = new HashSet<Type>
            {
                typeof(NetworkMessages.Handshake),
                typeof(NetworkMessages.VerifyPlayerRequest),
                typeof(ChatMessage),
                typeof(AnimationEmoteMessage),
                typeof(MoveToSlotMessage),
                typeof(TradingResultMessageFromServer)
            };

            // Add extra allowed message types here
            //allowedOnClient.Add(typeof(VoiceMessage));  // Example custom message
            allowedOnClient.Add(typeof(AudioClipMessage));  // Example custom message
            //allowedOnClient.Add(typeof(MorsePlayMessage));
            if (!isServer && !allowedOnClient.Contains(messageSerialisable.GetType()))
            {
                ConsoleWindow.PrintError($"** Messages should only be processed on the server. Message: ({messageSerialisable.GetType()}) {messageSerialisable}", false);
                return false; // Skip original method
            }

            // Process the message
            messageProcessable.Process(hostId);

            return false; // Skip original method since we've replaced the logic
        }

    }
}
