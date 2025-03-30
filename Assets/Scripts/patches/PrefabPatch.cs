using System;
using System.Collections.ObjectModel;
using Assets.Scripts;
using Assets.Scripts.Objects;
using HarmonyLib;
using StationeersMods.Interface;
using UnityEngine;
using UnityEngine.Rendering;
namespace BrainClock.PlayerComms
{
    [HarmonyPatch]
    public class PrefabPatch
    {
        public static ReadOnlyCollection<GameObject> prefabs { get; set; }
        [HarmonyPatch(typeof(Prefab), "LoadAll")]
        public static void Prefix()
        {
            try
            {
                Debug.Log("Prefab Patch started");
                foreach (var gameObject in prefabs)
                {
                    Thing thing = gameObject.GetComponent<Thing>();
                    // Additional patching goes here, like setting references to materials(colors) or tools from the game
                    if (thing != null)
                    {
                        // Replace all the paintable materials with the default ColorOrange
                        Material paintable = thing.PaintableMaterial;
                        if (paintable != null)
                        {
                            Debug.Log($"{gameObject.name} defines {paintable.name} as paintable material, setting up color.");
                            thing.CustomColor = GameManager.GetColorSwatch("ColorOrange");
                            Debug.Log($"Default custom color now is {thing.CustomColor.Name}");
                            thing.PaintableMaterial = thing.CustomColor.Normal;
                            foreach (var meshRender in gameObject.GetComponentsInChildren<MeshRenderer>())
                            {
                                if (meshRender.sharedMaterial == paintable)
                                    meshRender.sharedMaterial = thing.PaintableMaterial;

                                /*
                                meshRender.sharedMaterial = paintable;
                                for (int i = 0; i < meshRender.materials.Length; i++)
                                {
                                    if (meshRender.materials[i] == paintable)
                                    {
                                        meshRender.materials[i] = thing.PaintableMaterial;
                                    }
                                }
                                */
                            }
                        }


                        // Make it paintable and give it a default Orange Color
                        thing.CustomColor = GameManager.GetColorSwatch("ColorOrange");
                        //thing.PaintableMaterial = thing.CustomColor.Normal;


                        Debug.Log(gameObject.name + " added to WorldManager");
                        WorldManager.Instance.SourcePrefabs.Add(thing);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                Debug.LogException(ex);
            }
        }
    }
}
