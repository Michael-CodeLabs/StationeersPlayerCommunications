using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// We are only using the Singleton of the ManagerBase
    /// </summary>
    public class PlayerCommunicationsManager : MonoBehaviour //: Singleton<PlayerCommunicationsManager>
    {
        public SteamVoiceRecorder voiceRecorder;
        public List<GameObject> audioStreamReceiver = new List<GameObject>();

        public bool CaptureOnWorldStart = true;

        // Stationeers AppId
        private uint AppId = 544550U;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("PlayerCommunicationsManager.Start()");
            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit += HandleWorldExit;
        }

        // Called when the world starts
        private void HandleWorldStarted()
        {
            Console.WriteLine("World has started.. Setting up Voice capture");

            if (voiceRecorder == null)
                return;

            Debug.Log("PlayerCommunicationsManager.HandleWorldStarted() Checking steam...");

            // Try to initialize Steam if not done already.
            if (!SteamClient.IsValid)
                SteamClient.Init(AppId, true);

            if (!SteamClient.IsValid)
                return;
            Debug.Log("PlayerCommunicationsManager.HandleWorldStarted() Steam is valid");

            voiceRecorder.enabled = true;

            voiceRecorder.Initialize(CaptureOnWorldStart);
            
        }

        // Called when the world exits
        private void HandleWorldExit()
        {
            Console.WriteLine("World is exiting.. Stopping Voice capture");
            if (voiceRecorder != null)
                voiceRecorder.Shutdown();

            voiceRecorder.enabled = false;
        }

        private void OnDestroy()
        {
            Debug.Log("PlayerCommunicationsManager.OnDestroy()");
            HandleWorldExit();
            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit -= HandleWorldExit;
        }
    }
}
