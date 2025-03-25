using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UI.ConfirmationPanel;

namespace BrainClock.PlayerComms
{
    public class VoiceProcessingTest : MonoBehaviour, IAudioStreamReceiver
    {
        // Start is called before the first frame update
        public GameObject AudioReceiver;

        public int volume = 1;

        public bool IsReady = false;

        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            foreach(var stream in AudioReceiver.GetComponents<IAudioStreamReceiver>())
            {
                stream.ReceiveAudioStreamData(data, length);
            }
        }

        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
            Debug.Log("Not implemented");
        }
    }
}
