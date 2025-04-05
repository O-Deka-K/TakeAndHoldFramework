using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TNHFramework.Patches
{
    static class MiscPatches
    {
        private static readonly MethodInfo miUpdateSafetyGeo = typeof(TubeFedShotgun).GetMethod("UpdateSafetyGeo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miToggleFireSelector = typeof(OpenBoltReceiver).GetMethod("ToggleFireSelector", BindingFlags.Instance | BindingFlags.NonPublic);

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
