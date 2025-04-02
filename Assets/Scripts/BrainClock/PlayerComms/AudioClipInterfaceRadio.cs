using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using static Assets.Scripts.Networking.NetworkUpdateType.Thing;
using Assets.Scripts;
using Unity.Collections;

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
        /// Applies a volume multiplier to all audios sent to radios
        /// </summary>
        [Tooltip("Apply this volume multiplier to all clips sent to radio entities")]
        public float VolumeMultiplier = 1.0f;

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
            RadioThings.Add(radio);
        }


        private void OnRadioDestroyed(Radio radio)
        {
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

            //Debug.Log($"AudioClipInterfaceRadio.ReceiveAudioData() referenceId {referenceId}");

            // This is to prevent client's echo
            if (referenceId < 1 && NetworkManager.IsClient)
                return;

            // We are not accepting audio from no source, we need the source to find the radio being used.
            if (referenceId < 1)
            {
                if (InventoryManager.ParentHuman != null)
                    referenceId = InventoryManager.ParentHuman.ReferenceId;
                else
                {
                    //Debug.Log("Can't find human referenceId, returning");
                    return;
                }
            }

            //Debug.Log($"AudioClipInterfaceRadio.ReceiveAudioData() continuing with {referenceId}");
            bool send = false;
            int emittingChannel = -1;
            Transform emittingTransform = null;
            foreach (Human human in Human.AllHumans)
            {
                if (human.ReferenceId == referenceId)
                {
                    Radio lh = human.LeftHandSlot.Get() as Radio;
                    Radio rh = human.RightHandSlot.Get() as Radio;
                    if ((lh != null && lh.Activate > 0) || (rh != null && rh.Activate > 0))
                    {
                        if (lh != null)
                        {
                            if (lh.Activate > 0)
                            {
                                emittingChannel = lh.Channel;
                                emittingTransform = human.transform;
                            }
                        }
                        if (rh != null)
                        { 
                            if (rh.Activate > 0)
                            {
                                emittingChannel = rh.Channel;
                                emittingTransform = human.transform;
                            }
                        }

                        //Debug.Log($"SEND THIS Audio FROM referenceId {referenceId}");
                        send = true;
                    }
                }
            }

            if (!send)
            {
                //Debug.Log("No human detected with an active tool in hand");
                return;
            }

            /* This is US talking locally on a hosted session, there will no network traffic
            if (referenceId < 1)
            {
                Debug.Log("Received Audio for unknown referenceId, ignoring");
                return;
            }
            */

            foreach (Radio radio in RadioThings)
            {
                //Debug.Log($"Radio {radio.ReferenceId}");
                // Alternatively find channel through Radio.AllChannels
                if (radio.Channel == emittingChannel)
                {
                    IAudioDataReceiver receiver = radio as IAudioDataReceiver;
                    //Debug.Log($"Receiver {receiver}");
                    receiver.ReceiveAudioData(referenceId, data, length, volume * VolumeMultiplier, flags);
                }
            }

        }


        /// <summary>
        /// Initialize all the human information every time we start a new world.
        /// </summary>
        private void HandleWorldStarted()
        {
            //Console.WriteLine("AudioClipInterfaceRadio.HandleWOrldStart()");
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
            //Console.WriteLine("AudioClipInterfaceRadio.HandleWorldExit()");
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
