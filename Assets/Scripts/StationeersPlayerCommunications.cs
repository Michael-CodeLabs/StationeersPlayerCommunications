using Assets.Scripts.Networking;
using BepInEx.Configuration;
using BrainClock.PlayerComms;
using HarmonyLib;
using StationeersMods.Interface;
using System.Linq;
using UnityEngine;

[StationeersMod("StationeersPlayerCommunications", "StationeersPlayerCommunications", "0.2.4657.21547.1")]
public class StationeersPlayerCommunications : ModBehaviour
{
    public static KeyCode PushToTalk;
    public static KeyCode VoiceStrength;
    public static KeyCode RadioVolumeDown;
    public static KeyCode RadioVolumeUp;
    public static KeyCode RadioChannelDown;
    public static KeyCode RadioChannelUp;
    public static ConfigEntry<bool> TransmissionModeConfig; // true = PushToTalk
    public static ConfigEntry<float> VoiceVolume;
    public static ConfigEntry<float> HumanVolumeMultiplier;
    public static ConfigEntry<float> RadioVolumeMultipler;

    public override void OnLoaded(ContentHandler contentHandler)
    {
        Debug.Log("StationeersPlayerCommunications Loaded!");
        if (!NetworkManager.IsServer)
        {
            // Bind the config
            TransmissionModeConfig = Config.Bind(
                "Player Communications",
                "TransmissionMode",
                true, // default: PushToTalk
                "Sets the voice transmission mode. True = Push To Talk, False = Continuous.");

            HumanVolumeMultiplier = Config.Bind(
                "Player Communications",
                "Human Volume Multiplier",
                2f, // default: 2f
                "Sets Human Volume Multiplier");

            RadioVolumeMultipler = Config.Bind(
                "Player Communications",
                "Radio Volume Multiplier",
                2f, // default: 2f
                "Sets Radio Volume Multiplier");
        }

        Harmony harmony = new("StationeersPlayerCommunications");

        Debug.Log("+ Queueing the spawn of managers");

        var targetPrefab = contentHandler.prefabs.FirstOrDefault(prefab => prefab.name == "PlayerCommunicationsManagerPrefab");
        InventoryManagerPatch.PlayerCommunicationsManagerPrefab = targetPrefab;

        Debug.Log("+ Injecting network messages");
        MessageFactoryInjector.InjectCustomMessageType(typeof(AudioClipMessage));

        PrefabPatch.prefabs = contentHandler.prefabs;
        StationpediaPatches.prefabs = contentHandler.prefabs;
        harmony.PatchAll();

        Debug.Log("+ Patching complete, setup finished");

        KeyManager.OnControlsChanged += ControlsChangedEvent;
    }

    private static void ControlsChangedEvent()
    {
        PushToTalk = KeyManager.GetKey("Push To Talk");
        VoiceStrength = KeyManager.GetKey("Voice Strength");
        RadioVolumeDown = KeyManager.GetKey("Radio Volume Down");
        RadioVolumeUp = KeyManager.GetKey("Radio Volume Up");
        RadioChannelDown = KeyManager.GetKey("Radio Channel Down");
        RadioChannelUp = KeyManager.GetKey("Radio Channel Up");
    }

    public static void AudioConfigChanged(string target, float value)
    {
        // Update the config values based on the target
        switch (target.ToLowerInvariant())
        {
            case "human":
                HumanVolumeMultiplier.Value = value;
                //Debug.Log($"[SPC] Config updated: Human Multiplier = {value}");
                break;

            case "radio":
                RadioVolumeMultipler.Value = value;
                //Debug.Log($"[SPC] Config updated: Radio Multipler = {value}");
                break;

            default:
                Debug.LogWarning($"[SPC] Unknown config target: {target}");
                break;
        }
    }
}