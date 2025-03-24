using Assets.Scripts.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// We are only using the Singleton of the ManagerBase
    /// </summary>
    public class PlayerCommunicationsManager : Singleton<PlayerCommunicationsManager>
    {
        public SteamVoiceRecorder voiceRecorder;
        public List<GameObject> audioStreamReceiver;

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
