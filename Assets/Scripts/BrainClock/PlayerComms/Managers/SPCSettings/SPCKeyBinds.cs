using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Assets.Scripts.UI;

namespace BrainClock.PlayerComms
{
    class SPCKeybinds
    {
        [HarmonyPatch(typeof(KeyManager), "SetupKeyBindings")]
        class PlayerCommsKeybinds
        {
            public static void Postfix()
            {
                ControlsGroup controlsGroup9 = new ControlsGroup("Player Communications");
                KeyManager.AddGroupLookup(controlsGroup9);

                PlayerCommsKeybinds.AddKey("Push To Talk", KeyCode.B, controlsGroup9, false);
                PlayerCommsKeybinds.AddKey("Voice Strength", KeyCode.Semicolon, controlsGroup9, false);
                PlayerCommsKeybinds.AddKey("Radio Volume Down", KeyCode.DownArrow, controlsGroup9, false);
                PlayerCommsKeybinds.AddKey("Radio Volume Up", KeyCode.UpArrow, controlsGroup9, false);
                PlayerCommsKeybinds.AddKey("Radio Channel Down", KeyCode.LeftArrow, controlsGroup9, false);
                PlayerCommsKeybinds.AddKey("Radio Channel Up", KeyCode.RightArrow, controlsGroup9, false);
                ControlsAssignment.RefreshState();
            }

            private static void AddKey(string assignmentName, KeyCode keyCode, ControlsGroup controlsGroup, bool hidden = false)
            {
                // Add to the game's control group lookup so it shows up in the keybinding menu under your category
                Dictionary<string, ControlsGroup> controlsGroupLookup = Traverse.Create(typeof(KeyManager)).Field("_controlsGroupLookup").GetValue() as Dictionary<string, ControlsGroup>;

                if (controlsGroupLookup != null)
                {
                    controlsGroupLookup[assignmentName] = controlsGroup;
                    Traverse.Create(typeof(KeyManager)).Field("_controlsGroupLookup").SetValue(controlsGroupLookup);
                }
                else
                {
                    //Debug.logWarning("SPCKeybinds: _controlsGroupLookup is null. Key assignment may not appear in UI.");
                }

                // Create the key item and register it in the KeyManager
                KeyItem keyItem = new KeyItem(assignmentName, keyCode, hidden);
                KeyManager.KeyItemLookup[assignmentName] = keyItem;
                KeyManager.AllKeys.Add(keyItem);
            }
        }
    }
}