using Steamworks;
using System;
using System.IO;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class SteamVoiceRecorder : MonoBehaviour
    {
        public enum VoiceCaptureMode
        {
            FixedUpdate,
            OnDemand
        }

        private const int MAX_STREAM_SIZE = 1024 * 1024;
        private const float STREAM_CLEAN_INTERVAL = 30f;

        [SerializeField] private GameObject _audioReceiver;
        [SerializeField] private VoiceCaptureMode _voiceCaptureMode = VoiceCaptureMode.FixedUpdate;
        [SerializeField] private uint _appId = 0;
        [SerializeField] private bool _useStream = false;

        private bool _transmissionMode;
        private MemoryStream _voiceStream;
        private bool _isPushToTalkActive;
        private bool _wasPushToTalkActive;
        private IAudioStreamReceiver[] _audioStreamReceivers;
        private float _lastStreamCleanTime;
        private byte[] _reusableBuffer;

        public static SteamVoiceRecorder Instance { get; private set; }
        public bool TransmissionMode => _transmissionMode;
        public bool IsReady { get; private set; }
        public bool VoiceRecordEnabled { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _voiceStream = new MemoryStream(8192);
            _reusableBuffer = new byte[8192];

            StationeersPlayerCommunications.TransmissionModeConfig.SettingChanged += OnTransmissionModeChanged;
            UpdateTransmissionMode();

            _audioStreamReceivers = (_audioReceiver != null ? _audioReceiver : gameObject).GetComponents<IAudioStreamReceiver>();
        }

        private void Start()
        {
            InitializeSteam();
            IsReady = SteamClient.IsValid && _audioStreamReceivers != null && _audioStreamReceivers.Length > 0;
            SteamUser.VoiceRecord = true;
            _lastStreamCleanTime = Time.time;
        }

        private void InitializeSteam()
        {
            if (!SteamClient.IsValid)
            {
                SteamClient.Init(_appId, true);
            }

            if (SteamClient.IsValid)
            {
                Debug.Log("SteamVoiceRecorder: Steam initialized successfully");
            }
        }

        private void FixedUpdate()
        {
            UpdatePushToTalkState();

            if (_voiceCaptureMode == VoiceCaptureMode.FixedUpdate)
            {
                ProcessCapture();
            }
        }

        private void Update()
        {

            if (!_transmissionMode && Time.time - _lastStreamCleanTime > STREAM_CLEAN_INTERVAL)
            {
                if (_voiceStream.Capacity > MAX_STREAM_SIZE)
                {
                    ClearVoiceStream();
                }
                _lastStreamCleanTime = Time.time;
            }
        }

        private void UpdatePushToTalkState()
        {
            _wasPushToTalkActive = _isPushToTalkActive;

            _isPushToTalkActive = Radio.RadioIsActivating ||
                                (!_transmissionMode) ||
                                (_transmissionMode && IsPushToTalkKeyPressed());

            VoiceRecordEnabled = _isPushToTalkActive;

            if (_transmissionMode || _isPushToTalkActive && !_wasPushToTalkActive)
            {
                ClearVoiceStream();
            }
        }

        private void ClearVoiceStream()
        {
            _voiceStream.SetLength(0);
            _voiceStream.Position = 0;
            _voiceStream.Capacity = Math.Min(_voiceStream.Capacity, MAX_STREAM_SIZE);
        }

        public void ProcessCapture()
        {
            if (!IsReady || !VoiceRecordEnabled || _voiceStream == null)
                return;

            int compressedRead = SteamUser.ReadVoiceData(_voiceStream);
            _voiceStream.Position = 0;

            if (compressedRead > 0)
            {
                if (_useStream)
                {
                    SendToAllReceivers(_voiceStream, compressedRead);
                }
                else
                {

                    byte[] bufferToSend = compressedRead <= _reusableBuffer.Length
                        ? _reusableBuffer
                        : new byte[compressedRead];

                    Buffer.BlockCopy(_voiceStream.GetBuffer(), 0, bufferToSend, 0, compressedRead);
                    SendToAllReceivers(bufferToSend, compressedRead);
                }

                if (_transmissionMode)
                {
                    ClearVoiceStream();
                }
                else
                {
                    _voiceStream.SetLength(0);
                    _voiceStream.Position = 0;
                }
            }
        }

        private bool IsPushToTalkKeyPressed()
        {
            if (Radio.RadioIsActivating)
            {
                return true;
            }
            return KeyManager.GetButton(StationeersPlayerCommunications.PushToTalk);
        }

        private void SendToAllReceivers(MemoryStream stream, int length)
        {
            if (_audioStreamReceivers == null || length <= 0) return;

            foreach (var receiver in _audioStreamReceivers)
            {
                stream.Position = 0;
                receiver.ReceiveAudioStreamData(stream, length);
            }
        }

        private void SendToAllReceivers(byte[] data, int length)
        {
            if (_audioStreamReceivers == null || length <= 0) return;

            foreach (var receiver in _audioStreamReceivers)
            {
                receiver.ReceiveAudioStreamData(data, length);
            }
        }

        private void OnTransmissionModeChanged(object sender, EventArgs e)
        {
            UpdateTransmissionMode();
        }

        private void UpdateTransmissionMode()
        {
            _transmissionMode = StationeersPlayerCommunications.TransmissionModeConfig.Value;
            Debug.Log($"[SteamVoiceRecorder] TransmissionMode updated to: {_transmissionMode}");

            ClearVoiceStream();
        }

        public void Shutdown()
        {
            SteamUser.VoiceRecord = false;
            _voiceStream?.Dispose();
            _voiceStream = null;
        }

        //TODO SERVER MANAGEMENT 

        //private void OnServer()
        //{
        //    if (NetworkManager.IsServer)
        //    {
        //        byte AddEventCounter = 0;
        //        NetworkBase.PausedForClientDelegate PausedForClient = null;

        //        if (AddEventCounter == 0)
        //        {
        //            NetworkServer.PausedForClientConnectEvent += PausedForClient;
        //            AddEventCounter = 1;
        //        }

        //        PausedForClient.Invoke(UpdateClientsList)
        //    }

        //    void UpdateClientsList()
        //    {
        //        NetworkBase.Clients.RemoveAll
        //    }
        //}

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            StationeersPlayerCommunications.TransmissionModeConfig.SettingChanged -= OnTransmissionModeChanged;
            Shutdown();
        }
    }
}