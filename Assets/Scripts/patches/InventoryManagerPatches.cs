using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Util.Commands;

namespace BrainClock.PlayerComms
{

    [HarmonyPatch(typeof(Assets.Scripts.Inventory.InventoryManager), "ManagerAwake")]
    public class InventoryManagerPatch
    {
        public static GameObject VoiceTestPrefab;
        public static GameObject PlayerCommunicationsManagerPrefab;

        static void Postfix(Assets.Scripts.Inventory.InventoryManager __instance)
        {

            if (VoiceTestPrefab != null  && Application.platform != RuntimePlatform.WindowsServer)
            {
                GameObject.Instantiate(VoiceTestPrefab, Vector3.zero, Quaternion.identity, __instance.transform);
                Debug.Log("VoiceTestPrefab spawned after ManagerAwake()");
            }
            else
            {
                Debug.LogError("Failed to load VoiceTestPrefab!");
            }

            if (PlayerCommunicationsManagerPrefab != null && Application.platform != RuntimePlatform.WindowsServer)
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
