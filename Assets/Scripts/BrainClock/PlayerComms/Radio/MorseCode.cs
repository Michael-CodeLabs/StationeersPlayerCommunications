//using Assets.Scripts;
//using UnityEngine;

//public class MorseCode : ModStaticAudioSource
//{
//    public AudioClip Static;
//    public AudioClip stuckOnTitanClip;
//    public AudioClip baseFailureClip;
//    public AudioClip lowO2Clip;

//    public byte PlayCounter = 0; // Changed from static to instance variable

//    public void PlayMorse(bool IsBoosted)
//    {
//        if (!GameManager.RunSimulation || GameAudioSource?.AudioSource == null)
//            return;

//        var source = GameAudioSource.AudioSource;

//        if (!IsBoosted)
//        {
//            if (!source.isPlaying)
//            {
//                source.clip = Static;
//                source.loop = true;
//                source.Play();
//            }
//        }
//        else if (IsBoosted)
//        {
//            if (source.clip == Static)
//                source.Stop();

//            source.loop = false;

//            switch (PlayCounter % 3)
//            {
//                case 0:
//                    source.clip = stuckOnTitanClip;
//                    break;
//                case 1:
//                    source.clip = baseFailureClip;
//                    break;
//                case 2:
//                    source.clip = lowO2Clip;
//                    break;
//            }

//            source.Play();
//            PlayCounter++;
//        }
//    }
//}