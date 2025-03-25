using Assets.Scripts.Objects.Entities;
using UnityEngine.UIElements;

namespace BrainClock.PlayerComms
{
    internal interface INetworkStreamReceiver
    {
        void ReceiveVoiceRecording(long referenceId, byte[] Message, int Length, float VolumeMultiplier, bool HasHelmet);
    }
}