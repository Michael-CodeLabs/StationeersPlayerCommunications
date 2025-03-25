using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;

namespace BrainClock.PlayerComms
{

    /// <summary>
    /// This class is responsible for replaying back live audio in the right 
    /// 3d location of a player.
    /// 
    /// It will receive audio that can come from any source (network, microphone, a file)
    /// and replay it correctly in the entity.
    /// 
    /// While we can still do some audio processing before feeding the actual audio data
    /// into the AudioClip, ideally we receive audio that has to be played as is (in theory
    /// audio that should not be played must not be received here).
    /// 
    /// - Do not operate if we are a dedicated server (we don't need to play audio)
    /// - Will Initialize on OnWorldStart.
    /// - Will Deinitialize on OnWorldExit.
    /// - Will spawn a custom audiosource prefab on OnHumanCreated.
    /// - When receiving audio, it will propagate the audio data to the right entity's audioReceiver.
    /// </summary>
    public class AudioClipInterfaceHuman : MonoBehaviour, IAudioDataReceiver
    {

        /// <summary>
        /// Prefab to spawn with every human, contains a StaticAudioSource and
        /// must implement IAudioDataReceiver too.
        /// </summary>
        public GameObject HumanAudioPrefab;

        /// <summary>
        /// List of know humans in the world
        /// </summary>
        private Dictionary<long, IAudioDataReceiver> HumanAudioDataReceivers = new Dictionary<long, IAudioDataReceiver>();

        /// <summary>
        /// Whether our own audio must be feed into our Human character.
        /// </summary>
        [Tooltip("If our audio needs to be feed into our character too (unknown sources too)")]
        public bool HearOwnAudio = false;

        /// <summary>
        /// For those cases where we should not run.
        /// </summary>
        private bool isReady = false;


        /// <summary>
        /// Autoinitializes all the hooks required 
        /// </summary>
        void Start()
        {
            Debug.Log("AudioClipInterfaceHuman.Start()");

            if (Application.platform == RuntimePlatform.WindowsServer)
                return;

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit    += HandleWorldExit;

            Human.OnHumanCreated += OnHumanCreated;

            isReady = true;
        }

        /// <summary>
        /// Spawns the Custom audio prefab parented to the human entity and saves a reference in the Dict
        /// </summary>
        /// <param name="entity"></param>
        private void OnHumanCreated(Assets.Scripts.Objects.Entity entity)
        {
            Debug.Log($"AudioClipInterfaceHuman.OnHumanCreated() adding entity: {entity.ReferenceId} for {entity.CustomName}");

            if (HumanAudioPrefab == null)
                return;

            /*
            GameObject newGameObject = UnityEngine.Object.Instantiate(HumanAudioPrefab, entity.transform);
            if (newGameObject == null)
            {
                Debug.Log("Object not instanced correctly");
                return;
            }

            Debug.Log($"AudioClipInterfaceHuman.OnHumanCreated() Prefab spawned!");
            IAudioDataReceiver audioDataReceiver = newGameObject.GetComponent<IAudioDataReceiver>();
            */

            IAudioDataReceiver audioDataReceiver = UnityEngine.Object.Instantiate(HumanAudioPrefab, entity.transform).GetComponent<IAudioDataReceiver>();
            HumanAudioDataReceivers.Add(entity.ReferenceId, audioDataReceiver);
            Debug.Log($"AudioClipInterfaceHuman.OnHumanCreated() Saved {entity.ReferenceId} {audioDataReceiver}");
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

            Debug.Log($"AudioClipInterfaceHuman.ReceiveAudioData()");

            // If we can hear our own audio, we need to give it our own Human referenceId
            if (InventoryManager.ParentHuman)
                referenceId = (referenceId < 1 && HearOwnAudio) ? InventoryManager.ParentHuman.ReferenceId : referenceId;

            if (referenceId < 1)
            {
                Debug.Log("Received Audio for unknown referenceId, ignoring");
                return;
            }

            //IAudioDataReceiver humanAudioReceiver = HumanAudioDataReceivers.GetValueOrDefault(referenceId);
            //if (humanAudioReceiver == null)
            IAudioDataReceiver humanAudioReceiver;
            if (!HumanAudioDataReceivers.TryGetValue(referenceId, out humanAudioReceiver))
            {
                Debug.Log($"No saved human audio receiver for {referenceId}, searching in Humans");
                Human theHuman = null;
                foreach (Human human in Human.AllHumans){
                    if (human.ReferenceId == referenceId)
                    {
                        Debug.Log("Found our human");
                        theHuman = human;
                    }
                }

                if (theHuman)
                {
                    OnHumanCreated(theHuman.AsEntity);
                    humanAudioReceiver = HumanAudioDataReceivers.GetValueOrDefault(referenceId);
                }
                else
                {
                    return;
                }
            }

            if (humanAudioReceiver == null)
                return;

            // Apply the human custom receiver
            humanAudioReceiver.ReceiveAudioData(referenceId, data, length, volume, flags);
        }


        /// <summary>
        /// Initialize all the human information every time we start a new world.
        /// </summary>
        private void HandleWorldStarted()
        {
            Console.WriteLine("AudioClipInterfaceHuman.HandleWOrldStart()");
            //HumanAudioDataReceivers = new Dictionary<long, IAudioDataReceiver>();
        }

        /// <summary>
        /// Clean up after leaving a world.
        /// </summary>
        private void HandleWorldExit()
        {
            Console.WriteLine("AudioClipInterfaceHuman.HandleWorldExit()");
            HumanAudioDataReceivers = new Dictionary<long, IAudioDataReceiver>(); 
        }

        /// <summary>
        /// Clean up if we are being shut down.
        /// </summary>
        private void OnDestroy()
        {
            Console.WriteLine("AudioClipInterfaceHuman.OnDestroy()");

            if (!isReady) 
                return;

            HandleWorldExit();
            
            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit    -= HandleWorldExit;

            Human.OnHumanCreated -= OnHumanCreated;
        }

    }
}
