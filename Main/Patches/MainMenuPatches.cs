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
            if (GM.Version_UpdateNumber < 120)
            {
                string text = __instance.VersionNumberText1.text.Split('\n')[0];
                __instance.VersionNumberText1.text = text + "\nThis version of TNHFramework is only for Update 120p1 and up\nTNHFramework has been DISABLED";
            }
        }
    }
}
