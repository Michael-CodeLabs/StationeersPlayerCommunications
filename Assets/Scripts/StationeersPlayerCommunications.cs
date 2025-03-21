using System;
using BrainClock.PlayerComms;
using HarmonyLib;
using StationeersMods.Interface;
[StationeersMod("StationeersPlayerCommunications","StationeersPlayerCommunications [StationeersMods]","0.2.4657.21547.1")]
public class StationeersPlayerCommunications : ModBehaviour
{
    // private ConfigEntry<bool> configBool;
    
    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("StationeersPlayerCommunications says: Hello World!");
        
        //Config example
        // configBool = Config.Bind("Input",
        //     "Boolean",
        //     true,
        //     "Boolean description");
        
        Harmony harmony = new Harmony("StationeersPlayerCommunications");
        PrefabPatch.prefabs = contentHandler.prefabs;
        harmony.PatchAll();
        UnityEngine.Debug.Log("StationeersPlayerCommunications Loaded with " + contentHandler.prefabs.Count + " prefab(s)");
    }
}
