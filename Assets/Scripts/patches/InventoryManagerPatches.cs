using UnityEngine;
using HarmonyLib;
using Assets.Scripts.Networking;

namespace BrainClock.PlayerComms
{

    [HarmonyPatch(typeof(Assets.Scripts.Inventory.InventoryManager), "ManagerAwake")]
    public class InventoryManagerPatch
    {
        public static GameObject PlayerCommunicationsManagerPrefab;
        static void Postfix(Assets.Scripts.Inventory.InventoryManager __instance)
        {
            if (PlayerCommunicationsManagerPrefab != null && !NetworkManager.IsServer)
            {
                GameObject.Instantiate(PlayerCommunicationsManagerPrefab, Vector3.zero, Quaternion.identity, __instance.transform);
                Debug.Log("PlayerCommunicationsManagerPrefab spawned after ManagerAwake()");
            }
            else
            {
                Debug.LogError("Failed to load PlayerCommunicationsManagerPrefab!");
            }


        }
    }

}
