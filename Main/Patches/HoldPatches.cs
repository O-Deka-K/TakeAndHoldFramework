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
        private static readonly MethodInfo miDeletionBurst = typeof(TNH_HoldPoint).GetMethod("DeletionBurst", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miIdentifyEncryption = typeof(TNH_HoldPoint).GetMethod("IdentifyEncryption", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miLowerAllBarriers = typeof(TNH_HoldPoint).GetMethod("LowerAllBarriers", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetMaxTargsInHold = typeof(TNH_HoldPoint).GetMethod("GetMaxTargsInHold", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiValidSpawnPoints = typeof(TNH_HoldPoint).GetField("m_validSpawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiCurLevel = typeof(TNH_Manager).GetField("m_curLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        // Anton pls fix - Use TNHSeed
        [HarmonyPatch(typeof(TNH_HoldPoint), "BeginPhase")]
        [HarmonyPostfix]
        public static void BeginPhase_TNHSeed(ref float ___m_tickDownToNextGroupSpawn, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            ___m_tickDownToNextGroupSpawn = ___m_curPhase.WarmUp * 0.8f;

            if (GM.TNHOptions.TNHSeed < 0)
            {
                ___m_tickDownToNextGroupSpawn = ___m_curPhase.WarmUp * UnityEngine.Random.Range(0.8f, 1.1f);
            }
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
                    ___m_tickDownToIdentification = UnityEngine.Random.Range(___m_curPhase.ScanTime * 0.9f, ___m_curPhase.ScanTime * 1.1f) + 60f;
            }
            else
            {
                ___m_tickDownToIdentification = ___m_curPhase.ScanTime * 0.8f;

                if (GM.TNHOptions.TNHSeed < 0)
                    ___m_tickDownToIdentification = UnityEngine.Random.Range(___m_curPhase.ScanTime * 0.8f, ___m_curPhase.ScanTime * 1.2f);

                if (__instance.M.IsBigLevel)
                    ___m_tickDownToIdentification += 15f;
            }
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "IdentifyEncryption")]
        [HarmonyPrefix]
        public static bool IdentifyEncryptionReplacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase, ref TNH_HoldPoint.HoldState ___m_state,
            ref float ___m_tickDownToFailure, ref TNH_HoldPointSystemNode ___m_systemNode)
        {
            Phase currentPhase = LoadedTemplateManager.CurrentCharacter.GetCurrentPhase(___m_curPhase);

            // If we shouldn't spawn any targets, we exit out early
            if ((currentPhase.MaxTargets < 1 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) ||
                (currentPhase.MaxTargetsLimited < 1 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo))
            {
                //__instance.CompletePhase();
                miCompletePhase.Invoke(__instance, []);
                return false;
            }

            ___m_state = TNH_HoldPoint.HoldState.Hacking;
            ___m_tickDownToFailure = 120f;

            if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
            {
                __instance.M.EnqueueEncryptionLine(TNH_EncryptionType.Static);
                //__instance.DeleteAllActiveWarpIns();
                miDeleteAllActiveWarpIns.Invoke(__instance, []);
                SpawnEncryptionReplacement(__instance, currentPhase, true);
            }
            else
            {
                __instance.M.EnqueueEncryptionLine(currentPhase.Encryptions[0]);
                //__instance.DeleteAllActiveWarpIns();
                miDeleteAllActiveWarpIns.Invoke(__instance, []);
                SpawnEncryptionReplacement(__instance, currentPhase, false);
            }

            ___m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);
            return false;
        }

        public static void SpawnEncryptionReplacement(TNH_HoldPoint holdPoint, Phase currentPhase, bool isSimple)
        {
            int numTargets;
            if (holdPoint.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo)
            {
                numTargets = UnityEngine.Random.Range(currentPhase.MinTargetsLimited, currentPhase.MaxTargetsLimited + 1);
            }
            else
            {
                numTargets = UnityEngine.Random.Range(currentPhase.MinTargets, currentPhase.MaxTargets + 1);
            }

            List<FVRObject> encryptions;
            if (isSimple)
            {
                encryptions = [holdPoint.M.GetEncryptionPrefab(TNH_EncryptionType.Static)];
            }
            else
            {
                encryptions = currentPhase.Encryptions.Select(o => holdPoint.M.GetEncryptionPrefab(o)).ToList();
            }

            var validSpawnPoints = (List<Transform>)fiValidSpawnPoints.GetValue(holdPoint);

            for (int i = 0; i < numTargets && i < validSpawnPoints.Count; i++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(encryptions[i % encryptions.Count].GetGameObject(), validSpawnPoints[i].position, validSpawnPoints[i].rotation);
                TNH_EncryptionTarget encryption = gameObject.GetComponent<TNH_EncryptionTarget>();
                encryption.SetHoldPoint(holdPoint);
                holdPoint.RegisterNewTarget(encryption);
            }
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "CompletePhase")]
        [HarmonyPrefix]
        public static bool NextPhasePatch(TNH_HoldPoint __instance, ref int ___m_phaseIndex, ref TNH_HoldPointSystemNode ___m_systemNode,
            float ___m_tickDownToFailure, ref List<Transform> ___m_validSpawnPoints, ref TNH_HoldPoint.HoldState ___m_state, ref float ___m_tickDownTransition,
            bool ___m_hasBeenDamagedThisPhase)
        {
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            var curLevel = (TNH_Progression.Level)fiCurLevel.GetValue(__instance.M);

            if (character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].DespawnBetweenWaves)
            {
                //__instance.DeletionBurst();
                miDeletionBurst.Invoke(__instance, []);
                __instance.M.ClearMiscEnemies();
                UnityEngine.Object.Instantiate(__instance.VFX_HoldWave, ___m_systemNode.NodeCenter.position, ___m_systemNode.NodeCenter.rotation);
            }

            if (character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].UsesVFX)
            {
                SM.PlayCoreSound(FVRPooledAudioType.GenericLongRange, __instance.AUDEvent_HoldWave, __instance.transform.position);
                __instance.M.EnqueueLine(TNH_VoiceLineID.AI_Encryption_Neutralized);
            }

            __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldDecisecondsRemaining, (int)(___m_tickDownToFailure * 10f));
            ___m_phaseIndex++;

            if (character.GetCurrentLevel(curLevel).HoldPhases.Count > ___m_phaseIndex &&
                character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].ScanTime == 0 &&
                character.GetCurrentLevel(curLevel).HoldPhases[___m_phaseIndex].WarmupTime == 0)
            {
                __instance.SpawnPoints_Targets.Shuffle();
                ___m_validSpawnPoints.Shuffle();
                //__instance.IdentifyEncryption();
                miIdentifyEncryption.Invoke(__instance, []);
            }
            else
            {
                ___m_state = TNH_HoldPoint.HoldState.Transition;
                ___m_tickDownTransition = 5f;
                ___m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Hacking);
            }

            //__instance.LowerAllBarriers();
            miLowerAllBarriers.Invoke(__instance, []);

            if (!___m_hasBeenDamagedThisPhase)
            {
                __instance.M.IncrementScoringStat(TNH_Manager.ScoringEvent.HoldWaveCompleteNoDamage, 1);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawningRoutineUpdate")]
        [HarmonyPrefix]
        public static bool SpawningUpdateReplacement(TNH_HoldPoint __instance, ref float ___m_tickDownToNextGroupSpawn, ref List<Sosig> ___m_activeSosigs,
            TNH_HoldPoint.HoldState ___m_state, ref bool ___m_hasThrownNadesInWave, bool ___m_isFirstWave, int ___m_phaseIndex, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (___m_activeSosigs.Count < 1 && ___m_state == TNH_HoldPoint.HoldState.Analyzing)
                ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (!___m_hasThrownNadesInWave && ___m_tickDownToNextGroupSpawn <= 5f && !___m_isFirstWave)
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

                SpawnHoldEnemyGroup(___m_curPhase, ___m_phaseIndex, __instance.AttackVectors, __instance.SpawnPoints_Turrets, ___m_activeSosigs, __instance.M, ref ___m_isFirstWave);
                ___m_hasThrownNadesInWave = false;

                // Adjust spawn cadence depending on ammo mode
                float ammoMult = (__instance.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo ? 1.35f : 1f);
                float randomMult = (GM.TNHOptions.TNHSeed >= 0) ? 0.9f : UnityEngine.Random.Range(0.9f, 1.1f);
                ___m_tickDownToNextGroupSpawn = ___m_curPhase.SpawnCadence * randomMult * ammoMult;
            }

            return false;
        }

        public static void SpawnGrenades(List<TNH_HoldPoint.AttackVector> AttackVectors, TNH_Manager M, int phaseIndex)
        {
            var curLevel = (TNH_Progression.Level)fiCurLevel.GetValue(M);
            Level currLevel = LoadedTemplateManager.CurrentCharacter.GetCurrentLevel(curLevel);
            Phase currPhase = currLevel.HoldPhases[phaseIndex];

            float grenadeChance = currPhase.GrenadeChance;
            string grenadeType = currPhase.GrenadeType;

            if (grenadeChance >= UnityEngine.Random.Range(0f, 1f))
            {
                TNHFrameworkLogger.Log($"Throwing grenade [{grenadeType}]", TNHFrameworkLogger.LogType.TNH);

                // Get a random grenade vector to spawn a grenade at
                AttackVectors.Shuffle();
                TNH_HoldPoint.AttackVector randAttackVector = AttackVectors[UnityEngine.Random.Range(0, AttackVectors.Count)];

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

        public static void SpawnHoldEnemyGroup(TNH_HoldChallenge.Phase curPhase, int phaseIndex, List<TNH_HoldPoint.AttackVector> AttackVectors, List<Transform> SpawnPoints_Turrets, List<Sosig> ActiveSosigs, TNH_Manager M, ref bool isFirstWave)
        {
            TNHFrameworkLogger.Log("Spawning enemy wave", TNHFrameworkLogger.LogType.TNH);

            // TODO: Add custom property form MinDirections
            int numAttackVectors = UnityEngine.Random.Range(1, curPhase.MaxDirections + 1);
            numAttackVectors = Mathf.Clamp(numAttackVectors, 1, AttackVectors.Count);

            // Get the custom character data
            var curLevel = (TNH_Progression.Level)fiCurLevel.GetValue(M);
            Level currLevel = LoadedTemplateManager.CurrentCharacter.GetCurrentLevel(curLevel);
            Phase currPhase = currLevel.HoldPhases[phaseIndex];

            // Set first enemy to be spawned as leader
            SosigEnemyTemplate enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.LeaderType]];
            int enemiesToSpawn = UnityEngine.Random.Range(curPhase.MinEnemies, curPhase.MaxEnemies + 1);

            TNHFrameworkLogger.Log($"Spawning {enemiesToSpawn} hold guards (Phase {phaseIndex})", TNHFrameworkLogger.LogType.TNH);

            int sosigsSpawned = 0;
            int vectorSpawnPoint = 0;
            Vector3 targetVector;
            int vectorIndex = 0;
            
            while (sosigsSpawned < enemiesToSpawn)
            {
                TNHFrameworkLogger.Log("Spawning at attack vector: " + vectorIndex, TNHFrameworkLogger.LogType.TNH);

                if (AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack.Count <= vectorSpawnPoint)
                    break;

                // Set the sosig's target position
                if (currPhase.SwarmPlayer)
                {
                    targetVector = GM.CurrentPlayerBody.TorsoTransform.position;
                }
                else
                {
                    targetVector = SpawnPoints_Turrets[UnityEngine.Random.Range(0, SpawnPoints_Turrets.Count)].position;
                }

                Sosig enemy = TNHManagerPatches.SpawnEnemy(enemyTemplate, AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack[vectorSpawnPoint], M, curPhase.IFFUsed, true, targetVector, true);

                ActiveSosigs.Add(enemy);

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

            isFirstWave = false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTurrets")]
        [HarmonyPrefix]
        public static bool SpawnTurretsReplacement(TNH_HoldPoint __instance, ref List<AutoMeater> ___m_activeTurrets)
        {
            __instance.SpawnPoints_Turrets.Shuffle<Transform>();
            FVRObject turretPrefab = __instance.M.GetTurretPrefab(__instance.T.TurretType);

            for (int i = 0; i < __instance.T.NumTurrets && i < __instance.SpawnPoints_Turrets.Count; i++)
            {
                Vector3 pos = __instance.SpawnPoints_Turrets[i].position + Vector3.up * 0.25f;
                AutoMeater turret = UnityEngine.Object.Instantiate<GameObject>(turretPrefab.GetGameObject(), pos, __instance.SpawnPoints_Turrets[i].rotation).GetComponent<AutoMeater>();
                ___m_activeTurrets.Add(turret);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTakeEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnTakeGroupReplacement(TNH_HoldPoint __instance, ref List<Sosig> ___m_activeSosigs)
        {
            __instance.SpawnPoints_Sosigs_Defense.Shuffle();
            //__instance.SpawnPoints_Sosigs_Defense.Shuffle();

            TNHFrameworkLogger.Log($"Spawning {__instance.T.NumGuards} hold guards via SpawnTakeEnemyGroup()", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < __instance.T.NumGuards && i < __instance.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = __instance.SpawnPoints_Sosigs_Defense[i];
                //Debug.Log("Take challenge sosig ID : " + __instance.T.GID);
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[__instance.T.GID];

                Sosig enemy = TNHManagerPatches.SpawnEnemy(template, transform, __instance.M, __instance.T.IFFUsed, false, transform.position, true);

                ___m_activeSosigs.Add(enemy);
                __instance.M.RegisterGuard(enemy);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnHoldEnemyGroup")]
        [HarmonyPrefix]
        private static bool SpawnHoldEnemyGroupStub()
        {
            // We've replaced all calls to SpawnHoldEnemyGroup() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("SpawnHoldEnemyGroupStub() called! This should have been overridden!");
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

                    if (component == null)
                        ___m_validSpawnPoints.Add(__instance.SpawnPoints_Targets[i]);
                    else if (component.AllowedSpawns[(int)___m_curPhase.Encryption])
                        ___m_validSpawnPoints.Add(__instance.SpawnPoints_Targets[i]);
                }
            }

            if (___m_validSpawnPoints.Count <= 0)
                ___m_validSpawnPoints.Add(__instance.SpawnPoints_Targets[0]);

            ___m_numTargsToSpawn = UnityEngine.Random.Range(___m_curPhase.MinTargets, ___m_curPhase.MaxTargets + 1);

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
                GameObject item = UnityEngine.Object.Instantiate<GameObject>(__instance.M.Prefab_TargetWarpingIn, ___m_validSpawnPoints[j].position, ___m_validSpawnPoints[j].rotation);
                ___m_warpInTargets.Add(item);
            }

            return false;
        }
    }
}
