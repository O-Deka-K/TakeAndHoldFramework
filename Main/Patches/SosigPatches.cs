using FistVR;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework.Patches
{
    static class SosigPatches
    {
        private static readonly FieldInfo fiCurClothes = typeof(PlayerSosigBody).GetField("m_curClothes", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(FVRPlayerBody), "SetOutfit")]
        [HarmonyPrefix]
        public static bool SetOutfitReplacement(ref PlayerSosigBody ___m_sosigPlayerBody, SosigEnemyTemplate tem)
        {
            if (___m_sosigPlayerBody == null)
                return false;

            GM.Options.ControlOptions.MBClothing = tem.SosigEnemyID;
            if (tem.SosigEnemyID != SosigEnemyID.None)
            {
                if (tem.OutfitConfig.Count > 0 && LoadedTemplateManager.LoadedSosigsDict.ContainsKey(tem))
                {
                    OutfitConfig outfitConfig = LoadedTemplateManager.LoadedSosigsDict[tem].OutfitConfigs.GetRandom();

                    var curClothes = (List<GameObject>)fiCurClothes.GetValue(___m_sosigPlayerBody);
                    foreach (GameObject item in curClothes)
                    {
                        UnityEngine.Object.Destroy(item);
                    }
                    curClothes.Clear();

                    if (outfitConfig.Chance_Headwear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Headwear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllHead);
                    }

                    if (outfitConfig.Chance_Facewear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Facewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllFace);
                    }

                    if (outfitConfig.Chance_Eyewear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Eyewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllEye);
                    }

                    if (outfitConfig.Chance_Torsowear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Torsowear, curClothes, ___m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllTorso);
                    }

                    if (outfitConfig.Chance_Pantswear >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Pantswear, curClothes, ___m_sosigPlayerBody.Sosig_Abdomen, outfitConfig.ForceWearAllPants);
                    }

                    if (outfitConfig.Chance_Pantswear_Lower >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Pantswear_Lower, curClothes, ___m_sosigPlayerBody.Sosig_Legs, outfitConfig.ForceWearAllPantsLower);
                    }

                    if (outfitConfig.Chance_Backpacks >= UnityEngine.Random.value)
                    {
                        EquipSosigClothing(outfitConfig.Backpacks, curClothes, ___m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllBackpacks);
                    }
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(Sosig), "ClearSosig")]
        [HarmonyPrefix]
        public static void ClearSosig(Sosig __instance)
        {
            SosigLinkLootWrapper lootWrapper = __instance.GetComponentInChildren<SosigLinkLootWrapper>();
            if (lootWrapper != null)
            {
                lootWrapper.dontDrop = !lootWrapper.shouldDropOnCleanup;
            }
        }

        public static Sosig SpawnEnemy(SosigEnemyTemplate template, Transform spawnLocation, TNH_Manager M, int IFF, bool isAssault, Vector3 pointOfInterest, bool allowAllWeapons)
        {
            return SpawnEnemy(template, spawnLocation.position, spawnLocation.rotation, M, IFF, isAssault, pointOfInterest, allowAllWeapons);
        }

        public static Sosig SpawnEnemy(SosigEnemyTemplate template, Vector3 spawnLocation, Quaternion spawnRotation, TNH_Manager M, int IFF, bool isAssault, Vector3 pointOfInterest, bool allowAllWeapons)
        {
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

            TNHFrameworkLogger.Log("Spawning sosig: " + customTemplate.SosigEnemyID, TNHFrameworkLogger.LogType.TNH);

            // Fill out the sosig's config based on the difficulty
            SosigConfig config;

            if (M.AI_Difficulty == TNHModifier_AIDifficulty.Arcade && customTemplate.ConfigsEasy.Count > 0)
            {
                config = customTemplate.ConfigsEasy.GetRandom<SosigConfig>();
            }
            else if (customTemplate.Configs.Count > 0)
            {
                config = customTemplate.Configs.GetRandom<SosigConfig>();
            }
            else
            {
                TNHFrameworkLogger.LogError("Sosig did not have normal difficulty config when playing on normal difficulty! Not spawning this enemy!");
                return null;
            }

            // Create the sosig object
            GameObject sosigPrefab = UnityEngine.Object.Instantiate(IM.OD[customTemplate.SosigPrefabs.GetRandom<string>()].GetGameObject(), spawnLocation, spawnRotation);
            Sosig sosigComponent = sosigPrefab.GetComponentInChildren<Sosig>();

            sosigComponent.Configure(config.GetConfigTemplate());
            sosigComponent.SetIFF(IFF);

            // Set up the sosig's inventory
            sosigComponent.Inventory.Init();
            sosigComponent.Inventory.FillAllAmmo();
            sosigComponent.InitHands();

            // Equip the sosig's weapons
            if (customTemplate.WeaponOptions.Count > 0)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptions.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            if (character.ForceAllAgentWeapons)
                allowAllWeapons = true;

            if (customTemplate.WeaponOptionsSecondary.Count > 0 && allowAllWeapons && customTemplate.SecondaryChance >= UnityEngine.Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsSecondary.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            if (customTemplate.WeaponOptionsTertiary.Count > 0 && allowAllWeapons && customTemplate.TertiaryChance >= UnityEngine.Random.value)
            {
                GameObject weaponPrefab = IM.OD[customTemplate.WeaponOptionsTertiary.GetRandom<string>()].GetGameObject();
                EquipSosigWeapon(sosigComponent, weaponPrefab, M.AI_Difficulty);
            }

            // Equip clothing to the sosig
            OutfitConfig outfitConfig = customTemplate.OutfitConfigs.GetRandom<OutfitConfig>();

            if (outfitConfig.Headwear.Count > 0 && outfitConfig.Chance_Headwear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Headwear, sosigComponent.Links[0], outfitConfig.ForceWearAllHead);
            }

            if (outfitConfig.Facewear.Count > 0 && outfitConfig.Chance_Facewear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Facewear, sosigComponent.Links[0], outfitConfig.ForceWearAllFace);
            }

            if (outfitConfig.Eyewear.Count > 0 && outfitConfig.Chance_Eyewear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Eyewear, sosigComponent.Links[0], outfitConfig.ForceWearAllEye);
            }

            if (outfitConfig.Torsowear.Count > 0 && outfitConfig.Chance_Torsowear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Torsowear, sosigComponent.Links[1], outfitConfig.ForceWearAllTorso);
            }

            if (outfitConfig.Pantswear.Count > 0 && outfitConfig.Chance_Pantswear >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Pantswear, sosigComponent.Links[2], outfitConfig.ForceWearAllPants);
            }

            if (outfitConfig.Pantswear_Lower.Count > 0 && outfitConfig.Chance_Pantswear_Lower >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Pantswear_Lower, sosigComponent.Links[3], outfitConfig.ForceWearAllPantsLower);
            }

            if (outfitConfig.Backpacks.Count > 0 && outfitConfig.Chance_Backpacks >= UnityEngine.Random.value)
            {
                EquipSosigClothing(outfitConfig.Backpacks, sosigComponent.Links[1], outfitConfig.ForceWearAllBackpacks);
            }

            // Set up the sosig's orders
            if (isAssault)
            {
                sosigComponent.CurrentOrder = Sosig.SosigOrder.Assault;
                sosigComponent.FallbackOrder = Sosig.SosigOrder.Assault;
                sosigComponent.CommandAssaultPoint(pointOfInterest);
            }
            else
            {
                sosigComponent.CurrentOrder = Sosig.SosigOrder.Wander;
                sosigComponent.FallbackOrder = Sosig.SosigOrder.Wander;
                sosigComponent.CommandGuardPoint(pointOfInterest, true);
                sosigComponent.SetDominantGuardDirection(UnityEngine.Random.onUnitSphere);
            }
            sosigComponent.SetGuardInvestigateDistanceThreshold(25f);

            // Handle sosig dropping custom loot
            if (customTemplate.DroppedObjectPool != null && UnityEngine.Random.value < customTemplate.DroppedLootChance)
            {
                SosigLinkLootWrapper component = sosigComponent.Links[2].gameObject.AddComponent<SosigLinkLootWrapper>();
                component.M = M;
                component.character = character;
                component.shouldDropOnCleanup = !character.DisableCleanupSosigDrops;
                component.group = new(customTemplate.DroppedObjectPool);
                component.group.DelayedInit(character.GlobalObjectBlacklist);
            }

            return sosigComponent;
        }

        public static void EquipSosigWeapon(Sosig sosig, GameObject weaponPrefab, TNHModifier_AIDifficulty difficulty)
        {
            SosigWeapon weapon = UnityEngine.Object.Instantiate(weaponPrefab, sosig.transform.position + Vector3.up * 0.1f, sosig.transform.rotation).GetComponent<SosigWeapon>();
            weapon.SetAutoDestroy(true);
            weapon.O.SpawnLockable = false;

            //TNHFrameworkLogger.Log("Equipping sosig weapon: " + weapon.gameObject.name, TNHFrameworkLogger.LogType.TNH);

            // Equip the sosig weapon to the sosig
            sosig.ForceEquip(weapon);
            weapon.SetAmmoClamping(true);
            if (difficulty == TNHModifier_AIDifficulty.Arcade)
                weapon.FlightVelocityMultiplier = 0.3f;
        }

        public static void EquipSosigClothing(List<string> options, SosigLink link, bool wearAll)
        {
            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[clothing].GetGameObject(), link.transform.position, link.transform.rotation);
                    clothingObject.transform.SetParent(link.transform);
                    clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
                }
            }
            else
            {
                GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[options.GetRandom<string>()].GetGameObject(), link.transform.position, link.transform.rotation);
                clothingObject.transform.SetParent(link.transform);
                clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
            }
        }

        public static void EquipSosigClothing(List<string> options, List<GameObject> playerClothing, Transform link, bool wearAll)
        {
            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[clothing].GetGameObject(), link.position, link.rotation);

                    Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                    foreach (Component child in children)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                        if (!(child is Transform) && !(child is MeshFilter) && !(child is MeshRenderer))
                        {
                            UnityEngine.Object.Destroy(child);
                        }
                    }

                    playerClothing.Add(clothingObject);
                    clothingObject.transform.SetParent(link);
                }
            }
            else
            {
                GameObject clothingObject = UnityEngine.Object.Instantiate(IM.OD[options.GetRandom<string>()].GetGameObject(), link.position, link.rotation);

                Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                foreach (Component child in children)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                    if (!(child is Transform) && !(child is MeshFilter) && !(child is MeshRenderer))
                    {
                        UnityEngine.Object.Destroy(child);
                    }
                }

                playerClothing.Add(clothingObject);
                clothingObject.transform.SetParent(link);
            }
        }

        [HarmonyPatch(typeof(Sosig), "BuffHealing_Invis")]
        [HarmonyPrefix]
        public static bool OverrideCloaking()
        {
            return !TNHFramework.PreventOutfitFunctionality;
        }
    }
}
