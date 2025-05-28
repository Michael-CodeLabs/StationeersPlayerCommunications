using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Audio;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

namespace BrainClock.PlayerComms
{

    public class Radio : PowerTool, IAudioDataReceiver
    {

        private IAudioStreamReceiver[] audioStreamReceivers = new IAudioStreamReceiver[0];

        public static List<Radio> AllRadios = new();
        public static Dictionary<int, long> AllChannels = new();

        public static event Action<Radio> OnRadioCreated;
        public static event Action<Radio> OnRadioDestroyed;

        private static InventoryManager.Mode CurrentMode => InventoryManager.CurrentMode;

        public List<Tower> _towersInRange = new();

        [Header("Radio")]
        public StaticAudioSource SpeakerAudioSource;
        public StaticAudioSource MorseAudioSource;
        public int Channels = 1;
        private int _maxVolumeSteps = 10;

        [Tooltip("Radius of sphere for signal range. Requires a RadioRangeController to work")]
        public float Range = 200;
        [Tooltip("If not assigned, all radios will receive all audio. If assigned, only radios within this range will receive the audio")]
        public RadioRangeController RangeController;

        [Header("Controls")]
        [SerializeField] private Knob knobVolume;
        [SerializeField] private ActivateButton pushToTalk;
        [SerializeField] private Canvas Screen;
        [SerializeField] private TextMeshProUGUI ChannelIndicator;
        [SerializeField] private TextMeshProUGUI VolumeIndicator;
        [SerializeField] private GameObject SignalTower;
        [SerializeField] private GameObject BatteryIcon;

        [Header("UI")]
        public GameObject Battery20;
        public GameObject Battery40;
        public GameObject Battery60;
        public GameObject Battery80;
        public GameObject Battery100;

        [Header("Audio Clips")]
        public AudioClip IncomingTransmissionClip;
        public AudioClip OnRadioClip;
        public AudioClip OffRadioClip;
        public AudioClip MorseStaticClip;
        public AudioClip MorseStuckOnTitanClip;
        public AudioClip MorseBaseFailureClip;
        public AudioClip MorseLowO2Clip;
        #region Input Timing Control

        private float channelChangeCooldown = 0.25f;
        private float volumeChangeCooldown = 0.25f;
        private float lastChannelChangeTime = 0f;
        private float lastVolumeChangeTime = 0f;
        #endregion

        private byte PlayCounter = 0;
        private bool _playingMorseLoop = false;
        private bool _previousOnOffState = false;
        private bool _PlayedClip = false;
        private float currentTime;
        public static bool RadioIsActivating;
        private bool _primaryKey = false;

        private int _currentChannel;
        public int Channel
        {
            get => _currentChannel;
            set => _currentChannel = value;
        }

        private int _currentVolume;
        public int Volume
        {
            get => _currentVolume;
            set => _currentVolume = value;
        }

        public bool IsBoosted
        {
            get
            {

                bool result = false;
                for (int i = _towersInRange.Count - 1; i >= 0; i--)
                {
                    Tower tower = _towersInRange[i];
                    if (tower == null)
                    {
                        _towersInRange.RemoveAt(i);
                        continue;
                    }
                    result |= tower.enabled && tower.Powered && tower.OnOff;
                }
                return result;
            }
        }
        public List<Tower> TowersInRange => _towersInRange;
        public override void Awake()
        {

            try
            {
                IAudioParent audioParent = transform.GetComponent<Assets.Scripts.Objects.Thing>() as IAudioParent;
                SpeakerAudioSource.GameAudioSource.Init(audioParent);
                MorseAudioSource.GameAudioSource.Init(audioParent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Setting Speaker GameAudioSource.Init failed: {ex}");
            }

            Screen.enabled = false;
            base.Awake();

            Channel = Mode;
            Volume = Exporting;

            knobVolume.Initialize(this);
        }

        public override void Start()
        {
            base.Start();

            audioStreamReceivers = GetComponents<IAudioStreamReceiver>();

            OnRadioCreated?.Invoke(this);

            SetupGameAudioSource();

            SignalTower.SetActive(Powered && IsBoosted);
            BatteryIcon.SetActive(Powered);

            AllRadios.Add(this);

            if (RangeController != null)
                RangeController.Range = Range;

            UpdateAudioMaxDistance();
        }

        public void SetupGameAudioSource()
        {

            GameAudioSource source = SpeakerAudioSource.GameAudioSource;
            source.AudioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("External"));
            source.AudioSource.loop = true;
            source.AudioSource.volume = 1;
            source.AudioSource.Play();
            source.CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External");
            source.SetSpatialBlend(1);
            source.ManageOcclusion(true);
            source.CalculateAndSetAtmosphericVolume(true);
            source.SetEnabled(true);
            source.SourceVolume = 1;

            Singleton<AudioManager>.Instance.AddPlayingAudioSource(SpeakerAudioSource.GameAudioSource);
        }

