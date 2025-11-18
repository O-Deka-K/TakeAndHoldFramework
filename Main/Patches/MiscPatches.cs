using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace TNHFramework.Patches
{
    static class MiscPatches
    {
        private static readonly MethodInfo miUpdateSafetyGeo = typeof(TubeFedShotgun).GetMethod("UpdateSafetyGeo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miToggleFireSelector = typeof(OpenBoltReceiver).GetMethod("ToggleFireSelector", BindingFlags.Instance | BindingFlags.NonPublic);

        // Anton pls fix - Wrong sound plays when purchasing a clip at the new ammo reloader panel
        [HarmonyPatch(typeof(TNH_AmmoReloader2), "Button_SpawnClip")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Button_SpawnClip_AudioFix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = [.. instructions];

            // Find the insertion index
            int insertIndex = -1;
            for (int i = 0; i < code.Count - 2; i++)
            {
                // Search for "if (obj.CompatibleClips.Count > 0)"
                if (code[i].opcode == OpCodes.Ldfld &&
                    code[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                    code[i + 2].opcode == OpCodes.Ble)
                {
                    insertIndex = i + 3;
                    break;
                }
            }

            // If that failed, then just look for the first branch instruction
            if (insertIndex == -1)
            {
                for (int i = 0; i < code.Count; i++)
                {
                    // Search for ble
                    if (code[i].opcode == OpCodes.Ble)
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
            }

            // Set flag = true so that AudEvent_Spawn is played instead of AudEvent_Fail
            List<CodeInstruction> codeToInsert =
            [
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Stloc_0),
            ];

            // Insert the code
            if (insertIndex > -1)
            {
                code.InsertRange(insertIndex, codeToInsert);
            }

            return code;
        }

        // Anton pls fix - Don't play line to advance to next node when completing last hold
        [HarmonyPatch(typeof(TNH_HoldPoint), "CompleteHold")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CompleteHold_LineFix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = [.. instructions];

            for (int i = 0; i < code.Count - 3; i++)
            {
                // Search for "EnqueueLine(TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt)" and remove it
                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i + 1].opcode == OpCodes.Ldfld &&
                    code[i + 2].opcode == OpCodes.Ldc_I4_S && code[i + 2].operand.Equals((sbyte)90) &&
                    code[i + 3].opcode == OpCodes.Callvirt)
                {
                    code[i].opcode = OpCodes.Nop;
                    code[i + 1].opcode = OpCodes.Nop;
                    code[i + 2].opcode = OpCodes.Nop;
                    code[i + 3].opcode = OpCodes.Nop;
                    break;
                }
            }

            return code;
        }

        // Anton pls fix - Don't play line to advance to next node when completing last hold
        [HarmonyPatch(typeof(TNH_Manager), "HoldPointCompleted")]
        [HarmonyPostfix]
        public static void HoldPointCompleted_LineFix(TNH_Manager __instance, int ___m_level, int ___m_maxLevels)
        {
            // Play this only if it's NOT the last level
            if (___m_level < ___m_maxLevels)
            {
                __instance.EnqueueLine(TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt);
            }
        }

        // Anton pls fix - Pump action shotgun config not working
        [HarmonyPatch(typeof(TubeFedShotgun), "SetLoadedChambers")]
        [HarmonyPostfix]
        public static void SetLoadedChambers_SetExtractor(TubeFedShotgun __instance, ref bool ___m_isChamberRoundOnExtractor, ref FVRFirearmMovingProxyRound ___m_proxy)
        {
            if (__instance.Chamber.IsFull)
            {
                ___m_isChamberRoundOnExtractor = true;
                ___m_proxy.ClearProxy();
            }

        }

        // Anton pls fix - Pump action shotgun config not working
        [HarmonyPatch(typeof(TubeFedShotgun), "ConfigureFromFlagDic")]
        [HarmonyPostfix]
        public static void ConfigureFromFlagDic_CheckLock(TubeFedShotgun __instance, bool ___m_isHammerCocked, ref bool ___m_isSafetyEngaged, Dictionary<string, string> f)
        {
            if (__instance.Mode == TubeFedShotgun.ShotgunMode.PumpMode)
            {
                if (___m_isHammerCocked)
                {
                    if (__instance.HasHandle)
                        __instance.Handle.LockHandle();
                }
            }

            if (__instance.HasSafety)
            {
                if (f.ContainsKey("SafetyState"))
                {
                    if (f["SafetyState"] == "Off")
                        ___m_isSafetyEngaged = false;

                    //__instance.UpdateSafetyGeo();
                    miUpdateSafetyGeo.Invoke(__instance, []);
                }
            }
        }

        // Anton pls fix - OpenBoltReceiver doesn't even HAVE an override for ConfigureFromFlagDic(), so fire selector and bolt state can't be set there
        [HarmonyPatch(typeof(OpenBoltReceiver), "SetLoadedChambers")]
        [HarmonyPrefix]
        public static bool SetLoadedChambers_FireSelect(OpenBoltReceiver __instance, List<FireArmRoundClass> rounds)
        {
            // Kludge. Since open bolt guns are never saved with chambered rounds, we can edit the vault file to add one to trigger this.
            // Note that a round will be taken from the magazine, so there's no actual +1 round.
            if (rounds.Count > 0)
            {
                //__instance.ToggleFireSelector();
                miToggleFireSelector.Invoke(__instance, []);
                __instance.Bolt.SetBoltToRear();
                __instance.BeginChamberingRound();
                __instance.ChamberRound();
            }

            return false;
        }
    }
}
