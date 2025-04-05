using FistVR;
using HarmonyLib;
using TNHFramework.Utilities;

namespace TNHFramework.Patches
{
    public static class HighScorePatches
    {
        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool StartOfGamePatch(TNH_Manager __instance, bool ___m_hasInit)
        {
            if (!___m_hasInit && __instance.AIManager.HasInit)
            {
                TNHFrameworkLogger.Log("Delayed init", TNHFrameworkLogger.LogType.TNH);
            }
            
            return true;
        }

        [HarmonyPatch(typeof(TNH_Manager), "InitPlayerPosition")]
        [HarmonyPrefix]
        public static bool TrackPlayerSpawnPatch()
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
        public static bool TrackHoldStart()
        {
            TNHFrameworkLogger.Log("Hold Start", TNHFrameworkLogger.LogType.TNH);
            return true;
        }

        [HarmonyPatch(typeof(TNH_GunRecycler), "Button_Recycler")]
        [HarmonyPrefix]
        public static bool TrackRecyclePatch()
        {
            TNHFrameworkLogger.Log("Recycle button", TNHFrameworkLogger.LogType.TNH);
            return true;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "TestVisited")]
        [HarmonyPrefix]
        public static bool TrackSupplyVisitsPatch(TNH_SupplyPoint __instance, ref bool __result, ref bool ___m_hasBeenVisited, ref TAH_ReticleContact ___m_contact, bool ___m_isconfigured)
        {
            if (!___m_isconfigured)
            {
                __result = false;
                return false;
            }

            bool flag = __instance.TestVolumeBool(__instance.Bounds, GM.CurrentPlayerBody.transform.position);
            if (flag)
            {
                if (!___m_hasBeenVisited && ___m_contact != null)
                {
                    ___m_contact.SetVisited(true);
                    TNHFrameworkLogger.Log("Visiting supply", TNHFrameworkLogger.LogType.TNH);
                }
                ___m_hasBeenVisited = true;
            }

            __result = flag;
            return false;
        }


        [HarmonyPatch(typeof(TNH_ScoreDisplay), "UpdateHighScoreCallbacks")]
        [HarmonyPrefix]
        public static bool RequestScores(TNH_ScoreDisplay __instance, ref bool ___m_doRequestScoresTop, ref bool ___m_doRequestScoresPlayer)
        {
            // Custom TNH scoreboard is permanently offline, and official scoreboard doesn't support custom characters
            // Local scores still work
            ___m_doRequestScoresTop = false;
            ___m_doRequestScoresPlayer = false;

            return false;
        }

        [HarmonyPatch(typeof(TNH_ScoreDisplay), "SubmitScoreAndGoToBoard")]
        [HarmonyPrefix]
        public static bool PreventScoring(TNH_ScoreDisplay __instance, string ___m_curSequenceID, ref bool ___m_hasCurrentScore, ref int ___m_currentScore, int score)
        {
            TNHFrameworkLogger.Log("Preventing vanilla score submission", TNHFrameworkLogger.LogType.TNH);

            GM.Omni.OmniFlags.AddScore(___m_curSequenceID, score);

            ___m_hasCurrentScore = true;
            ___m_currentScore = score;

            // Draw local scores
            __instance.RedrawHighScoreDisplay(___m_curSequenceID);

            GM.Omni.SaveToFile();

            return false;
        }
    }
}