        public override void OnInteractableUpdated(Interactable interactable)
        {
            base.OnInteractableUpdated(interactable);

            if (interactable.Action == InteractableType.Activate)
            {
                AllChannels[Channel] = interactable.State > 0 ? ReferenceId : 0;
            }

            if (interactable.Action == InteractableType.OnOff && _previousOnOffState != OnOff)
            {
                _previousOnOffState = OnOff;
                if (Battery != null && !Battery.IsEmpty)
                {
                    PlayOneShot(SpeakerAudioSource, OnOff ? OnRadioClip : OffRadioClip, 3f);
                }
            }

            Channel = Mode;
            Volume = Exporting;

            Screen.enabled = Powered;
            SignalTower.SetActive(IsBoosted && Powered);
            BatteryIcon.SetActive(Powered);

            ChannelIndicator.text = (Channel + 1).ToString();
            VolumeIndicator.text = Volume.ToString();

            UpdateKnobVolume();
            UpdatePushToTalkButton();
        }

        private void PlayOneShot(StaticAudioSource source, AudioClip clip, float volumeScale = 1f)
        {
            if (source == null || source.GameAudioSource == null || source.GameAudioSource.AudioSource == null || clip == null)
                return;

            source.GameAudioSource.AudioSource.PlayOneShot(clip, volumeScale);
        }

        public override Assets.Scripts.Objects.Thing.DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {

            if (this.Powered && CurrentMode != InventoryManager.Mode.PrecisionPlacement && !Stationpedia.IsOpen && GameManager.GameState != GameState.Paused)
            {
                if (interactable.Action == InteractableType.Button1)
                {
                    if (Channel < Channels - 1)
                    {
                        if (!doAction)
                            return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("CH+");

                        Channel++;
                        Thing.Interact(this.InteractMode, Channel);
                    }
                    else
                        return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMax);
                }
                if (interactable.Action == InteractableType.Button2)
                {
                    if (Channel > 0)
                    {
                        if (!doAction)
                            return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("CH-");

                        Channel--;
                        Thing.Interact(this.InteractMode, Channel);
                    }
                    else
                        return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMin);
                }
                ;
            }

