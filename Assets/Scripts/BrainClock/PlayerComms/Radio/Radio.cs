using System;
using System.IO;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using UnityEngine;
using Assets.Scripts.Objects;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using Audio;
using Util;
using Assets.Scripts.Util;
using static Assets.Scripts.Objects.Thing;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Electrical;
using System.Threading;
using Assets.Scripts.Localization2;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Class for basic radio communications
    /// </summary>
    public class Radio : PowerTool, IAudioDataReceiver
    {

        private IAudioStreamReceiver[] audioStreamReceivers = new IAudioStreamReceiver[0];

        // List of all spawned radios 
        public static List<Radio> AllRadios = new List<Radio>();

        // Define events for subscription 
        public static event Action<Radio> OnRadioCreated;
        public static event Action<Radio> OnRadioDestroyed;

        [Header("Radio")]
        public StaticAudioSource SpeakerAudioSource;
        public int Channels = 1; 
        public float Range = 200;
        public Collider ChannelUp;
        public Collider ChannelDown;

        [Header("Controls")]
        [SerializeField] private Knob knobVolumen;
        [SerializeField] private ActivateButton pushToTalk;

        private bool _primaryKey = false;
        private bool _isActive = false;

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

        private int _maxVolumenSteps = 10;
        private int _currentVolumen;
        public int Volumen
        {
            get
            {
                return _currentVolumen;
            }
            set
            {
                _currentVolumen = value;
            }
        }


        public override void Awake()
        {
            // Basic setup of speaker gameaudio
            Debug.Log("Radio.Start()");
            try
            {
                Debug.Log($"transform.parent {transform}");
                IAudioParent audioparent = transform.GetComponent<Thing>() as IAudioParent;
                SpeakerAudioSource.GameAudioSource.Init((IAudioParent)audioparent);
            }
            catch (Exception ex)
            {
                Debug.Log("Setting Speaker GameAudioSource.Init failed " + ex.ToString());
            }



            base.Awake();

            Debug.Log($"Radio.Awake {ReferenceId}");

            AllRadios.Add(this);




            // Setting up channel from Mode.
            Debug.Log("Setting up channel and volumen from Mode and Open states.");
            Channel = InteractMode.State;
            Volumen = InteractOpen.State;

            // Initialize Volumen Knob
            knobVolumen.Initialize(this);

        }

        /// <summary>
        /// Ensure the paintable material is the valid one.
        /// </summary>
        public override void Start()
        {
            Debug.Log($"Radio.Start {ReferenceId}");
            base.Start();

            this.CustomColor = GameManager.GetColorSwatch("ColorBlue");
            this.PaintableMaterial = this.CustomColor.Normal;

            audioStreamReceivers = GetComponents<IAudioStreamReceiver>();

            // Trigger event when a new radio is created
            OnRadioCreated?.Invoke(this);

            SetupGameAudioSource();
        }

        public void SetupGameAudioSource()
        {
            // Setting up Speaker GameAudioSource
            Debug.Log("Setting up Speaker GameAudioSource");
            SpeakerAudioSource.GameAudioSource.AudioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("External"));
            SpeakerAudioSource.GameAudioSource.AudioSource.loop = true;
            SpeakerAudioSource.GameAudioSource.AudioSource.volume = 1;
            SpeakerAudioSource.GameAudioSource.AudioSource.Play();
            SpeakerAudioSource.GameAudioSource.CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External");
            SpeakerAudioSource.GameAudioSource.SetSpatialBlend(1);
            SpeakerAudioSource.GameAudioSource.ManageOcclusion(true);
            SpeakerAudioSource.GameAudioSource.CalculateAndSetAtmosphericVolume(true);
            SpeakerAudioSource.GameAudioSource.SetEnabled(true);
            SpeakerAudioSource.GameAudioSource.SourceVolume = 1;
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
            
            // Visually update volumen knob
            UpdateKnobVolumen();

            // Visually update PTT button
            UpdatePushToTalkButton();


            CheckError();


            if (interactable.Action == InteractableType.Activate)
            {
                Debug.Log($"{Time.timeSinceLevelLoad} OnInteractableUpdated ACTIVATE type ++++ {interactable.State}");
            }
            if (interactable.Action != InteractableType.OnOff || !this.OnOff)
                return;
            //this.InputHandling(); 
        }

        public override Assets.Scripts.Objects.Thing.DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {


            // TODO: these are not mechanical buttons, we should not allow interaction unless powered.
            if (interactable.Action == InteractableType.Button1)
            {
                if (Channel < Channels)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("CH-");

                    Channel++;
                    OnServer.Interact(this.InteractMode, Channel, false); 
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMax);
            }
            if (interactable.Action == InteractableType.Button2)
            {
                if (Channel > 0)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("CH+");

                    Channel--;
                    OnServer.Interact(this.InteractMode, Channel, false);
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMin);
            }

            // TODO: these are mechanical buttons
            if (interactable.Action == InteractableType.Button3)
            {
                if (Volumen < _maxVolumenSteps)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("V-");

                    Volumen++;
                    OnServer.Interact(this.InteractOpen, Volumen, false);
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMax);
            }
            if (interactable.Action == InteractableType.Button4)
            {
                if (Volumen > 0)
                {
                    if (!doAction)
                        return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("V+");

                    Volumen--;
                    OnServer.Interact(this.InteractOpen, Volumen, false);
                }
                else
                    return new Assets.Scripts.Objects.Thing.DelayedActionInstance().Fail(GameStrings.GlobalAlreadyMin);
            }

            /* We keep the interactable Activate hidden so it can't be triggered through buttons because
             * the mod requires someone to hold the radio in an active slot to use it.
             * 
            if (interactable.Action == InteractableType.Activate)
            {
                if (!doAction)
                    return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("Activate");
                OnServer.Interact(this.InteractActivate, interactable.State == 1 ? 0 : 1, false);
            }
            */
            return base.InteractWith(interactable, interaction, doAction);
        }

        public virtual void CheckError()
        {
            if (!GameManager.RunSimulation)
                return;
            /*
            if ((UnityEngine.Object)this.Cartridge == (UnityEngine.Object)null && this.Error == 0)
            {
                OnServer.Interact(this.InteractError, 1, false);
            }
            else
            {
                if (!(bool)((UnityEngine.Object)this.Cartridge) || this.Error != 1)
                    return;
                OnServer.Interact(this.InteractError, 0, false);
            }
            */
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
                //if (this.OnOff && this.Powered)
                if (this.Powered)
                   return true;
                return false;
            }
        }

        public bool IsAvailable()
        {
            //if (this.IsOperable && this.OnOff)
            //    return this.Powered;
            return true;
        }

        /*
        private bool InUse
        {
            get
            {
                if (!this.RootParent.HasAuthority || (!this.OnOff || !this.IsOperable))
                    return false;
                Slot activeHandSlot = InventoryManager.ActiveHandSlot;
                return (activeHandSlot != null ? activeHandSlot.Get() : null) == this;
            }
        }
        */


        public override void OnUsePrimary(Vector3 targetLocation, Quaternion targetRotation, ulong steamId, bool authoringMode)
        {
            Debug.Log("******** Using Primary!");
            base.OnUsePrimary(targetLocation, targetRotation, steamId, authoringMode);
            //if (!this.OnOff || !this.Powered)
            //    return;
            this.UseRadio().Forget();
        }

        private void FixedUpdate()
        {
            Human human = InventoryManager.ParentHuman;
            if (human == null)
                return;

            if (human.RightHandSlot.Get() as Radio != this && human.LeftHandSlot.Get() as Radio != this)
                return;

            Debug.Log($"Human is holding this radio {ReferenceId}");
            if (KeyManager.GetMouse("Primary") && !_primaryKey)
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

            Thing.Interact(radio.InteractActivate, 1);
            //OnServer.Interact(this.InteractActivate, 1, false);

            while (KeyManager.GetMouse("Primary") && !KeyManager.GetButton(KeyMap.SwapHands))// && (radio.OnOff && radio.Powered))
                await UniTask.NextFrame();

            Thing.Interact(radio.InteractActivate, 0);
            //OnServer.Interact(this.InteractActivate, 0, false);
            //miningDrill._drillInUse = false;
            _primaryKey = false;
        }

        public override void OnPrimaryUseStart()
        {
            Debug.Log("---------OnPrimaryUseStart()");
            base.OnPrimaryUseStart();
            if (this.Activate == 1)
                return;
            // Interaction disabled until it is setup
            Thing.Interact(this.InteractActivate, 1);
        }

        public override void OnPrimaryUseEnd()
        {
            Debug.Log("---------OnPrimaryUseEnd()");
            base.OnPrimaryUseEnd();
            // Interaction disabled until it is setup
            Thing.Interact(this.InteractActivate, 0);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);

            // Trigger event when a radio is destroyed
            OnRadioDestroyed?.Invoke(this);
        }

        #region knob Volumen control
        /// <summary>
        /// Knob/Volume is controlled through Button3 and Button4
        /// </summary>
        private void UpdateKnobVolumen()
        {
            if (!GameManager.IsMainThread)
                this.UpdateKnobVolumenFromThread().Forget();
            else
            {
                // Setting Knob value
                knobVolumen.SetKnob(InteractOpen.State, _maxVolumenSteps, 0).Forget();
                SpeakerAudioSource.GameAudioSource.SourceVolume = Volumen;
            }
        }

        private async UniTaskVoid UpdateKnobVolumenFromThread()
        {
            Radio radio = this;
            await UniTask.SwitchToMainThread(new CancellationToken());
            radio.UpdateKnobVolumen();
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
                // Setting Knob value
                pushToTalk.RefreshState();
            }
        }

        private async UniTaskVoid UpdatePushToTalkButtonFromThread()
        {
            Radio radio = this;
            await UniTask.SwitchToMainThread(new CancellationToken());
            radio.UpdatePushToTalkButton();
        }

        // TODO: Update Materials

        #endregion




        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            Debug.Log("Radio.ReceiveAudioStreamData()"); 

            if (!this.Powered || this.Activate != 0)
                return;

            

            if (audioStreamReceivers != null)
            {
                foreach (IAudioStreamReceiver audioStreamReceiver in audioStreamReceivers)
                {
                    /*
                    StaticAudioSource staticAudioSource = audioStreamReceiver as StaticAudioSource;
                    if (staticAudioSource != null)
                    {
                        if (staticAudioSource.GameAudioSource.SqDistanceFromListener == null)
                            Singleton<AudioManager>.Instance.AddPlayingAudioSource(SpeakerAudioSource.GameAudioSource);
                    }
                    ]*/

                    audioStreamReceiver.ReceiveAudioStreamData(data, length);
                }
            }
        }
    }
}
