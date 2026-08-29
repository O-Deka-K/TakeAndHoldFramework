using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class MiscPatches
    {
        private static readonly MethodInfo miUpdateSafetyGeo = typeof(TubeFedShotgun).GetMethod("UpdateSafetyGeo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miToggleFireSelector = typeof(OpenBoltReceiver).GetMethod("ToggleFireSelector", BindingFlags.Instance | BindingFlags.NonPublic);

        // Anton pls fix - DicSafeHoldIndiciesForSupplyPoint has missing entry. Also, DicSafeSupplyIndiciesForHoldPoint never has 32.
        [HarmonyPatch(typeof(TNH_Manager), "PrimeSafeDics")]
        [HarmonyPostfix]
        public static void PrimeSafeDics_MissingEntryFix(TNH_Manager __instance)
        {
            // For Northest Dakota, there's a missing entry
            if (__instance.DicSafeHoldIndiciesForSupplyPoint.Any() && __instance.SupplyPoints.Count == 38 && __instance.HoldPoints.Count == 32)
            {
#if (false)
                // Print out the table
                TNHFrameworkLogger.Log($"DicSafeHoldIndiciesForSupplyPoint for Northest Dakota:", TNHFrameworkLogger.LogType.TNH);
                foreach (KeyValuePair<int, List<int>> entry in __instance.DicSafeHoldIndiciesForSupplyPoint)
                {
                    string list = string.Join(",", [.. entry.Value.Select(o => o.ToString())]);
                    TNHFrameworkLogger.Log($"  {entry.Key}: [{list}]", TNHFrameworkLogger.LogType.TNH);
                }
#endif

                if (!__instance.DicSafeHoldIndiciesForSupplyPoint.ContainsKey(32))
                    __instance.DicSafeHoldIndiciesForSupplyPoint.Add(32, [25, 26, 27, 29]);
            }
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

        // Anton pls fix. There's a bug in TNH_Utilities.GenerateProcessedLootObject. This is a workaround that will cancel the spawn.
        // I added a fix to IM.CompatMags in the internal mag patcher, so maybe this never triggers
        [HarmonyPatch(typeof(TNH_Utilities), "GenerateProcessedLootObject")]
        [HarmonyPrefix]
        public static bool GenerateProcessedLootObject_MissingMagFix(ref GameObject __result, ref string ContentsID, Vector3 Pos, Quaternion Rot)
        {
            TNHFrameworkLogger.Log($"Spawning {ContentsID} as loot", TNHFrameworkLogger.LogType.TNH);

            if (!IM.OD.ContainsKey(ContentsID))
            {
                TNHFrameworkLogger.Log($"  Error: Object Database does not contain {ContentsID}!", TNHFrameworkLogger.LogType.TNH);
                return false;
            }

            GameObject gameObject = IM.OD[ContentsID].GetGameObject();

            if (gameObject.GetComponent<FVRFireArm>() != null)
            {
                FVRFireArm firearm = gameObject.GetComponent<FVRFireArm>();

                // Anton pls fix. Grapple gun doesn't spawn with mag (which is actually a speedloader)
                if (ContentsID == "GrappleGun")
                {
                    // Spawn grapple gun
                    GameObject goGrappleGun = Object.Instantiate(gameObject, Pos, Rot);
                    GrappleGun grappleGun = goGrappleGun.GetComponent<GrappleGun>();

                    // Spawn grapple gun speedloader. Gun has a special "proxy" mag that appears after it's loaded, so the speedloader object has to be destroyed
                    GameObject goSpeedLoader = Object.Instantiate(IM.OD["MagazineGrappleGun"].GetGameObject(), grappleGun.MagMountPoint.position, grappleGun.MagMountPoint.rotation);
                    grappleGun.LoadCylinder(goSpeedLoader.GetComponent<Speedloader>());
                    Object.Destroy(goSpeedLoader);

                    __result = goGrappleGun;
                    return false;
                }
                else if (firearm.Magazine == null && firearm.MagazineType != FireArmMagazineType.mNone)
                {
                    // Check if IM.CompatMags contains the key at all
                    if (!IM.CompatMags.ContainsKey(firearm.MagazineType) || !IM.CompatMags[firearm.MagazineType].Any())
                    {
                        TNHFrameworkLogger.Log($"  Error: {ContentsID} does not have any compatible magazines!", TNHFrameworkLogger.LogType.TNH);
                        FVRObject dummyObj = IM.OD["Charcoal"];
                        IM.CompatMags.Add(firearm.MagazineType, [dummyObj]);
                        return true;
                    }
                }
            }

            return true;
        }
    }
}
