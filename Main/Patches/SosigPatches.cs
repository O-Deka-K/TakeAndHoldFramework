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
                        EquipSosigClothing(outfitConfig.Headwear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllHead);

                    if (outfitConfig.Chance_Facewear >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Facewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllFace);

                    if (outfitConfig.Chance_Eyewear >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Eyewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, outfitConfig.ForceWearAllEye);

                    if (outfitConfig.Chance_Torsowear >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Torsowear, curClothes, ___m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllTorso);

                    if (outfitConfig.Chance_Pantswear >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Pantswear, curClothes, ___m_sosigPlayerBody.Sosig_Abdomen, outfitConfig.ForceWearAllPants);

                    if (outfitConfig.Chance_Pantswear_Lower >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Pantswear_Lower, curClothes, ___m_sosigPlayerBody.Sosig_Legs, outfitConfig.ForceWearAllPantsLower);

                    if (outfitConfig.Chance_Backpacks >= UnityEngine.Random.value)
                        EquipSosigClothing(outfitConfig.Backpacks, curClothes, ___m_sosigPlayerBody.Sosig_Torso, outfitConfig.ForceWearAllBackpacks);
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(Sosig), "ClearSosig")]
        [HarmonyPrefix]
        public static void ClearSosig(Sosig __instance)
        {
            SosigLinkLootWrapper lootWrapper = __instance.GetComponentInChildren<SosigLinkLootWrapper>();
            lootWrapper?.dontDrop = !lootWrapper.shouldDropOnCleanup;
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
                            UnityEngine.Object.Destroy(child);
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
                        UnityEngine.Object.Destroy(child);
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
