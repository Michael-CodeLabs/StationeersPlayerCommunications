    using System;
using System.Collections.Generic;
using System.Linq;  // Ensure LINQ is available
using System.Reflection;
using UnityEngine.Networking;
using UnityEngine;


namespace BrainClock.PlayerComms
{

    public static class MessageFactoryInjector
    {
        public static void InjectCustomMessageType(Type customMessageType)
        {
            // Get the IndexToMessageType array
            FieldInfo indexToMessageTypeField = typeof(MessageFactory).GetField("IndexToMessageType",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (indexToMessageTypeField == null)
                throw new InvalidOperationException("Failed to find IndexToMessageType field in MessageFactory.");

            Type[] originalArray = (Type[])indexToMessageTypeField.GetValue(null);

            // Get the MessageTypeToIndex dictionary
            FieldInfo messageTypeToIndexField = typeof(MessageFactory).GetField("MessageTypeToIndex",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (messageTypeToIndexField == null)
                throw new InvalidOperationException("Failed to find MessageTypeToIndex field in MessageFactory.");

            Dictionary<Type, byte> messageTypeToIndex = (Dictionary<Type, byte>)messageTypeToIndexField.GetValue(null);

            // Convert the ValueCollection to an IEnumerable<byte> and find the max index
            byte nextIndex = (byte)(messageTypeToIndex.Values.DefaultIfEmpty((byte)0).Max() + 1);

            // Extend array if needed
            if (nextIndex >= originalArray.Length)
            {
                int newSize = nextIndex + 1; // Make array just big enough
                Type[] newArray = new Type[newSize];

                Array.Copy(originalArray, newArray, originalArray.Length);
                indexToMessageTypeField.SetValue(null, newArray);

                Debug.Log($"Extended MessageFactory IndexToMessageType array to {newSize} elements.");
            }

            // Inject the custom type into the array
            Type[] updatedArray = (Type[])indexToMessageTypeField.GetValue(null);
            updatedArray[nextIndex] = customMessageType;

            Debug.Log($"Injected {customMessageType.Name} at index {nextIndex} in IndexToMessageType.");

            // Ensure the dictionary has space for a new byte index
            if (nextIndex > 255)
                throw new InvalidOperationException("Index exceeds byte limit (max 255).");

            // Inject custom message type into dictionary
            messageTypeToIndex[customMessageType] = nextIndex;
            Debug.Log($"Mapped {customMessageType.Name} to index {nextIndex} in MessageTypeToIndex.");
        }
    }

}
