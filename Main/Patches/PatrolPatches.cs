using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    public static class PatrolPatches
    {
        private static readonly MethodInfo miGetSpawnPoints = typeof(TNH_Manager).GetMethod("GetSpawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetForwardVectors = typeof(TNH_Manager).GetMethod("GetForwardVectors", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetPatrolPoints = typeof(TNH_Manager).GetMethod("GetPatrolPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetRandomSafeHoldIndexFromSupplyPoint = typeof(TNH_Manager).GetMethod("GetRandomSafeHoldIndexFromSupplyPoint", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetRandomSafeSupplyIndexFromSupplyPoint = typeof(TNH_Manager).GetMethod("GetRandomSafeSupplyIndexFromSupplyPoint", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetRandomSafeHoldIndexFromHoldPoint = typeof(TNH_Manager).GetMethod("GetRandomSafeHoldIndexFromHoldPoint", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetRandomSafeSupplyIndexFromHoldPoint = typeof(TNH_Manager).GetMethod("GetRandomSafeSupplyIndexFromHoldPoint", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiTimeTilPatrolCanSpawn = typeof(TNH_Manager).GetField("m_timeTilPatrolCanSpawn", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiPatrolSquads = typeof(TNH_Manager).GetField("m_patrolSquads", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Finds an index in the patrols list which can spawn, preventing bosses that have already spawned from spawning again
        /// </summary>
        /// <param name="patrols">List of patrols that can spawn</param>
        /// <returns>Returns -1 if no valid index is found, otherwise returns a random index for a patrol </returns>
        private static int GetValidPatrolIndex(List<Patrol> patrols)
        {
            // Create a pool of valid patrols
            // This allows us to generate one patrol of each type before generating more
            if (TNHFramework.PatrolIndexPool == null || TNHFramework.PatrolIndexPool.Count == 0)
            {
                for (int i = 0; i < patrols.Count; i++)
                {
                    if (TNHFramework.SpawnedBossIndexes == null || !TNHFramework.SpawnedBossIndexes.Contains(i))
                        TNHFramework.PatrolIndexPool.Add(i);
                }
            }

            // No valid patrols left
            if (TNHFramework.PatrolIndexPool.Count == 0)
            {
                TNHFrameworkLogger.Log("No valid patrols can spawn", TNHFrameworkLogger.LogType.TNH);
                return -1;
            }

            // Choose from the valid patrol pool
            int index = UnityEngine.Random.Range(0, TNHFramework.PatrolIndexPool.Count);
            int patrolIndex = TNHFramework.PatrolIndexPool[index];
            TNHFramework.PatrolIndexPool.RemoveAt(index);

            TNHFrameworkLogger.Log($"Valid patrol found: {patrolIndex}", TNHFrameworkLogger.LogType.TNH);
            return patrolIndex;
        }

        private static void SetTimeTilPatrolCanSpawn(TNH_Manager M, Patrol patrol)
        {
            if (M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking)
            {
                //instance.m_timeTilPatrolCanSpawn = patrol.PatrolCadenceLimited;
                fiTimeTilPatrolCanSpawn.SetValue(M, patrol.PatrolCadenceLimited);
            }
            else
            {
                //instance.m_timeTilPatrolCanSpawn = patrol.PatrolCadence;
                fiTimeTilPatrolCanSpawn.SetValue(M, patrol.PatrolCadence);
            }
        }

        [HarmonyPatch(typeof(TNH_Manager), "UpdatePatrols")]
        [HarmonyPrefix]
        public static bool UpdatePatrolsReplacement(TNH_Manager __instance, ref float ___m_timeTilPatrolCanSpawn, ref List<TNH_Manager.SosigPatrolSquad> ___m_patrolSquads,
            TNH_Progression.Level ___m_curLevel, int ___m_lastHoldIndex, int ___m_curHoldIndex, float ___m_AlertTickDownTime, Vector3 ___m_lastAlertSpottedPoint)
        {
            // Update global patrol spawn timer
            if (___m_timeTilPatrolCanSpawn > 0f)
            {
                ___m_timeTilPatrolCanSpawn -= Time.deltaTime;
            }

            Level level = LoadedTemplateManager.CurrentLevel;

            int maxPatrols = (__instance.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking) ?
                level.Patrols[0].MaxPatrolsLimited : level.Patrols[0].MaxPatrols;

            // Adjust max patrols for new patrol behavior
            if (!__instance.UsesClassicPatrolBehavior && !LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                maxPatrols += (__instance.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking) ? 1 : 2;
            }

            // Time to generate a new patrol
            if (___m_timeTilPatrolCanSpawn <= 0f && ___m_patrolSquads.Count < maxPatrols)
            {
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;

                // Check if player is in a supply point
                int supplyIndex = -1;
                for (int i = 0; i < __instance.SupplyPoints.Count; i++)
                {
                    if (__instance.SupplyPoints[i].IsPointInBounds(playerPos))
                    {
                        supplyIndex = i;
                        break;
                    }
                }

                // Check if player is in a hold point
                int holdIndex = -1;
                for (int i = 0; i < __instance.HoldPoints.Count; i++)
                {
                    if (i != ___m_lastHoldIndex && i != ___m_curHoldIndex && __instance.HoldPoints[i].IsPointInBounds(playerPos))
                    {
                        holdIndex = i;
                        break;
                    }
                }

                // Player is in a supply point
                if (supplyIndex > -1)
                {
                    if (__instance.UsesClassicPatrolBehavior)
                    {
                        GenerateValidPatrolReplacement(__instance, ref ___m_patrolSquads, ___m_curLevel, supplyIndex, ___m_curHoldIndex, true);
                    }
                    else if (level.Patrols.Count > 0)
                    {
                        TNHFrameworkLogger.Log($"Player is in Supply {supplyIndex} (S {__instance.SupplyPoints.Count}, H {__instance.HoldPoints.Count})", TNHFrameworkLogger.LogType.TNH);

                        int firstPoint;
                        TNH_Manager.SentryPatrolPointType firstSpawnPointType;
                        TNH_Manager.SentryPatrolPointType firstPatrolPointType;

                        // Choose supply or hold to generate sentry patrol from
                        if (UnityEngine.Random.value >= 0.5f)
                        {
                            //firstPoint = __instance.GetRandomSafeHoldIndexFromSupplyPoint(supplyIndex);
                            firstPoint = (int)miGetRandomSafeHoldIndexFromSupplyPoint.Invoke(__instance, [supplyIndex]);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Hold;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSHold;
                        }
                        else
                        {
                            //firstPoint = __instance.GetRandomSafeSupplyIndexFromSupplyPoint(supplyIndex);
                            firstPoint = (int)miGetRandomSafeSupplyIndexFromSupplyPoint.Invoke(__instance, [supplyIndex]);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Supply;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSSupply;
                        }

                        int patrolIndex = GetValidPatrolIndex(level.Patrols);

                        if (patrolIndex > -1)
                        {
                            Patrol patrol = level.Patrols[patrolIndex];

                            // Anton pls fix - GetSpawnPoints() sometimes uses wrong type
                            //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrolChallenge.Patrols[index], __instance.GetSpawnPoints(nextPoint, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, nextPoint, supplyIndex, supplyIndex, ___m_curHoldIndex));
                            List<Vector3> spawnPoints = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [firstPoint, firstSpawnPointType]);
                            List<Vector3> forwardVectors = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                            List<Vector3> patrolPoints = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, firstPoint, supplyIndex, supplyIndex, ___m_curHoldIndex]);
                            TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, spawnPoints, forwardVectors, patrolPoints, patrolIndex);
                            ___m_patrolSquads.Add(squad);

                            SetTimeTilPatrolCanSpawn(__instance, patrol);
                        }
                    }
                }
                // Player is in a hold point
                else if (holdIndex > -1)
                {
                    if (!__instance.UsesClassicPatrolBehavior && level.Patrols.Count > 0)
                    {
                        TNHFrameworkLogger.Log($"Player is in Hold {holdIndex} (S {__instance.SupplyPoints.Count}, H {__instance.HoldPoints.Count})", TNHFrameworkLogger.LogType.TNH);

                        int firstPoint;
                        TNH_Manager.SentryPatrolPointType firstSpawnPointType;
                        TNH_Manager.SentryPatrolPointType firstPatrolPointType;

                        // Choose supply or hold to generate sentry patrol from
                        if (UnityEngine.Random.value >= 0.5f)
                        {
                            //firstPoint = __instance.GetRandomSafeHoldIndexFromHoldPoint(holdIndex);
                            firstPoint = (int)miGetRandomSafeHoldIndexFromHoldPoint.Invoke(__instance, [holdIndex]);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Hold;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSHold;
                        }
                        else
                        {
                            //firstPoint = __instance.GetRandomSafeSupplyIndexFromHoldPoint(holdIndex);
                            firstPoint = (int)miGetRandomSafeSupplyIndexFromHoldPoint.Invoke(__instance, [holdIndex]);
                            firstSpawnPointType = TNH_Manager.SentryPatrolPointType.Supply;
                            firstPatrolPointType = TNH_Manager.SentryPatrolPointType.SPSSupply;
                        }

                        int patrolIndex = GetValidPatrolIndex(level.Patrols);

                        if (patrolIndex > -1)
                        {
                            Patrol patrol = level.Patrols[patrolIndex];

                            // Anton pls fix - GetSpawnPoints() sometimes uses wrong type
                            //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrolChallenge.Patrols[index], __instance.GetSpawnPoints(nextPoint, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, nextPoint, holdIndex, holdIndex, ___m_curHoldIndex));
                            List<Vector3> spawnPoints = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [firstPoint, firstSpawnPointType]);
                            List<Vector3> forwardVectors = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                            List<Vector3> patrolPoints = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [firstPatrolPointType, TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, firstPoint, holdIndex, holdIndex, ___m_curHoldIndex]);
                            TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, spawnPoints, forwardVectors, patrolPoints, patrolIndex);
                            ___m_patrolSquads.Add(squad);

                            SetTimeTilPatrolCanSpawn(__instance, patrol);
                        }
                    }
                }
                else
                {
                    // Try again later
                    ___m_timeTilPatrolCanSpawn = 6f;
                }
            }

            for (int squadIndex = 0; squadIndex < ___m_patrolSquads.Count; squadIndex++)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = ___m_patrolSquads[squadIndex];

                // Remove dead sosigs from squad
                for (int i = patrolSquad.Squad.Count - 1; i >= 0; i--)
                {
                    if (patrolSquad.Squad[i] == null)
                        patrolSquad.Squad.RemoveAt(i);
                }

                if (patrolSquad.Squad.Count > 0)
                {
                    if (__instance.UsesAlertPatrolSystem && ___m_AlertTickDownTime > 0f)
                    {
                        // Patrols are on alert
                        for (int i = 0; i < patrolSquad.Squad.Count; i++)
                        {
                            patrolSquad.Squad[i].UpdateAssaultPoint(___m_lastAlertSpottedPoint);
                        }
                    }
                    else
                    {
                        // Determine whether all sosigs in squad are near the patrol point target
                        bool hasReachedPatrolPoint = false;
                        int reached = 0;
                        int total = patrolSquad.Squad.Count;

                        for (int i = 0; i < total; i++)
                        {
                            if ((float)Vector3.Distance(patrolSquad.Squad[i].transform.position, patrolSquad.PatrolPoints[patrolSquad.CurPatrolPointIndex]) <= 4f)
                                reached++;
                        }

                        if (total > 0 && reached == total)
                            hasReachedPatrolPoint = true;

                        if (hasReachedPatrolPoint)
                        {
                            SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[patrolSquad.ID_Leader];
                            SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

                            // Last patrol point
                            if (patrolSquad.CurPatrolPointIndex + 1 >= patrolSquad.PatrolPoints.Count && patrolSquad.IsPatrollingUp)
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached last patrol point ({patrolSquad.CurPatrolPointIndex})! Going back down", TNHFrameworkLogger.LogType.TNH);
                                patrolSquad.IsPatrollingUp = false;
                            }
                            // First patrol point
                            else if (patrolSquad.CurPatrolPointIndex == 0 && !patrolSquad.IsPatrollingUp)
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached first patrol point ({patrolSquad.CurPatrolPointIndex})! Going back up", TNHFrameworkLogger.LogType.TNH);
                                patrolSquad.IsPatrollingUp = true;
                            }
                            else
                            {
                                TNHFrameworkLogger.Log($"Patrol {squadIndex + 1} [{customTemplate.SosigEnemyID}] reached patrol point ({patrolSquad.CurPatrolPointIndex})! Continuing patrol", TNHFrameworkLogger.LogType.TNH);
                            }

                            // Go back and forth between patrol points
                            if (patrolSquad.IsPatrollingUp)
                                patrolSquad.CurPatrolPointIndex++;
                            else
                                patrolSquad.CurPatrolPointIndex--;

                            // Set the patrol point as target
                            foreach (var sosig in patrolSquad.Squad)
                            {
                                sosig.CommandAssaultPoint(patrolSquad.PatrolPoints[patrolSquad.CurPatrolPointIndex]);
                            }
                        }
                    }

                    // Adjust squad orders
                    for (int i = 0; i < patrolSquad.Squad.Count; i++)
                    {
                        if (patrolSquad.Squad[i] != null)
                        {
                            if (patrolSquad.Squad[i].CurrentOrder == Sosig.SosigOrder.Wander)
                            {
                                patrolSquad.Squad[i].CurrentOrder = Sosig.SosigOrder.Assault;
                            }

                            patrolSquad.Squad[i].FallbackOrder = Sosig.SosigOrder.Assault;
                        }
                    }
                }
            }

            for (int squadIndex = 0; squadIndex < ___m_patrolSquads.Count; squadIndex++)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = ___m_patrolSquads[squadIndex];

                // If this squad still needs to be spawned
                if (patrolSquad.NumLeftToSpawn > 0)
                {
                    if (patrolSquad.TimeTilNextSpawn > 0f)
                    {
                        // Update timer for this patrol squad only
                        patrolSquad.TimeTilNextSpawn -= Time.deltaTime;
                    }
                    else
                    {
                        int patrolIndex = patrolSquad.HoldPointStart;
                        Patrol patrol = level.Patrols[patrolIndex];

                        // Choose leader or regular sosig type
                        SosigEnemyTemplate template;
                        bool allowAllWeapons;

                        if (patrolSquad.IndexOfNextSpawn == 0)
                        {
                            SosigEnemyID sosigID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.LeaderType];
                            template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[sosigID];
                            string sosigName = LoadedTemplateManager.LoadedSosigsDict[template].SosigEnemyID;
                            allowAllWeapons = true;

                            TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Spawning {patrolSquad.NumLeftToSpawn} sosigs for Patrol {squadIndex + 1} [{sosigName}]", TNHFrameworkLogger.LogType.TNH);
                        }
                        else if (patrol.EnemyType.Count > 0)
                        {
                            SosigEnemyID sosigID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.EnemyType.GetRandom()];
                            template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[sosigID];
                            allowAllWeapons = false;
                        }
                        else
                        {
                            TNHFrameworkLogger.Log($"Cannot spawn sosigs for Patrol. EnemyType list is empty!", TNHFrameworkLogger.LogType.TNH);
                            patrolSquad.NumLeftToSpawn = 0;
                            return false;
                        }

                        Vector3 spawnPos = patrolSquad.SpawnPoints[patrolSquad.IndexOfNextSpawn];
                        Quaternion lookRot = Quaternion.LookRotation(patrolSquad.ForwardVectors[patrolSquad.IndexOfNextSpawn], Vector3.up);
                        Vector3 pointOfInterest = patrolSquad.PatrolPoints[0];

                        // Spawn the sosig
                        Sosig sosig = __instance.SpawnEnemy(template, spawnPos, lookRot, patrolSquad.IFF, true, pointOfInterest, allowAllWeapons);

                        // Add random health pickup to leader
                        if (patrolSquad.IndexOfNextSpawn == 0 && (float)UnityEngine.Random.Range(0f, 1f) > 0.65f)
                        {
                            sosig.Links[1].RegisterSpawnOnDestroy(__instance.Prefab_HealthPickupMinor);
                        }

                        // Add the sosig to the squad
                        sosig.SetAssaultSpeed(patrol.AssualtSpeed);
                        patrolSquad.Squad.Add(sosig);
                        patrolSquad.NumLeftToSpawn--;
                        patrolSquad.IndexOfNextSpawn++;
                    }
                }
            }

            // Clean up squad if all sosigs in it are dead
            for (int squadIndex = ___m_patrolSquads.Count - 1; squadIndex >= 0; squadIndex--)
            {
                TNH_Manager.SosigPatrolSquad patrolSquad = ___m_patrolSquads[squadIndex];

                if (patrolSquad.Squad.Count < 1 && patrolSquad.NumLeftToSpawn <= 0)
                {
                    ___m_patrolSquads[squadIndex].PatrolPoints.Clear();
                    ___m_patrolSquads.RemoveAt(squadIndex);
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_Manager), "GenerateInitialTakeSentryPatrols")]
        [HarmonyPrefix]
        private static bool GenerateInitialTakeSentryPatrolsReplacement(TNH_Manager __instance, ref List<TNH_Manager.SosigPatrolSquad> ___m_patrolSquads, List<int> ___m_activeSupplyPointIndicies, TNH_Progression.Level ___m_curLevel, TNH_PatrolChallenge P, int curSupplyPoint, int lastHoldIndex, int curHoldIndex, bool isStart)
        {
            Level level = LoadedTemplateManager.CurrentLevel;

            // Get a valid patrol index, and exit if there are no valid patrols
            int patrolIndex = GetValidPatrolIndex(level.Patrols);

            if (patrolIndex == -1)
                return false;

            Patrol patrol = level.Patrols[patrolIndex];

            int maxPatrols = (__instance.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking) ?
                level.Patrols[0].MaxPatrolsLimited : level.Patrols[0].MaxPatrols;

            if (isStart)
            {
                //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, curHoldIndex, curSupplyPoint, curHoldIndex, curHoldIndex));
                List<Vector3> spawnPoints = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold]);
                List<Vector3> forwardVectors = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                List<Vector3> patrolPoints = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, curHoldIndex, curSupplyPoint, curHoldIndex, curHoldIndex]);
                TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, spawnPoints, forwardVectors, patrolPoints, patrolIndex);
                ___m_patrolSquads.Add(squad);

                for (int i = 1; i < Mathf.Min(___m_activeSupplyPointIndicies.Count, maxPatrols); i++)
                {
                    patrolIndex = GetValidPatrolIndex(level.Patrols);

                    if (patrolIndex == -1)
                        return false;

                    patrol = level.Patrols[patrolIndex];

                    //TNH_Manager.SosigPatrolSquad squad2 = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(___m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, ___m_activeSupplyPointIndicies[i], ___m_activeSupplyPointIndicies[i], curSupplyPoint, curHoldIndex));
                    List<Vector3> spawnPoints2 = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [___m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply]);
                    List<Vector3> forwardVectors2 = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                    List<Vector3> patrolPoints2 = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSHold, ___m_activeSupplyPointIndicies[i], ___m_activeSupplyPointIndicies[i], curSupplyPoint, curHoldIndex]);
                    TNH_Manager.SosigPatrolSquad squad2 = GenerateSentryPatrol(__instance, patrol, spawnPoints2, forwardVectors2, patrolPoints2, patrolIndex);
                    ___m_patrolSquads.Add(squad2);
                }
            }
            else
            {
                //TNH_Manager.SosigPatrolSquad squad = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.Hold, curHoldIndex, lastHoldIndex, lastHoldIndex, lastHoldIndex));
                List<Vector3> spawnPoints = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [curHoldIndex, TNH_Manager.SentryPatrolPointType.Hold]);
                List<Vector3> forwardVectors = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                List<Vector3> patrolPoints = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [TNH_Manager.SentryPatrolPointType.Hold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.SPSHold, TNH_Manager.SentryPatrolPointType.Hold, curHoldIndex, lastHoldIndex, lastHoldIndex, lastHoldIndex]);
                TNH_Manager.SosigPatrolSquad squad = GenerateSentryPatrol(__instance, patrol, spawnPoints, forwardVectors, patrolPoints, patrolIndex);
                ___m_patrolSquads.Add(squad);

                for (int i = 1; i < Mathf.Min(___m_activeSupplyPointIndicies.Count, maxPatrols); i++)
                {
                    patrolIndex = GetValidPatrolIndex(level.Patrols);

                    if (patrolIndex == -1)
                        return false;

                    patrol = level.Patrols[patrolIndex];

                    //TNH_Manager.SosigPatrolSquad squad2 = __instance.GenerateSentryPatrol(patrol, __instance.GetSpawnPoints(___m_activeSupplyPointIndicies[j], TNH_Manager.SentryPatrolPointType.Supply), __instance.GetForwardVectors(), __instance.GetPatrolPoints(TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, ___m_activeSupplyPointIndicies[j], ___m_activeSupplyPointIndicies[j], ___m_activeSupplyPointIndicies[j], ___m_activeSupplyPointIndicies[j]));
                    List<Vector3> spawnPoints2 = (List<Vector3>)miGetSpawnPoints.Invoke(__instance, [___m_activeSupplyPointIndicies[i], TNH_Manager.SentryPatrolPointType.Supply]);
                    List<Vector3> forwardVectors2 = (List<Vector3>)miGetForwardVectors.Invoke(__instance, []);
                    List<Vector3> patrolPoints2 = (List<Vector3>)miGetPatrolPoints.Invoke(__instance, [TNH_Manager.SentryPatrolPointType.Supply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, TNH_Manager.SentryPatrolPointType.SPSSupply, ___m_activeSupplyPointIndicies[i], ___m_activeSupplyPointIndicies[i], ___m_activeSupplyPointIndicies[i], ___m_activeSupplyPointIndicies[i]]);
                    TNH_Manager.SosigPatrolSquad squad2 = GenerateSentryPatrol(__instance, patrol, spawnPoints2, forwardVectors2, patrolPoints2, patrolIndex);
                    ___m_patrolSquads.Add(squad2);
                }
            }

            SetTimeTilPatrolCanSpawn(__instance, patrol);
            return false;
        }

        public static TNH_Manager.SosigPatrolSquad GenerateSentryPatrol(TNH_Manager instance, Patrol patrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints, int patrolIndex)
        {
            var patrolSquads = (List<TNH_Manager.SosigPatrolSquad>)fiPatrolSquads.GetValue(instance);
            TNHFrameworkLogger.Log($"Generating a sentry patrol -- There are currently {patrolSquads.Count} patrols active", TNHFrameworkLogger.LogType.TNH);

            return GeneratePatrol(patrol, SpawnPoints, ForwardVectors, PatrolPoints, patrolIndex);
        }

        [HarmonyPatch(typeof(TNH_Manager), "GenerateSentryPatrol")]
        [HarmonyPrefix]
        public static bool GenerateSentryPatrolStub(TNH_PatrolChallenge.Patrol curPatrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints)
        {
            // We've replaced all calls to GenerateSentryPatrol() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("GenerateSentryPatrolStub() called! This should have been overridden!");
            return false;
        }

        /// <summary>
        /// Decides the spawning location and patrol pathing for sosig patrols, and then spawns the patrol
        /// </summary>
        [HarmonyPatch(typeof(TNH_Manager), "GenerateValidPatrol")]
        [HarmonyPrefix]
        public static bool GenerateValidPatrolReplacement(TNH_Manager __instance, ref List<TNH_Manager.SosigPatrolSquad> ___m_patrolSquads, TNH_Progression.Level ___m_curLevel, int curStandardIndex, int excludeHoldIndex, bool isStart)
        {
            List<int> validLocations = [];
            float minDist = __instance.TAHReticle.Range * 1.2f;

            // Get a safe starting point for the patrol to spawn
            TNH_SafePositionMatrix.PositionEntry startingEntry = (isStart) ?
                __instance.SafePosMatrix.Entries_SupplyPoints[curStandardIndex] : __instance.SafePosMatrix.Entries_HoldPoints[curStandardIndex];

            for (int i = 0; i < startingEntry.SafePositions_HoldPoints.Count; i++)
            {
                if (i != excludeHoldIndex && startingEntry.SafePositions_HoldPoints[i])
                {
                    float playerDist = Vector3.Distance(GM.CurrentPlayerBody.transform.position, __instance.HoldPoints[i].transform.position);
                    if (playerDist > minDist)
                    {
                        validLocations.Add(i);
                    }
                }
            }

            if (validLocations.Count < 1)
                return false;
            
            validLocations.Shuffle();

            Level level = LoadedTemplateManager.CurrentLevel;

            // Get a valid patrol index, and exit if there are no valid patrols
            int patrolIndex = GetValidPatrolIndex(level.Patrols);
            if (patrolIndex == -1)
                return false;

            Patrol patrol = level.Patrols[patrolIndex];
            TNH_Manager.SosigPatrolSquad squad = GeneratePatrol(__instance, validLocations[0], patrol, patrolIndex);
            ___m_patrolSquads.Add(squad);

            SetTimeTilPatrolCanSpawn(__instance, patrol);
            return false;
        }

        /// <summary>
        /// Spawns a patrol at the desired patrol point
        /// </summary>
        public static TNH_Manager.SosigPatrolSquad GeneratePatrol(TNH_Manager instance, int HoldPointStart, Patrol patrol, int patrolIndex)
        {
            var patrolSquads = (List<TNH_Manager.SosigPatrolSquad>)fiPatrolSquads.GetValue(instance);
            TNHFrameworkLogger.Log($"Generating a patrol -- There are currently {patrolSquads.Count} patrols active", TNHFrameworkLogger.LogType.TNH);

            List<int> list = [];

            // Add all hold points except starting point
            for (int i = 0; i < instance.HoldPoints.Count; i++)
            {
                if (i != HoldPointStart)
                    list.Add(i);
            }

            // Shuffle list, then insert starting point at beginning
            list.Shuffle();
            list.Insert(0, HoldPointStart);

            List<Vector3> PatrolPoints = [];

            // Create patrol path, limited to 6 points
            for (int i = 0; i < list.Count && i < 6; i++)
            {
                PatrolPoints.Add(instance.HoldPoints[list[i]].SpawnPoints_Sosigs_Defense.GetRandom<Transform>().position);
            }
            TNHFrameworkLogger.Log($"Patrol path is: {string.Join(", ", list.GetRange(0, Mathf.Min(list.Count, 6)).Select(x => x.ToString()).ToArray())}", TNHFrameworkLogger.LogType.TNH);

            List<Vector3> SpawnPoints = [];
            List<Vector3> ForwardVectors = [];

            foreach (Transform spawnPoint in instance.HoldPoints[HoldPointStart].SpawnPoints_Sosigs_Defense)
            {
                SpawnPoints.Add(spawnPoint.position);
                ForwardVectors.Add(spawnPoint.forward);
            }

            return GeneratePatrol(patrol, SpawnPoints, ForwardVectors, PatrolPoints, patrolIndex);
        }

        // TODO: To choose spawn based on IFF, we need to basically generate spawn points on our own in this method!
        public static TNH_Manager.SosigPatrolSquad GeneratePatrol(Patrol patrol, List<Vector3> SpawnPoints, List<Vector3> ForwardVectors, List<Vector3> PatrolPoints, int patrolIndex)
        {
            // If this is a boss, then we can only spawn it once, so add it to the list of spawned bosses
            if (patrol.IsBoss)
            {
                TNHFramework.SpawnedBossIndexes.Add(patrolIndex);
            }

            TNH_Manager.SosigPatrolSquad squad = new()
            {
                PatrolPoints = [.. PatrolPoints],
                IsPatrollingUp = true,
                ID_Leader = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.LeaderType],
                ID_Regular = (patrol.EnemyType.Count > 0) ? (SosigEnemyID)LoadedTemplateManager.SosigIDDict[patrol.EnemyType[0]] : SosigEnemyID.None,
                HoldPointStart = patrolIndex,  // Commandeering this to hold patrolIndex because it's not used anywhere
                IFF = patrol.IFFUsed,
                IndexOfNextSpawn = 0,
                NumLeftToSpawn = Mathf.Clamp(patrol.PatrolSize, 1, SpawnPoints.Count)
            };

            // If squad is set to swarm, the first point they path to should be the player's current position
            if (patrol.SwarmPlayer)
            {
                squad.PatrolPoints[0] = GM.CurrentPlayerBody.transform.position;
            }

            for (int i = 0; i < squad.NumLeftToSpawn; i++)
            {
                squad.SpawnPoints.Add(SpawnPoints[i]);
                squad.ForwardVectors.Add(ForwardVectors[i]);
            }

            return squad;
        }

        [HarmonyPatch(typeof(TNH_Manager), "GeneratePatrol")]
        [HarmonyPrefix]
        public static bool GeneratePatrolStub()
        {
            // We've replaced all calls to GeneratePatrol() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("GeneratePatrolStub() called! This should have been overridden!");
            return false;
        }
    }
}
