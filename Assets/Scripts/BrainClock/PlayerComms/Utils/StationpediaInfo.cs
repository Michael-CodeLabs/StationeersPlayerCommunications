using UnityEngine;

namespace BrainClock.PlayerComms
{
    /// <summary>
    /// Class used to store Stationpedia Faction information.
    /// </summary>
    public class StationpediaInfo : MonoBehaviour
{
    [Tooltip("Unique Key of the faction, used to build links")]
    public string Key;

    [Tooltip("Localization key for the faction title")]
    public string Title;

    [Tooltip("Localization key for the faction description")]
    public string Description;

    [Tooltip("Sprite of the faction, high-res")]
    public Sprite Sprite;
}
}
