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
    static class SupplyPatches
    {
        private static readonly MethodInfo miSpawnDefenses = typeof(TNH_SupplyPoint).GetMethod("SpawnDefenses", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnConstructor = typeof(TNH_SupplyPoint).GetMethod("SpawnConstructor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnSecondaryPanel = typeof(TNH_SupplyPoint).GetMethod("SpawnSecondaryPanel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnBoxes = typeof(TNH_SupplyPoint).GetMethod("SpawnBoxes", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiNumSpawnBonus = typeof(TNH_SupplyPoint).GetField("numSpawnBonus", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveSosigs = typeof(TNH_SupplyPoint).GetField("m_activeSosigs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiTrackedObjects = typeof(TNH_SupplyPoint).GetField("m_trackedObjects", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int NumConstructors;
        public static int PanelIndex = 0;

        [HarmonyPatch(typeof(TNH_SupplyPoint), "Reinforce")]
        [HarmonyPrefix]
        public static bool Reinforce_Replacement(TNH_SupplyPoint __instance, ref float ___m_timeSinceReinforceCall, ref List<Sosig> ___m_activeSosigs)
        {
            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
                return false;

            if (___m_timeSinceReinforceCall < 8f)
                return false;

            ___m_timeSinceReinforceCall = 0f;

            if (!___m_activeSosigs.Any())
                AnvilManager.Run(SpawnTakeEnemyGroup(__instance, LoadedTemplateManager.CurrentLevel));

            return false;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "Configure")]
        [HarmonyPrefix]
        public static bool Configure_Replacement(TNH_SupplyPoint __instance, ref GameObject ___m_gameBounds, ref System.Random ___m_assortedRand, ref bool ___m_isconfigured,
            ref int ___numPanelsSpawned, ref bool ___m_hasBeenVisited, TNH_TakeChallenge t, bool spawnSosigs, bool spawnDefenses, bool spawnConstructor,
            TNH_SupplyPoint.SupplyPanelType panelType, int minBoxPiles, int maxBoxPiles, bool SpawnToken)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
            {
                ___m_gameBounds = Object.Instantiate<GameObject>(__instance.M.ResourceLib.Prefab_WarpBounds, __instance.Bounds.transform.position, __instance.Bounds.transform.rotation);
                ___m_gameBounds.transform.position = __instance.Bounds.transform.position;
                ___m_gameBounds.transform.rotation = __instance.Bounds.transform.rotation;
                ___m_gameBounds.transform.localScale = __instance.Bounds.transform.localScale + Vector3.one * 0.1f;
            }

            ___m_assortedRand = new System.Random(__instance.M.CoreRand.Next());
            __instance.InitLights();
            __instance.T = t;
            ___m_isconfigured = true;
            ___numPanelsSpawned = 0;

            if (spawnSosigs)
            {
                //AnvilManager.Run(SpawnTakeEnemyGroup(__instance, level));
                __instance.StartCoroutine(SpawnTakeEnemyGroup(__instance, level));
            }

            if (spawnDefenses)
            {
                //SpawnSupplyTurrets(__instance, level);
                miSpawnDefenses.Invoke(__instance, []);
            }

            //int numConstructors = Random.Range(level.MinConstructors, level.MaxConstructors + 1);

            if (spawnConstructor)
            {
                //SpawnConstructor();
                //SpawnSecondaryPanel(panelType);
                miSpawnConstructor.Invoke(__instance, []);
                miSpawnSecondaryPanel.Invoke(__instance, [panelType]);
            }

            if (maxBoxPiles > 0)
            {
                //SpawnSupplyBoxes(__instance, level, minBoxPiles, maxBoxPiles, SpawnToken);
                miSpawnBoxes.Invoke(__instance, [minBoxPiles, maxBoxPiles, SpawnToken]);
            }

            ___m_hasBeenVisited = false;
            return false;
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
        public static bool SpawnConstructor_Replacement(TNH_SupplyPoint __instance, ref int ___numPanelsSpawned)
        {
            if (!__instance.M.C.UsesObjectConstructor)
                return false;

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
                ___numPanelsSpawned++;
            }

            return false;
        }

        // Spawn all the new types of panels
        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnSecondaryPanel")]
        [HarmonyPrefix]
        public static bool SpawnSecondaryPanel_Replacement(TNH_SupplyPoint __instance, TNH_SupplyPoint.SupplyPanelType t)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            TNHFrameworkLogger.Log("Spawning secondary panels", TNHFrameworkLogger.LogType.TNH);

            int minPanels = level.MinPanels;
            int maxPanels = level.MaxPanels;

            if (t == TNH_SupplyPoint.SupplyPanelType.All)
            {
                // For custom characters, spawn at least 3 panels from the possible panel types
                if (LoadedTemplateManager.CurrentCharacter.isCustom)
                {
                    minPanels = Mathf.Max(minPanels, 3, minPanels);
                    maxPanels = Mathf.Max(maxPanels, minPanels, maxPanels);
                }
                // For vanilla/workshop characters, spawn all three vanilla panel types (if allowed)
                else
                {
                    GameObject panel;
                    int i = NumConstructors;

                    if (__instance.SpawnPoints_Panels.Count > i && __instance.M.C.UsesAmmoReloader)
                    {
                        panel = __instance.M.SpawnAmmoReloader(__instance.SpawnPoints_Panels[i++]);
                        TNHFramework.SpawnedPanels.Add(panel);
                    }

                    if (__instance.SpawnPoints_Panels.Count > i && __instance.M.C.UsesMagDuplicator)
                    {
                        panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i++]);

                        if (TNHFramework.AlwaysMagUpgrader.Value)
                            panel.AddComponent(typeof(MagazinePanel));

                        TNHFramework.SpawnedPanels.Add(panel);
                    }

                    if (__instance.SpawnPoints_Panels.Count > i && __instance.M.C.UsesGunRecycler)
                    {
                        panel = __instance.M.SpawnGunRecycler(__instance.SpawnPoints_Panels[i++], __instance.M.C.SosigLootTable != null);
                        TNHFramework.SpawnedPanels.Add(panel);
                    }

                    return false;
                }
            }

            List<PanelType> panelTypes = [.. level.PossiblePanelTypes];
            int numPanels = Random.Range(minPanels, maxPanels + 1);

            if (!panelTypes.Any() || numPanels <= 0)
                return false;

            numPanels = Mathf.Clamp(numPanels, 0, __instance.SpawnPoints_Panels.Count - NumConstructors);

            for (int i = NumConstructors; i < NumConstructors + numPanels; i++)
            {
                TNHFrameworkLogger.Log("Panel index : " + i, TNHFrameworkLogger.LogType.TNH);

                // Go through the panels, and loop if we have gone too far 
                PanelType panelType = panelTypes[PanelIndex % panelTypes.Count];
                PanelIndex = (PanelIndex + 1) % panelTypes.Count;

                TNHFrameworkLogger.Log("Panel type selected : " + panelType, TNHFrameworkLogger.LogType.TNH);

                GameObject panel = null;

                if (panelType == PanelType.AmmoReloader && __instance.M.C.UsesAmmoReloader)
                {
                    panel = __instance.M.SpawnAmmoReloader(__instance.SpawnPoints_Panels[i]);
                }
                else if (panelType == PanelType.MagDuplicator && __instance.M.C.UsesMagDuplicator)
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
                else if (panelType == PanelType.Recycler && __instance.M.C.UsesGunRecycler)
                {
                    panel = __instance.M.SpawnGunRecycler(__instance.SpawnPoints_Panels[i], __instance.M.C.SosigLootTable != null);
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

                // If nothing was spawned because of restrictions, try to spawn a fallback
                if (panel == null)
                {
                    if (__instance.M.C.UsesGunRecycler)
                    {
                        panel = __instance.M.SpawnGunRecycler(__instance.SpawnPoints_Panels[i], __instance.M.C.SosigLootTable != null);
                    }
                    else if (__instance.M.C.UsesMagDuplicator)
                    {
                        panel = __instance.M.SpawnMagDuplicator(__instance.SpawnPoints_Panels[i]);

                        if (TNHFramework.AlwaysMagUpgrader.Value)
                            panel.AddComponent(typeof(MagazinePanel));
                    }
                    else if (__instance.M.C.UsesAmmoReloader)
                    {
                        panel = __instance.M.SpawnAmmoReloader(__instance.SpawnPoints_Panels[i]);
                    }
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
                bool useLoot = (__instance.M.C.SosigLootTable != null && __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance > 0f);

                if (!__instance.M.UsesUberShatterableCrates)
                {
                    foreach (GameObject boxObj in ___m_spawnBoxes)
                    {
                        if (tokensSpawned < minTokens)
                        {
                            if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetUsesLoot(__instance.M);
                            }
                            else
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(__instance.M);
                                tokensSpawned++;
                            }
                        }
                        else if (tokensSpawned < maxTokens && Random.value < level.BoxTokenChance)
                        {
                            if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetUsesLoot(__instance.M);
                            }
                            else
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(__instance.M);
                                tokensSpawned++;
                            }
                        }
                        else if (useLoot)
                        {
                            if (Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetUsesLoot(__instance.M);
                            }
                        }
                        else
                        {
                            if (Random.value < level.BoxHealthChance)
                            {
                                boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(__instance.M);
                            }
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
                            SpawnBoxWithToken(__instance, boxComp, useLoot);
                            tokensSpawned++;
                        }
                        else if (tokensSpawned < maxTokens && Random.value < level.BoxTokenChance)
                        {
                            SpawnBoxWithToken(__instance, boxComp, useLoot);
                            tokensSpawned++;
                        }
                        else if (Random.value < level.BoxHealthChance)
                        {
                            SpawnBoxWithHealth(__instance, boxComp, useLoot);
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
                if (boxPiles <= 0)
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
                bool useLoot = (__instance.M.C.SosigLootTable != null && __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance > 0f);

                if (!__instance.M.UsesUberShatterableCrates)
                {
                    int spawnIndex = 0;
                    TNH_ShatterableCrate boxComp;

                    if (SpawnToken && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            boxComp.SetUsesLoot(__instance.M);
                        else
                            boxComp.SetHoldingToken(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth1 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            boxComp.SetUsesLoot(__instance.M);
                        else
                            boxComp.SetHoldingHealth(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth2 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            boxComp.SetUsesLoot(__instance.M);
                        else
                            boxComp.SetHoldingHealth(__instance.M);
                        spawnIndex++;
                    }

                    if (spawnHealth3 && ___m_spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = ___m_spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        if (useLoot && Random.value <= __instance.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
                            boxComp.SetUsesLoot(__instance.M);
                        else
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
                            SpawnBoxWithToken(__instance, boxComp, useLoot);
                        }
                        else if (spawnHealth1)
                        {
                            spawnHealth1 = false;
                            SpawnBoxWithHealth(__instance, boxComp, useLoot);
                        }
                        else if (spawnHealth2)
                        {
                            spawnHealth2 = false;
                            SpawnBoxWithHealth(__instance, boxComp, useLoot);
                        }
                        else if (spawnHealth3)
                        {
                            spawnHealth3 = false;
                            SpawnBoxWithHealth(__instance, boxComp, useLoot);
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

        private static void SpawnBoxWithToken(TNH_SupplyPoint point, UberShatterable boxComp, bool useLoot)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
            
            if (useLoot && Random.value <= point.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
            {
                boxComp.SetUsesLoot();
            }
            else
            {
                boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Token);
                boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
                boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
            }
        }

        private static void SpawnBoxWithHealth(TNH_SupplyPoint point, UberShatterable boxComp, bool useLoot)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);

            if (useLoot && Random.value <= point.M.C.SosigLootTable.LootGroup_Boxes.SpawnChance)
            {
                boxComp.SetUsesLoot();
            }
            else
            {
                boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_HealthMinor);
                boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
                boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
            }
        }

        private static void SpawnBoxEmpty(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Empty);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
        }

        public static IEnumerator SpawnTakeEnemyGroup(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Sosigs_Defense.Shuffle<Transform>();

            int numToSpawn = Random.Range(level.SupplyChallenge.NumGuards - 1, level.SupplyChallenge.NumGuards + 1);
            int numSpawnBonus = (int)fiNumSpawnBonus.GetValue(point);
            numToSpawn += numSpawnBonus;

            if (!LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                numToSpawn = Mathf.Clamp(numToSpawn, 0, 5);
                fiNumSpawnBonus.SetValue(point, numSpawnBonus + 1);
            }

            numToSpawn = Mathf.Clamp(numToSpawn, 0, point.SpawnPoints_Sosigs_Defense.Count);

            TNHFrameworkLogger.Log($"Spawning {numToSpawn} supply guards", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numToSpawn; i++)
            {
                Transform transform = point.SpawnPoints_Sosigs_Defense[i];

                SosigEnemyTemplate template = point.T.OverrideGID ?? ManagerSingleton<IM>.Instance.odicSosigObjsByID[level.SupplyChallenge.GetTakeChallenge().GID];
                Sosig enemy = point.M.SpawnEnemy(template, transform.position, transform.rotation, level.SupplyChallenge.IFFUsed, false, transform.position, true);

                //point.m_activeSosigs.Add(enemy);
                var activeSosigs = (List<Sosig>)fiActiveSosigs.GetValue(point);
                activeSosigs.Add(enemy);
                point.M.RegisterSupplyGuard(enemy);

                yield return new WaitForSeconds(0.1f);
            }

            yield break;
        }

        [HarmonyPatch(typeof(TNH_SupplyPoint), "ConfigureAtBeginning")]
        [HarmonyPrefix]
        public static bool ConfigureAtBeginning_Replacement(TNH_SupplyPoint __instance, ref GameObject ___m_gameBounds, ref System.Random ___m_assortedRand, ref List<GameObject> ___m_trackedObjects, int seed)
        {
            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
            {
                ___m_gameBounds = Object.Instantiate<GameObject>(__instance.M.ResourceLib.Prefab_WarpBounds, __instance.Bounds.transform.position, __instance.Bounds.transform.rotation);
                ___m_gameBounds.transform.position = __instance.Bounds.transform.position;
                ___m_gameBounds.transform.rotation = __instance.Bounds.transform.rotation;
                ___m_gameBounds.transform.localScale = __instance.Bounds.transform.localScale + Vector3.one * 0.1f;
            }

            ___m_assortedRand = new System.Random(seed);
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
                GameObject originalTable = __instance.OverrideTable ?? __instance.M.Prefab_MetalTable;
                GameObject item = Object.Instantiate(originalTable, __instance.SpawnPoint_Tables[i].position, __instance.SpawnPoint_Tables[i].rotation);
                ___m_trackedObjects.Add(item);
            }

            // TODO: Split this into a coroutine

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

            if (character.HasPrimaryWeapon && character.PrimaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.PrimaryWeapon.PrimaryGroup ?? character.PrimaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    TNHFrameworkLogger.Log("Spawning Primary Weapon", TNHFrameworkLogger.LogType.TNH);
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();

                    FVRObject selectedItem = IM.OD[selectedGroup.GetRandomObject()];
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

            if (character.HasSecondaryWeapon && character.SecondaryWeapon != null)
            {
                TNHFrameworkLogger.Log("Spawning Secondary Weapon", TNHFrameworkLogger.LogType.TNH);
                EquipmentGroup selectedGroup = character.SecondaryWeapon.PrimaryGroup ?? character.SecondaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();

                    FVRObject selectedItem = IM.OD[selectedGroup.GetRandomObject()];
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

            if (character.HasTertiaryWeapon && character.TertiaryWeapon != null)
            {
                TNHFrameworkLogger.Log("Spawning Tertiary Weapon", TNHFrameworkLogger.LogType.TNH);
                EquipmentGroup selectedGroup = character.TertiaryWeapon.PrimaryGroup ?? character.TertiaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoint_Melee.position, __instance.SpawnPoint_Melee.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.HasPrimaryItem && character.PrimaryItem != null)
            {
                TNHFrameworkLogger.Log("Spawning Primary Item", TNHFrameworkLogger.LogType.TNH);
                EquipmentGroup selectedGroup = character.PrimaryItem.PrimaryGroup ?? character.PrimaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoints_SmallItem[0].position, __instance.SpawnPoints_SmallItem[0].rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.HasSecondaryItem && character.SecondaryItem != null)
            {
                TNHFrameworkLogger.Log("Spawning Secondary Item", TNHFrameworkLogger.LogType.TNH);
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

            if (character.HasTertiaryItem && character.TertiaryItem != null)
            {
                TNHFrameworkLogger.Log("Spawning Tertiary Item", TNHFrameworkLogger.LogType.TNH);
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
                TNHFrameworkLogger.Log("Spawning Shield", TNHFrameworkLogger.LogType.TNH);
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

        public static IEnumerator SpawnStartingEquipment(TNH_SupplyPoint point, CustomCharacter c)
        {
            Dictionary<LoadoutEntry, FVRObject> spawnedLoadoutObj = [];

            if (c.PrimaryWeapon != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.PrimaryWeapon, point.SpawnPoint_CaseLarge, point.M.Prefab_WeaponCaseLarge);

            if (c.SecondaryWeapon != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.SecondaryWeapon, point.SpawnPoint_CaseSmall, point.M.Prefab_WeaponCaseSmall);

            if (c.TertiaryWeapon != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.TertiaryWeapon, point.SpawnPoint_Melee, null);

            if (c.PrimaryItem != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.PrimaryItem, point.SpawnPoints_SmallItem[0], null);

            if (c.SecondaryItem != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.SecondaryItem, point.SpawnPoints_SmallItem[1], null);

            if (c.TertiaryItem != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.TertiaryItem, point.SpawnPoints_SmallItem[2], null);

            if (c.Shield != null)
                yield return SpawnLoadoutEntry(point, spawnedLoadoutObj, c, c.Shield, point.SpawnPoint_Shield, null);

            yield break;
        }

        public static IEnumerator SpawnLoadoutEntry(TNH_SupplyPoint point, Dictionary<LoadoutEntry, FVRObject> entrySpawns, CustomCharacter c, LoadoutEntry entry, Transform spawnTrans, GameObject casePrefab)
        {
            if (entry == null)
                yield break;

            if (entry == c.SecondaryWeapon && c.SecondaryWeaponCopiesPrimary)
            {
                FVRObject objToSpawn = entrySpawns[c.PrimaryWeapon];
                EquipmentGroup group = c.PrimaryWeapon.PrimaryGroup ?? c.PrimaryWeapon.BackupGroup;

                if (objToSpawn != null)
                {
                    if (casePrefab != null)
                    {
                        SpawnLoadoutCase(point, group, c.PrimaryWeapon.AmmoObjectOverride, objToSpawn, null, spawnTrans, casePrefab);
                    }
                    else
                    {
                        AnvilCallback<GameObject> loadReq = objToSpawn.GetGameObjectAsync();
                        yield return loadReq;

                        GameObject spawnedObj = Object.Instantiate<GameObject>(loadReq.Result, spawnTrans.position, spawnTrans.rotation);
                        point.M.AddObjectToTrackedList(spawnedObj);

                        if (objToSpawn.UsesRoundTypeFlag)
                            yield return SpawnAmmoForObject(point, c, group, null, objToSpawn, spawnTrans.position + spawnTrans.right * 0.15f);
                    }
                }
            }
            else
            {
                EquipmentGroup selectedGroup = c.PrimaryWeapon.PrimaryGroup ?? c.PrimaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();
                    ObjectTable table = new();
                    table.Initialize(selectedGroup.GetObjectTableDef());

                    FVRObject objToSpawn = null;
                    VaultFile vaultFile = null;
                    SavedGunSerializable vaultFileLegacy = null;

                    // Vault files cannot be spawned from a case
                    if (casePrefab != null || !table.UsesVaultFiles())
                    {
                        string item = selectedGroup.GetRandomObject();
                        TNHFrameworkLogger.Log("Item selected: " + item, TNHFrameworkLogger.LogType.TNH);

                        if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(item))
                        {
                            TNHFrameworkLogger.Log("Item is a vaulted gun", TNHFrameworkLogger.LogType.TNH);
                            vaultFile = LoadedTemplateManager.LoadedVaultFiles[item];
                            objToSpawn = IM.OD[vaultFile.Objects[0].Elements[0].ObjectID];
                        }
                        else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(item))
                        {
                            TNHFrameworkLogger.Log("Item is a legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                            vaultFileLegacy = LoadedTemplateManager.LoadedLegacyVaultFiles[item];
                            objToSpawn = vaultFileLegacy.GetGunObject();
                        }
                        else
                        {
                            TNHFrameworkLogger.Log("Item is a normal object", TNHFrameworkLogger.LogType.TNH);
                            objToSpawn = IM.OD[item];
                        }
                    }

                    entrySpawns[entry] = objToSpawn;

                    if (casePrefab != null)
                    {
                        SpawnLoadoutCase(point, selectedGroup, c.PrimaryWeapon.AmmoObjectOverride, objToSpawn, null, spawnTrans, casePrefab);
                    }
                    else if (table.UsesVaultFiles())
                    {
                        VaultFile vf = table.GetRandomVaultFile();
                        List<FVRPhysicalObject> spawnedObjs = null;
                        bool success;

                        if (table.FileUsage == ObjectTableDef.VaultFileUsage.WholeFile)
                        {
                            success = VaultSystem.SpawnVaultFile(vf, spawnTrans, true, false, false, out string errorMessage, Vector3.up * 0.4f, delegate (List<FVRPhysicalObject> vfo)
                            {
                                spawnedObjs = vfo;
                            }, false, -1);
                        }
                        else
                        {
                            if (table.FileUsage != ObjectTableDef.VaultFileUsage.SingleObject)
                                throw new System.InvalidOperationException();

                            success = VaultSystem.SpawnVaultFile(vf, spawnTrans, true, false, false, out string errorMessage, Vector3.up * 0.4f, delegate (List<FVRPhysicalObject> vfo)
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

                        if (!spawnedObjs.Any() || spawnedObjs[0] == null)
                            yield break;

                        FVRObject spawnedObj = spawnedObjs[0].ObjectWrapper;

                        if (spawnedObj != null && spawnedObj.UsesRoundTypeFlag && !spawnedObjs.Any() && table.FileUsage == ObjectTableDef.VaultFileUsage.SingleObject)
                            yield return SpawnAmmoForObject(point, c, selectedGroup, table, spawnedObj, spawnTrans.position + spawnTrans.right * 0.15f);
                    }
                    else if (vaultFile != null)
                    {
                        VaultSystem.ReturnObjectListDelegate del = new((objs) => TNHFrameworkUtils.TrackVaultObjects(point.M, objs));
                        TNHFrameworkLogger.Log("Spawning vault gun", TNHFrameworkLogger.LogType.TNH);
                        VaultSystem.SpawnVaultFile(vaultFile, spawnTrans, true, false, false, out _, Vector3.zero, del, false);
                    }
                    // If this is a vault file, we have to spawn it through a routine. Otherwise we just instantiate it
                    else if (vaultFileLegacy != null)
                    {
                        TNHFrameworkLogger.Log("Spawning legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                        AnvilManager.Run(TNHFrameworkUtils.SpawnLegacyVaultFile(vaultFileLegacy, spawnTrans.position, spawnTrans.rotation, point.M));
                        // SpawnFirearm adds the objects to the tracked objects list
                    }
                    else
                    {
                        TNHFrameworkLogger.Log("Spawning normal item", TNHFrameworkLogger.LogType.TNH);
                        AnvilCallback<GameObject> loadReq = objToSpawn.GetGameObjectAsync();
                        yield return loadReq;

                        if (objToSpawn.GetGameObject() != null)
                        {
                            GameObject spawnedObj = Object.Instantiate(objToSpawn.GetGameObject(), spawnTrans.position, spawnTrans.rotation);
                            point.M.AddObjectToTrackedList(spawnedObj);

                            if (TNHFramework.FixLegacyModulGuns.Value)
                                TNHFrameworkUtils.FixPremadeFirearm(spawnedObj, false);

                            TNHFrameworkLogger.Log("Normal item spawned", TNHFrameworkLogger.LogType.TNH);

                            if (objToSpawn.UsesRoundTypeFlag)
                                yield return SpawnAmmoForObject(point, c, selectedGroup, null, objToSpawn, spawnTrans.position + spawnTrans.right * 0.15f);
                        }
                    }
                }
            }

            yield break;
        }

        // AmmoObjectOverride ONLY applies to Primary and Secondary weapons (which always spawn in a case).
        // Vault files cannot be spawned from cases
        public static void SpawnLoadoutCase(TNH_SupplyPoint point, EquipmentGroup group, FVRObject objAmmoOverride, FVRObject obj, ObjectTableDef tableDef, Transform spawnTrans, GameObject casePrefab)
        {
            int minAmmo = -1;
            int maxAmmo = -1;

            if (tableDef != null)
            {
                minAmmo = tableDef.MinAmmoCapacity;
                maxAmmo = tableDef.MaxAmmoCapacity;
            }

            GameObject gameObject = point.M.SpawnWeaponCase(casePrefab, spawnTrans.position, spawnTrans.forward, obj, group.NumMagsSpawned, group.NumRoundsSpawned, minAmmo, maxAmmo, objAmmoOverride);
            //point.m_trackedObjects.Add(gameObject);
            var trackedObjects = (List<GameObject>)fiTrackedObjects.GetValue(point);
            trackedObjects.Add(gameObject);
            gameObject.GetComponent<TNH_WeaponCrate>().M = point.M;
        }

        public static IEnumerator SpawnAmmoForObject(TNH_SupplyPoint point, CustomCharacter charDef, EquipmentGroup group, ObjectTable table, FVRObject o, Vector3 spawnPosition)
        {
            FVRObject ammoObj = point.M.GetSeededRandomAmmoObject(o, table.MinCapacity, table.MaxCapacity);

            if (ammoObj == null)
                yield break;

            AnvilCallback<GameObject> loadReq = ammoObj.GetGameObjectAsync();
            yield return loadReq;

            GameObject ammoObjPrefab = loadReq.Result;

            if (ammoObj.Category == FVRObject.ObjectCategory.Cartridge && point.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo && ammoObj.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.Ordnance)
            {
                GameObject ammoBox = AM.GetAmmoBox(ammoObj.RoundType);
                FVRFireArmRound component = ammoObjPrefab.GetComponent<FVRFireArmRound>();
                FireArmRoundClass rc = (!(component != null)) ? AM.SRoundDisplayDataDic[ammoObj.RoundType].Classes[0].Class : component.RoundClass;

                GameObject gameObject = Object.Instantiate<GameObject>(ammoBox, spawnPosition, Quaternion.identity);
                CartridgeBox component2 = gameObject.GetComponent<CartridgeBox>();
                component2.ConfigureShapeForRoundType(component.RoundType, rc);

                if (point.M != null)
                    point.M.AddObjectToTrackedList(gameObject);
            }
            else
            {
                Vector3 vector = spawnPosition;
                int num = (ammoObj.Category != FVRObject.ObjectCategory.Cartridge) ? group.NumMagsSpawned : group.NumRoundsSpawned;

                for (int i = 0; i < num; i++)
                {
                    GameObject g = Object.Instantiate<GameObject>(ammoObjPrefab, vector, Quaternion.identity);
                    point.M.AddObjectToTrackedList(g);
                    vector += Vector3.up * 0.15f;
                }
            }

            yield break;
        }
    }
}
