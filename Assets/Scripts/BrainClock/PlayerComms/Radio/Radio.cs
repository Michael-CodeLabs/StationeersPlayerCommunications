using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Inventory;
using Assets.Scripts.Sound;
using Audio;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Objects.Pipes;
using System.Threading;
using Assets.Scripts.Localization2;
using System.Text;
using TMPro;
using Assets.Scripts.UI;

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

        [Header("Radio")]
        public StaticAudioSource SpeakerAudioSource;
        public int Channels = 1; 
        public float Range = 200;
        private int _maxVolumeSteps = 10;
        [Header("Controls")]
        [SerializeField] private Knob knobVolume;
        [SerializeField] private ActivateButton pushToTalk;
        [SerializeField] private Canvas Screen;
        [SerializeField] private TextMeshProUGUI ChannelIndicator;
        [SerializeField] private TextMeshProUGUI VolumeIndicator;
        [SerializeField] private GameObject SignalTower;
        [SerializeField] private GameObject BatteryIcon;

        [Header("UI")]
        [SerializeField] private BatteryDisplay batteryDisplay;


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


        // Current booster of this radio
        private int _boosterReferenceId = -1;
        public bool isBoosted
        {
            get
            {
                return _boosterReferenceId > 0;
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
            SignalTower.SetActive(Powered);
            BatteryIcon.SetActive(Powered);

            AllRadios.Add(this);
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


            // Update our states
            Channel = Mode;
            Volume = Exporting;

            Screen.enabled = Powered;
            SignalTower.SetActive(Powered);
            BatteryIcon.SetActive(Powered);

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
            if (Powered)
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
            if (this.Battery != null && batteryDisplay.isActiveAndEnabled)
                batteryDisplay.SetBatteryStatus(this.Battery.CurrentPowerPercentage);
        }                

        /// <summary>
        /// Check if the player wants to use the radio (using the mouse button).
        /// </summary>
        private void FixedUpdate()
        {
            // Visual update of the Activate button
            UpdateChannelBusy();

            // Update Battery status
            if (batteryDisplay != null)
                UpdateBatteryStatus();

            // TODO update booster status

            // Return if radio isn't used.
            Slot activeSlot = InventoryManager.ActiveHandSlot;
            if (activeSlot == null || activeSlot.Get() as Radio != this)
                return;

            // Return if channel is busy
            if (IsChannelBusy())
                return;

            // Only operate if everything is ok
            //Debug.Log($"Human is holding this radio {ReferenceId}");
            if (KeyManager.GetMouse("Primary") && !_primaryKey && !KeyManager.GetButton(KeyMap.MouseControl))
            {
                _primaryKey = true;
                this.UseRadio().Forget();
            }
        }

        private async UniTaskVoid UseRadio()
        {
            Radio radio = this;

            // Can't use the radio if not powered.
            if (!Powered)
            {
                _primaryKey = false;
                return;
            }

            // Start using the radio
            // TODO: Add audio effect here
            Thing.Interact(radio.InteractActivate, 1);

            while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands) && Powered)
                await UniTask.NextFrame();

            // Stop using the radio
            // TODO: Add audio effect here
            Thing.Interact(radio.InteractActivate, 0);
            _primaryKey = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);

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
                knobVolume.SetKnob(Exporting, _maxVolumeSteps, 0).Forget(); 
                float vol = (float)Exporting * (1f / _maxVolumeSteps);
                //Debug.Log($"UpdaingKnobVolume {Exporting} {vol}");
                SpeakerAudioSource.GameAudioSource.SourceVolume = vol;
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



        private void UpdateChannelBusy()
        {
            if (!GameManager.IsMainThread)
            {
                this.UpdateUpdateChannelBusyFromThread().Forget();
            }
            else
            {
                // Ensure the radio is powered before updating the material
                if (!Powered)
                    return;

                long referenceId;
                if (AllChannels.TryGetValue(Channel, out referenceId))
                {
                    if (referenceId > 0 && referenceId != ReferenceId)
                        pushToTalk.MaterialChanger.ChangeState(2); // Set as busy
                    else
                        pushToTalk.MaterialChanger.ChangeState(Activate);
                }
                else
                {
                    pushToTalk.MaterialChanger.ChangeState(Activate);
                }
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
