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
        [HarmonyPatch("CompletePhase")]
        [HarmonyPrefix]
        public static bool NextPhasePatch(TNH_HoldPoint __instance, ref int ___m_phaseIndex, ref TNH_HoldPointSystemNode ___m_systemNode,
            float ___m_tickDownToFailure, ref List<Transform> ___m_validSpawnPoints, ref TNH_HoldPoint.HoldState ___m_state, ref float ___m_tickDownTransition,
            bool ___m_hasBeenDamagedThisPhase)
        {
            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
            var curLevel = (TNH_Progression.Level)typeof(TNH_Manager).GetField("m_curLevel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance.M);

            if (character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].DespawnBetweenWaves)
            {
                //__instance.DeletionBurst();
                var miDeletionBurst = __instance.GetType().GetMethod("DeletionBurst", BindingFlags.Instance | BindingFlags.NonPublic);
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
                var miIdentifyEncryption = __instance.GetType().GetMethod("IdentifyEncryption", BindingFlags.Instance | BindingFlags.NonPublic);
                miIdentifyEncryption.Invoke(__instance, []);
            }
            else
            {
                ___m_state = TNH_HoldPoint.HoldState.Transition;
                ___m_tickDownTransition = 5f;
                ___m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
            }

            //__instance.LowerAllBarriers();
            var miLowerAllBarriers = __instance.GetType().GetMethod("LowerAllBarriers", BindingFlags.Instance | BindingFlags.NonPublic);
            miLowerAllBarriers.Invoke(__instance, []);

            if (!___m_hasBeenDamagedThisPhase)
            {
                __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }

            return false;
        }
    }
}
