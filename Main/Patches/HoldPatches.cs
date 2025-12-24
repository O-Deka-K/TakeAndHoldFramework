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
    public class HoldPatches
    {
        private static readonly MethodInfo miCompletePhase = typeof(TNH_HoldPoint).GetMethod("CompletePhase", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miDeleteAllActiveWarpIns = typeof(TNH_HoldPoint).GetMethod("DeleteAllActiveWarpIns", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSpawnHoldEnemyGroup = typeof(TNH_HoldPoint).GetMethod("SpawnHoldEnemyGroup", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetMaxTargsInHold = typeof(TNH_HoldPoint).GetMethod("GetMaxTargsInHold", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(TNH_HoldPoint), "Init")]
        [HarmonyPostfix]
        public static void Init_DisableConstructs(TNH_HoldPoint __instance)
        {
            foreach (Construct_Volume construct in __instance.M.ConstructSpawners)
            {
                if (!TNHFramework.EnableBlister.Value && construct is Construct_Blister_Volume)
                    __instance.ExcludeConstructVolumes.Add(construct);
                else if (!TNHFramework.EnableFloater.Value && construct is Construct_Floater_Volume)
                    __instance.ExcludeConstructVolumes.Add(construct);
                else if (!TNHFramework.EnableIris.Value && construct is Construct_Iris_Volume)
                    __instance.ExcludeConstructVolumes.Add(construct);
                else if (!TNHFramework.EnableSentinel.Value && construct is Construct_Sentinel_Path)
                    __instance.ExcludeConstructVolumes.Add(construct);
            }
        }

        // Anton pls fix - Use TNHSeed
        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
        [HarmonyPostfix]
        public static void BeginPhase_TNHSeed(ref float ___m_tickDownToNextGroupSpawn, TNH_HoldChallenge.Phase ___m_curPhase, int ___m_phaseIndex)
        {
            TNHFrameworkLogger.Log($"Beginning HOLD PHASE -- Wave {___m_phaseIndex}", TNHFrameworkLogger.LogType.TNH);

            ___m_tickDownToNextGroupSpawn = ___m_curPhase.WarmUp * 0.8f;

            if (GM.TNHOptions.TNHSeed < 0)
                ___m_tickDownToNextGroupSpawn = ___m_curPhase.WarmUp * Random.Range(0.8f, 1.1f);
        }

        // Anton pls fix - Use TNHSeed
        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginAnalyzing")]
        [HarmonyPostfix]
        public static void BeginAnalyzing_TNHSeed(TNH_HoldPoint __instance, ref float ___m_tickDownToIdentification, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            if (__instance.M.TargetMode == TNHSetting_TargetMode.NoTargets)
            {
                ___m_tickDownToIdentification = ___m_curPhase.ScanTime * 0.9f + 60f;

                if (GM.TNHOptions.TNHSeed < 0)
                    ___m_tickDownToIdentification = Random.Range(___m_curPhase.ScanTime * 0.9f, ___m_curPhase.ScanTime * 1.1f) + 60f;
            }
            else
            {
                ___m_tickDownToIdentification = ___m_curPhase.ScanTime * 0.8f;

                if (GM.TNHOptions.TNHSeed < 0)
                    ___m_tickDownToIdentification = Random.Range(___m_curPhase.ScanTime * 0.8f, ___m_curPhase.ScanTime * 1.2f);

                if (__instance.M.IsBigLevel)
                    ___m_tickDownToIdentification += 15f;
            }
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "IdentifyEncryption")]
        [HarmonyPrefix]
        public static bool IdentifyEncryption_NoTargets(TNH_HoldPoint __instance, ref TNH_HoldChallenge.Phase ___m_curPhase)
        {
            Phase currentPhase = LoadedTemplateManager.CurrentCharacter.GetCurrentPhase(___m_curPhase);

            // If we shouldn't spawn any targets, we exit out early
            if ((currentPhase.MaxTargets < 1 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) || currentPhase.MaxTargetsLimited < 1)
            {
                //__instance.CompletePhase();
                miCompletePhase.Invoke(__instance, []);
                return false;
            }

            ___m_curPhase.Encryption = currentPhase.Encryptions[0];
            return true;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "CompletePhase")]
        [HarmonyPostfix]
        public static void CompletePhase_NoScanTime(ref TNH_HoldChallenge.Phase ___m_curPhase, ref float ___m_tickDownToIdentification)
        {
            // Handle case where ScanTime is less than 0
            if (___m_curPhase.ScanTime < 0f)
                ___m_tickDownToIdentification = 0f;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawningRoutineUpdate")]
        [HarmonyPrefix]
        public static bool SpawningRoutineUpdate_Replacement(TNH_HoldPoint __instance, ref float ___m_tickDownToNextGroupSpawn, ref List<Sosig> ___m_activeSosigs,
            TNH_HoldPoint.HoldState ___m_state, ref bool ___m_hasThrownNadesInWave, bool ___m_isFirstWave, int ___m_phaseIndex, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (___m_activeSosigs.Count < 1 && ___m_state == TNH_HoldPoint.HoldState.Analyzing)
                ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (!___m_hasThrownNadesInWave && ___m_tickDownToNextGroupSpawn <= 5f && !___m_isFirstWave && LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                // Check if grenade vectors exist before throwing grenades
                if (__instance.AttackVectors[0].GrenadeVector != null)
                    SpawnGrenades(__instance.AttackVectors, __instance.M, ___m_phaseIndex);

                ___m_hasThrownNadesInWave = true;
            }

            // Handle spawning of a wave if it is time
            if (___m_curPhase != null && ___m_tickDownToNextGroupSpawn <= 0 && ___m_activeSosigs.Count + ___m_curPhase.MaxEnemies <= ___m_curPhase.MaxEnemiesAlive)
            {
                __instance.AttackVectors.Shuffle();

                //SpawnHoldEnemyGroup(___m_curPhase, ___m_phaseIndex, __instance.AttackVectors, __instance.SpawnPoints_Turrets, ___m_activeSosigs, __instance.M, ref ___m_isFirstWave);
                miSpawnHoldEnemyGroup.Invoke(__instance, []);
                ___m_hasThrownNadesInWave = false;

                // Adjust spawn cadence depending on ammo mode
                float ammoMult = (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking ? 1.35f : 1f);
                float randomMult = (GM.TNHOptions.TNHSeed < 0) ? Random.Range(0.9f, 1.1f) : 0.9f;
                ___m_tickDownToNextGroupSpawn = ___m_curPhase.SpawnCadence * randomMult * ammoMult;
            }

            return false;
        }

        public static void SpawnGrenades(List<TNH_HoldPoint.AttackVector> AttackVectors, TNH_Manager M, int phaseIndex)
        {
            Phase currPhase = LoadedTemplateManager.CurrentLevel.HoldPhases[phaseIndex];

            float grenadeChance = currPhase.GrenadeChance;
            string grenadeType = currPhase.GrenadeType;

            if (grenadeChance >= Random.value)
            {
                TNHFrameworkLogger.Log($"Throwing grenade [{grenadeType}]", TNHFrameworkLogger.LogType.TNH);

                // Get a random grenade vector to spawn a grenade at
                AttackVectors.Shuffle();
                TNH_HoldPoint.AttackVector randAttackVector = AttackVectors[Random.Range(0, AttackVectors.Count)];

                // Instantiate the grenade object
                if (IM.OD.ContainsKey(grenadeType))
                {
                    GameObject grenadeObject = UnityEngine.Object.Instantiate(IM.OD[grenadeType].GetGameObject(), randAttackVector.GrenadeVector.position, randAttackVector.GrenadeVector.rotation);

                    // Give the grenade an initial velocity based on the grenade vector
                    grenadeObject.GetComponent<Rigidbody>().velocity = 15 * randAttackVector.GrenadeVector.forward;
                    grenadeObject.GetComponent<SosigWeapon>().FuseGrenade();
                }
            }
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnHoldEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnHoldEnemyGroup_Replacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase, int ___m_phaseIndex,
            ref List<Sosig> ___m_activeSosigs, ref bool ___m_isFirstWave)
        {
            TNHFrameworkLogger.Log("Spawning enemy wave", TNHFrameworkLogger.LogType.TNH);

            // TODO: Add custom property form MinDirections
            int numAttackVectors = Random.Range(1, ___m_curPhase.MaxDirections + 1);
            numAttackVectors = Mathf.Clamp(numAttackVectors, 1, __instance.AttackVectors.Count);

            // Get the custom character data
            Phase currPhase = LoadedTemplateManager.CurrentLevel.HoldPhases[___m_phaseIndex];

            // Set first enemy to be spawned as leader
            SosigEnemyTemplate enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.LeaderType]];
            int enemiesToSpawn = Random.Range(___m_curPhase.MinEnemies, ___m_curPhase.MaxEnemies + 1);

            TNHFrameworkLogger.Log($"Spawning {enemiesToSpawn} hold guards (Phase {___m_phaseIndex})", TNHFrameworkLogger.LogType.TNH);

            int sosigsSpawned = 0;
            int vectorSpawnPoint = 0;
            Vector3 targetVector;
            int vectorIndex = 0;

            while (sosigsSpawned < enemiesToSpawn)
            {
                TNHFrameworkLogger.Log("Spawning at attack vector: " + vectorIndex, TNHFrameworkLogger.LogType.TNH);

                if (__instance.AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack.Count <= vectorSpawnPoint)
                    break;

                // Set the sosig's target position
                if (currPhase.SwarmPlayer)
                    targetVector = GM.CurrentPlayerBody.TorsoTransform.position;
                else
                    targetVector = __instance.SpawnPoints_Turrets[Random.Range(0, __instance.SpawnPoints_Turrets.Count)].position;

                Transform spawnPoint = __instance.AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack[vectorSpawnPoint];
                Sosig enemy = __instance.M.SpawnEnemy(enemyTemplate, spawnPoint.position, spawnPoint.rotation, ___m_curPhase.IFFUsed, true, targetVector, true);

                ___m_activeSosigs.Add(enemy);

                // At this point, the leader has been spawned, so always set enemy to be regulars
                enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.EnemyType.GetRandom<string>()]];
                sosigsSpawned++;

                vectorIndex++;
                if (vectorIndex >= numAttackVectors)
                {
                    vectorIndex = 0;
                    vectorSpawnPoint++;
                }
            }

            ___m_isFirstWave = false;
            return false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTakeEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnTakeGroupReplacement(TNH_HoldPoint __instance, ref List<Sosig> ___m_activeSosigs)
        {
            __instance.SpawnPoints_Sosigs_Defense.Shuffle();
            //__instance.SpawnPoints_Sosigs_Defense.Shuffle();

            int numGuards = Mathf.Clamp(__instance.T.NumGuards, 0, __instance.SpawnPoints_Sosigs_Defense.Count);
            TNHFrameworkLogger.Log($"Spawning {numGuards} hold guards via SpawnTakeEnemyGroup()", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numGuards; i++)
            {
                Transform transform = __instance.SpawnPoints_Sosigs_Defense[i];
                //Debug.Log("Take challenge sosig ID : " + __instance.T.GID);
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[__instance.T.GID];

                Sosig enemy = __instance.M.SpawnEnemy(template, transform.position, transform.rotation, __instance.T.IFFUsed, false, transform.position, true);

                ___m_activeSosigs.Add(enemy);
                __instance.M.RegisterGuard(enemy);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnWarpInMarkers")]
        [HarmonyPrefix]
        public static bool SpawnWarpInMarkers_Replacement(TNH_HoldPoint __instance, ref List<Transform> ___m_validSpawnPoints, TNH_HoldChallenge.Phase ___m_curPhase,
            ref int ___m_numTargsToSpawn, int ___m_phaseIndex, ref List<GameObject> ___m_warpInTargets)
        {
            ___m_validSpawnPoints.Clear();

            for (int i = 0; i < __instance.SpawnPoints_Targets.Count; i++)
            {
                if (__instance.SpawnPoints_Targets[i] != null)
                {
                    TNH_EncryptionSpawnPoint component = __instance.SpawnPoints_Targets[i].gameObject.GetComponent<TNH_EncryptionSpawnPoint>();
                    TNH_EncryptionType type = (__instance.M.TargetMode == TNHSetting_TargetMode.Simple) ? TNH_EncryptionType.Static : ___m_curPhase.Encryption;

                    if (component == null || component.AllowedSpawns[(int)type])
                        ___m_validSpawnPoints.Add(__instance.SpawnPoints_Targets[i]);
                }
            }

            if (!___m_validSpawnPoints.Any())
                ___m_validSpawnPoints.Add(__instance.SpawnPoints_Targets[0]);

            ___m_numTargsToSpawn = Random.Range(___m_curPhase.MinTargets, ___m_curPhase.MaxTargets + 1);

            if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
            {
                //___m_numTargsToSpawn = this.GetMaxTargsInHold();
                ___m_numTargsToSpawn = (int)miGetMaxTargsInHold.Invoke(__instance, []);

                if (___m_phaseIndex == 0)
                    ___m_numTargsToSpawn -= 2;

                if (___m_phaseIndex == 1)
                    ___m_numTargsToSpawn--;

                if (LoadedTemplateManager.CurrentCharacter.isCustom && ___m_numTargsToSpawn < 5)  // ODK - Need a few more
                    ___m_numTargsToSpawn = 5;
                else if (___m_numTargsToSpawn < 3)
                    ___m_numTargsToSpawn = 3;
            }

            ___m_numTargsToSpawn = Mathf.Min(___m_numTargsToSpawn, ___m_validSpawnPoints.Count);  // ODK - Moved this down
            ___m_validSpawnPoints.Shuffle<Transform>();

            for (int j = 0; j < ___m_numTargsToSpawn; j++)
            {
                GameObject item = Object.Instantiate<GameObject>(__instance.M.Prefab_TargetWarpingIn, ___m_validSpawnPoints[j].position, ___m_validSpawnPoints[j].rotation);
                ___m_warpInTargets.Add(item);
            }

            return false;
        }

        // Replaced because TNHFramework.Phase has extra features
        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTargetGroup")]
        [HarmonyPrefix]
        public static bool SpawnTargetGroup_Replacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase, List<Transform> ___m_validSpawnPoints)
        {
            Phase currentPhase = LoadedTemplateManager.CurrentCharacter.GetCurrentPhase(___m_curPhase);

            //__instance.DeleteAllActiveWarpIns();
            miDeleteAllActiveWarpIns.Invoke(__instance, []);

            int numTargets;
            int minTargets = currentPhase.MinTargets;
            int maxTargets = currentPhase.MaxTargets;

            if (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking)
            {
                minTargets = currentPhase.MinTargetsLimited;
                maxTargets = currentPhase.MaxTargetsLimited;

                if (LoadedTemplateManager.CurrentCharacter.isCustom && __instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                    maxTargets = Mathf.Max(maxTargets, 3);
            }
            else
            {
                if (LoadedTemplateManager.CurrentCharacter.isCustom && __instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                {
                    minTargets = Mathf.Max(minTargets, 3);
                    maxTargets = Mathf.Max(maxTargets, 5);
                }
            }

            List<FVRObject> encryptions;
            if (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking)
            {
                if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                    encryptions = [__instance.M.GetEncryptionPrefabSimple(TNH_EncryptionType.Static)];
                else
                    encryptions = [.. currentPhase.Encryptions.Select(o => __instance.M.GetEncryptionPrefabSimple(o))];
            }
            else
            {
                if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                    encryptions = [__instance.M.GetEncryptionPrefab(TNH_EncryptionType.Static)];
                else
                    encryptions = [.. currentPhase.Encryptions.Select(o => __instance.M.GetEncryptionPrefab(o))];
            }

            minTargets = Mathf.Min(minTargets, ___m_validSpawnPoints.Count);
            maxTargets = Mathf.Min(maxTargets, ___m_validSpawnPoints.Count);
            numTargets = Random.Range(minTargets, maxTargets + 1);

            for (int i = 0; i < numTargets && i < ___m_validSpawnPoints.Count; i++)
            {
                GameObject gameObject = Object.Instantiate(encryptions[i % encryptions.Count].GetGameObject(), ___m_validSpawnPoints[i].position, ___m_validSpawnPoints[i].rotation);
                TNH_EncryptionTarget encryption = gameObject.GetComponent<TNH_EncryptionTarget>();
                encryption.SetHoldPoint(__instance);
                __instance.RegisterNewTarget(encryption);
            }

            return false;
        }
    }
}
