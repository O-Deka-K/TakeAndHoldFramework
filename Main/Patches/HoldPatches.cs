using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using UnityEngine;

namespace TNHFramework.Patches
{
    [HarmonyPatch(typeof(TNH_HoldPoint))]
    public class HoldPatches
    {
        private static readonly MethodInfo miDeletionBurst = typeof(TNH_HoldPoint).GetMethod("DeletionBurst", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miIdentifyEncryption = typeof(TNH_HoldPoint).GetMethod("IdentifyEncryption", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miLowerAllBarriers = typeof(TNH_HoldPoint).GetMethod("LowerAllBarriers", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiCurLevel = typeof(TNH_Manager).GetField("m_curLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch("CompletePhase")]
        [HarmonyPrefix]
        public static bool NextPhasePatch(TNH_HoldPoint __instance, ref int ___m_phaseIndex, ref TNH_HoldPointSystemNode ___m_systemNode,
            float ___m_tickDownToFailure, ref List<Transform> ___m_validSpawnPoints, ref TNH_HoldPoint.HoldState ___m_state, ref float ___m_tickDownTransition,
            bool ___m_hasBeenDamagedThisPhase)
        {
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            var curLevel = (TNH_Progression.Level)fiCurLevel.GetValue(__instance.M);

            if (character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].DespawnBetweenWaves)
            {
                //__instance.DeletionBurst();
                miDeletionBurst.Invoke(__instance, []);
                __instance.M.ClearMiscEnemies();
                UnityEngine.Object.Instantiate(__instance.VFX_HoldWave, ___m_systemNode.NodeCenter.position, ___m_systemNode.NodeCenter.rotation);
            }

            if (character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].UsesVFX)
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, __instance.AUDEvent_HoldWave, __instance.transform.position);
                __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
            }

            __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldDecisecondsRemaining, (int)(___m_tickDownToFailure * 10f));
            ___m_phaseIndex++;

            if (character.GetCurrentLevel(curLevel).HoldPhases.Count > ___m_phaseIndex &&
                character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].ScanTime == 0 && 
                character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].WarmupTime == 0)
            {
                __instance.SpawnPoints_Targets.Shuffle();
                ___m_validSpawnPoints.Shuffle();
                //__instance.IdentifyEncryption();
                miIdentifyEncryption.Invoke(__instance, []);
            }
            else
            {
                ___m_state = TNH_HoldPoint.HoldState.Transition;
                ___m_tickDownTransition = 5f;
                ___m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
            }

            //__instance.LowerAllBarriers();
            miLowerAllBarriers.Invoke(__instance, []);

            if (!___m_hasBeenDamagedThisPhase)
            {
                __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }

            return false;
        }
    }
}
