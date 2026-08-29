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
                if ((!TNHFramework.SavedConfig.EnableBlister && construct is Construct_Blister_Volume)
                    || (!TNHFramework.SavedConfig.EnableFloater && construct is Construct_Floater_Volume)
                    || (!TNHFramework.SavedConfig.EnableIris && construct is Construct_Iris_Volume)
                    || (!TNHFramework.SavedConfig.EnableSentinel && construct is Construct_Sentinel_Path))
                {
                    __instance.ExcludeConstructVolumes.Add(construct);
                }
            }
        }

        // Anton pls fix - Use TNHSeed
        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
        [HarmonyPostfix]
        public static void BeginPhase_TNHSeed(ref float ___m_tickDownToNextGroupSpawn, TNH_HoldChallenge.Phase ___m_curPhase, int ___m_phaseIndex)
        {
            TNHFrameworkLogger.Log($"Beginning HOLD PHASE -- Wave {___m_phaseIndex + 1}", TNHFrameworkLogger.LogType.TNH);

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
            if ((currentPhase.MaxTargets <= 0 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) || (currentPhase.MaxTargets <= 0 && currentPhase.MaxTargetsLimited <= 0))
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

            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
                ___m_tickDownToNextGroupSpawn -= Time.deltaTime * (0.4f + (float)___m_phaseIndex * 0.4f);

            if (!___m_activeSosigs.Any() && ___m_state == TNH_HoldPoint.HoldState.Analyzing)
                ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (!___m_hasThrownNadesInWave && ___m_tickDownToNextGroupSpawn <= 5f)
            {
                // Throw grenades at start of each Hold wave (custom characters only). This was in the vanilla game, but later removed.
                // I don't think GrenadeVector exists anywhere anymore, so this might never happen.
                if (!___m_isFirstWave && LoadedTemplateManager.CurrentCharacter.isCustom)
                {
                    // Check if grenade vectors exist before throwing grenades
                    if (__instance.AttackVectors[0].GrenadeVector != null)
                        SpawnGrenades(__instance.AttackVectors, __instance.M, ___m_phaseIndex);
                }

                ___m_hasThrownNadesInWave = true;
                __instance.AttackVectors.Shuffle();
            }

            // Handle spawning of a wave if it is time
            if (___m_curPhase != null && ___m_tickDownToNextGroupSpawn <= 0 /* && ___m_activeSosigs.Count + ___m_curPhase.MaxEnemies <= ___m_curPhase.MaxEnemiesAlive */)
            {
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
                    GameObject grenadeObject = Object.Instantiate(IM.OD[grenadeType].GetGameObject(), randAttackVector.GrenadeVector.position, randAttackVector.GrenadeVector.rotation);

                    // Give the grenade an initial velocity based on the grenade vector
                    grenadeObject.GetComponent<Rigidbody>().velocity = 15 * randAttackVector.GrenadeVector.forward;
                    grenadeObject.GetComponent<SosigWeapon>().FuseGrenade();
                }
            }
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnHoldEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnHoldEnemyGroup_Replacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase, int ___m_phaseIndex,
            ref Sosig ___m_holdGroupLeader, ref bool ___m_isCurrentWaveBoss, ref List<Sosig> ___m_activeSosigs, ref bool ___m_isFirstWave)
        {
            // Get the custom character data
            Phase currPhase = LoadedTemplateManager.CurrentLevel.HoldPhases[___m_phaseIndex];

            int maxNumSpawnable = ___m_curPhase.MaxEnemiesAlive - ___m_activeSosigs.Count;
            if (maxNumSpawnable <= 0)
                return false;

            TNHFrameworkLogger.Log("Spawning enemy wave", TNHFrameworkLogger.LogType.TNH);

            int numToSpawn = Random.Range(___m_curPhase.MinEnemies, ___m_curPhase.MaxEnemies + 1);
            numToSpawn = Mathf.Clamp(numToSpawn, 0, maxNumSpawnable);

            if (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking && numToSpawn > 2)
                numToSpawn--;

            int maxDirectionsToSpawnFrom = Mathf.Clamp(___m_curPhase.MaxDirections, 0, __instance.AttackVectors.Count);

            // Find the maximum number that can be spawned based on the number of directions to spawn from
            maxNumSpawnable = 0;
            for (int direction = 0; direction < maxDirectionsToSpawnFrom; direction++)
                maxNumSpawnable += __instance.AttackVectors[direction].SpawnPoints_Sosigs_Attack.Count;

            numToSpawn = Mathf.Clamp(numToSpawn, 0, maxNumSpawnable);

            // Set first enemy to be spawned as leader
            SosigEnemyTemplate sosigTemplate = currPhase.OverrideLType ?? ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.LeaderType]];
            SosigEnemyTemplate sosigEType = currPhase.OverrideEType ?? ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.EnemyType.GetRandom()]];

            TNHFrameworkLogger.Log($"Spawning {numToSpawn} hold guards (Phase {___m_phaseIndex + 1})", TNHFrameworkLogger.LogType.TNH);

            int numSpawned = 0;
            int spawnIndex = 0;
            Vector3 targetPosition;

            while (numSpawned < numToSpawn)
            {
                for (int direction = 0; direction < maxDirectionsToSpawnFrom; direction++)
                {
                    if (spawnIndex >= __instance.AttackVectors[direction].SpawnPoints_Sosigs_Attack.Count)
                        continue;

                    Transform spawnPoint = __instance.AttackVectors[direction].SpawnPoints_Sosigs_Attack[spawnIndex];

                    bool isLeader = true;
                    if (numSpawned > 0 || ___m_holdGroupLeader != null)
                    {
                        sosigTemplate = sosigEType;
                        isLeader = false;
                    }

                    // Set the sosig's target vector
                    if (currPhase.SwarmPlayer)
                        targetPosition = GM.CurrentPlayerBody.TorsoTransform.position;
                    else
                        targetPosition = __instance.SpawnPoints_Turrets[Random.Range(0, __instance.SpawnPoints_Turrets.Count)].position;

                    // Only the first sosig spawning from each direction is allowed all weapons
                    // In vanilla, only the sosigs spawning from the first direction are allowed all weapons. Not sure if this is the intended behavior.
                    Sosig sosig = __instance.M.SpawnEnemy(sosigTemplate, spawnPoint.position, spawnPoint.rotation, ___m_curPhase.IFFUsed, true, targetPosition, spawnIndex == 0);
                    ___m_activeSosigs.Add(sosig);
                    numSpawned++;

                    if (isLeader)
                    {
                        ___m_holdGroupLeader = sosig;

                        if (___m_curPhase.IsLeaderBoss)
                            ___m_isCurrentWaveBoss = true;
                    }

                }

                spawnIndex++;
            }

            ___m_isFirstWave = false;
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

            ___m_validSpawnPoints.Shuffle<Transform>();

            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
            {
                ___m_numTargsToSpawn = ___m_phaseIndex + 1;
            }
            else
            {
                int min = ___m_curPhase.MinTargets;
                int max = ___m_curPhase.MaxTargets;

                if (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking)
                {
                    if (___m_curPhase.MinTargets_Limited > 0)
                        min = ___m_curPhase.MinTargets_Limited;

                    if (___m_curPhase.MaxTargets_Limited > 0)
                        max = ___m_curPhase.MaxTargets_Limited;
                }

                min = Mathf.Clamp(min, 0, ___m_validSpawnPoints.Count);
                max = Mathf.Clamp(max, 0, ___m_validSpawnPoints.Count);
                ___m_numTargsToSpawn = Random.Range(min, max + 1);

                if (__instance.M.GameMode == TNHSetting_GameMode.Classic && __instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                {
                    //___m_numTargsToSpawn = this.GetMaxTargsInHold();
                    ___m_numTargsToSpawn = (int)miGetMaxTargsInHold.Invoke(__instance, []);

                    if (___m_phaseIndex == 0)
                        ___m_numTargsToSpawn -= 2;

                    if (___m_phaseIndex == 1)
                        ___m_numTargsToSpawn--;

                    if (max >= 3)
                        ___m_numTargsToSpawn = Mathf.Max(___m_numTargsToSpawn, ___m_numTargsToSpawn, 3);
                    else
                        ___m_numTargsToSpawn = Mathf.Max(___m_numTargsToSpawn, ___m_numTargsToSpawn, max);
                }

                ___m_numTargsToSpawn = Mathf.Clamp(___m_numTargsToSpawn, 0, ___m_validSpawnPoints.Count);  // ODK - Moved this down
            }

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
        public static bool SpawnTargetGroup_Replacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase, int ___m_numTargsToSpawn, List<Transform> ___m_validSpawnPoints)
        {
            Phase currentPhase = LoadedTemplateManager.CurrentCharacter.GetCurrentPhase(___m_curPhase);

            //__instance.DeleteAllActiveWarpIns();
            miDeleteAllActiveWarpIns.Invoke(__instance, []);

            int numTargets = ___m_numTargsToSpawn;

            if (!LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                if (__instance.M.EquipmentMode != TNHSetting_EquipmentMode.Spawnlocking && currentPhase.Encryptions.Any())
                {
                    if (currentPhase.Encryptions[0] == TNH_EncryptionType.Static || __instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                        numTargets = Mathf.Clamp(numTargets, 1, 3);
                    else
                        numTargets = 1;
                }
            }

            List<FVRObject> encryptions;
            if (__instance.M.GameMode == TNHSetting_GameMode.Rampart)
            {
                encryptions = __instance.M.ResourceLib.EncryptionUnknown;
            }
            else if (__instance.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking)
            {
                if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                {
                    encryptions = [__instance.M.GetEncryptionPrefab(TNH_EncryptionType.Static)];
                }
                else
                {
                    encryptions = [];

                    foreach (TNH_EncryptionType encryption in currentPhase.Encryptions)
                    {
                        if ((encryption == TNH_EncryptionType.Regenerative && TNHFramework.SavedConfig.SimpleRegenerative)
                            || (encryption == TNH_EncryptionType.Cascading && TNHFramework.SavedConfig.SimpleCascading)
                            || (encryption == TNH_EncryptionType.Orthagonal && TNHFramework.SavedConfig.SimpleOrthogonal))
                        {
                            TNHFrameworkLogger.Log($"Spawning simple {encryption} encryption", TNHFrameworkLogger.LogType.TNH);
                            encryptions.Add(__instance.M.GetEncryptionPrefabSimple(encryption));
                        }
                        else
                        {
                            encryptions.Add(__instance.M.GetEncryptionPrefab(encryption));
                        }
                    }
                }
            }
            else
            {
                if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
                    encryptions = [__instance.M.GetEncryptionPrefabSimple(TNH_EncryptionType.Static)];
                else
                    encryptions = [.. currentPhase.Encryptions.Select(__instance.M.GetEncryptionPrefabSimple)];
            }

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
