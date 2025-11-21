using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class ConstructorPatches
    {
        private static readonly MethodInfo miUpdateTokenDisplay = typeof(TNH_AmmoReloader2).GetMethod("UpdateTokenDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetState = typeof(TNH_ObjectConstructor).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miUpdateLockUnlockButtonState = typeof(TNH_ObjectConstructor).GetMethod("UpdateLockUnlockButtonState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miUpdateRerollButtonState = typeof(TNH_ObjectConstructor).GetMethod("UpdateRerollButtonState", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiAllowEntry = typeof(TNH_ObjectConstructor).GetField("allowEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSpawnedCase = typeof(TNH_ObjectConstructor).GetField("m_spawnedCase", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiLevel = typeof(TNH_Manager).GetField("m_level", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiWeaponCases = typeof(TNH_Manager).GetField("m_weaponCases", BindingFlags.Instance | BindingFlags.NonPublic);

        // This is a patch for using a character's global ammo blacklist in the new ammo reloader
        [HarmonyPatch(typeof(TNH_AmmoReloader2), "RefreshDisplayWithType")]
        [HarmonyPrefix]
        public static bool RefreshDisplayWithTypeBlacklist(TNH_AmmoReloader2 __instance, List<FireArmRoundType> ___m_detectedTypes, ref bool ___m_isConfirmingPurchase,
            ref bool ___hasDisplayedType, ref FireArmRoundType ___m_displayedType, ref List<FireArmRoundClass> ___m_displayedClasses, int ___m_selectedClass,
            int ___m_confirmingClass, List<FVRObject.OTagEra> ___m_validEras, List<FVRObject.OTagSet> ___m_validSets, FireArmRoundType t, bool confirmPurchase)
        {
            __instance.AmmoTypeField.text = AM.SRoundDisplayDataDic[t].DisplayName;

            if (___m_detectedTypes.Count > 1)
            {
                __instance.DisplayedTypeNext.enabled = true;
                __instance.DisplayedTypePrevious.enabled = true;
            }
            else
            {
                __instance.DisplayedTypeNext.enabled = false;
                __instance.DisplayedTypePrevious.enabled = false;
            }

            ___m_isConfirmingPurchase = confirmPurchase;
            ___hasDisplayedType = true;
            ___m_displayedType = t;
            ___m_displayedClasses.Clear();

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

            for (int i = 0; i < AM.SRoundDisplayDataDic[t].Classes.Length; i++)
            {
                FVRObject objectID = AM.SRoundDisplayDataDic[t].Classes[i].ObjectID;

                if (___m_validEras.Contains(objectID.TagEra) && ___m_validSets.Contains(objectID.TagSet))
                {
                    if (character.GlobalAmmoBlacklist == null || !character.GlobalAmmoBlacklist.Contains(objectID.ItemID))
                    {
                        ___m_displayedClasses.Add(AM.SRoundDisplayDataDic[t].Classes[i].Class);
                    }
                }
            }

            if (___m_displayedClasses.Count == 0)
            {
                ___m_displayedClasses.Add(AM.SRoundDisplayDataDic[t].Classes[0].Class);
            }

            if (!__instance.M.UnlockedClassesByType.ContainsKey(t))
            {
                List<FireArmRoundClass> list = [___m_displayedClasses[0]];
                __instance.M.UnlockedClassesByType.Add(t, list);
            }

            for (int j = 0; j < __instance.AmmoTokenFields.Count; j++)
            {
                if (j < ___m_displayedClasses.Count)
                {
                    __instance.AmmoTokenButtons[j].enabled = true;
                    int costByClass = AM.GetCostByClass(___m_displayedType, ___m_displayedClasses[j]);

                    if (__instance.M.UnlockedClassesByType[___m_displayedType].Contains(___m_displayedClasses[j]) || costByClass < 1)
                    {
                        if (___m_selectedClass == j)
                        {
                            __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Arrow;
                            __instance.AmmoTokenFields[j].text = "[Selected] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                        }
                        else
                        {
                            __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Select;
                            __instance.AmmoTokenFields[j].text = AM.STypeDic[t][___m_displayedClasses[j]].Name;
                        }
                    }
                    else if (___m_isConfirmingPurchase && j == ___m_confirmingClass)
                    {
                        __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Check;
                        __instance.AmmoTokenFields[j].text = "[Confirm Purchase?] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                    }
                    else
                    {
                        __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Token;
                        __instance.AmmoTokenFields[j].text = "[Buy (" + costByClass.ToString() + ")] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                    }
                }
                else
                {
                    __instance.AmmoTokenButtons[j].enabled = false;
                    __instance.AmmoTokenFields[j].text = string.Empty;
                }
            }

            //__instance.UpdateTokenDisplay(__instance.M.GetNumTokens());
            miUpdateTokenDisplay.Invoke(__instance, [__instance.M.GetNumTokens()]);
            return false;
        }

        // Fix backported from build 120b5
        [HarmonyPatch(typeof(TNH_ObjectConstructor), "UnlockPoolCategory")]
        [HarmonyPostfix]
        public static void UnlockPoolCategory_UnlockOnAll()
        {
            TNH_ObjectConstructor[] constructors = UnityEngine.Object.FindObjectsOfType<TNH_ObjectConstructor>();

            foreach (TNH_ObjectConstructor constructor in constructors)
            {
                if (constructor.UsesUnlock)
                {
                    //constructor.UpdateLockUnlockButtonState(false);
                    miUpdateLockUnlockButtonState.Invoke(constructor, [false]);
                }
                else
                {
                    //constructor.UpdateRerollButtonState(false);
                    miUpdateRerollButtonState.Invoke(constructor, [false]);
                }
            }
        }

        [HarmonyPatch(typeof(TNH_ObjectConstructor), "GetPoolEntry")]
        [HarmonyPrefix]
        public static bool GetPoolEntry_Replacement(ref EquipmentPoolDef.PoolEntry __result, int level, EquipmentPoolDef poolDef, EquipmentPoolDef.PoolEntry.PoolEntryType t, EquipmentPoolDef.PoolEntry prior)
        {
            if (!TNHFramework.SpawnedPoolsDictionary.TryGetValue(t, out List<EquipmentPoolDef.PoolEntry> validPools) || !validPools.Any())
            {
                validPools = poolDef.Entries.Where(o => o.Type == t && o.MinLevelAppears <= level && level <= o.MaxLevelAppears).ToList();
                TNHFramework.SpawnedPoolsDictionary[t] = validPools;
            }

            // If we didn't find a single pool, we cry about it
            if (validPools == null || !validPools.Any())
            {
                TNHFrameworkLogger.LogWarning("No valid pool could spawn at constructor for type (" + t + ")");
                __result = null;
                return false;
            }

            float summedRarity = validPools.Sum(o => o.Rarity);

            // Select a random value within the summed rarity, and select a pool based on that value
            float selectValue = Random.Range(0, summedRarity);
            float currentSum = 0;
            foreach (EquipmentPoolDef.PoolEntry entry in validPools)
            {
                currentSum += entry.Rarity;

                if (selectValue <= currentSum)
                {
                    __result = entry;
                    //TNHFramework.SpawnedPools.Add(entry);
                    validPools.Remove(entry);
                    return false;
                }
            }

            TNHFrameworkLogger.LogError("Somehow escaped pool entry rarity selection! This is not good!");
            __result = poolDef.Entries[0];
            return false;
        }

        // This allows the object constructor to spawn vault objects by calling SpawnObjectAtConstructor() instead of SpawnObject()
        [HarmonyPatch(typeof(TNH_ObjectConstructor), "ButtonClicked")]
        [HarmonyPriority(800)]
        [HarmonyPrefix]
        public static bool ButtonClicked_Replacement(TNH_ObjectConstructor __instance, bool ___allowEntry, List<EquipmentPoolDef.PoolEntry> ___m_poolEntries,
            ref int ___m_selectedEntry, GameObject ___m_spawnedCase, ref int ___m_numTokensSelected, ref List<int> ___m_poolAddedCost, int i)
        {
            if (!__instance.Icons[i].gameObject.activeSelf)
                return false;

            if (!___allowEntry)
                return false;

            if (__instance.State == TNH_ObjectConstructor.ConstructorState.EntryList)
            {
                int cost = 0;
                if (___m_poolEntries[i] != null)
                    cost = ___m_poolEntries[i].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[i];

                if (__instance.M.GetNumTokens() >= cost)
                {
                    //__instance.SetState(TNH_ObjectConstructor.ConstructorState.Confirm, i);
                    miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.Confirm, i]);
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Select, __instance.transform.position);
                }
                else
                {
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                }
            }
            else if (__instance.State == TNH_ObjectConstructor.ConstructorState.Confirm)
            {
                if (i == 1)
                {
                    //__instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
                    miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.EntryList, 0]);
                    ___m_selectedEntry = -1;
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Back, __instance.transform.position);
                }
                else if (i == 3)
                {
                    int cost = 0;
                    if (___m_poolEntries[i] != null)
                        cost = ___m_poolEntries[___m_selectedEntry].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[___m_selectedEntry];

                    if (__instance.M.GetNumTokens() >= cost)
                    {
                        if ((!___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInSmallCase && !___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInLargeCase) || ___m_spawnedCase == null)
                        {
                            AnvilManager.Run(SpawnObjectAtConstructor(___m_poolEntries[___m_selectedEntry], __instance));
                            ___m_numTokensSelected = 0;
                            __instance.M.SubtractTokens(cost);
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Spawn, __instance.transform.position);

                            if (__instance.M.C.UsesPurchasePriceIncrement)
                                ___m_poolAddedCost[___m_selectedEntry]++;

                            //__instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
                            miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.EntryList, 0]);
                            ___m_selectedEntry = -1;
                        }
                        else
                        {
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                        }
                    }
                    else
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                    }
                }
            }

            return false;
        }

        private static IEnumerator SpawnObjectAtConstructor(EquipmentPoolDef.PoolEntry entry, TNH_ObjectConstructor constructor)
        {
            TNHFrameworkLogger.Log("Spawning item at constructor", TNHFrameworkLogger.LogType.TNH);

            //constructor.allowEntry = false;
            fiAllowEntry.SetValue(constructor, false);
            EquipmentPool pool = LoadedTemplateManager.EquipmentPoolDictionary[entry];
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            List<EquipmentGroup> selectedGroups = pool.GetSpawnedEquipmentGroups();
            AnvilCallback<GameObject> gameObjectCallback;

            if (pool.SpawnsInLargeCase || pool.SpawnsInSmallCase)
            {
                TNHFrameworkLogger.Log("Item will spawn in a container", TNHFrameworkLogger.LogType.TNH);

                GameObject caseFab = constructor.M.Prefab_WeaponCaseLarge;
                if (pool.SpawnsInSmallCase)
                    caseFab = constructor.M.Prefab_WeaponCaseSmall;

                FVRObject item = IM.OD[selectedGroups[0].GetObjects().GetRandom()];
                GameObject itemCase = SpawnWeaponCase(constructor.M, selectedGroups[0].BespokeAttachmentChance, caseFab, constructor.SpawnPoint_Case.position, constructor.SpawnPoint_Case.forward, item, selectedGroups[0].NumMagsSpawned, selectedGroups[0].NumRoundsSpawned, selectedGroups[0].MinAmmoCapacity, selectedGroups[0].MaxAmmoCapacity);

                //constructor.m_spawnedCase = itemCase;
                fiSpawnedCase.SetValue(constructor, itemCase);
                itemCase.GetComponent<TNH_WeaponCrate>().M = constructor.M;
            }
            else
            {
                TNHFrameworkLogger.Log("Item will spawn without a container", TNHFrameworkLogger.LogType.TNH);

                int mainSpawnCount = 0;
                int requiredSpawnCount = 0;
                int ammoSpawnCount = 0;
                int objectSpawnCount = 0;

                TNHFrameworkLogger.Log("Pool has " + selectedGroups.Count + " groups to spawn from", TNHFrameworkLogger.LogType.TNH);
                for (int groupIndex = 0; groupIndex < selectedGroups.Count; groupIndex++)
                {
                    EquipmentGroup group = selectedGroups[groupIndex];

                    TNHFrameworkLogger.Log("Group will spawn " + group.ItemsToSpawn + " items from it", TNHFrameworkLogger.LogType.TNH);
                    for (int itemIndex = 0; itemIndex < group.ItemsToSpawn; itemIndex++)
                    {
                        FVRObject mainObject;
                        VaultFile vaultFile = null;
                        SavedGunSerializable vaultFileLegacy = null;

                        Transform primarySpawn = constructor.SpawnPoint_Object;
                        Transform requiredSpawn = constructor.SpawnPoint_Object;
                        Transform ammoSpawn = constructor.SpawnPoint_Mag;
                        float objectDistancing = 0.2f;

                        if (group.IsCompatibleMagazine)
                        {
                            TNHFrameworkLogger.Log("Item will be a compatible magazine", TNHFrameworkLogger.LogType.TNH);
                            mainObject = FirearmUtils.GetAmmoContainerForEquipped(group.MinAmmoCapacity, group.MaxAmmoCapacity, character.GlobalObjectBlacklist, character.GetMagazineBlacklist());
                            if (mainObject == null)
                            {
                                TNHFrameworkLogger.LogWarning("Failed to spawn a compatible magazine!");
                                break;
                            }
                        }
                        else
                        {
                            string item = group.GetObjects().GetRandom();
                            TNHFrameworkLogger.Log("Item selected: " + item, TNHFrameworkLogger.LogType.TNH);

                            if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(item))
                            {
                                TNHFrameworkLogger.Log("Item is a vaulted gun", TNHFrameworkLogger.LogType.TNH);
                                vaultFile = LoadedTemplateManager.LoadedVaultFiles[item];
                                mainObject = IM.OD[vaultFile.Objects[0].Elements[0].ObjectID];
                            }
                            else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(item))
                            {
                                TNHFrameworkLogger.Log("Item is a legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                                vaultFileLegacy = LoadedTemplateManager.LoadedLegacyVaultFiles[item];
                                mainObject = vaultFileLegacy.GetGunObject();
                            }
                            else
                            {
                                TNHFrameworkLogger.Log("Item is a normal object", TNHFrameworkLogger.LogType.TNH);
                                mainObject = IM.OD[item];
                            }
                        }

                        // Assign spawn points based on the type of item we are spawning
                        if (mainObject.Category == FVRObject.ObjectCategory.Firearm)
                        {
                            primarySpawn = constructor.SpawnPoints_GunsSize[Mathf.Clamp(mainObject.TagFirearmSize - FVRObject.OTagFirearmSize.Pocket, 0, constructor.SpawnPoints_GunsSize.Count - 1)];
                            requiredSpawn = constructor.SpawnPoint_Grenade;
                            mainSpawnCount++;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.Explosive || mainObject.Category == FVRObject.ObjectCategory.Thrown)
                        {
                            primarySpawn = constructor.SpawnPoint_Grenade;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.MeleeWeapon)
                        {
                            primarySpawn = constructor.SpawnPoint_Melee;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.Cartridge)
                        {
                            primarySpawn = constructor.SpawnPoint_Ammo;
                            objectDistancing = 0.05f;
                            mainSpawnCount++;
                        }

                        int level = (int)fiLevel.GetValue(constructor.M);
                        TNHFrameworkLogger.Log("Level: " + level, TNHFrameworkLogger.LogType.TNH);

                        // J: New vault files have a method for spawning them. Thank god. Or, y'know, thank Anton.
                        if (vaultFile != null)
                        {
                            VaultSystem.ReturnObjectListDelegate del = new((objs) => TrackVaultObjects(constructor.M, objs));
                            TNHFrameworkLogger.Log("Spawning vault gun", TNHFrameworkLogger.LogType.TNH);
                            VaultSystem.SpawnVaultFile(vaultFile, primarySpawn, true, false, false, out _, Vector3.zero, del, false);
                        }
                        // If this is a vault file, we have to spawn it through a routine. Otherwise we just instantiate it
                        else if (vaultFileLegacy != null)
                        {
                            TNHFrameworkLogger.Log("Spawning legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                            AnvilManager.Run(TNHFrameworkUtils.SpawnLegacyVaultFile(vaultFileLegacy, primarySpawn.position, primarySpawn.rotation, constructor.M));
                            // SpawnFirearm adds the objects to the tracked objects list
                        }
                        else
                        {
                            TNHFrameworkLogger.Log("Spawning normal item", TNHFrameworkLogger.LogType.TNH);
                            gameObjectCallback = mainObject.GetGameObjectAsync();
                            yield return gameObjectCallback;

                            GameObject spawnedObject = Object.Instantiate(mainObject.GetGameObject(), primarySpawn.position + Vector3.up * objectDistancing * mainSpawnCount, primarySpawn.rotation);
                            constructor.M.AddObjectToTrackedList(spawnedObject);
                            TNHFrameworkLogger.Log("Normal item spawned", TNHFrameworkLogger.LogType.TNH);
                        }

                        // Spawn any required objects
                        if (mainObject.RequiredSecondaryPieces != null)
                        {
                            for (int j = 0; j < mainObject.RequiredSecondaryPieces.Count; j++)
                            {
                                if (mainObject.RequiredSecondaryPieces[j] == null)
                                {
                                    TNHFrameworkLogger.Log("Null required object! Skipping", TNHFrameworkLogger.LogType.TNH);
                                    continue;
                                }

                                FVRObject requiredObject = mainObject.RequiredSecondaryPieces[j];
                                gameObjectCallback = requiredObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning required secondary item ({requiredObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                GameObject requiredItem = Object.Instantiate(requiredObject.GetGameObject(), requiredSpawn.position + -requiredSpawn.right * 0.2f * requiredSpawnCount + Vector3.up * 0.2f * j, requiredSpawn.rotation);
                                constructor.M.AddObjectToTrackedList(requiredItem);
                                requiredSpawnCount++;
                            }
                        }

                        // Handle spawning for ammo objects if the main object has any
                        if (FirearmUtils.FVRObjectHasAmmoObject(mainObject))
                        {
                            Dictionary<string, MagazineBlacklistEntry> blacklist = character.GetMagazineBlacklist();
                            MagazineBlacklistEntry blacklistEntry = null;
                            if (blacklist.ContainsKey(mainObject.ItemID))
                                blacklistEntry = blacklist[mainObject.ItemID];

                            // Get lists of ammo objects for this firearm with filters and blacklists applied
                            List<FVRObject> compatibleMagazines = FirearmUtils.GetCompatibleMagazines(mainObject, group.MinAmmoCapacity, group.MaxAmmoCapacity, true, character.GlobalObjectBlacklist, blacklistEntry);
                            List<FVRObject> compatibleRounds = FirearmUtils.GetCompatibleRounds(mainObject, character.ValidAmmoEras, character.ValidAmmoSets, character.GlobalAmmoBlacklist, character.GlobalObjectBlacklist, blacklistEntry);
                            List<FVRObject> compatibleClips = mainObject.CompatibleClips;

                            // If we are supposed to spawn magazines and clips, perform special logic for that
                            if (group.SpawnMagAndClip && compatibleMagazines.Count > 0 && compatibleClips.Count > 0 && group.NumMagsSpawned > 0 && group.NumClipsSpawned > 0)
                            {
                                TNHFrameworkLogger.Log("Spawning with both magazine and clips", TNHFrameworkLogger.LogType.TNH);

                                FVRObject magazineObject = compatibleMagazines.GetRandom();
                                FVRObject clipObject = compatibleClips.GetRandom();
                                ammoSpawn = constructor.SpawnPoint_Mag;

                                gameObjectCallback = magazineObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning magazine ({magazineObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                GameObject spawnedMag = Object.Instantiate(magazineObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedMag);
                                ammoSpawnCount++;

                                gameObjectCallback = clipObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning clip ({clipObject.ItemID}), Count = {group.NumClipsSpawned}", TNHFrameworkLogger.LogType.TNH);
                                for (int i = 0; i < group.NumClipsSpawned; i++)
                                {
                                    GameObject spawnedClip = Object.Instantiate(clipObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(spawnedClip);
                                    ammoSpawnCount++;
                                }
                            }
                            // Otherwise, perform normal logic for spawning ammo objects from current group
                            else
                            {
                                FVRObject ammoObject;
                                int numSpawned = 0;

                                if (compatibleMagazines.Count > 0 && group.NumMagsSpawned > 0)
                                {
                                    ammoObject = compatibleMagazines.GetRandom();
                                    numSpawned = group.NumMagsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (compatibleClips.Count > 0 && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = compatibleClips.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (mainObject.CompatibleSpeedLoaders != null && mainObject.CompatibleSpeedLoaders.Count > 0 && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = mainObject.CompatibleSpeedLoaders.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else
                                {
                                    ammoObject = compatibleRounds.GetRandom();
                                    numSpawned = group.NumRoundsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Ammo;
                                }

                                TNHFrameworkLogger.Log($"Spawning ammo object normally ({ammoObject.ItemID}), Count = {numSpawned}", TNHFrameworkLogger.LogType.TNH);

                                gameObjectCallback = ammoObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                for (int i = 0; i < numSpawned; i++)
                                {
                                    GameObject spawned = Object.Instantiate(ammoObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(spawned);
                                    ammoSpawnCount++;
                                }
                            }
                        }

                        // If this object requires picatinny sights, we should try to spawn one
                        if (mainObject.RequiresPicatinnySight && character.RequireSightTable != null)
                        {
                            TNHFrameworkLogger.Log("Spawning required sights", TNHFrameworkLogger.LogType.TNH);

                            FVRObject sight = IM.OD[character.RequireSightTable.GetSpawnedEquipmentGroups().GetRandom().GetObjects().GetRandom()];
                            gameObjectCallback = sight.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject spawnedSight = UnityEngine.Object.Instantiate(sight.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                            constructor.M.AddObjectToTrackedList(spawnedSight);

                            TNHFrameworkLogger.Log($"Required sight spawned ({sight.ItemID})", TNHFrameworkLogger.LogType.TNH);

                            for (int j = 0; j < sight.RequiredSecondaryPieces.Count; j++)
                            {
                                FVRObject objectRequired = sight.RequiredSecondaryPieces[j];
                                gameObjectCallback = objectRequired.GetGameObjectAsync();
                                yield return gameObjectCallback;
                                GameObject spawnedRequired = UnityEngine.Object.Instantiate(objectRequired.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount + Vector3.up * 0.15f * j, constructor.SpawnPoint_Object.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedRequired);
                                TNHFrameworkLogger.Log($"Required secondary item for sight spawned ({objectRequired.ItemID})", TNHFrameworkLogger.LogType.TNH);
                            }

                            objectSpawnCount++;
                        }
                        // If this object has bespoke attachments we'll try to spawn one
                        else if (mainObject.BespokeAttachments.Count > 0 && Random.value < group.BespokeAttachmentChance)
                        {
                            TNHFrameworkLogger.Log("Spawning bespoke attachment", TNHFrameworkLogger.LogType.TNH);
                            FVRObject bespoke = mainObject.BespokeAttachments.GetRandom();
                            gameObjectCallback = bespoke.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject bespokeObject = UnityEngine.Object.Instantiate(bespoke.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                            constructor.M.AddObjectToTrackedList(bespokeObject);
                            objectSpawnCount++;

                            TNHFrameworkLogger.Log($"Bespoke item spawned ({bespoke.ItemID})", TNHFrameworkLogger.LogType.TNH);
                        }
                    }
                }
            }

            //constructor.allowEntry = true;
            fiAllowEntry.SetValue(constructor, true);
            yield break;
        }

        public static GameObject SpawnWeaponCase(TNH_Manager M, float bespokeAttachmentChance, GameObject caseFab, Vector3 position, Vector3 forward,
            FVRObject weapon, int numMag, int numRound, int minAmmo, int maxAmmo, FVRObject ammoObjOverride = null)
        {
            GameObject caseObj = Object.Instantiate<GameObject>(caseFab, position, Quaternion.LookRotation(forward, Vector3.up));

            //M.m_weaponCases.Add(caseObj);
            var weaponCases = (List<GameObject>)fiWeaponCases.GetValue(M);
            weaponCases.Add(caseObj);

            TNH_WeaponCrate createComp = caseObj.GetComponent<TNH_WeaponCrate>();

            FVRObject ammoObj = ammoObjOverride ?? weapon.GetRandomAmmoObject(weapon, M.C.ValidAmmoEras, minAmmo, maxAmmo, M.C.ValidAmmoSets);
            int numClipSpeedLoaderRound = (ammoObj != null && ammoObj.Category == FVRObject.ObjectCategory.Cartridge) ? numRound : numMag;

            FVRObject sightObj = null;
            FVRObject requiredAttachment_B = null;
            if (weapon.RequiresPicatinnySight)
            {
                sightObj = M.GetObjectTable(M.C.RequireSightTable).GetRandomObject();

                if (sightObj.RequiredSecondaryPieces.Count > 0)
                    requiredAttachment_B = sightObj.RequiredSecondaryPieces[0];
            }
            // Check the bespoke attachment chance here
            // In vanilla TNH, it ALWAYS spawns a bespoke attachment if there is one
            else if (weapon.BespokeAttachments.Count > 0 && Random.value < bespokeAttachmentChance)
            {
                sightObj = weapon.BespokeAttachments[Random.Range(0, weapon.BespokeAttachments.Count)];
            }

            if (weapon.RequiredSecondaryPieces.Count > 0)
                requiredAttachment_B = weapon.RequiredSecondaryPieces[0];

            bool spawnAmmoAsBox = false;
            if (M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking && weapon.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.Ordnance)
                spawnAmmoAsBox = true;

            createComp.PlaceWeaponInContainer(weapon, sightObj, requiredAttachment_B, ammoObj, numClipSpeedLoaderRound, spawnAmmoAsBox);
            return caseObj;
        }

        /// <summary>
        /// Delegate for tracking all GameObjects created by a vault gun spawn
        /// </summary>
        /// <param name="objs"></param>
        private static void TrackVaultObjects(TNH_Manager M, List<FVRPhysicalObject> objs)
        {
            foreach (FVRPhysicalObject obj in objs)
            {
                if (obj != null)
                    M.AddObjectToTrackedList(obj.GameObject);
            }
        }
    }
}
