using HarmonyLib;
using System.Collections.ObjectModel;
using Assets.Scripts;
using Assets.Scripts.UI;
using UnityEngine;
using System.Collections.Generic;

namespace BrainClock.PlayerComms
{

    /// <summary>
    /// The following patches will add all StationpediaInfo component in your project
    /// as factions into the game stationpedia.
    /// </summary>
    [HarmonyPatch]
    public static class StationpediaPatches
{
    public static ReadOnlyCollection<GameObject> prefabs { get; set; }

    /// <summary>
    /// Adds the actual Stationpedia page to the system.
    /// </summary>
    [HarmonyPatch]
    public static class PopulateFactionLorePages_Patch
    {
        [HarmonyPatch(typeof(Stationpedia), "PopulateFactionLorePages")]
        public static void Postfix(Stationpedia __instance)
        {
            Debug.Log("StationpediaPatches.PopulateFactionLorePages()");
            foreach (var gameObject in prefabs)
            {
                StationpediaInfo info = gameObject.GetComponent<StationpediaInfo>();
                if (info != null)
                {
                    StationpediaPage page = new StationpediaPage
                    {
                        Key = info.Key,
                        Title = Localization.GetInterface(info.Title, false),
                        Description = Localization.GetInterface(info.Description, false),
                        CustomSpriteToUse = info.Sprite,
                    };
                    page.ParsePage();

                    // Fallback set to true means the page will not be added if its key
                    // is already found in the list of pages.
                    Stationpedia.Register(page, true);
                    Debug.Log($"+ Added {info.Key} page to the Stationpedia.");
                }
            }
        }
    }

    /// <summary>
    /// Links the Stationpedia Page to the list of factions.
    /// </summary>

    [HarmonyPatch]
    public static class GenerateLoreList_Patch
    {
        /// <summary>
        /// Keep track of keys added to the stationpedia to avoid adding the same factions more than once.
        /// </summary>
        public static List<string> keys = new List<string>();

        [HarmonyPatch(typeof(Stationpedia), "GenerateLoreList")]
        public static void Postfix(Stationpedia __instance)
        {
            Debug.Log("StationpediaPatches.GenerateLoreList()");
            foreach (var gameObject in prefabs)
            {
                StationpediaInfo info = gameObject.GetComponent<StationpediaInfo>();
                if (info != null)
                {
                    if (keys.Contains(info.Key))
                        return;

                    StationCategoryInsert category = new StationCategoryInsert();
                    category.NameOfThing = Localization.GetInterface(info.Key, false);
                    category.PageLink = info.Key;
                    category.InsertImage = info.Sprite;
                    Stationpedia.DataHandler.AddNewListItem("Factions", "Factions", category);
                    Debug.Log($"+ Added {info.Key} reference to the faction list.");

                    keys.Add(info.Key);
                }
            }

        }
    }
}
}