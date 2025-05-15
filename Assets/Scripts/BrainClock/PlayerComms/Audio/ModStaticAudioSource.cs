using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Audio;
using BrainClock.PlayerComms;
using System;
using UnityEngine;
using Util;
using static BrainClock.PlayerComms.AudioClipMessage;

public class ModStaticAudioSource : StaticAudioSource, IAudioDataReceiver, IAudioDistanceAdjustable
{
    private IAudioStreamReceiver[] audioStreamReceivers = new IAudioStreamReceiver[0];

    private float volume = 0;
    private int flags = 0;
    public float VolumeMultiplier = 1.0f;

    public void ReceiveAudioData(long referenceId, byte[] data, int length, float volume, int flags)
    {
        foreach (IAudioStreamReceiver receiver in audioStreamReceivers)
        {
            receiver.ReceiveAudioStreamData(data, length);
        }

        float newVolume = volume * VolumeMultiplier;
        if (!Mathf.Approximately(this.volume, newVolume))
        {
            this.volume = newVolume;
            GameAudioSource.SourceVolume = this.volume;
        }

        if (this.flags != flags)
        {
            this.flags = flags;

            // Apply internals audio effects
            bool hasInternals = (flags & 1) != 0;
            GetComponent<AudioLowPassFilter>().enabled = hasInternals;
            GetComponent<AudioReverbFilter>().enabled = hasInternals;

            // Adjust distance based on voice flags
            SetVoiceModeDistance(flags);
        }
    }

    private void Start()
    {
        try
        {
            IAudioParent audioparent = transform.parent.GetComponent<Human>() as IAudioParent;
            GameAudioSource.Init(audioparent);
        }
        catch (Exception ex)
        {
            //Debug.log("GameAudioSource.Init failed " + ex);
        }

        GameAudioSource.AudioSource.outputAudioMixerGroup = AudioManager.Instance.GetMixerGroup(Animator.StringToHash("External"));
        GameAudioSource.CurrentMixerGroupNameHash = Animator.StringToHash("External");
        GameAudioSource.SetSpatialBlend(1);
        GameAudioSource.ManageOcclusion(true);
        GameAudioSource.CalculateAndSetAtmosphericVolume(true);
        GameAudioSource.SetEnabled(true);
        this.SetEnable(true);

        Singleton<AudioManager>.Instance.AddPlayingAudioSource(GameAudioSource);

        audioStreamReceivers = GetComponents<IAudioStreamReceiver>();
    }

    public void SetVoiceModeDistance(int flags)
    {
        if (GameAudioSource?.AudioSource == null) return;

        AudioFlags voiceFlag = (AudioFlags)(flags & (int)(AudioFlags.VoiceWhisper | AudioFlags.VoiceNormal | AudioFlags.VoiceShout));
        float distance = voiceFlag switch
        {
            AudioFlags.VoiceWhisper => 5f,
            AudioFlags.VoiceShout => 80f,
            AudioFlags.VoiceNormal => 20f,
            _ => 20f // fallback
        };

        GameAudioSource.AudioSource.maxDistance = distance;
    }
}
