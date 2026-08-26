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
        private static readonly MethodInfo miSetState = typeof(TNH_ObjectConstructor).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnAmmoForObject = typeof(TNH_ObjectConstructor).GetMethod("SpawnAmmoForObject", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiAllowEntry = typeof(TNH_ObjectConstructor).GetField("allowEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSpawnedCase = typeof(TNH_ObjectConstructor).GetField("m_spawnedCase", BindingFlags.Instance | BindingFlags.NonPublic);

        private static float BespokeAttachmentChance = 0f;

        // This is a patch for using a character's global ammo blacklist in the new ammo reloader
        [HarmonyPatch(typeof(TNH_Manager), "GetAcceptableRoundClassesForType")]
        [HarmonyPrefix]
        public static bool GetAcceptableRoundClassesForType_Replacement(TNH_Manager __instance, ref List<FireArmRoundClass> __result, FireArmRoundType t)
        {
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

            List<FireArmRoundClass> list = [];
            if (!AM.SRoundDisplayDataDic.TryGetValue(t, out FVRFireArmRoundDisplayData fvrfireArmRoundDisplayData) || fvrfireArmRoundDisplayData.Classes.Length == 0)
            {
                throw new System.InvalidOperationException("What? How does this even happen?");
            }

            foreach (FVRFireArmRoundDisplayData.DisplayDataClass displayDataClass in fvrfireArmRoundDisplayData.Classes)
            {
                FireArmRoundClass roundClass = displayDataClass.Class;
                if (__instance.C.ValidAmmoEras.Contains(displayDataClass.ObjectID.TagEra) && __instance.C.ValidAmmoSets.Contains(displayDataClass.ObjectID.TagSet))
                {
                    if (!__instance.C.RoundClassBlacklist.Contains(roundClass))
                    {
                        if (character.GlobalAmmoBlacklist == null || !character.GlobalAmmoBlacklist.Contains(displayDataClass.ObjectID.ItemID))
                        {
                            list.Add(roundClass);
                        }
                    }
                }
            }

            if (!list.Any())
            {
                Debug.LogWarning("No acceptable round classes were found for type: " + t);
                list.Add(fvrfireArmRoundDisplayData.Classes[0].Class);
            }

            __result =  list;
            return false;
        }

        // Attempt to spread pools out evenly. Original method only ensures that next pool is not the same as the previous one.
        [HarmonyPatch(typeof(TNH_ObjectConstructor), "GetPoolEntry")]
        [HarmonyPrefix]
        public static bool GetPoolEntry_Replacement(TNH_ObjectConstructor __instance, ref EquipmentPoolDef.PoolEntry __result, int level, EquipmentPoolDef poolDef, EquipmentPoolDef.PoolEntry.PoolEntryType t, EquipmentPoolDef.PoolEntry prior)
        {
            if (!TNHFramework.SpawnedPoolsDictionary.TryGetValue(t, out List<EquipmentPoolDef.PoolEntry> validPools) || !validPools.Any())
            {
                validPools = [.. poolDef.Entries.Where(o => o.Type == t && o.MinLevelAppears <= level && level <= o.MaxLevelAppears)];
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
            float selectValue = (float)__instance.M.PoolEntryRand.NextDouble() * summedRarity;
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

            ObjectTable table = constructor.M.GetObjectTable(entry.TableDef);

            if (table.UsesVaultFiles())
            {
                TNHFrameworkLogger.Log("Item will be a vault file", TNHFrameworkLogger.LogType.TNH);
                VaultFile vf = table.GetRandomVaultFile();
                List<FVRPhysicalObject> spawnedObjs = null;
                bool success;

                if (table.FileUsage == ObjectTableDef.VaultFileUsage.WholeFile)
                {
                    success = VaultSystem.SpawnVaultFile(vf, constructor.SpawnPoint_VaultScanRoot, true, false, false, out string errorMessage, Vector3.zero, delegate (List<FVRPhysicalObject> vfo)
                    {
                        spawnedObjs = vfo;
                    }, false, -1);
                }
                else
                {
                    if (table.FileUsage != ObjectTableDef.VaultFileUsage.SingleObject)
                        throw new System.InvalidOperationException();

                    success = VaultSystem.SpawnVaultFile(vf, constructor.SpawnPoint_VaultScanRoot, true, false, false, out string errorMessage, Vector3.zero, delegate (List<FVRPhysicalObject> vfo)
                    {
                        spawnedObjs = vfo;
                    }, false, 0);
                }

                if (!success)
                {
                    TNHFrameworkLogger.Log("Failed to spawn vault file? What?", TNHFrameworkLogger.LogType.TNH);
                    yield break;
                }

                while (spawnedObjs == null)
                {
                    yield return null;
                }

                if (spawnedObjs.Any() && table.FileUsage == ObjectTableDef.VaultFileUsage.SingleObject)
                {
                    TNHFrameworkLogger.Log("Spawning ammo for vault file", TNHFrameworkLogger.LogType.TNH);
                    //yield return constructor.SpawnAmmoForObject(table, spawnedObjs[0].ObjectWrapper);

                    // Yielding the result from Invoke() doesn't seem to work, so start another coroutine instead
                    AnvilManager.Run((IEnumerator)miSpawnAmmoForObject.Invoke(constructor, [table, spawnedObjs[0].ObjectWrapper]));
                }
            }
            else if (table.NumSpawns == 1 && (pool.SpawnsInLargeCase || pool.SpawnsInSmallCase))
            {
                TNHFrameworkLogger.Log("Item will spawn in a container", TNHFrameworkLogger.LogType.TNH);

                GameObject caseFab = constructor.M.Prefab_WeaponCaseLarge;
                if (pool.SpawnsInSmallCase)
                    caseFab = constructor.M.Prefab_WeaponCaseSmall;

                FVRObject item = IM.OD[selectedGroups[0].GetRandomObject()];
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
                            string item = group.GetRandomObject();
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

                        if (vaultFile != null)
                        {
                            VaultSystem.ReturnObjectListDelegate del = new((objs) => TNHFrameworkUtils.TrackVaultObjects(constructor.M, objs));
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

                            if (mainObject.GetGameObject() != null)
                            {
                                GameObject spawnedObject = Object.Instantiate(mainObject.GetGameObject(), primarySpawn.position + Vector3.up * objectDistancing * mainSpawnCount, primarySpawn.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedObject);

                                if (TNHFramework.FixLegacyModulGuns.Value)
                                    TNHFrameworkUtils.FixPremadeFirearm(spawnedObject, false);

                                TNHFrameworkLogger.Log("Normal item spawned", TNHFrameworkLogger.LogType.TNH);
                            }
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

                                if (requiredObject.GetGameObject() != null)
                                {
                                    TNHFrameworkLogger.Log($"Spawning required secondary item ({requiredObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                    GameObject requiredItem = Object.Instantiate(requiredObject.GetGameObject(), requiredSpawn.position + -requiredSpawn.right * 0.2f * requiredSpawnCount + Vector3.up * 0.2f * j, requiredSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(requiredItem);
                                    requiredSpawnCount++;
                                }
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
                            if (group.SpawnMagAndClip && compatibleMagazines.Any() && compatibleClips.Any() && group.NumMagsSpawned > 0 && group.NumClipsSpawned > 0)
                            {
                                TNHFrameworkLogger.Log("Spawning with both magazine and clips", TNHFrameworkLogger.LogType.TNH);

                                FVRObject magazineObject = compatibleMagazines.GetRandom();
                                FVRObject clipObject = compatibleClips.GetRandom();
                                ammoSpawn = constructor.SpawnPoint_Mag;

                                gameObjectCallback = magazineObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                if (magazineObject.GetGameObject() != null)
                                {
                                    TNHFrameworkLogger.Log($"Spawning magazine ({magazineObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                    GameObject spawnedMag = Object.Instantiate(magazineObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(spawnedMag);
                                    ammoSpawnCount++;
                                }

                                gameObjectCallback = clipObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                if (clipObject.GetGameObject() != null)
                                {
                                    TNHFrameworkLogger.Log($"Spawning clip ({clipObject.ItemID}), Count = {group.NumClipsSpawned}", TNHFrameworkLogger.LogType.TNH);
                                    for (int i = 0; i < group.NumClipsSpawned; i++)
                                    {
                                        GameObject spawnedClip = Object.Instantiate(clipObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                        constructor.M.AddObjectToTrackedList(spawnedClip);
                                        ammoSpawnCount++;
                                    }
                                }
                            }
                            // Otherwise, perform normal logic for spawning ammo objects from current group
                            else
                            {
                                FVRObject ammoObject = null;
                                int numSpawned = 0;

                                if (compatibleMagazines.Any() && group.NumMagsSpawned > 0)
                                {
                                    ammoObject = compatibleMagazines.GetRandom();
                                    numSpawned = group.NumMagsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (compatibleClips.Any() && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = compatibleClips.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (mainObject.CompatibleSpeedLoaders != null && mainObject.CompatibleSpeedLoaders.Any() && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = mainObject.CompatibleSpeedLoaders.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (compatibleRounds.Any() && group.NumRoundsSpawned > 0)
                                {
                                    ammoObject = compatibleRounds.GetRandom();
                                    numSpawned = group.NumRoundsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Ammo;
                                }

                                if (ammoObject != null)
                                {
                                    gameObjectCallback = ammoObject.GetGameObjectAsync();
                                    yield return gameObjectCallback;

                                    if (ammoObject.GetGameObject() != null)
                                    {
                                        TNHFrameworkLogger.Log($"Spawning ammo object normally ({ammoObject.ItemID}), Count = {numSpawned}", TNHFrameworkLogger.LogType.TNH);

                                        for (int i = 0; i < numSpawned; i++)
                                        {
                                            GameObject spawned = Object.Instantiate(ammoObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                            constructor.M.AddObjectToTrackedList(spawned);
                                            ammoSpawnCount++;
                                        }
                                    }
                                }
                            }
                        }

                        // If this object requires picatinny sights, we should try to spawn one
                        if (mainObject.RequiresPicatinnySight && character.RequireSightTable != null)
                        {
                            TNHFrameworkLogger.Log("Spawning required sights", TNHFrameworkLogger.LogType.TNH);

                            FVRObject sight = IM.OD[character.RequireSightTable.GetSpawnedEquipmentGroups().GetRandom().GetRandomObject()];
                            gameObjectCallback = sight.GetGameObjectAsync();
                            yield return gameObjectCallback;

                            if (sight.GetGameObject() != null)
                            {
                                GameObject spawnedSight = Object.Instantiate(sight.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedSight);
                                objectSpawnCount++;

                                TNHFrameworkLogger.Log($"Required sight spawned ({sight.ItemID})", TNHFrameworkLogger.LogType.TNH);
                            }

                            for (int j = 0; j < sight.RequiredSecondaryPieces.Count; j++)
                            {
                                FVRObject objectRequired = sight.RequiredSecondaryPieces[j];
                                gameObjectCallback = objectRequired.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                if (objectRequired.GetGameObject() != null)
                                {
                                    GameObject spawnedRequired = Object.Instantiate(objectRequired.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount + Vector3.up * 0.15f * j, constructor.SpawnPoint_Object.rotation);
                                    constructor.M.AddObjectToTrackedList(spawnedRequired);
                                    objectSpawnCount++;

                                    TNHFrameworkLogger.Log($"Required secondary item for sight spawned ({objectRequired.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                }
                            }
                        }
                        // If this object has bespoke attachments we'll try to spawn one
                        else if (mainObject.BespokeAttachments.Any() && Random.value < group.BespokeAttachmentChance)
                        {
                            TNHFrameworkLogger.Log("Spawning bespoke attachment", TNHFrameworkLogger.LogType.TNH);

                            FVRObject bespoke = null;
                            gameObjectCallback = null;

                            // Bespoke attachment list has not been verified previously, so do try-catch
                            try
                            {
                                bespoke = mainObject.BespokeAttachments.GetRandom();

                                if (bespoke != null)
                                    gameObjectCallback = bespoke.GetGameObjectAsync();
                            }
                            catch
                            {
                                TNHFrameworkLogger.Log($"  Failed to get bespoke object", TNHFrameworkLogger.LogType.TNH);
                            }

                            if (bespoke != null)
                            {
                                yield return gameObjectCallback;

                                if (bespoke.GetGameObject() != null)
                                {
                                    GameObject bespokeObject = Object.Instantiate(bespoke.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                                    constructor.M.AddObjectToTrackedList(bespokeObject);
                                    objectSpawnCount++;
                                    TNHFrameworkLogger.Log($"Bespoke attachment spawned ({bespoke.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                }
                            }
                        }
                    }
                }
            }

            //constructor.allowEntry = true;
            fiAllowEntry.SetValue(constructor, true);
            yield break;
        }

        // This is a wrapper that allows bespokeAttachmentChance to work
        public static GameObject SpawnWeaponCase(TNH_Manager M, float bespokeAttachmentChance, GameObject caseFab, Vector3 position, Vector3 forward,
            FVRObject weapon, int numMag, int numRound, int minAmmo, int maxAmmo, FVRObject ammoObjOverride = null)
        {
            BespokeAttachmentChance = bespokeAttachmentChance;
            return M.SpawnWeaponCase(caseFab, position, forward, weapon, numMag, numRound, minAmmo, maxAmmo, ammoObjOverride);
        }

        [HarmonyPatch(typeof(TNH_Manager), "SpawnWeaponCase")]
        [HarmonyPrefix]
        public static bool SpawnWeaponCase_Replacement(TNH_Manager __instance, ref GameObject __result, ref List<GameObject> ___m_weaponCases,
            GameObject caseFab, Vector3 position, Vector3 forward, FVRObject weapon, int numMag, int numRound, int minAmmo, int maxAmmo, FVRObject ammoObjOverride)
        {
            GameObject caseObj = Object.Instantiate(caseFab, position, Quaternion.LookRotation(forward, Vector3.up));
            TNH_WeaponCrate crateComp = caseObj.GetComponent<TNH_WeaponCrate>();
            ___m_weaponCases.Add(caseObj);

            FVRObject ammoObj = ammoObjOverride ?? __instance.GetSeededRandomAmmoObject(weapon, minAmmo, maxAmmo);

            // Anton pls fix. There's no such thing as round type "none", so it returns a22_LR instead. Check if it matches the weapon.
            if (ammoObj.RoundType == FireArmRoundType.a22_LR && weapon.Category != FVRObject.ObjectCategory.Firearm)
                ammoObj = null;

            int numClipSpeedLoaderRound = 0;

            // Clamp number of ammo objects spawned
            if (ammoObj != null)
            {
                switch (ammoObj.Category)
                {
                case FVRObject.ObjectCategory.Magazine:
                case FVRObject.ObjectCategory.Clip:
                case FVRObject.ObjectCategory.SpeedLoader:
                    numClipSpeedLoaderRound = Mathf.Clamp(numMag, 0, crateComp.Points_MagClipSpeedloader.Count);
                    break;

                case FVRObject.ObjectCategory.Cartridge:
                    numClipSpeedLoaderRound = Mathf.Clamp(numRound, 0, crateComp.Points_Cartridge.Count);
                    break;
                }
            }

            FVRObject sightObj = null;
            FVRObject requiredAttachment_B = null;

            if (weapon.RequiresPicatinnySight)
            {
                sightObj = __instance.GetObjectTable(__instance.C.RequireSightTable).GetRandomObject();

                if (sightObj.RequiredSecondaryPieces.Any())
                    requiredAttachment_B = sightObj.RequiredSecondaryPieces[0];
            }
            // Check the bespoke attachment chance here
            // In vanilla TNH, it ALWAYS spawns a bespoke attachment if there is one
            else if (weapon.BespokeAttachments.Any() && Random.value < BespokeAttachmentChance)
            {
                sightObj = weapon.BespokeAttachments[Random.Range(0, weapon.BespokeAttachments.Count)];
            }

            if (weapon.RequiredSecondaryPieces.Any())
                requiredAttachment_B = weapon.RequiredSecondaryPieces[0];

            bool spawnAmmoAsBox = false;
            if (__instance.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking && weapon.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.Ordnance)
                spawnAmmoAsBox = true;

            crateComp.PlaceWeaponInContainer(weapon, sightObj, requiredAttachment_B, ammoObj, numClipSpeedLoaderRound, spawnAmmoAsBox);

            __result = caseObj;
            return false;
        }
    }
}
