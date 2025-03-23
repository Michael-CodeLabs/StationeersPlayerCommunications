using System;
using BrainClock.PlayerComms;
using HarmonyLib;
using System.Linq;
using StationeersMods.Interface;
using UnityEngine;
using Assets.Scripts.Networking;

[StationeersMod("StationeersPlayerCommunications","StationeersPlayerCommunications [StationeersMods]","0.2.4657.21547.1")]
public class StationeersPlayerCommunications : ModBehaviour
{
    // private ConfigEntry<bool> configBool;
    
    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("StationeersPlayerCommunications.OnLoaded()");
        
        //Config example
        // configBool = Config.Bind("Input",
        //     "Boolean",
        //     true,
        //     "Boolean description");
        
        Harmony harmony = new Harmony("StationeersPlayerCommunications");

        InventoryManagerPatch.VoiceTestPrefab = contentHandler.prefabs.FirstOrDefault(prefab => prefab.name == "VoiceTesting");

        PrefabPatch.prefabs = contentHandler.prefabs;
        harmony.PatchAll();
        UnityEngine.Debug.Log("StationeersPlayerCommunications Loaded with " + contentHandler.prefabs.Count + " prefab(s)");

        // Adding custom message type
        Debug.Log("MessageFactoryInjector injectiong VoiceMessage");
        MessageFactoryInjector.InjectCustomMessageType(typeof(VoiceMessage));

    }
}
