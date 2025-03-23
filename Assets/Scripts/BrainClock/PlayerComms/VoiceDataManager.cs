using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Sound;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RootMotion.FinalIK.VRIK;

namespace BrainClock.PlayerComms
{
    public class VoiceDataManager : MonoBehaviour
    {
        public static GameObject HumanVoicePrefab;

        public static VoiceDataManager Instance;

        public static Dictionary<long, VoiceDataToAudioClip> HumanAudioSources;

        // Called from every VoiceDataToAudioClip component to remove itself from the list OnDestroy();
        public void RemoveHumanAudioSource(VoiceDataToAudioClip obj)
        {
            long keyToRemove = HumanAudioSources.FirstOrDefault(x => x.Value == obj).Key;
            if (HumanAudioSources.ContainsKey(keyToRemove))
            {
                HumanAudioSources.Remove(keyToRemove);
            }
        }

        public VoiceDataToAudioClip GetHumanAudioSource(long Id)
        {
            return HumanAudioSources.TryGetValue(Id, out VoiceDataToAudioClip value) ? value : null;
        }

        private Human FindHumanEntity(long Id)
        {
            return Human.AllHumans.Find(p => p.ReferenceId == Id);
            
        }


        private void Awake()
        {
            Debug.Log("VoiceDataManager.Awake()");

            // Implement as proper Singleton
            Instance = this;
            HumanAudioSources = new Dictionary<long, VoiceDataToAudioClip>();
        }

        public void SendVoiceRecording(long referenceId, byte[] data, int Length)
        {
            Debug.Log($"VoiceDataManager.SendVoiceRecording({referenceId})");

            VoiceDataToAudioClip source = GetHumanAudioSource(referenceId);
            
            // Try to create the human audio source first
            // TODO: Move this to a human spawn event later.
            if (source == null)
            {
                source = CreateHumanVoicePrefab(referenceId);
            }

            if (source != null)
            {
                source.SendVoiceRecording(data, Length);
            }
        }

        VoiceDataToAudioClip CreateHumanVoicePrefab(long referenceId)
        {
            Debug.Log($"VoiceDataManager.CreateHumanVoicePrefab({referenceId})");

            VoiceDataToAudioClip voiceDataToAudioClip = null;

            // find human instance
            Human human = FindHumanEntity(referenceId);
            if (human == null)
                return null;

            // Instantiate voice source prefab
            Debug.Log($"Intancing HumanVoicePrefab for refId {referenceId}");
            GameObject go = Object.Instantiate(HumanVoicePrefab, human.transform);
            if (go == null)
                return null;
             
            Debug.Log($"Finding VoiceDataToAudioClip for refId {referenceId}");
            voiceDataToAudioClip = go.GetComponent<VoiceDataToAudioClip>();
            if (voiceDataToAudioClip == null)
                return null;

            StaticAudioSource staticAudioSource = go.GetComponent<StaticAudioSource>();
            staticAudioSource.GameAudioSource.CurrentMixerGroupNameHash = UnityEngine.Animator.StringToHash("External");

            // Add to the sources list
            Debug.Log($"Adding HumanAudioSource for refId {referenceId}");
            HumanAudioSources.Add(referenceId, voiceDataToAudioClip);

            return voiceDataToAudioClip;
        }

        private void OnDestroy()
        {
            Debug.Log("VoiceDataManager.OnDestroy()");

        }
    }
}
