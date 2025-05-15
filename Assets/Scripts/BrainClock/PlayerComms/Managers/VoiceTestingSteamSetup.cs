using Steamworks;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Class to test Voice Capture and Playback. Initializes Steam and the 
    /// voice components.
    /// </summary>

    public class VoiceTestingSteamSetup : MonoBehaviour
    {
        public SteamVoiceRecorder SteamVoiceRecorder;
        public AudioStreamToAudioClip AudioStreamToAudioClip;

        // Stationeers AppId
        private uint AppId = 544550U;

        // Start is called before the first frame update
        void Start()
        {
            if (!SteamClient.IsValid)
            {
                // Make sure Steam is intialized to operate Asynchronously
                SteamClient.Init(AppId, true);
                //Debug.log("SteamAPI initialized");
            }
        }

        private void OnDestroy()
        {
            SteamClient.Shutdown();
        }

    }
}
