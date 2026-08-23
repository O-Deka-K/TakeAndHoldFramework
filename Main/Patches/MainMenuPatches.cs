using FistVR;
using HarmonyLib;

namespace TNHFramework.Patches
{
    static class MainMenuPatches
    {
        [HarmonyPatch(typeof(MainMenuScreen), "SetVersionText")]
        [HarmonyPostfix]
        private static void SetVersionText_Message(MainMenuScreen __instance)
        {
            if (GM.Version_UpdateNumber == 119 && GM.Version_PatchNumber > 0 || GM.Version_UpdateNumber > 119)
            {
                string text = __instance.VersionNumberText1.text.Split('\n')[0];
                __instance.VersionNumberText1.text = text + "\nThis version of TNHFramework is for Update 119 (NOT 119p5)\nDo NOT use on Main or Experimental!";
            }
        }
    }
}
