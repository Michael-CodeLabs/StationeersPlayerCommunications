using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Util;
using RootMotion;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// We are only using the Singleton of the ManagerBase
    /// </summary>
    public class PlayerCommunicationsManager : MonoBehaviour, IAudioStreamReceiver
    {
        public static PlayerCommunicationsManager Instance { get; private set; }

        private IAudioDataReceiver[] audioDataReceivers;

        private bool InGame= false;

        private void Awake()
        {
            // If there is an instance, and it's not me, delete myself.

            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("PlayerCommunicationsManager.Start()");

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit += HandleWorldExit;

            audioDataReceivers = gameObject.GetComponents<IAudioDataReceiver>();
        }

        // Called when the world starts
        private void HandleWorldStarted()
        {
            Console.WriteLine("World has started.. Setting up Voice capture");

            Debug.Log("PlayerCommunicationsManager.HandleWorldStarted() Checking steam...");

            InGame = true;
        }

        // Called when the world exits
        private void HandleWorldExit()
        {
            Console.WriteLine("World is exiting.. Stopping Voice capture");
        }

        private void OnDestroy()
        {
            Debug.Log("PlayerCommunicationsManager.OnDestroy()");
            HandleWorldExit();
            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit -= HandleWorldExit;
        }

        // Connects voice input with voice output controllers
        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            if (!InGame)
                return; 

            foreach (IAudioDataReceiver audioDataReceiver in audioDataReceivers)
            {
                audioDataReceiver.ReceiveAudioData(-1, data, length, 1, 0);
            }
        }

        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
            if (!InGame)
                return;

            throw new NotImplementedException();
        }
    }

}
