using Assets.Scripts.Objects;
using HarmonyLib;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    [HarmonyPatch(typeof(Prefab), "LoadCorePrefabs")]
    public class AddStructureIntoKit
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // Find the kit and the structure
            MultiConstructor itemKitLocker = Prefab.Find<MultiConstructor>("ItemKitLocker");
            Structure structureRadioStorage = Prefab.Find<Structure>("StructureRadioStorage");

            // Debug log to check if they were found
            if (itemKitLocker == null)
            {
                Debug.LogError("[BrainClock.PlayerComms] Failed to find ItemKitLocker!");
                return;
            }
            if (structureRadioStorage == null)
            {
                Debug.LogError("[BrainClock.PlayerComms] Failed to find StructureRadioStorage!");
                return;
            }

            // Add the structure to the kit
            itemKitLocker.Constructables.Add(structureRadioStorage);
            Debug.Log("[BrainClock.PlayerComms] Successfully added StructureRadioStorage to ItemKitLocker!");

            // Ensure BuildStates exists before accessing it
            if (structureRadioStorage.BuildStates != null && structureRadioStorage.BuildStates.Count > 0)
            {
                structureRadioStorage.BuildStates[0].Tool.ToolEntry = itemKitLocker;
                Debug.Log("[BrainClock.PlayerComms] Successfully set ToolEntry for StructureRadioStorage.");
            }
        }
    }
}
