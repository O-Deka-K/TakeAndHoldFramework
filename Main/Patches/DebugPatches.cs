using FistVR;
using HarmonyLib;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework
{
    public class DebugPatches
    {
        [HarmonyPatch(typeof(TNH_Manager), "Start")]
        [HarmonyPrefix]
        public static void AddPointDebugText(TNH_Manager __instance)
        {
            foreach (TNH_HoldPoint hold in __instance.HoldPoints)
            {
                TNHFrameworkLogger.Log("Adding text!", TNHFrameworkLogger.LogType.TNH);

                GameObject canvas = new("Canvas");
                canvas.transform.rotation = Quaternion.LookRotation(Vector3.right);
                canvas.transform.position = hold.SpawnPoint_SystemNode.position + Vector3.up * 0.2f;

                Canvas canvasComp = canvas.AddComponent<Canvas>();
                RectTransform rect = canvasComp.GetComponent<RectTransform>();
                canvasComp.renderMode = RenderMode.WorldSpace;
                rect.sizeDelta = new Vector2(1, 1);

                GameObject text = new("Text");
                text.transform.SetParent(canvas.transform);
                text.transform.rotation = canvas.transform.rotation;
                text.transform.localPosition = Vector3.zero;

                text.AddComponent<CanvasRenderer>();
                Text textComp = text.AddComponent<Text>();
                Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

                textComp.text = "Hold " + __instance.HoldPoints.IndexOf(hold);
                textComp.alignment = TextAnchor.MiddleCenter;
                textComp.fontSize = 32;
                text.transform.localScale = new Vector3(0.0015f, 0.0015f, 0.0015f);
                textComp.font = ArialFont;
                textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
        }
    }
}
