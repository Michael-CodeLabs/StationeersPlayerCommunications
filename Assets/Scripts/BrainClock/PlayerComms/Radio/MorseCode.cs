using Assets.Scripts;
using Assets.Scripts.Sound;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class MorseCode : ModStaticAudioSource
    {
        public AudioClip stuckOnTitanClip;
        public AudioClip baseFailureClip;
        public AudioClip lowO2Clip;

        private static byte PlayCounter = 0;

        public void PlayMorse()
        {
            if (!GameManager.RunSimulation || GameAudioSource?.AudioSource == null)
                return;

            switch (PlayCounter % 3)
            {
                case 0:
                    GameAudioSource.AudioSource.clip = stuckOnTitanClip;
                    break;
                case 1:
                    GameAudioSource.AudioSource.clip = baseFailureClip;
                    break;
                case 2:
                    GameAudioSource.AudioSource.clip = lowO2Clip;
                    break;
            }

            GameAudioSource.AudioSource.Play();
            PlayCounter++;
        }
    }
}
