using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class NetworkToAudioStream : MonoBehaviour, INetworkStreamReceiver
    {
        public bool ReceiveOwnAudio = false;

        public void ReceiveVoiceRecording(long referenceId, byte[] Message, int Length, float VolumeMultiplier, bool HasHelmet)
        {
            Debug.Log($"ReceiveVoiceRecording in PlayerCommunicationsManager from {referenceId} with {Length} bytes ");


        }

    }
}
