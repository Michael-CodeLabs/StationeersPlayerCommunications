using System;
using System.IO;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Objects.Items;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Class for basic radio communications
    /// </summary>
    public class Radio : PowerTool, IAudioStreamReceiver
    {

        // List of all spawned radios 
        public static List<Radio> AllRadios = new List<Radio>();

        // Define events for subscription
        public static event Action<Radio> OnRadioCreated;
        public static event Action<Radio> OnRadioDestroyed;

        public override void Awake()
        {
            base.Awake();
            AllRadios.Add(this);

            // Trigger event when a new radio is created
            OnRadioCreated?.Invoke(this);
        }

        /// <summary>
        /// Ensure the paintable material is the valid one.
        /// </summary>
        public override void Start()
        {
            base.Start();

            this.CustomColor = GameManager.GetColorSwatch("ColorBlue");
            this.PaintableMaterial = this.CustomColor.Normal;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            AllRadios.Remove(this);

            // Trigger event when a radio is destroyed
            OnRadioDestroyed?.Invoke(this);
        }

        /// <summary>
        /// Should only receive audio data when set in the right frequency
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void ReceiveAudioStreamData(byte[] data, int length)
        {
            Debug.Log("Radio.ReceiveAudioStreamData()");
        }

        /// <summary>
        /// Dont implement, we can't use streams over newtork for now.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void ReceiveAudioStreamData(MemoryStream stream, int length)
        {
            throw new NotImplementedException();
        }
    }
}
