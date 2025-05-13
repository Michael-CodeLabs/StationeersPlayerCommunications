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
    /// <summary>
    /// Class for basic radio communications
    /// </summary>
    public class Radio : PowerTool, IAudioDataReceiver
    {
        // Cached list of audio receivers in this Radio
        private IAudioStreamReceiver[] audioStreamReceivers = new IAudioStreamReceiver[0];

        // List of all spawned radios and status of all channels
        public static List<Radio> AllRadios = new List<Radio>();
        public static Dictionary<int, long> AllChannels = new Dictionary<int, long>();

        // Define events for subscription 
        public static event Action<Radio> OnRadioCreated;
        public static event Action<Radio> OnRadioDestroyed;

        private static InventoryManager.Mode CurrentMode => InventoryManager.CurrentMode;
        // RadioTowers boost this radio
        private List<Tower> _towersInRange = new List<Tower>();

        //MorseAssigner
        [SerializeField] private MorseCode _morseCode;
        private bool _playingMorseLoop = false;
        private bool _PlayedClip = false;
        //[SerializeField] private AudioSource MorseAudioSource;

        [Header("Radio")]
        public StaticAudioSource SpeakerAudioSource;
        public StaticAudioSource MorseAudioSource;
        public int Channels = 1;
        private int _maxVolumeSteps = 10;
        private bool Initalized;
        private bool Ready;
        [Tooltip("Radius of sphere for signal range. Requires a RadioRangeController to work")]
        public float Range = 200;
        [Tooltip("If not assigned, all radios will receive all audio. If assigned, only radios within this range will receive the audio")]
        public RadioRangeController RangeController;

        [Header("Controls")]
        [SerializeField] private Knob knobVolume;
        [SerializeField] private ActivateButton pushToTalk;
        [SerializeField] private Canvas Screen;
        [SerializeField] private GameObject ScreenBackground;
        [SerializeField] private GameObject Logo;
        [SerializeField] private GameObject ChannelGroup;
        [SerializeField] private GameObject VolumeGroup;
        [SerializeField] private TextMeshProUGUI ChannelIndicator;
        [SerializeField] private TextMeshProUGUI VolumeIndicator;
        [SerializeField] private GameObject SignalTower;
        [SerializeField] private GameObject BatteryIcon;

        [Header("UI")]
        [SerializeField] private BatteryDisplay batteryDisplay;
        private float currentTime;
        #region Input Timing Control
        // Channel/volume change rate limiting
        private float channelChangeCooldown = 0.25f;
        private float volumeChangeCooldown = 0.25f;

        private float lastChannelChangeTime = 0f;
        private float lastVolumeChangeTime = 0f;
        #endregion

        [Header("Audio Clips")]
        public AudioClip IncomingTransmissionClip;
        public AudioClip OnRadioClip;
        public AudioClip OffRadioClip;
        public static bool RadioIsActivating;
        private bool _primaryKey = false;

        // Current channel of this radio
        private int _currentChannel;
        public int Channel
        {
            get
            {
                return _currentChannel;
            }
            set
            {
                _currentChannel = value;
            }
        }


        // Current Volume of this radio
        private int _currentVolume;
        public int Volume
        {
            get
            {
                return _currentVolume;
            }
            set
            {
                _currentVolume = value;
            }
        }

        public bool isBoosted
        {
            get
            {
                // At least one of the tower needs to be turned on
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

        public List<Tower> TowersInRange
        {
            get
            {
                return _towersInRange;
            }
        }

        public override void Awake()
        {
            // Basic setup of speaker gameaudio. We need to initialize the GameAudioSource
            // first so base.Awake() finds it ready to be used.
            Debug.Log("Radio.Start()");
            try
            {
                IAudioParent audioparent = transform.GetComponent<Assets.Scripts.Objects.Thing>() as IAudioParent;
                SpeakerAudioSource.GameAudioSource.Init((IAudioParent)audioparent);
                MorseAudioSource.GameAudioSource.Init((IAudioParent)audioparent);
            }
            catch (Exception ex)
            {
                Debug.Log("Setting Speaker GameAudioSource.Init failed " + ex.ToString());
            }

            // Force screen offline at spawn, it will be updated with the Powered interactable.
            Screen.enabled = false;


            base.Awake();

            // Setting up channel from Mode and Volume from Exporting states.
            Channel = Mode;
            Volume = Exporting;

            // Other components needing initialization can go here.
            // Initialize Volume Knob
            knobVolume.Initialize(this);

        }

        /// <summary>
        /// Ensure the paintable material is the valid one.
        /// </summary>
        public override void Start()
        {
            base.Start();

            // Cache all audio receivers
            audioStreamReceivers = GetComponents<IAudioStreamReceiver>();

            // Trigger event when a new radio is created, it should have a referenceId
            OnRadioCreated?.Invoke(this);

            // Finish adding the GameAudioSource to the audio manager.
            SetupGameAudioSource();

            // Force all screen icons to use the Powered state.
            this.SignalTower.SetActive(Powered && Ready);
            this.BatteryIcon.SetActive(Powered && Ready);

            AllRadios.Add(this);

            // Setup radio range if range controller is available
            if (RangeController != null)
                RangeController.Range = Range;

            UpdateAudioMaxDistance();
        }

        public void SetupGameAudioSource()
        {
            // Setting up Speaker GameAudioSource
            Debug.Log("Setting up Speaker GameAudioSource");
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

            // Force Adding to thread because the game won't do it unless we 'Play' an audio
            Singleton<AudioManager>.Instance.AddPlayingAudioSource(SpeakerAudioSource.GameAudioSource);
        }

        /// <summary>
        /// Used to Visual update different controls and also to track what radio channels are busy
        /// </summary>
        /// <param name="interactable"></param>
        public override void OnInteractableUpdated(Interactable interactable)
        {
            base.OnInteractableUpdated(interactable);

            // If a radio becomes active, mark that channel used by that reference Id.
            if (interactable.Action == InteractableType.Activate)
            {
                Debug.Log($"Updating Channel {Channel} status: {interactable.State} from Radio: {ReferenceId}");
                long refid;
                if (AllChannels.TryGetValue(Channel, out refid))
                {
                    AllChannels[Channel] = (interactable.State > 0) ? ReferenceId : 0;
                }
                else
                {
                    AllChannels.Add(Channel, (interactable.State > 0) ? ReferenceId : 0);
                }

            }

            if (interactable.Action == InteractableType.OnOff && this.Battery != null && !this.Battery.IsEmpty)
            {
                SpeakerAudioSource.GameAudioSource.AudioSource.PlayOneShot(OnOff ? OnRadioClip : OffRadioClip, 3f);
            }

            // Update our states
            Channel = Mode;
            Volume = Exporting;

            Screen.enabled = Powered;
            SignalTower.SetActive(isBoosted && Powered && Ready);
            BatteryIcon.SetActive(Powered && Ready);

            ChannelIndicator.text = (Channel + 1).ToString();
            VolumeIndicator.text = Volume.ToString();

            // Visually update Volume knob
            UpdateKnobVolume();

            // Visually update PTT button
            UpdatePushToTalkButton();
        }

        public override Assets.Scripts.Objects.Thing.DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {

            // These are the channel buttons, can only be interacted if the device is Online
            // TODO: Should this buttons have a visual effect?
            if (this.Powered && CurrentMode != InventoryManager.Mode.PrecisionPlacement && this.Ready && !Stationpedia.IsOpen && GameManager.GameState != GameState.Paused)
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

                if (this.Channel == 15 && this.Powered && !this._playingMorseLoop && this.Ready)
                {
                    StartMorseLoop().Forget();
                }
            }

            // Volume Knob buttons, can be interacted if the device is offline.
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

            long referenceId;
            if (AllChannels.TryGetValue(Channel, out referenceId))
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
            long referenceId;
            if (AllChannels.TryGetValue(Channel, out referenceId) && referenceId > 0)
                return true;
            return false;
        }


        private void UpdateBatteryStatus()
        {
            foreach (Radio radio in AllRadios)
            {
                if (radio.Battery != null && radio.batteryDisplay.isActiveAndEnabled)
                    radio.batteryDisplay.SetBatteryStatus(radio.Battery.PowerRatio);
            }

            this.SignalTower.SetActive(isBoosted && Powered && Ready);
        }

        private void UpdateBoosterStatus()
        {
            this.SignalTower.SetActive(isBoosted && Powered && Ready);
        }

        // Update the radios within range for this radio
        public override void Update1000MS(float deltaTime)
        {
            base.Update1000MS(deltaTime);

            // Clean up null towers
            for (int i = _towersInRange.Count - 1; i >= 0; i--)
            {
                if (_towersInRange[i] == null)
                {
                    _towersInRange.RemoveAt(i);
                }
            }

            if (this.Channel != 15 || !this.Powered)
            {
                this.MorseAudioSource.GameAudioSource.AudioSource.Stop();
                this._playingMorseLoop = false;
            }

            if (RangeController != null)
                RangeController.CalculateIntruders();
        }

        // Update the list of Towers we are within range
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


        /// <summary>
        /// Check if the player wants to use the radio (using the mouse button).
        /// </summary>
        private void FixedUpdate()
        {
            currentTime = Time.time;

            // Visual update of the Activate button
            UpdateChannelBusy();

            // TODO update booster status
            if (SignalTower != null)
                UpdateBoosterStatus();

            // Update Battery status && Display Logo
            if (this.Powered && this.Ready)
                UpdateBatteryStatus();

            // Return if radio isn't used.
            Slot activeSlot = InventoryManager.ActiveHandSlot;
            if (activeSlot == null || activeSlot.Get() as Radio != this)
                return;

            // Return if channel is busy
            if (IsChannelBusy())
                return;

            if (CurrentMode == InventoryManager.Mode.PrecisionPlacement || Stationpedia.IsOpen || GameManager.GameState == GameState.Paused)
                return;

            if (this.OnOff)
            {
                if (this.Powered)
                {
                    this.UpdateLogo().Forget();
                    this.Initalized = true;
                }
                else 
                {this.Initalized = false;
                 this.Ready = false; }
            }
            // Only operate if everything is ok
            //Debug.Log($"Human is holding this radio {ReferenceId}");
            if (KeyManager.GetMouse("Primary") && !_primaryKey && !KeyManager.GetButton(KeyMap.MouseControl))
            {
                Radio.RadioIsActivating = true;
                this._primaryKey = true;
                this.UseRadio().Forget();
            }
            // Channel Up
            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioChannelUp) &&
                Channel < Channels - 1 &&
                currentTime - lastChannelChangeTime > channelChangeCooldown)
            {
                Channel++;
                Thing.Interact(this.InteractMode, Channel);
                lastChannelChangeTime = currentTime;
            }

            // Channel Down
            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioChannelDown) &&
                Channel > 0 &&
                currentTime - lastChannelChangeTime > channelChangeCooldown)
            {
                Channel--;
                Thing.Interact(this.InteractMode, Channel);
                lastChannelChangeTime = currentTime;
            }

            // Volume Up
            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioVolumeUp) &&
                Volume < _maxVolumeSteps &&
                currentTime - lastVolumeChangeTime > volumeChangeCooldown)
            {
                Volume++;
                Thing.Interact(this.InteractExport, Volume);
                lastVolumeChangeTime = currentTime;
            }

            // Volume Down
            if (KeyManager.GetButton(StationeersPlayerCommunications.RadioVolumeDown) &&
                Volume > 0 &&
                currentTime - lastVolumeChangeTime > volumeChangeCooldown)
            {
                Volume--;
                Thing.Interact(this.InteractExport, Volume);
                lastVolumeChangeTime = currentTime;
            }
        }

        private async UniTaskVoid UseRadio()
        {
            Radio radio = this;
            // Can't use the radio if not powered.
            if (!this.Powered)
            {
                this._primaryKey = false;
                return;
            }

            // Start using the radio
            // Done: Add audio effect here
            Thing.Interact(radio.InteractActivate, 1);

            while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && Powered)
                await UniTask.NextFrame();

            // Stop using the radio
            Thing.Interact(radio.InteractActivate, 0);
            this._primaryKey = false;
            Radio.RadioIsActivating = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);
            AllChannels.Remove(Channel);

            // Trigger event when a radio is destroyed
            OnRadioDestroyed?.Invoke(this);
        }

        #region knob Volume control
        /// <summary>
        /// Knob/Volume is controlled through Button4 and Button5
        /// </summary>
        private void UpdateKnobVolume()
        {
            if (!GameManager.IsMainThread)
                this.UpdateKnobVolumeFromThread().Forget();
            else
            {
                // Setting Knob value
                this.knobVolume.SetKnob(Exporting, _maxVolumeSteps, 0).Forget();
                float vol = (float)Exporting * (1f / _maxVolumeSteps);
                //Debug.Log($"UpdaingKnobVolume {Exporting} {vol}");
                this.SpeakerAudioSource.GameAudioSource.SourceVolume = vol;
                this.MorseAudioSource.GameAudioSource.AudioSource.volume = vol;
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
        /// <summary>
        /// PTT Is controlled through Activate
        /// </summary>
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


        private async UniTaskVoid UpdateLogo()
        {
            if (!GameManager.RunSimulation)
                return;

            if (this.Initalized)
                return;

            this.Logo.SetActive(true);
            this.ScreenBackground.SetActive(false);
            this.ChannelGroup.SetActive(false);
            this.VolumeGroup.SetActive(false);
            this.SignalTower.SetActive(false);
            this.BatteryIcon.SetActive(false);
            await UniTask.Delay(2000);

            this.Logo.SetActive(false);
            this.ScreenBackground.SetActive(true);
            this.ChannelGroup.SetActive(true);
            this.VolumeGroup.SetActive(true);
            this.SignalTower.SetActive(true);
            this.BatteryIcon.SetActive(true);
            this.Ready = true;
        }

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
                this.Ready = false;
                return;
            }

            if (AllChannels.TryGetValue(Channel, out var refId) && refId > 0 && refId != ReferenceId)
            {
                pushToTalk.MaterialChanger.ChangeState(2); // Busy

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
            extendedText.AppendLine("Boosted: " + isBoosted.ToString());
            return extendedText;
        }
        private async UniTaskVoid StartMorseLoop()
        {
            this._playingMorseLoop = true;

            while (this.Channel == 15 && this.Powered && !this.MorseAudioSource.GameAudioSource.AudioSource.isPlaying && this.Ready)
            {
                _morseCode.PlayMorse(isBoosted);

                while (this.MorseAudioSource.GameAudioSource.AudioSource.isPlaying && this.Channel == 15 && this.Powered && this.Ready)
                    await UniTask.Yield();

                await UniTask.Delay(7000);
            }
            this._playingMorseLoop = false;
        }

        public void UpdateAudioMaxDistance()
        {
            _ = this;
            float maxDistance = RocketMath.PiOverTwo * 2 * Volume;

            if (SpeakerAudioSource != null)
                SpeakerAudioSource.GameAudioSource.maxDistance = maxDistance;

            if (MorseAudioSource != null)
                MorseAudioSource.GameAudioSource.AudioSource.maxDistance = maxDistance;
        }

        /// <summary>
        /// Populate audio to all receiver components
        /// </summary>
        /// <param name="referenceId"></param>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <param name="volume"></param>
        /// <param name="flags"></param>
        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            //Debug.Log("Radio.ReceiveAudioStreamData()"); 

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