            if (interactable.Action == InteractableType.Button5)
            {
                if (Volume < _maxVolumeSteps)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("V+");

                    Volume++;
                    Thing.Interact(this.InteractExport, Volume);
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMax);
            }
            if (interactable.Action == InteractableType.Button4)
            {
                if (Volume > 0)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("V-");

                    Volume--;
                    Thing.Interact(this.InteractExport, Volume);
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMin);
                UpdateAudioMaxDistance();
            }

            return base.InteractWith(interactable, interaction, doAction);
        }

        public virtual void CheckError()
        {
            if (!GameManager.RunSimulation)
                return;

            if (AllChannels.TryGetValue(Channel, out long referenceId))
            {
                if (referenceId > 0 && referenceId != ReferenceId)
                    pushToTalk.MaterialChanger.ChangeState(2);
                else
                    pushToTalk.MaterialChanger.ChangeState(Activate);
            }
            else
                pushToTalk.MaterialChanger.ChangeState(Activate);
        }

        public void OnDocked()
        {
            foreach (Interactable interactable in this.Interactables)
            {
                if (interactable != null && !(interactable.Collider == null))
                    interactable.Collider.enabled = true;
            }
        }

        public override bool IsOperable
        {
            get
            {
                if (this.Powered)
                    return true;
                return false;
            }
        }

        public bool IsChannelBusy()
        {
            if (AllChannels.TryGetValue(Channel, out long referenceId) && referenceId > 0)
                return true;
            return false;
        }

        private void UpdateBatteryStatus()
        {
            if (Battery != null)
            {
                byte percentage = Battery.CurrentPowerPercentage;
                Battery20.SetActive(percentage > 10);
                Battery40.SetActive(percentage > 30);
                Battery60.SetActive(percentage > 50);
                Battery80.SetActive(percentage > 70);
                Battery100.SetActive(percentage > 90);
            }
            SignalTower.SetActive(IsBoosted && Powered);
        }

        private void UpdateBoosterStatus()
        {
            this.SignalTower.SetActive(IsBoosted && Powered);
        }

        public override void Update1000MS(float deltaTime)
        {
            base.Update1000MS(deltaTime);

            for (int i = _towersInRange.Count - 1; i >= 0; i--)
            {
                if (_towersInRange[i] == null)
                {
                    _towersInRange.RemoveAt(i);
                }
            }
            ;

            if (RangeController != null)
                RangeController.CalculateIntruders();

            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioChannelUp) &&
                Channel < Channels - 1 &&
                currentTime - lastChannelChangeTime > channelChangeCooldown)
            {
                Channel++;
                Thing.Interact(this.InteractMode, Channel);
                lastChannelChangeTime = currentTime;
            }

            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioChannelDown) &&
                Channel > 0 &&
                currentTime - lastChannelChangeTime > channelChangeCooldown)
            {
                Channel--;
                Thing.Interact(this.InteractMode, Channel);
                lastChannelChangeTime = currentTime;
            }

            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioVolumeUp) &&
                Volume < _maxVolumeSteps &&
                currentTime - lastVolumeChangeTime > volumeChangeCooldown)
            {
                Volume++;
                Thing.Interact(this.InteractExport, Volume);
                lastVolumeChangeTime = currentTime;
            }

            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioVolumeDown) &&
                Volume > 0 &&
                currentTime - lastVolumeChangeTime > volumeChangeCooldown)
            {
                Volume--;
                Thing.Interact(this.InteractExport, Volume);
                lastVolumeChangeTime = currentTime;
            }
        }

        public void OnTowerInRadius(Tower tower)
        {
            if (tower == null) return;

            if (!_towersInRange.Contains(tower))
                _towersInRange.Add(tower);
        }

        public void OnTowerOutRadius(Tower tower)
        {
            if (tower == null) return;

            if (_towersInRange.Contains(tower))
                _towersInRange.Remove(tower);
        }

        private CancellationTokenSource morseLoopCts;
        private void FixedUpdate()
        {

            UpdateChannelBusy();

            if (SignalTower != null)
                UpdateBoosterStatus();

            if (this.Powered)
                UpdateBatteryStatus();

            if (Channel != 15 || !Powered)
            {
                _playingMorseLoop = false;
                MorseAudioSource?.Stop();
                morseLoopCts?.Cancel();
                morseLoopCts?.Dispose();
                morseLoopCts = null;
            }
            else if (Channel == 15 && Powered && !_playingMorseLoop)
            {

                _playingMorseLoop = true;
                morseLoopCts?.Cancel();
                morseLoopCts?.Dispose();

                morseLoopCts = new CancellationTokenSource();
                StartMorseLoop(morseLoopCts.Token).Forget();
            }
            else if (Channel == 15 && Powered && !IsBoosted && _playingMorseLoop)
            {

                if (MorseAudioSource?.GameAudioSource?.AudioSource != null &&
                    MorseAudioSource.GameAudioSource.AudioSource.isPlaying &&
                    MorseAudioSource.GameAudioSource.AudioSource.clip != MorseStaticClip)
                {

                    MorseAudioSource.GameAudioSource.AudioSource.Stop();

                    MorseAudioSource.GameAudioSource.AudioSource.clip = MorseStaticClip;
                    MorseAudioSource.GameAudioSource.AudioSource.loop = true;
                    MorseAudioSource.GameAudioSource.AudioSource.Play();

                    morseLoopCts?.Cancel();
                    morseLoopCts?.Dispose();
                    morseLoopCts = new CancellationTokenSource();
                    _playingMorseLoop = false;
                }
            }

            Slot activeSlot = InventoryManager.ActiveHandSlot;
            if (activeSlot == null || activeSlot.Get() as Radio != this)
                return;

            if (IsChannelBusy())
                return;

            if (CurrentMode == InventoryManager.Mode.PrecisionPlacement || Stationpedia.IsOpen || GameManager.GameState == GameState.Paused)
                return;

            if (KeyManager.GetMouse("Primary") && !_primaryKey && !KeyManager.GetButton(KeyMap.MouseControl))
            {
                Radio.RadioIsActivating = true;
                this._primaryKey = true;
                this.UseRadio().Forget();
            }
        }

        private async UniTaskVoid UseRadio()
        {
            Radio radio = this;

            if (!this.Powered)
            {
                this._primaryKey = false;
                return;
            }

            Thing.Interact(radio.InteractActivate, 1);

            while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && Powered)
                await UniTask.NextFrame();

            Thing.Interact(radio.InteractActivate, 0);
            this._primaryKey = false;
            Radio.RadioIsActivating = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);
            AllChannels.Remove(Channel);

            OnRadioDestroyed?.Invoke(this);

            morseLoopCts?.Cancel();
            morseLoopCts?.Dispose();
            morseLoopCts = null;
        }

        #region knob Volume control

        private void UpdateKnobVolume()
        {
            if (!GameManager.IsMainThread)
                this.UpdateKnobVolumeFromThread().Forget();
            else
            {

                this.knobVolume.SetKnob(Exporting, _maxVolumeSteps, 0).Forget();
                float vol = (float)Exporting * (1f / _maxVolumeSteps);

                SpeakerAudioSource.GameAudioSource.AudioSource.volume = vol;
                MorseAudioSource.GameAudioSource.AudioSource.volume = vol;
            }
        }

        private async UniTaskVoid UpdateKnobVolumeFromThread()
        {
            Radio radio = this;
            await UniTask.SwitchToMainThread(new CancellationToken());
            radio.UpdateKnobVolume();
        }
        #endregion

        #region PushToTalk button

        private void UpdatePushToTalkButton()
        {
            if (!GameManager.IsMainThread)
                this.UpdatePushToTalkButtonFromThread().Forget();
            else
            {
                pushToTalk.RefreshState();
            }
        }

        private async UniTaskVoid UpdatePushToTalkButtonFromThread()
        {
            Radio radio = this;
            await UniTask.SwitchToMainThread(new CancellationToken());
            radio.UpdatePushToTalkButton();
        }
        #endregion

        private void UpdateChannelBusy()
        {
            if (!GameManager.IsMainThread)
            {
                this.UpdateUpdateChannelBusyFromThread().Forget();
                return;
            }

            if (!Powered)
            {
                pushToTalk.MaterialChanger.ChangeState(0);
                return;
            }

            if (AllChannels.TryGetValue(Channel, out var refId) && refId > 0 && refId != ReferenceId)
            {
                pushToTalk.MaterialChanger.ChangeState(2);

                if (!_PlayedClip)
                {
                    SpeakerAudioSource.GameAudioSource.AudioSource.PlayOneShot(IncomingTransmissionClip);
                    this._PlayedClip = true;
                }
            }
            else
            {
                pushToTalk.MaterialChanger.ChangeState(Activate);
                this._PlayedClip = false;
            }
        }

        private async UniTaskVoid UpdateUpdateChannelBusyFromThread()
        {
            Radio radio = this;
            await UniTask.SwitchToMainThread(new CancellationToken());
            radio.UpdatePushToTalkButton();
        }

        public override StringBuilder GetExtendedText()
        {
            StringBuilder extendedText = base.GetExtendedText();
            extendedText.AppendLine("Volume: " + (SpeakerAudioSource.GameAudioSource.SourceVolume * 100).ToString("0") + "%");
            extendedText.AppendLine("Channel: " + (Channel + 1).ToString());
            extendedText.AppendLine("Boosted: " + IsBoosted.ToString());
            return extendedText;
        }
        private async UniTask StartMorseLoop(CancellationToken cancellationToken = default)
        {
            try
            {

                if (MorseAudioSource?.GameAudioSource?.AudioSource != null)
                {
                    MorseAudioSource.GameAudioSource.AudioSource.enabled = true;
                }
                else
                {
                    _playingMorseLoop = false;
                    return;
                }

                if (!GameManager.RunSimulation || MorseAudioSource == null || !Powered || Channel != 15)
                {
                    _playingMorseLoop = false;
                    return;
                }

                MorseAudioSource.GameAudioSource.AudioSource.Stop();
                MorseAudioSource.GameAudioSource.AudioSource.loop = false;

                while (GameManager.RunSimulation && Powered && Channel == 15 && !cancellationToken.IsCancellationRequested)
                {
                    if (IsBoosted)
                    {

                        AudioClip clip = (PlayCounter % 3) switch
                        {
                            0 => MorseStuckOnTitanClip,
                            1 => MorseBaseFailureClip,
                            2 => MorseLowO2Clip,
                            _ => null
                        };

                        if (clip != null)
                        {
                            MorseAudioSource.GameAudioSource.AudioSource.clip = clip;
                            await PlayAudioAsync(MorseAudioSource.GameAudioSource.AudioSource, cancellationToken);
                            PlayCounter++;

                            await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {

                        MorseAudioSource.GameAudioSource.AudioSource.clip = MorseStaticClip;
                        MorseAudioSource.GameAudioSource.AudioSource.loop = true;
                        MorseAudioSource.GameAudioSource.AudioSource.Play();

                        await UniTask.WaitUntil(() => !GameManager.RunSimulation || !Powered || Channel != 15 || IsBoosted || cancellationToken.IsCancellationRequested, cancellationToken: cancellationToken);

                        MorseAudioSource.GameAudioSource.AudioSource.Stop();
                    }
                }
            }
            catch (OperationCanceledException)
            {

                MorseAudioSource?.GameAudioSource?.AudioSource.Stop();
            }
            catch (Exception ex)
            {

                Debug.LogError($"Error in StartMorseLoop: {ex.Message}");
            }
            finally
            {

                _playingMorseLoop = false;
            }
        }

        private async UniTask PlayAudioAsync(AudioSource audioSource, CancellationToken cancellationToken)
        {
            if (audioSource == null || audioSource.clip == null)
            {
                return;
            }

            audioSource.Play();
            await UniTask.Delay(TimeSpan.FromSeconds(audioSource.clip.length), cancellationToken: cancellationToken);
        }
        public void UpdateAudioMaxDistance()
        {
            float maxDistance = Mathf.PI * Volume;
            if (SpeakerAudioSource.GameAudioSource != null && MorseAudioSource != null)
            {
                SpeakerAudioSource.GameAudioSource.maxDistance = maxDistance;
                MorseAudioSource.GameAudioSource.maxDistance = maxDistance;
            }
        }

        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {

            if (!this.Powered || this.Activate != 0)
                return;

            if (audioStreamReceivers != null)
            {
                foreach (IAudioStreamReceiver audioStreamReceiver in audioStreamReceivers)
                {
                    audioStreamReceiver.ReceiveAudioStreamData(data, length);
                }
            }
        }
    }
}