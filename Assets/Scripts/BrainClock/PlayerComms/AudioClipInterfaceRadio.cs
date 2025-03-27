using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using static Assets.Scripts.Networking.NetworkUpdateType.Thing;

namespace BrainClock.PlayerComms
{

    /// <summary>
    /// This class is responsible for replaying back live audio in the right 
    /// 3d location of a radio.
    /// 
    /// It will receive audio that can come from any source (network, microphone, a file)
    /// and replay it correctly in the entity.
    /// 
    /// Needs to keep track of all the radios and their channels. The radio will only receive the 
    /// audio data, volume etc needs to be handled by the radio.
    /// 
    /// - Do not operate if we are a dedicated server (we don't need to play audio)
    /// - Will Initialize on OnWorldStart.
    /// - Will Deinitialize on OnWorldExit.
    /// - Will track all Radios created/removed from the game.
    /// - When receiving audio, it will propagate the audio data to the right entity's audioReceiver.
    /// </summary>
    public class AudioClipInterfaceRadio : MonoBehaviour, IAudioDataReceiver
    {

        /// <summary>
        /// Prefab to spawn with every Radio, contains a StaticAudioSource and
        /// must implement IAudioDataReceiver too. Not needed if Radios include
        /// an specific audiosource for this.
        /// </summary>
        public GameObject RadioAudioPrefab;

        /// <summary>
        /// List of know Radios in the world
        /// </summary>
        private List<Radio> RadioThings = new List<Radio>();

        /// <summary>
        /// For those cases where we should not run.
        /// </summary>
        private bool isReady = false;

        /// <summary>
        /// Autoinitializes all the hooks required 
        /// </summary>
        void Start()
        {
            Debug.Log("AudioClipInterfaceRadio.Start()");

            if (Application.platform == RuntimePlatform.WindowsServer)
                return;

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit    += HandleWorldExit;

            Radio.OnRadioCreated += OnRadioCreated;
            Radio.OnRadioDestroyed += OnRadioDestroyed;

            isReady = true;
        }

        /// <summary>
        /// Tracks the current radio id saving a reference in the Dict
        /// </summary>
        /// <param name="entity"></param>
        private void OnRadioCreated(Radio radio)
        {
            if (RadioAudioPrefab != null)
            {
            }

            RadioThings.Add(radio);
            Debug.Log($"AudioClipInterfaceHuman.OnRadioCreated() Saved {radio}");
        }


        private void OnRadioDestroyed(Radio radio)
        {
            Debug.Log($"AudioClipInterfaceHuman.OnRadioDestroyed() removing {radio.ReferenceId}");
            RadioThings.Remove(radio);
        }

        /// <summary>
        /// Receives live audio data that needs to be sent to the clip.
        /// </summary>
        /// <param name="referenceId"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <param name="volume"></param>
        /// <param name="flags"></param>
        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            if (!isReady) 
                return;

            Debug.Log($"AudioClipInterfaceRadio.ReceiveAudioData() referenceId {referenceId}");

            // We are not accepting audio from no source, we need the source to find the radio being used.
            if (referenceId < 1)
            {
                if (InventoryManager.ParentHuman != null)
                    referenceId = InventoryManager.ParentHuman.ReferenceId;
                else
                {
                    Debug.Log("Can't find human referenceId, returning");
                    return;
                }
            }

            Debug.Log($"AudioClipInterfaceRadio.ReceiveAudioData() continuing with {referenceId}");

            foreach (Human human in Human.AllHumans)
            {
                if (human.ReferenceId == referenceId)
                {
                    if ((human.LeftHandSlot.Get()?.Activate < 1) && (human.RightHandSlot.Get()?.Activate < 1))
                    {
                        Debug.Log("Human doesn't have anything activated on any hands.");
                        return;
                    }
                }
            }

            /* This is US talking locally on a hosted session, there will no network traffic
            if (referenceId < 1)
            {
                Debug.Log("Received Audio for unknown referenceId, ignoring");
                return;
            }
            */

            foreach(Radio radio in RadioThings)
            {
                Debug.Log($"Radio {radio.ReferenceId}");

                IAudioDataReceiver receiver = radio as IAudioDataReceiver;
                Debug.Log($"Receiver {receiver}");
                receiver.ReceiveAudioData(-1, data, length, volume, flags);
            }

        }


        /// <summary>
        /// Initialize all the human information every time we start a new world.
        /// </summary>
        private void HandleWorldStarted()
        {
            Console.WriteLine("AudioClipInterfaceRadio.HandleWOrldStart()");
            //HumanAudioDataReceivers = new Dictionary<long, IAudioDataReceiver>();
            foreach(Radio radio in RadioThings)
            {
                try
                {
                    radio.SetupGameAudioSource();
                }
                catch (Exception e)
                {
                    Debug.Log("Exception running SetupGameAudioSource");
                }
            }

        }

        /// <summary>
        /// Clean up after leaving a world.
        /// </summary>
        private void HandleWorldExit()
        {
            Console.WriteLine("AudioClipInterfaceRadio.HandleWorldExit()");
            RadioThings = new List<Radio>(); 
        }

        /// <summary>
        /// Clean up if we are being shut down.
        /// </summary>
        private void OnDestroy()
        {
            Console.WriteLine("AudioClipInterfaceRadio.OnDestroy()");

            if (!isReady) 
                return;

            HandleWorldExit();
            
            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit    -= HandleWorldExit;

            Radio.OnRadioCreated -= OnRadioCreated;
            Radio.OnRadioDestroyed -= OnRadioDestroyed;
        }

    }
}
