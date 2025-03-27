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
        public int Channel;

        public override void Awake()
        {
            // Basic setup of speaker gameaudio
            Debug.Log("Radio.Start()");
            try
            {
                Debug.Log($"transform.parent {transform.parent}");
                IAudioParent audioparent = transform.parent.GetComponent<Thing>() as IAudioParent;
                AudioSources[1].Init((IAudioParent)audioparent);
            }
            catch (Exception ex)
            {
                Debug.Log("Setting Speaker GameAudioSource.Init failed " + ex.ToString());
            }



            base.Awake();

            Debug.Log($"Radio.Awake {ReferenceId}");

            AllRadios.Add(this);

            // Trigger event when a new radio is created
            OnRadioCreated?.Invoke(this);

            // Setting up Speaker GameAudioSource
            Debug.Log("Setting up Speaker GameAudioSource");
            AudioSources[1].AudioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("External"));
            AudioSources[1].AudioSource.loop = true;
            AudioSources[1].AudioSource.Play();
            AudioSources[1].CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External"); 
            AudioSources[1].SetSpatialBlend(1);
            AudioSources[1].ManageOcclusion(true);
            AudioSources[1].CalculateAndSetAtmosphericVolume(true);
            AudioSources[1].SetEnabled(true);
            // Force Adding to thread because the game won't do it unless we 'Play' an audio
            Singleton<AudioManager>.Instance.AddPlayingAudioSource(AudioSources[1]);


            // Setting up channel from Mode.
            Debug.Log("Setting up channel from Mode.");
            Channel = Mode;
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

        }

        public override void OnInteractableUpdated(Interactable interactable)
        {
            base.OnInteractableUpdated(interactable);
            this.CheckError();
            if (interactable.Action != InteractableType.OnOff || !this.OnOff)
                return;
            //this.InputHandling();
        }

        public override Assets.Scripts.Objects.Thing.DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
        {
            if (interactable.Action == InteractableType.Button1)
            {
                if (!doAction)
                    return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("Set");
                if (Channel < 7)
                {
                    Debug.Log("[Radio] Increasing Channel number");
                    Channel++;
                    OnServer.Interact(this.InteractMode, Channel, false);
                }
            }
            if (interactable.Action == InteractableType.Button2)
            {
                if (!doAction)
                    return Assets.Scripts.Objects.Thing.DelayedActionInstance.Success("Set");
                if (Channel > 0)
                {
                    Debug.Log("[Radio] Decreasing Channel number");
                    Channel--;
                    OnServer.Interact(this.InteractMode, Channel, false);
                }
            }
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

        public void SendStartTransmission()
        {
            //OnServer.PlayClip(this.ReferenceId, CursorManager.CursorHit.point, true);
        }

        public void SendEndTransmission()
        {
            //OnServer.PlayClip(this.ReferenceId, CursorManager.CursorHit.point, true);
        }

        public override void OnPrimaryUseStart()
        {
            base.OnPrimaryUseStart();
            if (this.Activate == 1)
                return;
            this.SendStartTransmission();
            // Interaction disabled until it is setup
            //Thing.Interact(this.InteractActivate, 1);
        }

        public override void OnPrimaryUseEnd()
        {
            base.OnPrimaryUseEnd();
            this.SendEndTransmission();
            // Interaction disabled until it is setup
            //Thing.Interact(this.InteractActivate, 0);
        }

        /*
        public override void OnPowerTick()
        {
            // This function will only remove passive power when activated, 
            // needs to be update to remove passive power all the time it is 
            // powered and remove active power when activated
            base.OnPowerTick();
            if (this.Activate != 1 || this.Battery == null)
                return;
            this.Battery.PowerStored -= this.UsedPowerPassive;
        }
        */

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);

            // Trigger event when a radio is destroyed
            OnRadioDestroyed?.Invoke(this);
        }

        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            Debug.Log("Radio.ReceiveAudioStreamData()");
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
