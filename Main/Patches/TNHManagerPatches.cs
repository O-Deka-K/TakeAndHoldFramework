using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class TNHManagerPatches
    {
        private static readonly MethodInfo miGenerateValidPatrol = typeof(TNH_Manager).GetMethod("GenerateValidPatrol", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGenerateInitialTakeSentryPatrols = typeof(TNH_Manager).GetMethod("GenerateInitialTakeSentryPatrols", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool InitTNH(TNH_Manager __instance, bool ___m_hasInit)
        {
            if (!___m_hasInit)
                __instance.CharDB.Characters = TNHMenuInitializer.SavedCharacters;

            return true;
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetLevel")]
        [HarmonyPostfix]
        public static void SetLevel(TNH_Progression.Level ___m_curLevel)
        {
            LoadedTemplateManager.CurrentLevel = LoadedTemplateManager.CurrentCharacter.GetCurrentLevel(___m_curLevel);
            SupplyPatches.PanelIndex = 0;
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
        [HarmonyPrefix]
        public static bool SetPhase_Take_Replacement(TNH_Manager __instance, ref List<int> ___m_activeSupplyPointIndicies, ref TNH_Progression.Level ___m_curLevel,
            ref int ___m_lastHoldIndex, ref int ___m_curHoldIndex, ref TNH_HoldPoint ___m_curHoldPoint, TNH_PointSequence ___m_curPointSequence, int ___m_level)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            TNHFramework.SpawnedConstructors.Clear();
            TNHFramework.SpawnedBossIndexes.Clear();
            TNHFramework.SpawnedPoolsDictionary.Clear();
            TNHFramework.PatrolIndexPool.Clear();
            TNHFramework.PreventOutfitFunctionality = LoadedTemplateManager.CurrentCharacter.ForceDisableOutfitFunctionality;

            __instance.ResetAlertedThisPhase();
            __instance.ResetPlayerTookDamageThisPhase();
            __instance.ResetHasGuardBeenKilledThatWasAltered();
            ___m_activeSupplyPointIndicies.Clear();

            // Reset the TNH radar
            if (__instance.RadarMode == TNHModifier_RadarMode.Standard)
                __instance.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = __instance.ReticleMask_Take;
            else if (__instance.RadarMode == TNHModifier_RadarMode.Omnipresent)
                __instance.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = __instance.ReticleMask_Hold;

            ___m_lastHoldIndex = ___m_curHoldIndex;
            __instance.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
            __instance.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Supply);

            // Get the next hold point and configure it
            ___m_curHoldIndex = GetNextHoldPointIndex(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
            ___m_curHoldPoint = __instance.HoldPoints[___m_curHoldIndex];
            ___m_curHoldPoint.ConfigureAsSystemNode(___m_curLevel.TakeChallenge, ___m_curLevel.HoldChallenge, ___m_curLevel.NumOverrideTokensForHold);
            __instance.TAHReticle.RegisterTrackedObject(___m_curHoldPoint.SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);

            // Shuffle panel types
            level.PossiblePanelTypes.Shuffle();
            TNHFrameworkLogger.Log("Panel types for this hold:", TNHFrameworkLogger.LogType.TNH);
            level.PossiblePanelTypes.ForEach(o => TNHFrameworkLogger.Log(o.ToString(), TNHFrameworkLogger.LogType.TNH));

            // Ensure ammo reloaders spawn first if this is limited ammo
            if (level.PossiblePanelTypes.Contains(PanelType.AmmoReloader) && __instance.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking)
            {
                level.PossiblePanelTypes.Remove(PanelType.AmmoReloader);
                level.PossiblePanelTypes.Insert(0, PanelType.AmmoReloader);
            }

            // For default characters, only a single supply point spawns in each level of Institution
            // We will allow multiple supply points for custom characters
            bool allowExplicitSingleSupplyPoints = !LoadedTemplateManager.CurrentCharacter.isCustom;

            // Now spawn and set up all of the supply points
            if (allowExplicitSingleSupplyPoints && ___m_curPointSequence.UsesExplicitSingleSupplyPoints && ___m_level < 5)
            {
                TNHFrameworkLogger.Log("Spawning explicit single supply point", TNHFrameworkLogger.LogType.TNH);

                int supplyPointIndex = ___m_curPointSequence.SupplyPoints[___m_level];
                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointIndex];
                supplyPoint.Configure(___m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                ___m_activeSupplyPointIndicies.Add(supplyPointIndex);
            }
            else if (allowExplicitSingleSupplyPoints && ___m_curPointSequence.UsesExplicitSingleSupplyPoints && ___m_level >= 5)
            {
                List<int> supplyPointsIndexes = GetNextSupplyPointIndexes(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
                int supplyPointIndex = supplyPointsIndexes[0];

                TNHFrameworkLogger.Log($"Spawning explicit single supply point", TNHFrameworkLogger.LogType.TNH);

                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointIndex];
                supplyPoint.Configure(___m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                ___m_activeSupplyPointIndicies.Add(supplyPointIndex);
            }
            else
            {
                // Generate all of the supply points for this level
                List<int> supplyPointsIndexes = GetNextSupplyPointIndexes(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
                supplyPointsIndexes.Shuffle<int>();

                int numSupplyPoints = Random.Range(level.MinSupplyPoints, level.MaxSupplyPoints + 1);
                numSupplyPoints = Mathf.Clamp(numSupplyPoints, 0, supplyPointsIndexes.Count);

                TNHFrameworkLogger.Log($"Spawning {numSupplyPoints} supply points", TNHFrameworkLogger.LogType.TNH);

                bool spawnToken = true;
                for (int i = 0; i < numSupplyPoints; i++)
                {
                    TNHFrameworkLogger.Log($"Configuring supply point : {i}", TNHFrameworkLogger.LogType.TNH);

                    TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointsIndexes[i]];
                    supplyPoint.Configure(___m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 1, 2, spawnToken);
                    spawnToken = false;
                    TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                    supplyPoint.SetContact(contact);
                    ___m_activeSupplyPointIndicies.Add(supplyPointsIndexes[i]);
                }
            }

            // Spawn the initial patrol
            if (__instance.UsesClassicPatrolBehavior)
            {
                if (___m_level == 0)
                {
                    //__instance.GenerateValidPatrol(___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, ___m_curHoldIndex, true);
                    miGenerateValidPatrol.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, ___m_curHoldIndex, true]);
                }
                else
                {
                    //__instance.GenerateValidPatrol(___m_curLevel.PatrolChallenge, ___m_curHoldIndex, ___m_curHoldIndex, false);
                    miGenerateValidPatrol.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curHoldIndex, ___m_curHoldIndex, false]);
                }
            }
            else
            {
                if (___m_level == 0)
                {
                    //__instance.GenerateInitialTakeSentryPatrols(___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, -1, ___m_curHoldIndex, true);
                    miGenerateInitialTakeSentryPatrols.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, -1, ___m_curHoldIndex, true]);
                }
                else
                {
                    //__instance.GenerateInitialTakeSentryPatrols(___m_curLevel.PatrolChallenge, -1, ___m_curHoldIndex, ___m_curHoldIndex, false);
                    miGenerateInitialTakeSentryPatrols.Invoke(__instance, [___m_curLevel.PatrolChallenge, -1, ___m_curHoldIndex, ___m_curHoldIndex, false]);
                }
            }

            if (__instance.BGAudioMode == TNH_BGAudioMode.Default)
                __instance.FMODController.SwitchTo(0, 2f, false, false);

            return false;
        }

        public static int GetNextHoldPointIndex(TNH_Manager M, TNH_PointSequence pointSequence, int currLevel, int currHoldIndex)
        {
            int index;

            // If we haven't gone through all the hold points, we just select the next one we haven't been to
            if (currLevel < pointSequence.HoldPoints.Count)
            {
                index = pointSequence.HoldPoints[currLevel];
            }
            // If we have been to all the points, then we just select a random safe one
            else
            {
                List<int> pointIndexes = [];
                for (int i = 0; i < M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_HoldPoints.Count; i++)
                {
                    if (i != currHoldIndex && M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_HoldPoints[i])
                    {
                        pointIndexes.Add(i);
                    }
                }

                pointIndexes.Shuffle();
                index = pointIndexes[0];
            }

            return index;
        }

        public static List<int> GetNextSupplyPointIndexes(TNH_Manager M, TNH_PointSequence pointSequence, int currLevel, int currHoldIndex)
        {
            List<int> indexes = [];

            if (currLevel == 0)
            {
                for (int i = 0; i < M.SafePosMatrix.Entries_SupplyPoints[pointSequence.StartSupplyPointIndex].SafePositions_SupplyPoints.Count; i++)
                {
                    if (M.SafePosMatrix.Entries_SupplyPoints[pointSequence.StartSupplyPointIndex].SafePositions_SupplyPoints[i])
                    {
                        indexes.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_SupplyPoints.Count; i++)
                {
                    if (M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_SupplyPoints[i])
                    {
                        indexes.Add(i);
                    }
                }
            }

            indexes.Shuffle();
            return indexes;
        }

        // Clean up references so they can be garbage collected. This normally happens during the Hold phase,
        // but we should do this during the Take phase too. It won't delete existing any objects.
        [HarmonyPatch(typeof(TNH_Manager), "Update_Take")]
        [HarmonyPostfix]
        public static void TakeCleanup(TNH_Manager __instance, ref HashSet<FVRPhysicalObject> ___m_knownObjsHash, ref List<FVRPhysicalObject> ___m_knownObjs,
            ref int ___knownObjectCheckIndex)
        {
            if (!___m_knownObjs.Any())
                return;

            ___knownObjectCheckIndex++;
            if (___knownObjectCheckIndex >= ___m_knownObjs.Count)
                ___knownObjectCheckIndex = 0;

            if (___m_knownObjs[___knownObjectCheckIndex] == null)
            {
                ___m_knownObjsHash.Remove(___m_knownObjs[___knownObjectCheckIndex]);
                ___m_knownObjs.RemoveAt(___knownObjectCheckIndex);
            }
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Hold")]
        [HarmonyPostfix]
        public static void AfterSetHold()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
        [HarmonyPostfix]
        public static void AfterSetDead()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
        [HarmonyPostfix]
        public static void AfterSetComplete()
        {
            ClearAllPanels();
        }

        public static void ClearAllPanels()
        {
            //TNHFramework.SpawnedPools.Clear();
            TNHFramework.SpawnedPoolsDictionary.Clear();

            for (int i = TNHFramework.SpawnedConstructors.Count - 1; i >= 0; i--)
            {
                try
                {
                    TNH_ObjectConstructor constructor = TNHFramework.SpawnedConstructors[i].GetComponent<TNH_ObjectConstructor>();
                    constructor?.ClearCase();

                    Object.Destroy(TNHFramework.SpawnedConstructors[i]);
                }
                catch
                {
                    TNHFrameworkLogger.LogWarning("Failed to destroy constructor! It's likely that the constructor is already destroyed, so everything is probably just fine :)");
                }
            }

            TNHFramework.SpawnedConstructors.Clear();

            for (int i = TNHFramework.SpawnedPanels.Count - 1; i >= 0; i--)
            {
                Object.Destroy(TNHFramework.SpawnedPanels[i]);
            }

            TNHFramework.SpawnedPanels.Clear();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SpawnEnemy")]
        [HarmonyPrefix]
        public static bool SpawnEnemy_Replacement(TNH_Manager __instance, ref Sosig __result, SosigEnemyTemplate t, Vector3 pos, Quaternion rot, int IFF, bool IsAssault, Vector3 pointOfInterest, bool AllowAllWeapons)
        {
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[t];

            TNHFrameworkLogger.Log("Spawning sosig: " + customTemplate.SosigEnemyID, TNHFrameworkLogger.LogType.TNH);

            // Fill out the sosig's config based on the difficulty
            SosigConfig config;

            if (__instance.AI_Difficulty == TNHModifier_AIDifficulty.Arcade && customTemplate.ConfigsEasy.Any())
            {
                config = customTemplate.ConfigsEasy.GetRandom<SosigConfig>();
            }
            else if (customTemplate.Configs.Any())
            {
                config = customTemplate.Configs.GetRandom<SosigConfig>();
            }
            else
            {
                TNHFrameworkLogger.LogError("Sosig did not have normal difficulty config when playing on normal difficulty! Not spawning this enemy!");
                __result = null;
                return false;
            }

            // Create the sosig object
            GameObject sosigPrefab = Object.Instantiate(IM.OD[customTemplate.SosigPrefabs.GetRandom<string>()].GetGameObject(), pos, rot);  // ODK - Validate SosigPrefabs
            Sosig sosig = sosigPrefab.GetComponentInChildren<Sosig>();

            sosig.Configure(config.GetConfigTemplate());
            sosig.SetIFF(IFF);

            // Set up the sosig's inventory
            sosig.Inventory.Init();
            sosig.Inventory.FillAllAmmo();
            sosig.InitHands();

            // Equip the sosig's weapons
            if (customTemplate.WeaponOptions.Any())
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptions.GetRandom<string>()].GetGameObject();
                SosigPatches.EquipSosigWeapon(sosig, weaponPrefab, __instance.AI_Difficulty);
            }

            if (character.ForceAllAgentWeapons)
                AllowAllWeapons = true;

            if (customTemplate.WeaponOptionsSecondary.Any() && AllowAllWeapons && customTemplate.SecondaryChance >= Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsSecondary.GetRandom<string>()].GetGameObject();
                SosigPatches.EquipSosigWeapon(sosig, weaponPrefab, __instance.AI_Difficulty);
            }

            if (customTemplate.WeaponOptionsTertiary.Any() && AllowAllWeapons && customTemplate.TertiaryChance >= Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsTertiary.GetRandom<string>()].GetGameObject();
                SosigPatches.EquipSosigWeapon(sosig, weaponPrefab, __instance.AI_Difficulty);
            }

            // Equip clothing to the sosig
            OutfitConfig outfitConfig = customTemplate.OutfitConfigs.GetRandom<OutfitConfig>();  // ODK - Validate OutfitConfigs

            int torsoIndex = -1;
            if (outfitConfig.Torsowear.Any() && outfitConfig.Chance_Torsowear >= Random.value)
                torsoIndex = SosigPatches.EquipSosigClothing(outfitConfig.Torsowear, sosig.Links[1], -1, outfitConfig.ForceWearAllTorso);

            if (outfitConfig.Headwear.Any() && outfitConfig.Chance_Headwear >= Random.value)
            {
                int headIndex = (outfitConfig.HeadUsesTorsoIndex) ? torsoIndex : -1;
                SosigPatches.EquipSosigClothing(outfitConfig.Headwear, sosig.Links[0], headIndex, outfitConfig.ForceWearAllHead);
            }

            int pantsIndex = -1;
            if (outfitConfig.Pantswear.Any() && outfitConfig.Chance_Pantswear >= Random.value)
            {
                pantsIndex = (outfitConfig.PantsUsesTorsoIndex) ? torsoIndex : -1;
                pantsIndex = SosigPatches.EquipSosigClothing(outfitConfig.Pantswear, sosig.Links[2], pantsIndex, outfitConfig.ForceWearAllPants);
            }

            if (outfitConfig.Pantswear_Lower.Any() && outfitConfig.Chance_Pantswear_Lower >= Random.value)
            {
                int pantsLowerIndex = (outfitConfig.PantsLowerUsesPantsIndex) ? pantsIndex : -1;
                SosigPatches.EquipSosigClothing(outfitConfig.Pantswear_Lower, sosig.Links[3], pantsLowerIndex, outfitConfig.ForceWearAllPantsLower);
            }

            if (outfitConfig.Facewear.Any() && outfitConfig.Chance_Facewear >= Random.value)
                SosigPatches.EquipSosigClothing(outfitConfig.Facewear, sosig.Links[0], -1, outfitConfig.ForceWearAllFace);

            if (outfitConfig.Eyewear.Any() && outfitConfig.Chance_Eyewear >= Random.value)
                SosigPatches.EquipSosigClothing(outfitConfig.Eyewear, sosig.Links[0], -1, outfitConfig.ForceWearAllEye);

            if (outfitConfig.Backpacks.Any() && outfitConfig.Chance_Backpacks >= Random.value)
                SosigPatches.EquipSosigClothing(outfitConfig.Backpacks, sosig.Links[1], -1, outfitConfig.ForceWearAllBackpacks);

            if (outfitConfig.TorosDecoration.Any() && outfitConfig.Chance_TorosDecoration >= Random.value)
                SosigPatches.EquipSosigClothing(outfitConfig.Backpacks, sosig.Links[1], -1, outfitConfig.ForceWearAllTorosDecorations);

            if (outfitConfig.Belt.Any() && outfitConfig.Chance_Belt >= Random.value)
                SosigPatches.EquipSosigClothing(outfitConfig.Backpacks, sosig.Links[2], -1, outfitConfig.ForceWearAllBelts);

            // Set up the sosig's orders
            if (IsAssault)
            {
                sosig.CurrentOrder = Sosig.SosigOrder.Assault;
                sosig.FallbackOrder = Sosig.SosigOrder.Assault;
                sosig.CommandAssaultPoint(pointOfInterest);
            }
            else
            {
                sosig.CurrentOrder = Sosig.SosigOrder.Wander;
                sosig.FallbackOrder = Sosig.SosigOrder.Wander;
                sosig.CommandGuardPoint(pointOfInterest, true);
                sosig.SetDominantGuardDirection(Random.onUnitSphere);
            }
            sosig.SetGuardInvestigateDistanceThreshold(25f);

            // Handle sosig dropping custom loot
            if (customTemplate.DroppedObjectPool != null)
            {
                if (Random.value < customTemplate.DroppedLootChance)
                {
                    SosigLinkLootWrapper component = sosig.Links[2].gameObject.AddComponent<SosigLinkLootWrapper>();
                    component.M = __instance;
                    component.character = character;
                    component.shouldDropOnCleanup = !character.DisableCleanupSosigDrops;
                    component.group = new(customTemplate.DroppedObjectPool);
                    component.group.DelayedInit(character.GlobalObjectBlacklist);
                }
            }

            __result = sosig;
            return false;
        }
    }
}
