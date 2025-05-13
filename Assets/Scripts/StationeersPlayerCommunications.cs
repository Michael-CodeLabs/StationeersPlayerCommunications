using BepInEx.Configuration;
using BrainClock.PlayerComms;
using HarmonyLib;
using StationeersMods.Interface;
using System.Linq;
using UnityEngine;

[StationeersMod("StationeersPlayerCommunications", "StationeersPlayerCommunications [StationeersMods]", "0.2.4657.21547.1")]
public class StationeersPlayerCommunications : ModBehaviour
{
    /// <summary>
    /// StationeersMods/BepinEx ModBehaviour to handle the mod initialization.
    /// </summary>
    /// <param name="contentHandler">Contains the assets of the mod package</param>

    public static KeyCode PushToTalk;
    public static KeyCode VoiceStrength;
    public static KeyCode RadioVolumeDown;
    public static KeyCode RadioVolumeUp;
    public static KeyCode RadioChannelDown;
    public static KeyCode RadioChannelUp;

    public static ConfigEntry<bool> TransmissionModeConfig; // true = PushToTalk

    public override void OnLoaded(ContentHandler contentHandler)
    {
        Debug.Log("StationeersPlayerCommunications Loaded!");

        // Bind the config
        TransmissionModeConfig = Config.Bind(
            "Player Communications",
            "TransmissionMode",
            true, // default: PushToTalk
            "Sets the voice transmission mode. True = Push To Talk, False = Continuous.");

        Harmony harmony = new Harmony("StationeersPlayerCommunications");

        // The InventoryManager patch will spawn our main Manager into the 
        // game alongside the rest of game managers. This manager is created
        // after the game has loaded all files/resources and will survive 
        // between games.
        Debug.Log("+ Queueing the spawn of managers");

        var targetPrefab = contentHandler.prefabs.FirstOrDefault(prefab => prefab.name == "PlayerCommunicationsManagerPrefab");
        InventoryManagerPatch.PlayerCommunicationsManagerPrefab = targetPrefab;

        // Adding custom message types through an injector. The message Class
        // needs to be added to the factory in two lookup tables, and because
        // this message type needs to be processed in the client to it also 
        // needs an additional injection in the Network process() handler.

        Debug.Log("+ Injecting network messages");
        MessageFactoryInjector.InjectCustomMessageType(typeof(AudioClipMessage));
        //MessageFactoryInjector.InjectCustomMessageType(typeof(MorsePlayMessage));
        //MessageFactoryInjector.InjectCustomMessageType(typeof(IncomingTransmissionMessage));
        // Add the content (Thing) prefabs to the game.
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
}