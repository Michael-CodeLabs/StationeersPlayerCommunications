using Assets.Scripts.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BrainClock.PlayerComms
{
    [HarmonyPatch(typeof(PlayerStateWindow))]
    public class VolumeUIPatch
    {
        private static TextMeshProUGUI _voiceModeText;

        [HarmonyPatch(typeof(PlayerStateWindow), "Update")]
        [HarmonyPostfix]
        public static void PlayerStateWindowUpdatePostfix(PlayerStateWindow __instance)
        {
            if (__instance == null || __instance.InfoExternal == null || __instance.Parent == null)
                return;

            Transform uiTransform = __instance.InfoExternal.transform;

            if (_voiceModeText == null)
            {
                _voiceModeText = CreateVoiceModeText(uiTransform, __instance);
            }

            if (PlayerCommunicationsManager.Instance != null)
            {
                var voiceMode = PlayerCommunicationsManager.Instance.GetCurrentVoiceMode();
                _voiceModeText.SetText($"<size=18>VOICE: <b>{voiceMode.ToString().ToUpper()}</b></size>", true);
                
            }
        }
        private static TextMeshProUGUI CreateVoiceModeText(Transform parentTransform,PlayerStateWindow window)
        {
            foreach (TextMeshProUGUI existing in parentTransform.GetComponentsInChildren<TextMeshProUGUI>())
            {
                if (existing.name.Equals("VoiceModeText"))
                    Object.Destroy(existing.gameObject);
            }

            GameObject obj = new GameObject("VoiceModeText");
            obj.SetActive(false);

            var text = obj.AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(parentTransform, false);
            text.name = "VoiceModeText";
            text.font = window.InfoExternalPressure.font;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.TopRight;
            text.color = Color.white;
            text.margin = new Vector4(10f, 20f, 10f, 10f);
            text.autoSizeTextContainer = true;

            obj.SetActive (true);
            return text;
        }
    }
}
