using HarmonyLib;
using System.Linq;
using UnityEngine;
using StationeersMods.Interface;
using BrainClock.PlayerComms;

[StationeersMod("StationeersPlayerCommunications", "StationeersPlayerCommunications [StationeersMods]", "0.2.4657.21547.1")]
public class StationeersPlayerCommunications : ModBehaviour
{
    /// <summary>
    /// StationeersMods/BepinEx ModBehaviour to handle the mod initialization.
    /// </summary>
    /// <param name="contentHandler">Contains the assets of the mod package</param>
    public override void OnLoaded(ContentHandler contentHandler)
    {
        UnityEngine.Debug.Log("StationeersPlayerCommunications setup");
        
        // Configuration setup.
        // configBool = Config.Bind("Input",
        //     "Boolean",
        //     true,
        //     "Boolean description");
        
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

        // Add the content (Thing) prefabs to the game.
        PrefabPatch.prefabs = contentHandler.prefabs;
        harmony.PatchAll();

        UnityEngine.Debug.Log("+ Patching complete, setup finished");
    }
}
