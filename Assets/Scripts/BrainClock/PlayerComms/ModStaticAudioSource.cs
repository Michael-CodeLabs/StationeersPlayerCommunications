using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace BrainClock.PlayerComms
{

    /*  Game defined audio mixers
        Mixer: Master -  715499232
        Mixer: World -  -71942585
        Mixer: LocalPlayerInternal -  755418609
        Mixer: LocalPlayerInternalNoFx -  1539486488
        Mixer: LocalPlayer -  -1929936868
        Mixer: External -  -1591426066
        Mixer: Occluded -  -1140213991
        Mixer: OccludedStorm -  -1699109206
        Mixer: Vacuum -  -710576460
        Mixer: HelmetClosed -  944442496
        Mixer: HelmetFX -  -992743594
        Mixer: Silent -  1006664890
        Mixer: System -  -824109891
        Mixer: Interface -  -1241158018
        Mixer: Music -  210963790
        Mixer: Menu -  -583559763
    */

    public class ModStaticAudioSource : StaticAudioSource, IAudioDataReceiver
    {
        private IAudioStreamReceiver[] audioStreamReceivers = new IAudioStreamReceiver[0];

        private float volume = 0;
        private int flags = 0;

        public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
        {
            Debug.Log("ModStaticAudioSource.ReceiveAudioData()");
            if (audioStreamReceivers != null)
            {
                foreach (IAudioStreamReceiver audioStreamReceiver in audioStreamReceivers)
                {
                    audioStreamReceiver.ReceiveAudioStreamData(data, length);
                }
            }

            // Adjust audio settings
            if (this.volume != volume)
            {
                this.volume = volume;
                GameAudioSource.SourceVolume = this.volume;
            }

            if (this.flags != flags)
            {
                this.flags = flags;
                gameObject.GetComponent<AudioLowPassFilter>().enabled = (this.flags == 1);
                gameObject.GetComponent<AudioReverbFilter>().enabled = (this.flags == 1);
                // Enable/Disable the audio effects
            }
        }

        private void Start()
        {
            Debug.Log("ModStaticAudioSource.Start()");
            try
            {
                Debug.Log($"transform.parent {transform.parent}");
                IAudioParent audioparent = transform.parent.GetComponent<Human>() as IAudioParent;
                GameAudioSource.Init((IAudioParent)audioparent);
            }
            catch (Exception ex)
            {
                Debug.Log("GameAudioSource.Init failed " + ex.ToString());
            }

            GameAudioSource.AudioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(UnityEngine.Animator.StringToHash("External"));

            GameAudioSource.CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External");
            GameAudioSource.SetSpatialBlend(1);
            GameAudioSource.ManageOcclusion(true);
            GameAudioSource.CalculateAndSetAtmosphericVolume(true);
            GameAudioSource.SetEnabled(true);
            this.SetEnable(true);
            
            Debug.Log("GameAudioSource setup");
            Singleton<AudioManager>.Instance.AddPlayingAudioSource(GameAudioSource);


            // Find and cache the audio receivers
            audioStreamReceivers = GetComponents<IAudioStreamReceiver>();
        }


    }
}
