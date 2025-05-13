using System;
using Assets.Scripts.Inventory;
using Assets.Scripts.Atmospherics;
using System.IO;
using UnityEngine;
using Assets.Scripts.Networking;

namespace BrainClock.PlayerComms
{
    public class PlayerCommunicationsManager : MonoBehaviour, IAudioStreamReceiver
    {
        public static PlayerCommunicationsManager Instance { get; private set; }

        private IAudioDataReceiver[] audioDataReceivers;

        private bool InGame = false;

        [Tooltip("Allow recording voice while sleeping")]
        public bool VoiceWhenSleeping;

        [Tooltip("Allow recording voice while unconscious")]
        public bool VoiceWhenUnresponsive;

        [Tooltip("Average microphone volume multiplier")]
        public float VoiceVolume = 0.5f;

        private VoiceMode currentVoiceMode = VoiceMode.Normal;

        private float lastKeypressTime = 0f;
        private float keypressCooldown = 0.25f;

        public enum VoiceMode
        {
            Whisper,
            Normal,
            Shout
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                Instance = this;
            }
        }

        void Start()
        {
            Debug.Log("PlayerCommunicationsManager.Start()");

            WorldManager.OnWorldStarted += HandleWorldStarted;
            WorldManager.OnWorldExit += HandleWorldExit;

            audioDataReceivers = gameObject.GetComponents<IAudioDataReceiver>();
        }

        private void Update()
        {
            // Handle voice mode input
            if (KeyManager.GetButton(StationeersPlayerCommunications.VoiceStrength) && Time.time - lastKeypressTime > keypressCooldown)
            {
                CycleVoiceMode();
                lastKeypressTime = Time.time;
            }
        }

        private void CycleVoiceMode()
        {
            currentVoiceMode = (VoiceMode)(((int)currentVoiceMode + 1) % Enum.GetValues(typeof(VoiceMode)).Length);
            Debug.Log($"[Voice Mode] Switched to: {currentVoiceMode}");
        }

        private void HandleWorldStarted()
        {
            Console.WriteLine("World has started.. Setting up Voice capture");
            InGame = true;
        }

        private void HandleWorldExit()
        {
            Console.WriteLine("World is exiting.. Stopping Voice capture");
            InGame = false;
        }

        private void OnDestroy()
        {
            Debug.Log("PlayerCommunicationsManager.OnDestroy()");
            HandleWorldExit();
            WorldManager.OnWorldStarted -= HandleWorldStarted;
            WorldManager.OnWorldExit -= HandleWorldExit;
        }

        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            if (!InGame)
                return;

            if (!NetworkManager.IsActive || InventoryManager.ParentHuman == null)
                return;

            if (InventoryManager.ParentHuman.IsSleeping && !VoiceWhenSleeping)
                return;

            if (InventoryManager.ParentHuman.IsUnresponsive && !VoiceWhenUnresponsive)
                return;

            float volume = VoiceVolume;
            switch (currentVoiceMode)
            {
                case VoiceMode.Whisper:
                    volume *= 0.3f; // Whisper volume
                    break;

                case VoiceMode.Normal:
                    volume *= 1.0f; // Normal volume
                    break;

                case VoiceMode.Shout:
                    volume *= 1.5f; // Shout volume
                    break;
            }

            int flags = 0;

            if (InventoryManager.ParentHuman)
            {
                if (InventoryManager.ParentHuman.HasInternals && InventoryManager.ParentHuman.InternalsOn)
                {
                    volume *= (InventoryManager.ParentHuman.BreathingAtmosphere != null
                        ? Mathf.Clamp01((InventoryManager.ParentHuman.BreathingAtmosphere.PressureGassesAndLiquids / new PressurekPa(3.0)).ToFloat())
                        : 0.0f);
                    flags |= 1; // Internals flag
                }
            }

            // Add voice mode flag
            switch (currentVoiceMode)
            {
                case VoiceMode.Whisper:
                    flags |= (int)AudioClipMessage.AudioFlags.VoiceWhisper;
                    break;
                case VoiceMode.Normal:
                    flags |= (int)AudioClipMessage.AudioFlags.VoiceNormal;
                    break;
                case VoiceMode.Shout:
                    flags |= (int)AudioClipMessage.AudioFlags.VoiceShout;
                    break;
            }

            foreach (IAudioDataReceiver receiver in audioDataReceivers)
            {
                receiver.ReceiveAudioData(-1, data, length, volume, flags);
            }
        }

        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
            if (!InGame)
                return;

            throw new NotImplementedException();
        }

        public VoiceMode GetCurrentVoiceMode() => currentVoiceMode;
    }
}
