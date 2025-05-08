using Assets.Scripts.Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ilodev.stationeersmods.tools.visualizers
{
    [ExecuteAlways]
    public class SlotVisualizer : MonoBehaviour
    {
        [Header("Slot info")]
        public float InventoryScale = 1f;
        public Vector3 size = Vector3.one;
        public Color color = Color.white;

        [Header("Item info")]
        public Vector3 ChildSlotOffsetRotation = Vector3.zero;
        public Vector3 ChildSlotOffsetPosition = Vector3.zero;
        public GameObject Item;
        private GameObject spawnedItem = null;

        // Update is called once per frame
        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UpdateItem();
            }
#endif
        }

        void OnDrawGizmos()
        {
            Gizmos.color = color;
            Gizmos.DrawCube(Vector3.zero, size);
        }

        void OnValidate()
        {
            UpdateItem();
        }

        void UpdateItem()
        {
            // If Item is assigned and we don't have a spawned item yet ? spawn it
            if (Item != null && spawnedItem == null)
            {
                spawnedItem = GameObject.Instantiate(Item, transform);
                spawnedItem.name = Item.name;
            }

            // If Item is unassigned and a spawned item exists ? destroy it
            if (Item == null && spawnedItem != null)
            {
                DestroyImmediate(spawnedItem);
            }

            // If we have a spawned item, update its transform
            if (spawnedItem != null)
            {
                spawnedItem.transform.localPosition = ChildSlotOffsetPosition;
                spawnedItem.transform.localEulerAngles = ChildSlotOffsetRotation + new Vector3(45f, 90f, 90f);
                spawnedItem.transform.localScale = Vector3.one * InventoryScale;
            }
        }

    }
}