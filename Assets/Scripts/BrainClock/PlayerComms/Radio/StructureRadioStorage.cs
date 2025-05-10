using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Structures;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    public class StructureRadioStorage : Shelf
    {
        private Vector3 ChildRotation = new Vector3(45f, 90f, 90f);

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
        public override void SetSlotOccupantTransformData(DynamicThing newChild)
        {
            if ((object)newChild != null)
            {
                newChild.ThingTransformLocalRotation = Quaternion.Euler(ChildRotation + newChild.ChildSlotOffset);
                newChild.ThingTransformLocalPosition = newChild.ChildSlotOffsetPosition + new Vector3(0f, 0.09f, 0f);
            }
        }
    }
}
