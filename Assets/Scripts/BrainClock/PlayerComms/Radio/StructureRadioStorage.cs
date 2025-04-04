using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class StructureRadioStorage : Shelf
    {
        public override void Start()
        {
            base.Start();
            AssignToolExit();
        }

        private void AssignToolExit()
        {
            Item itemCrowbar = Prefab.Find<Item>("ItemCrowbar");

            if (itemCrowbar == null)
            {
                Debug.LogError("[BrainClock.PlayerComms] Failed to find ItemCrowbar!");
                return;
            }

            if (BuildStates != null && BuildStates.Count > 0)
            {
                BuildStates[0].Tool.ToolExit = itemCrowbar;
                Debug.Log("[BrainClock.PlayerComms] Successfully set ToolExit to ItemCrowbar.");
            }
            else
            {
                Debug.LogWarning("[BrainClock.PlayerComms] StructureRadioHolder has no BuildStates!");
            }
        }
    }
}
