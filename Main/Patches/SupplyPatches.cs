using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class SupplyPatches
    {
        private static readonly MethodInfo miSpawnDefenses = typeof(TNH_SupplyPoint).GetMethod("SpawnDefenses", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnConstructor = typeof(TNH_SupplyPoint).GetMethod("SpawnConstructor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnSecondaryPanel = typeof(TNH_SupplyPoint).GetMethod("SpawnSecondaryPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnBoxes = typeof(TNH_SupplyPoint).GetMethod("SpawnBoxes", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiNumSpawnBonus = typeof(TNH_SupplyPoint).GetField("numSpawnBonus", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveSosigs = typeof(TNH_SupplyPoint).GetField("m_activeSosigs", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int NumConstructors;
        public static int PanelIndex = 0;

        [HarmonyPatch(typeof(TNH_SupplyPoint), "Configure")]
        [HarmonyPrefix]
        public static bool Configure_Replacement(TNH_SupplyPoint __instance, ref bool ___m_isconfigured, ref bool ___m_hasBeenVisited,
            TNH_TakeChallenge t, bool spawnSosigs, bool spawnDefenses, bool spawnConstructor, int minBoxPiles, int maxBoxPiles, bool SpawnToken)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            __instance.InitLights();
            __instance.T = t;
            ___m_isconfigured = true;

            if (spawnSosigs)
            {
                //__instance.SpawnTakeEnemyGroup();
                //miSpawnTakeEnemyGroup.Invoke(__instance, []);
                AnvilManager.Run(SpawnSupplyGroup(__instance, level));
            }

            if (spawnDefenses)
            {
                //SpawnSupplyTurrets(__instance, level);
                miSpawnDefenses.Invoke(__instance, []);
            }

            //int numConstructors = Random.Range(level.MinConstructors, level.MaxConstructors + 1);

            if (spawnConstructor)
            {
                //SpawnSupplyConstructor(__instance, numConstructors);
                //SpawnSecondarySupplyPanel(__instance, level, numConstructors);
                miSpawnConstructor.Invoke(__instance, []);
                miSpawnSecondaryPanel.Invoke(__instance, [TNH_SupplyPoint.SupplyPanelType.All]);
            }

            if (maxBoxPiles > 0)
            {
                //SpawnSupplyBoxes(__instance, level, minBoxPiles, maxBoxPiles, SpawnToken);
                miSpawnBoxes.Invoke(__instance, [minBoxPiles, maxBoxPiles, SpawnToken]);
            }

            ___m_hasBeenVisited = false;
            return false;
        }

        public static IEnumerator SpawnSupplyGroup(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Sosigs_Defense.Shuffle<Transform>();

            int numToSpawn = Random.Range(level.SupplyChallenge.NumGuards - 1, level.SupplyChallenge.NumGuards + 1);
            int numSpawnBonus = (int)fiNumSpawnBonus.GetValue(point);
            numToSpawn += numSpawnBonus;

            if (!LoadedTemplateManager.CurrentCharacter.isCustom)
                numToSpawn = Mathf.Clamp(numToSpawn, 0, 5);

            fiNumSpawnBonus.SetValue(point, numSpawnBonus + 1);
            numToSpawn = Mathf.Clamp(numToSpawn, 0, point.SpawnPoints_Sosigs_Defense.Count);

            TNHFrameworkLogger.Log($"Spawning {numToSpawn} supply guards", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numToSpawn; i++)
            {
                Transform transform = point.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[level.SupplyChallenge.GetTakeChallenge().GID];

                Sosig enemy = point.M.SpawnEnemy(template, transform.position, transform.rotation, level.SupplyChallenge.IFFUsed, false, transform.position, true);

                //point.m_activeSosigs.Add(enemy);
                var activeSosigs = (List<Sosig>)fiActiveSosigs.GetValue(point);
                activeSosigs.Add(enemy);

                yield return new WaitForSeconds(0.1f);
            }

            yield break;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnDefenses")]
        [HarmonyPrefix]
        public static void SpawnDefenses_ShuffleSpawnPoints(TNH_SupplyPoint __instance)
        {
            __instance.SpawnPoints_Turrets.Shuffle<Transform>();
        }

        // Allow spawning of multiple Object Constructors
        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnConstructor")]
        [HarmonyPrefix]
        public static bool SpawnConstructor_Replacement(TNH_SupplyPoint __instance, ref GameObject ___m_constructor)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            TNHFrameworkLogger.Log("Spawning constructor panel", TNHFrameworkLogger.LogType.TNH);

            __instance.SpawnPoints_Panels.Shuffle<Transform>();

            int numConstructors = Random.Range(level.MinConstructors, level.MaxConstructors + 1);
            numConstructors = Mathf.Clamp(numConstructors, 0,  __instance.SpawnPoints_Panels.Count);
            NumConstructors = numConstructors;

            for (int i = 0; i < numConstructors; i++)
            {
                GameObject constructor = __instance.M.SpawnObjectConstructor(__instance.SpawnPoints_Panels[i]);
                TNHFramework.SpawnedConstructors.Add(constructor);
            }

            return false;
        }

        // Spawn all the new types of panels
        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnSecondaryPanel")]
        [HarmonyPrefix]
        public static bool SpawnSecondaryPanel_Replacement(TNH_SupplyPoint __instance)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            TNHFrameworkLogger.Log("Spawning secondary panels", TNHFrameworkLogger.LogType.TNH);

            List<PanelType> panelTypes;
            int numPanels;

            if (__instance.M.LevelName == "Institution" && !LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                panelTypes = [PanelType.AmmoReloader, PanelType.MagDuplicator, PanelType.Recycler];
                numPanels = panelTypes.Count;
            }
            else
            {
                panelTypes = [.. level.PossiblePanelTypes];
                numPanels = Random.Range(level.MinPanels, level.MaxPanels + 1);
            }

            if (panelTypes.Count < 1 || numPanels < 1)
                return false;

            numPanels = Mathf.Clamp(numPanels, 0, __instance.SpawnPoints_Panels.Count - NumConstructors);

            for (int i = NumConstructors; i < NumConstructors + numPanels; i++)
            {
                TNHFrameworkLogger.Log("Panel index : " + i, TNHFrameworkLogger.LogType.TNH);

                // Go through the panels, and loop if we have gone too far 
                PanelType panelType = panelTypes[PanelIndex % panelTypes.Count];
                PanelIndex = (PanelIndex + 1) % panelTypes.Count;

                TNHFrameworkLogger.Log("Panel type selected : " + panelType, TNHFrameworkLogger.LogType.TNH);

                GameObject panel;

                if (panelType == PanelType.AmmoReloader)
                {
                    panel = __instance.M.SpawnAmmoReloader(__instance.SpawnPoints_Panels[i]);
                }
                else if (panelType == PanelType.MagDuplicator)
                {
                    panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);

                    if (TNHFramework.AlwaysMagUpgrader.Value)
                        panel.AddComponent(typeof(MagazinePanel));
                }
                else if (panelType == PanelType.MagUpgrader || panelType == PanelType.MagPurchase)
                {
                    panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(MagazinePanel));
                }
                else if (panelType == PanelType.Recycler)
                {
                    panel = __instance.M.SpawnGunRecycler(__instance.SpawnPoints_Panels[i]);
                }
                else if (panelType == PanelType.AmmoPurchase)
                {
                    panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(AmmoPurchasePanel));
                }
                else if (panelType == PanelType.AddFullAuto)
                {
                    panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FullAutoPanel));
                }
                else if (panelType == PanelType.FireRateUp || panelType == PanelType.FireRateDown)
                {
                    panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FireRatePanel));
                }
                else
                {
                    panel = __instance.M.SpawnAmmoReloader(__instance.SpawnPoints_Panels[i]);
                }

                // If we spawned a panel, add it to the global list
                if (panel != null)
                {
                    TNHFrameworkLogger.Log("Panel spawned successfully", TNHFrameworkLogger.LogType.TNH);
                    TNHFramework.SpawnedPanels.Add(panel);
                }
                else
                {
                    TNHFrameworkLogger.LogWarning("Failed to spawn secondary panel!");
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnBoxes")]
        [HarmonyPrefix]
        public static bool SpawnBoxes_Replacement(TNH_SupplyPoint __instance, ref List<GameObject> ___m_spawnBoxes, int min, int max, bool SpawnToken)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            __instance.SpawnPoints_Boxes.Shuffle();

            // Custom Character behavior:
            // - Every supply point has the same min and max number of boxes
            // - Every supply point has the same min and max number of tokens
            // - Every box that doesn't have a token has the same probability of having health
            if (LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                int minTokens = level.MinTokensPerSupply;
                int maxTokens = level.MaxTokensPerSupply;

                int minBoxes = level.MinBoxesSpawned;
                int maxBoxes = level.MaxBoxesSpawned;
                int boxesToSpawn = Random.Range(minBoxes, maxBoxes + 1);

                TNHFrameworkLogger.Log($"Going to spawn {boxesToSpawn} boxes at this point -- Min ({minBoxes}), Max ({maxBoxes})", TNHFrameworkLogger.LogType.TNH);

                for (int i = 0; i < boxesToSpawn; i++)
                {
                    Transform spawnTransform = __instance.SpawnPoints_Boxes[Random.Range(0, __instance.SpawnPoints_Boxes.Count)];
                    Vector3 position = spawnTransform.position + Vector3.up * 0.1f + Vector3.right * Random.Range(-0.5f, 0.5f) + Vector3.forward * Random.Range(-0.5f, 0.5f);
                    Quaternion rotation = Quaternion.Slerp(spawnTransform.rotation, Random.rotation, 0.1f);

                    GameObject box = Object.Instantiate(__instance.M.Prefabs_ShatterableCrates[Random.Range(0, __instance.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                    ___m_spawnBoxes.Add(box);
                }

                int tokensSpawned = 0;

                if (!__instance.M.UsesUberShatterableCrates)
                {
                    foreach (GameObject boxObj in ___m_spawnBoxes)
                    {
                        if (tokensSpawned < minTokens)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(__instance.M);
                            tokensSpawned++;
                        }
                        else if (tokensSpawned < maxTokens && Random.value < level.BoxTokenChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(__instance.M);
                            tokensSpawned++;
                        }
                        else if (Random.value < level.BoxHealthChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(__instance.M);
                        }
                    }
                }
                else if (__instance.M.UsesUberShatterableCrates)
                {
                    for (int k = 0; k < ___m_spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = ___m_spawnBoxes[k].GetComponent<UberShatterable>();
                        if (tokensSpawned < minTokens)
                        {
                            SpawnBoxWithToken(__instance, boxComp);
                            tokensSpawned++;
                        }
                        else if (tokensSpawned < maxTokens && Random.value < level.BoxTokenChance)
                        {
                            SpawnBoxWithToken(__instance, boxComp);
                            tokensSpawned++;
                        }
                        else if (Random.value < level.BoxHealthChance)
                        {
                            SpawnBoxWithHealth(__instance, boxComp);
                        }
                        else
                        {
                            SpawnBoxEmpty(__instance, boxComp);
                        }
                    }
                }
            }
            // Vanilla character behavior:
            // - Only one box per Take phase has a token (spawnToken is only true for one supply point)
            // - Hallways has 1-2 piles of 1-3 boxes per supply point; large maps have only 1 supply point with 2-3 piles of 1-3 boxes
            // - Each supply point has up to 3 health, and each of these has a different probability of spawning
            else
            {
                bool spawnHealth1 = (Random.Range(0f, 1f) > 0.1f);
                bool spawnHealth2 = (Random.Range(0f, 1f) > 0.4f);
                bool spawnHealth3 = (Random.Range(0f, 1f) > 0.8f);

                __instance.SpawnPoints_Boxes.Shuffle<Transform>();

                int boxPiles = Random.Range(min, max + 1);
                if (boxPiles < 1)
                    return false;

                for (int i = 0; i < boxPiles; i++)
                {
                    Transform transform = __instance.SpawnPoints_Boxes[i];

                    int boxesPerPile = Random.Range(1, 3);
                    for (int j = 0; j < boxesPerPile; j++)
                    {
                        Vector3 position = transform.position + Vector3.up * 0.1f + Vector3.up * 0.85f * (float)j;
                        Vector3 onUnitSphere = Random.onUnitSphere;
                        onUnitSphere.y = 0f;
                        onUnitSphere.Normalize();
                        Quaternion rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(onUnitSphere, Vector3.up), 0.1f);

                        GameObject item = Object.Instantiate<GameObject>(__instance.M.Prefabs_ShatterableCrates[Random.Range(0, __instance.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                        ___m_spawnBoxes.Add(item);
                    }
                }

                ___m_spawnBoxes.Shuffle();

                if (!__instance.M.UsesUberShatterableCrates)
                {
                    int spawnIndex = 0;
                    TNH_ShatterableCrate boxComp;

                    if (SpawnToken && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingToken(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth1 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth2 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth3 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(__instance.M);
                        //spawnIndex++;
                    }
                }
                else
                {
                    for (int k = 0; k < ___m_spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = ___m_spawnBoxes[k].GetComponent<UberShatterable>();

                        if (SpawnToken)
                        {
                            SpawnToken = false;
                            SpawnBoxWithToken(__instance, boxComp);
                        }
                        else if (spawnHealth1)
                        {
                            spawnHealth1 = false;
                            SpawnBoxWithHealth(__instance, boxComp);
                        }
                        else if (spawnHealth2)
                        {
                            spawnHealth2 = false;
                            SpawnBoxWithHealth(__instance, boxComp);
                        }
                        else if (spawnHealth3)
                        {
                            spawnHealth3 = false;
                            SpawnBoxWithHealth(__instance, boxComp);
                        }
                        else
                        {
                            SpawnBoxEmpty(__instance, boxComp);
                        }
                    }
                }
            }

            return false;
        }

        private static void SpawnBoxWithToken(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Token);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
        }

        private static void SpawnBoxWithHealth(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_HealthMinor);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
        }

        private static void SpawnBoxEmpty(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Empty);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnTakeEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnTakeEnemyGroupReplacement(TNH_SupplyPoint __instance, ref int ___numSpawnBonus, ref List<Sosig> ___m_activeSosigs)
        {
            __instance.SpawnPoints_Sosigs_Defense.Shuffle();
            //__instance.SpawnPoints_Sosigs_Defense.Shuffle();

            int numGuards;
            if (LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                numGuards = __instance.T.NumGuards;
            }
            else
            {
                numGuards = Random.Range(__instance.T.NumGuards - 1, __instance.T.NumGuards + 1);
                numGuards += ___numSpawnBonus;
                numGuards = Mathf.Clamp(numGuards, 0, 5);
                ___numSpawnBonus++;
            }

            TNHFrameworkLogger.Log($"Spawning {__instance.T.NumGuards} supply guards via SpawnTakeEnemyGroup()", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numGuards && i < __instance.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = __instance.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[__instance.T.GID];

                Sosig enemy = __instance.M.SpawnEnemy(template, transform.position, transform.rotation, __instance.T.IFFUsed, false, transform.position, true);
                ___m_activeSosigs.Add(enemy);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "ConfigureAtBeginning")]
        [HarmonyPrefix]
        public static bool SpawnStartingEquipment(TNH_SupplyPoint __instance, ref List<GameObject> ___m_trackedObjects)
        {
            __instance.InitLights();
            ___m_trackedObjects.Clear();

            if (__instance.M.ItemSpawnerMode == TNH_ItemSpawnerMode.On)
            {
                __instance.M.ItemSpawner.transform.position = __instance.SpawnPoints_Panels[0].position + Vector3.up * 0.8f;
                __instance.M.ItemSpawner.transform.rotation = __instance.SpawnPoints_Panels[0].rotation;
                __instance.M.ItemSpawner.SetActive(true);
            }

            for (int i = 0; i < __instance.SpawnPoint_Tables.Count; i++)
            {
                GameObject item = Object.Instantiate(__instance.M.Prefab_MetalTable, __instance.SpawnPoint_Tables[i].position, __instance.SpawnPoint_Tables[i].rotation);
                ___m_trackedObjects.Add(item);
            }

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

            if (character.PrimaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.PrimaryWeapon.PrimaryGroup ?? character.PrimaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();

                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = ConstructorPatches.SpawnWeaponCase(__instance.M, selectedGroup.BespokeAttachmentChance, __instance.M.Prefab_WeaponCaseLarge, __instance.SpawnPoint_CaseLarge.position, __instance.SpawnPoint_CaseLarge.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    ___m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.SecondaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.SecondaryWeapon.PrimaryGroup ?? character.SecondaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();

                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = ConstructorPatches.SpawnWeaponCase(__instance.M, selectedGroup.BespokeAttachmentChance, __instance.M.Prefab_WeaponCaseSmall, __instance.SpawnPoint_CaseSmall.position, __instance.SpawnPoint_CaseSmall.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    ___m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.TertiaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryWeapon.PrimaryGroup ?? character.TertiaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoint_Melee.position, __instance.SpawnPoint_Melee.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.PrimaryItem != null)
            {
                EquipmentGroup selectedGroup = character.PrimaryItem.PrimaryGroup ?? character.PrimaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoints_SmallItem[0].position, __instance.SpawnPoints_SmallItem[0].rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.SecondaryItem != null)
            {
                EquipmentGroup selectedGroup = character.SecondaryItem.PrimaryGroup ?? character.SecondaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    Transform spawnPoint = __instance.SpawnPoints_SmallItem.Count >= 2 ? __instance.SpawnPoints_SmallItem[1] : __instance.SpawnPoints_SmallItem[0];

                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, spawnPoint.position, spawnPoint.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.TertiaryItem != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryItem.PrimaryGroup ?? character.TertiaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    Transform spawnPoint = __instance.SpawnPoints_SmallItem.Count >= 3 ? __instance.SpawnPoints_SmallItem[2] : __instance.SpawnPoints_SmallItem[__instance.SpawnPoints_SmallItem.Count - 1];

                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, spawnPoint.position, spawnPoint.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.Shield != null)
            {
                EquipmentGroup selectedGroup = character.Shield.PrimaryGroup ?? character.Shield.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoint_Shield.position, __instance.SpawnPoint_Shield.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (TNHFramework.UnlimitedTokens.Value)
                __instance.M.AddTokens(999999, false);

            return false;
        }
    }
}
