using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public interface IAudioStreamReceiver
    {
        void ReceiveAudioStreamData(byte[] data, int length);

        void ReceiveAudioStreamData(MemoryStream stream, int length);
    }
}
