using FistVR;
using HarmonyLib;
using TNHFramework.Utilities;

namespace TNHFramework.Patches
{
    public static class HighScorePatches
    {
        [HarmonyPatch(typeof(TNH_ScoreDisplay), "UpdateHighScoreCallbacks")]
        [HarmonyPrefix]
        public static bool RequestScores(ref bool ___m_doRequestScoresTop, ref bool ___m_doRequestScoresPlayer)
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
