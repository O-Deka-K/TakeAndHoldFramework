using FistVR;
using HarmonyLib;
using TNHFramework.Utilities;

namespace TNHFramework.Patches
{
    public static class HighScorePatches
    {
        // The only reason for overriding this function is to get rid of the debug message "Requesting High Score Chart".
        // The actual request happens in UpdateHighScoreCallbacks(), which is overridden below.
        [HarmonyPatch(typeof(TNH_ScoreDisplay), "RequestHighScoreChart")]
        [HarmonyPrefix]
        public static bool RequestHighScoreChart_Disable(TNH_ScoreDisplay __instance)
        {
            __instance.ClearGlobalHighScoreDisplay();
            return false;
        }

        [HarmonyPatch(typeof(TNH_ScoreDisplay), "UpdateHighScoreCallbacks")]
        [HarmonyPrefix]
        public static void UpdateHighScoreCallbacks_Disable(ref bool ___m_doRequestScoresTop, ref bool ___m_doRequestScoresPlayer)
        {
            // Custom TNHTweaker scoreboard is permanently offline
            // Local scores still work
            ___m_doRequestScoresTop = false;
            ___m_doRequestScoresPlayer = false;
        }

        [HarmonyPatch(typeof(TNH_ScoreDisplay), "SubmitScoreAndGoToBoard")]
        [HarmonyPrefix]
        public static bool SubmitScoreAndGoToBoard_PreventScoring(TNH_ScoreDisplay __instance, string ___m_curSequenceID, ref bool ___m_hasCurrentScore, ref int ___m_currentScore, int score)
        {
            TNHFrameworkLogger.Log("Preventing vanilla score submission", TNHFrameworkLogger.LogType.TNH);

            if (TNHFramework.SavedConfig.DisableScoring)
                return false;

            __instance.ClearGlobalHighScoreDisplay();
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
