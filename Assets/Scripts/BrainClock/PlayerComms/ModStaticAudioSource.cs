using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        private float volume = 1;
        private int flags = 0;

        public List<MonoBehaviour> monoBehaviours = new List<MonoBehaviour>();

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
            if (volume != this.volume)
            {
                volume = this.volume;
                GameAudioSource.SourceVolume = volume;
            }

            if (flags != this.flags)
            {
                flags = this.flags;
                // Enable/Disable the audio effects
            }
        }

        private void Start()
        {
            Debug.Log("ModStaticAudioSource.Start()");
            //GameAudioSource.Init((IAudioParent)transform.parent);
            GameAudioSource.Init((IAudioParent)null);
            GameAudioSource.CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External");
            GameAudioSource.SetSpatialBlend(1);
            GameAudioSource.ManageOcclusion(true);
            GameAudioSource.CalculateAndSetAtmosphericVolume(true);
            GameAudioSource.SetEnabled(true);
            
            Debug.Log("GameAudioSource setup");
            Singleton<AudioManager>.Instance.AddPlayingAudioSource(GameAudioSource);


            // Find and cache the audio receivers
            audioStreamReceivers = GetComponents<IAudioStreamReceiver>();
        }


    }
}
