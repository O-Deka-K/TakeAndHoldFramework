using FistVR;
using HarmonyLib;
using TNHFramework.Utilities;

namespace TNHFramework.Patches
{
    public static class HighScorePatches
    {

        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool StartOfGamePatch(TNH_Manager __instance)
        {
            if (!__instance.m_hasInit && __instance.AIManager.HasInit)
            {
                TNHFrameworkLogger.Log("Delayed init", TNHFrameworkLogger.LogType.TNH);
            }
            
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "InitPlayerPosition")]
        [HarmonyPrefix]
        public static bool TrackPlayerSpawnPatch(TNH_Manager __instance)
        {
            TNHFrameworkLogger.Log("Spawned Player", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "HoldPointCompleted")]
        [HarmonyPrefix]
        public static bool TrackHoldCompletion()
        {
            TNHFrameworkLogger.Log("Hold Completion", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetLevel")]
        [HarmonyPrefix]
        public static bool TrackNextLevel()
        {
            TNHFrameworkLogger.Log("Set Level", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
        [HarmonyPrefix]
        public static bool TrackDeath()
        {
            TNHFrameworkLogger.Log("Died", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
        [HarmonyPrefix]
        public static bool TrackVictory()
        {
            TNHFrameworkLogger.Log("Victory", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginHoldChallenge")]
        [HarmonyPrefix]
        public static bool TrackHoldStart(TNH_HoldPoint __instance)
        {
            TNHFrameworkLogger.Log("Hold Start", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_GunRecycler), "Button_Recycler")]
        [HarmonyPrefix]
        public static bool TrackRecyclePatch(TNH_GunRecycler __instance)
        {
            TNHFrameworkLogger.Log("Recycle button", TNHFrameworkLogger.LogType.TNH);
            return true;
        }


        [HarmonyPatch(typeof(TNH_SupplyPoint), "TestVisited")]
        [HarmonyPrefix]
        public static bool TrackSupplyVisitsPatch(TNH_SupplyPoint __instance, ref bool __result)
        {
            if (!__instance.m_isconfigured)
            {
                __result = false;
                return false;
            }

            bool flag = __instance.TestVolumeBool(__instance.Bounds, GM.CurrentPlayerBody.transform.position);
            if (flag)
            {
                if (!__instance.m_hasBeenVisited && __instance.m_contact != null)
                {
                    __instance.m_contact.SetVisited(true);
                    TNHFrameworkLogger.Log("Visiting supply", TNHFrameworkLogger.LogType.TNH);
                }
                __instance.m_hasBeenVisited = true;
            }

            __result = flag;
            return false;
        }
        

        [HarmonyPatch(typeof(TNH_ScoreDisplay), "UpdateHighScoreCallbacks")]
        [HarmonyPrefix]
        public static bool RequestScores(TNH_ScoreDisplay __instance)
        {
            // Custom TNH scoreboard is permanently offline, and official scoreboard doesn't support custom characters
            // Local scores still work
            __instance.m_doRequestScoresTop = false;
            __instance.m_doRequestScoresPlayer = false;

            return false;
        }
    }
}
