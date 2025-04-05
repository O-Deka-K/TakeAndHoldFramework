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
    static class SupplyPatches
    {
        private static readonly FieldInfo fiIsConfigured = typeof(TNH_SupplyPoint).GetField("m_isconfigured", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiHasBeenVisited = typeof(TNH_SupplyPoint).GetField("m_hasBeenVisited", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveSosigs = typeof(TNH_SupplyPoint).GetField("m_activeSosigs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveTurrets = typeof(TNH_SupplyPoint).GetField("m_activeTurrets", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSpawnBoxes = typeof(TNH_SupplyPoint).GetField("m_spawnBoxes", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(TNH_SupplyPoint), "ConfigureAtBeginning")]
        [HarmonyPrefix]
        public static bool SpawnStartingEquipment(TNH_SupplyPoint __instance, ref List<GameObject> ___m_trackedObjects)
        {
            ___m_trackedObjects.Clear();
            if (__instance.M.ItemSpawnerMode == TNH_ItemSpawnerMode.On)
            {
                __instance.M.ItemSpawner.transform.position = __instance.SpawnPoints_Panels[0].position + Vector3.up * 0.8f;
                __instance.M.ItemSpawner.transform.rotation = __instance.SpawnPoints_Panels[0].rotation;
                __instance.M.ItemSpawner.SetActive(true);
            }

            for (int i = 0; i < __instance.SpawnPoint_Tables.Count; i++)
            {
                GameObject item = UnityEngine.Object.Instantiate(__instance.M.Prefab_MetalTable, __instance.SpawnPoint_Tables[i].position, __instance.SpawnPoint_Tables[i].rotation);
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
                numGuards = UnityEngine.Random.Range(__instance.T.NumGuards - 1, __instance.T.NumGuards + 1);
                numGuards += ___numSpawnBonus;
                numGuards = Mathf.Clamp(numGuards, 0, 5);
                ___numSpawnBonus++;
            }

            TNHFrameworkLogger.Log($"Spawning {__instance.T.NumGuards} supply guards via SpawnTakeEnemyGroup()", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numGuards && i < __instance.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = __instance.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[__instance.T.GID];

                Sosig enemy = SosigPatches.SpawnEnemy(template, transform, __instance.M, __instance.T.IFFUsed, false, transform.position, true);
                ___m_activeSosigs.Add(enemy);
            }

            return false;
        }

        public static void ConfigureSupplyPoint(TNH_SupplyPoint supplyPoint, Level level, ref int panelIndex, int minBoxPiles, int maxBoxPiles, bool spawnToken)
        {

            supplyPoint.T = level.SupplyChallenge.GetTakeChallenge();
            //supplyPoint.m_isconfigured = true;
            fiIsConfigured.SetValue(supplyPoint, true);

            SpawnSupplyGroup(supplyPoint, level);

            SpawnSupplyTurrets(supplyPoint, level);

            int numConstructors = UnityEngine.Random.Range(level.MinConstructors, level.MaxConstructors + 1);

            SpawnSupplyConstructor(supplyPoint, numConstructors);

            SpawnSecondarySupplyPanel(supplyPoint, level, numConstructors, ref panelIndex);

            SpawnSupplyBoxes(supplyPoint, level, minBoxPiles, maxBoxPiles, spawnToken);

            //supplyPoint.m_hasBeenVisited = false;
            fiHasBeenVisited.SetValue(supplyPoint, false);
        }

        public static void SpawnSupplyConstructor(TNH_SupplyPoint point, int toSpawn)
        {
            TNHFrameworkLogger.Log("Spawning constructor panel", TNHFrameworkLogger.LogType.TNH);

            point.SpawnPoints_Panels.Shuffle();

            for (int i = 0; i < toSpawn && i < point.SpawnPoints_Panels.Count; i++)
            {
                GameObject constructor = point.M.SpawnObjectConstructor(point.SpawnPoints_Panels[i]);
                TNHFramework.SpawnedConstructors.Add(constructor);
            }
        }

        public static void SpawnSecondarySupplyPanel(TNH_SupplyPoint point, Level level, int startingPanelIndex, ref int panelIndex)
        {
            TNHFrameworkLogger.Log("Spawning secondary panels", TNHFrameworkLogger.LogType.TNH);

            bool isCustomCharacter = ((int)point.M.C.CharacterID >= 1000);
            int numPanels = UnityEngine.Random.Range(level.MinPanels, level.MaxPanels + 1);

            if (point.M.LevelName == "Institution" && !isCustomCharacter)
            {
                numPanels = 3;
            }

            for (int i = startingPanelIndex; i < startingPanelIndex + numPanels && i < point.SpawnPoints_Panels.Count && level.PossiblePanelTypes.Count > 0; i++)
            {
                TNHFrameworkLogger.Log("Panel index : " + i, TNHFrameworkLogger.LogType.TNH);

                // Go through the panels, and loop if we have gone too far 
                if (panelIndex >= level.PossiblePanelTypes.Count)
                    panelIndex = 0;
                
                PanelType panelType = level.PossiblePanelTypes[panelIndex];
                panelIndex += 1;

                TNHFrameworkLogger.Log("Panel type selected : " + panelType, TNHFrameworkLogger.LogType.TNH);

                GameObject panel = null;

                if (panelType == PanelType.AmmoReloader)
                {
                    panel = point.M.SpawnAmmoReloader(point.SpawnPoints_Panels[i]);
                }
                else if (panelType == PanelType.MagDuplicator)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);

                    if (TNHFramework.AlwaysMagUpgrader.Value)
                        panel.AddComponent(typeof(MagazinePanel));
                }
                else if (panelType == PanelType.MagUpgrader || panelType == PanelType.MagPurchase)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(MagazinePanel));
                }
                else if (panelType == PanelType.Recycler)
                {
                    panel = point.M.SpawnGunRecycler(point.SpawnPoints_Panels[i]);
                }
                else if (panelType == PanelType.AmmoPurchase)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(AmmoPurchasePanel));
                }
                else if (panelType == PanelType.AddFullAuto)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FullAutoPanel));
                }
                else if (panelType == PanelType.FireRateUp || panelType == PanelType.FireRateDown)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FireRatePanel));
                }
                else
                {
                    panel = point.M.SpawnAmmoReloader(point.SpawnPoints_Panels[i]);
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
        }

        public static void SpawnSupplyGroup(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Sosigs_Defense.Shuffle<Transform>();

            TNHFrameworkLogger.Log($"Spawning {level.SupplyChallenge.NumGuards} supply guards", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < level.SupplyChallenge.NumGuards && i < point.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = point.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[level.SupplyChallenge.GetTakeChallenge().GID];

                Sosig enemy = SosigPatches.SpawnEnemy(template, transform, point.M, level.SupplyChallenge.IFFUsed, false, transform.position, true);

                //point.m_activeSosigs.Add(enemy);
                var activeSosigs = (List<Sosig>)fiActiveSosigs.GetValue(point);
                activeSosigs.Add(enemy);
            }
        }

        public static void SpawnSupplyTurrets(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Turrets.Shuffle<Transform>();
            FVRObject turretPrefab = point.M.GetTurretPrefab(level.SupplyChallenge.TurretType);

            for (int i = 0; i < level.SupplyChallenge.NumTurrets && i < point.SpawnPoints_Turrets.Count; i++)
            {
                Vector3 pos = point.SpawnPoints_Turrets[i].position + Vector3.up * 0.25f;
                AutoMeater turret = UnityEngine.Object.Instantiate<GameObject>(turretPrefab.GetGameObject(), pos, point.SpawnPoints_Turrets[i].rotation).GetComponent<AutoMeater>();

                //point.m_activeTurrets.Add(turret);
                var activeTurrets = (List<AutoMeater>)fiActiveTurrets.GetValue(point);
                activeTurrets.Add(turret);
            }
        }

        public static void SpawnSupplyBoxes(TNH_SupplyPoint point, Level level, int minBoxPiles, int maxBoxPiles, bool spawnToken)
        {
            point.SpawnPoints_Boxes.Shuffle();

            bool isCustomCharacter = ((int)point.M.C.CharacterID >= 1000);
            var spawnBoxes = (List<GameObject>)fiSpawnBoxes.GetValue(point);

            // Custom Character behavior:
            // - Every supply point has the same min and max number of boxes
            // - Every supply point has the same min and max number of tokens
            // - Every box that doesn't have a token has the same probability of having health
            if (isCustomCharacter)
            {
                int minTokens = level.MinTokensPerSupply;
                int maxTokens = level.MaxTokensPerSupply;

                int minBoxes = level.MinBoxesSpawned;
                int maxBoxes = level.MaxBoxesSpawned;
                int boxesToSpawn = UnityEngine.Random.Range(minBoxes, maxBoxes + 1);

                TNHFrameworkLogger.Log($"Going to spawn {boxesToSpawn} boxes at this point -- Min ({minBoxes}), Max ({maxBoxes})", TNHFrameworkLogger.LogType.TNH);

                for (int i = 0; i < boxesToSpawn; i++)
                {
                    Transform spawnTransform = point.SpawnPoints_Boxes[UnityEngine.Random.Range(0, point.SpawnPoints_Boxes.Count)];
                    Vector3 position = spawnTransform.position + Vector3.up * 0.1f + Vector3.right * UnityEngine.Random.Range(-0.5f, 0.5f) + Vector3.forward * UnityEngine.Random.Range(-0.5f, 0.5f);
                    Quaternion rotation = Quaternion.Slerp(spawnTransform.rotation, UnityEngine.Random.rotation, 0.1f);

                    GameObject box = UnityEngine.Object.Instantiate(point.M.Prefabs_ShatterableCrates[UnityEngine.Random.Range(0, point.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                    //point.m_spawnBoxes.Add(box);
                    spawnBoxes.Add(box);
                }

                int tokensSpawned = 0;

                // J: If you're asking "why is this an if/elseif check if it's a boolean value?", I... I don't know. I don't know why Anton does this. It's not a big deal but I don't know why.
                if (!point.M.UsesUberShatterableCrates)
                {
                    foreach (GameObject boxObj in spawnBoxes)
                    {
                        if (tokensSpawned < minTokens)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(point.M);
                            tokensSpawned += 1;
                        }
                        else if (tokensSpawned < maxTokens && UnityEngine.Random.value < level.BoxTokenChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(point.M);
                            tokensSpawned += 1;
                        }
                        else if (UnityEngine.Random.value < level.BoxHealthChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(point.M);
                        }
                    }
                }
                else if (point.M.UsesUberShatterableCrates)
                {
                    for (int k = 0; k < spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = spawnBoxes[k].GetComponent<UberShatterable>();
                        if (tokensSpawned < minTokens)
                        {
                            SpawnBoxWithToken(point, boxComp);
                            tokensSpawned += 1;
                        }
                        else if (tokensSpawned < maxTokens && UnityEngine.Random.value < level.BoxTokenChance)
                        {
                            SpawnBoxWithToken(point, boxComp);
                            tokensSpawned += 1;
                        }
                        else if (UnityEngine.Random.value < level.BoxHealthChance)
                        {
                            SpawnBoxWithHealth(point, boxComp);
                        }
                        else
                        {
                            SpawnBoxEmpty(point, boxComp);
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
                bool spawnHealth1 = (UnityEngine.Random.Range(0f, 1f) > 0.1f);
                bool spawnHealth2 = (UnityEngine.Random.Range(0f, 1f) > 0.4f);
                bool spawnHealth3 = (UnityEngine.Random.Range(0f, 1f) > 0.8f);

                point.SpawnPoints_Boxes.Shuffle<Transform>();

                int boxPiles = UnityEngine.Random.Range(minBoxPiles, maxBoxPiles + 1);
                if (boxPiles < 1)
                    return;

                for (int i = 0; i < boxPiles; i++)
                {
                    Transform transform = point.SpawnPoints_Boxes[i];

                    int boxesPerPile = UnityEngine.Random.Range(1, 3);
                    for (int j = 0; j < boxesPerPile; j++)
                    {
                        Vector3 position = transform.position + Vector3.up * 0.1f + Vector3.up * 0.85f * (float)j;
                        Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
                        onUnitSphere.y = 0f;
                        onUnitSphere.Normalize();
                        Quaternion rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(onUnitSphere, Vector3.up), 0.1f);

                        GameObject item = UnityEngine.Object.Instantiate<GameObject>(point.M.Prefabs_ShatterableCrates[UnityEngine.Random.Range(0, point.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                        //point.m_spawnBoxes.Add(item);
                        spawnBoxes.Add(item);
                    }
                }

                //point.m_spawnBoxes.Shuffle();
                spawnBoxes.Shuffle();
                //miShuffle.Invoke(spawnBoxes, []);

                if (!point.M.UsesUberShatterableCrates)
                {
                    int spawnIndex = 0;
                    TNH_ShatterableCrate boxComp;

                    if (spawnToken && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingToken(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth1 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth2 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth3 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        //spawnIndex++;
                    }
                }
                else
                {
                    for (int k = 0; k < spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = spawnBoxes[k].GetComponent<UberShatterable>();

                        if (spawnToken)
                        {
                            spawnToken = false;
                            SpawnBoxWithToken(point, boxComp);
                        }
                        else if (spawnHealth1)
                        {
                            spawnHealth1 = false;
                            SpawnBoxWithHealth(point, boxComp);
                        }
                        else if (spawnHealth2)
                        {
                            spawnHealth2 = false;
                            SpawnBoxWithHealth(point, boxComp);
                        }
                        else if (spawnHealth3)
                        {
                            spawnHealth3 = false;
                            SpawnBoxWithHealth(point, boxComp);
                        }
                        else
                        {
                            SpawnBoxEmpty(point, boxComp);
                        }
                    }
                }
            }
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
    }
}
