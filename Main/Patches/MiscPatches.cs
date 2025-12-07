using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class MiscPatches
    {
        private static readonly MethodInfo miUpdateSafetyGeo = typeof(TubeFedShotgun).GetMethod("UpdateSafetyGeo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miToggleFireSelector = typeof(OpenBoltReceiver).GetMethod("ToggleFireSelector", BindingFlags.Instance | BindingFlags.NonPublic);

        // Anton pls fix - Wrong sound plays when purchasing a clip at the new ammo reloader panel
        [HarmonyPatch(typeof(TNH_AmmoReloader2), "Button_SpawnClip")]
        [HarmonyPrefix]
        public static bool Button_SpawnClip_AudioFix(TNH_AmmoReloader2 __instance, bool ___hasDisplayedType, bool ___m_isConfirmingPurchase,
            TNH_AmmoReloader2.AmmoReloaderSelectedObject ___m_selectedObjectType, FVRFireArm ___m_selectedFA, FireArmRoundType ___m_displayedType,
            List<FireArmRoundClass> ___m_displayedClasses, int ___m_selectedClass)
        {
            if (!___hasDisplayedType || ___m_isConfirmingPurchase)
            {
                SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                return false;
            }

            bool found = false;
            if (___m_selectedObjectType == TNH_AmmoReloader2.AmmoReloaderSelectedObject.Gun && ___m_selectedFA != null && ___m_displayedType == ___m_selectedFA.RoundType && IM.OD.ContainsKey(___m_selectedFA.ObjectWrapper.ItemID))
            {
                FVRObject firearmObj = IM.OD[___m_selectedFA.ObjectWrapper.ItemID];

                if (firearmObj.CompatibleClips.Count > 0)
                {
                    found = true;
                    FVRObject clipObj = firearmObj.CompatibleClips[0];
                    GameObject clip = Object.Instantiate(clipObj.GetGameObject(), __instance.Spawnpoint_Round.position, __instance.Spawnpoint_Round.rotation);
                    __instance.M.AddObjectToTrackedList(clip);

                    FireArmRoundClass roundClip = ___m_displayedClasses[___m_selectedClass];
                    FVRFireArmClip clipComp = clip.GetComponent<FVRFireArmClip>();
                    clipComp.ReloadClipWithType(roundClip);
                }
                else if (firearmObj.CompatibleMagazines.Count > 0)
                {
                    FVRObject magazineObj = firearmObj.CompatibleMagazines[0];
                    GameObject magazineGO = magazineObj.GetGameObject();

                    FVRFireArmMagazine magazineComp = magazineGO.GetComponent<FVRFireArmMagazine>();

                    if (magazineComp.IsEnBloc)
                    {
                        found = true;
                        GameObject magazine = Object.Instantiate(magazineGO, __instance.Spawnpoint_Round.position, __instance.Spawnpoint_Round.rotation);
                        __instance.M.AddObjectToTrackedList(magazine);

                        FireArmRoundClass roundMag = ___m_displayedClasses[___m_selectedClass];
                        magazineComp = magazine.GetComponent<FVRFireArmMagazine>();
                        magazineComp.ReloadMagWithType(roundMag);
                    }
                }
            }

            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, (found ? __instance.AudEvent_Spawn : __instance.AudEvent_Fail), __instance.transform.position);
            return false;
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

        // Anton pls fix - DicSafeHoldIndiciesForSupplyPoint has missing entry. Also, DicSafeSupplyIndiciesForHoldPoint never has 32.
        [HarmonyPatch(typeof(TNH_Manager), "PrimeSafeDics")]
        [HarmonyPostfix]
        public static void PrimeSafeDics_MissingEntryFix(TNH_Manager __instance)
        {
            // For Northest Dakota, there's a missing entry
            if (__instance.DicSafeHoldIndiciesForSupplyPoint.Any() && __instance.SupplyPoints.Count == 38 && __instance.HoldPoints.Count == 32)
            {

                if (!__instance.DicSafeHoldIndiciesForSupplyPoint.ContainsKey(32))
                    __instance.DicSafeHoldIndiciesForSupplyPoint.Add(32, [25, 26, 27, 29]);
            }
        }

        // Anton pls fix - Wrong list used (DicSafeHoldIndiciesForHoldPoint)
        [HarmonyPatch(typeof(TNH_Manager), "GetRandomSafeSupplyIndexFromHoldPoint")]
        [HarmonyPrefix]
        private static bool GetRandomSafeSupplyIndexFromHoldPoint_BugFix(TNH_Manager __instance, out int __result, int index)
        {
            __result = __instance.DicSafeSupplyIndiciesForHoldPoint[index][UnityEngine.Random.Range(0, __instance.DicSafeSupplyIndiciesForHoldPoint[index].Count)];
            return false;
        }

        // Anton pls fix - Pump action shotgun config not working
        [HarmonyPatch(typeof(TubeFedShotgun), "SetLoadedChambers")]
        [HarmonyPostfix]
        public static void SetLoadedChambers_SetExtractor(TubeFedShotgun __instance, ref bool ___m_isChamberRoundOnExtractor, ref FVRFirearmMovingProxyRound ___m_proxy)
        {
            if (__instance.Chamber.IsFull && __instance.Magazine.HasARound())
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
            if (rounds.Any())
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
