using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class AudioStreamToNetwork : MonoBehaviour, IAudioStreamReceiver
    {
        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
        }

        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            throw new System.NotImplementedException();
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
