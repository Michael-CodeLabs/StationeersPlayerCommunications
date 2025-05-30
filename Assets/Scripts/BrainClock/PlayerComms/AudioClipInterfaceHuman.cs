using Assets.Scripts;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Util.Commands;
using static BrainClock.PlayerComms.AudioClipMessage;

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
        /// Volume multiplier applied only to human entities.
        /// </summary>
        [Tooltip("Applies this volume multiplier to all audio data sent to the human audiosources")]
        public float VolumeMultiplier = 0.5f;


        /// <summary>
        /// For those cases where we should not run.
        /// </summary>
        private bool isReady = false;

        private void VolumeMultiplierSetter(object sender, EventArgs e)
        {
            VolumeMultiplier = StationeersPlayerCommunications.HumanVolumeMultiplier.Value;
            HumanAudioPrefab.GetComponent<AudioSource>().outputAudioMixerGroup.audioMixer.SetFloat("LocalPlayer", VolumeMultiplier);
            ConsoleWindow.Print($"Volume Multiplier set: {VolumeMultiplier}", ConsoleColor.Green);
        }

        private void Awake()
        {
            VolumeMultiplier = StationeersPlayerCommunications.RadioVolumeMultipler.Value;
        }
        /// <summary>
        /// Autoinitializes all the hooks required 
        /// </summary>
        void Start()
        {
            //Debug.log("AudioClipInterfaceHuman.Start()");

            if (Application.platform == RuntimePlatform.WindowsServer)
                return;

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit    += HandleWorldExit;

            StationeersPlayerCommunications.HumanVolumeMultiplier.SettingChanged += VolumeMultiplierSetter;
            Human.OnHumanCreated += OnHumanCreated;

            isReady = true;
        }

        /// <summary>
        /// Spawns the Custom audio prefab parented to the human entity and saves a reference in the Dict
        /// </summary>
        /// <param name="entity"></param>
        private void OnHumanCreated(Assets.Scripts.Objects.Entity entity)
        {
            ////Debug.log($"AudioClipInterfaceHuman.OnHumanCreated() adding entity: {entity.ReferenceId} for {entity.CustomName}");

            if (HumanAudioPrefab == null)
                return;

            /*
            GameObject newGameObject = UnityEngine.Object.Instantiate(HumanAudioPrefab, entity.transform);
            if (newGameObject == null)
            {
                //Debug.log("Object not instanced correctly");
                return;
            }

            //Debug.log($"AudioClipInterfaceHuman.OnHumanCreated() Prefab spawned!");
            IAudioDataReceiver audioDataReceiver = newGameObject.GetComponent<IAudioDataReceiver>();
            */

            IAudioDataReceiver audioDataReceiver = UnityEngine.Object.Instantiate(HumanAudioPrefab, entity.transform).GetComponent<IAudioDataReceiver>();
            HumanAudioDataReceivers.Add(entity.ReferenceId, audioDataReceiver);
            //Debug.log($"AudioClipInterfaceHuman.OnHumanCreated() Saved {entity.ReferenceId} {audioDataReceiver}");
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

            ////Debug.log($"AudioClipInterfaceHuman.ReceiveAudioData()");

            // If we can hear our own audio, we need to give it our own Human referenceId
            if (InventoryManager.ParentHuman)
            {
                if (referenceId == InventoryManager.ParentHuman.ReferenceId)
                {
                    ////Debug.log("Received Audio for unknown referenceId, ignoring");
                    return;
                }
                // Otherwise assign the audio as our 
                referenceId = (referenceId < 1 && HearOwnAudio) ? InventoryManager.ParentHuman.ReferenceId : referenceId;
            }

            if (referenceId < 1)
            {
                ////Debug.log("Received Audio for unknown referenceId, ignoring");
                return;
            }

            //IAudioDataReceiver humanAudioReceiver = HumanAudioDataReceivers.GetValueOrDefault(referenceId);
            //if (humanAudioReceiver == null)
            IAudioDataReceiver humanAudioReceiver;
            if (!HumanAudioDataReceivers.TryGetValue(referenceId, out humanAudioReceiver))
            {
               // //Debug.log($"No saved human audio receiver for {referenceId}, searching in Humans");
                Human theHuman = null;
                foreach (Human human in Human.AllHumans){
                    if (human.ReferenceId == referenceId)
                    {
                        ////Debug.log("Found our human");
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

            // Adjust maxDistance based on flags (voice mode)
            if (humanAudioReceiver is MonoBehaviour receiverMono)
            {
                var audioSource = receiverMono.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    var mode = (AudioFlags)flags;
                    switch (mode)
                    {
                        case AudioFlags.VoiceWhisper:
                            audioSource.maxDistance = 5f;
                            break;
                        case AudioFlags.VoiceNormal:
                            audioSource.maxDistance = 20f;
                            break;
                        case AudioFlags.VoiceShout:
                            audioSource.maxDistance = 40f;
                            break;
                        default:
                            audioSource.maxDistance = 20f;
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Initialize all the human information every time we start a new world.
        /// </summary>
        private void HandleWorldStarted()
        {
            //Console.WriteLine("AudioClipInterfaceHuman.HandleWOrldStart()");
            //HumanAudioDataReceivers = new Dictionary<long, IAudioDataReceiver>();
        }

        /// <summary>
        /// Clean up after leaving a world.
        /// </summary>
        private void HandleWorldExit()
        {
            //Console.WriteLine("AudioClipInterfaceHuman.HandleWorldExit()");
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
