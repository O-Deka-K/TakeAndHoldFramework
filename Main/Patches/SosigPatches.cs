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
    static class SosigPatches
    {
        private static readonly FieldInfo fiCurClothes = typeof(PlayerSosigBody).GetField("m_curClothes", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(FVRPlayerBody), "SetOutfit")]
        [HarmonyPrefix]
        public static bool SetOutfit_Replacement(ref PlayerSosigBody ___m_sosigPlayerBody, SosigEnemyTemplate tem)
        {
            if (___m_sosigPlayerBody == null)
                return false;

            GM.Options.ControlOptions.MBClothing = tem.SosigEnemyID;

            if (tem.SosigEnemyID != SosigEnemyID.None)
            {
                if (tem.OutfitConfig.Any() && LoadedTemplateManager.LoadedSosigsDict.ContainsKey(tem))
                {
                    OutfitConfig outfitConfig = LoadedTemplateManager.LoadedSosigsDict[tem].OutfitConfigs.GetRandom();

                    var curClothes = (List<GameObject>)fiCurClothes.GetValue(___m_sosigPlayerBody);
                    foreach (GameObject item in curClothes)
                    {
                        Object.Destroy(item);
                    }

                    curClothes.Clear();

                    int torsoIndex = -1;
                    if (outfitConfig.Chance_Torsowear >= Random.value)
                    {
                        torsoIndex = EquipSosigClothing(outfitConfig.Torsowear, curClothes, ___m_sosigPlayerBody.Sosig_Torso, -1, outfitConfig.ForceWearAllTorso);
                    }

                    if (outfitConfig.Chance_Headwear >= Random.value)
                    {
                        int headIndex = (outfitConfig.HeadUsesTorsoIndex) ? torsoIndex : -1;
                        EquipSosigClothing(outfitConfig.Headwear, curClothes, ___m_sosigPlayerBody.Sosig_Head, headIndex, outfitConfig.ForceWearAllHead);
                    }

                    int pantsIndex = -1;
                    if (outfitConfig.Chance_Pantswear >= Random.value)
                    {
                        pantsIndex = (outfitConfig.PantsUsesTorsoIndex) ? torsoIndex : -1;
                        pantsIndex = EquipSosigClothing(outfitConfig.Pantswear, curClothes, ___m_sosigPlayerBody.Sosig_Abdomen, pantsIndex, outfitConfig.ForceWearAllPants);
                    }

                    if (outfitConfig.Chance_Pantswear_Lower >= Random.value)
                    {
                        int pantsLowerIndex = (outfitConfig.PantsLowerUsesPantsIndex) ? pantsIndex : -1;
                        EquipSosigClothing(outfitConfig.Pantswear_Lower, curClothes, ___m_sosigPlayerBody.Sosig_Legs, pantsLowerIndex, outfitConfig.ForceWearAllPantsLower);
                    }

                    if (outfitConfig.Chance_Facewear >= Random.value)
                        EquipSosigClothing(outfitConfig.Facewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, -1, outfitConfig.ForceWearAllFace);

                    if (outfitConfig.Chance_Eyewear >= Random.value)
                        EquipSosigClothing(outfitConfig.Eyewear, curClothes, ___m_sosigPlayerBody.Sosig_Head, -1, outfitConfig.ForceWearAllEye);

                    if (outfitConfig.Chance_Backpacks >= Random.value)
                        EquipSosigClothing(outfitConfig.Backpacks, curClothes, ___m_sosigPlayerBody.Sosig_Torso, -1, outfitConfig.ForceWearAllBackpacks);

                    if (outfitConfig.Chance_TorosDecoration >= Random.value)
                        EquipSosigClothing(outfitConfig.TorosDecoration, curClothes, ___m_sosigPlayerBody.Sosig_Torso, -1, outfitConfig.ForceWearAllTorosDecorations);

                    if (outfitConfig.Chance_Belt >= Random.value)
                        EquipSosigClothing(outfitConfig.Belt, curClothes, ___m_sosigPlayerBody.Sosig_Abdomen, -1, outfitConfig.ForceWearAllBackpacks);
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
            SosigWeapon weapon = Object.Instantiate(weaponPrefab, sosig.transform.position + Vector3.up * 0.1f, sosig.transform.rotation).GetComponent<SosigWeapon>();
            weapon.SetAutoDestroy(true);
            weapon.O.SpawnLockable = false;

            //TNHFrameworkLogger.Log("Equipping sosig weapon: " + weapon.gameObject.name, TNHFrameworkLogger.LogType.TNH);

            // Equip the sosig weapon to the sosig
            sosig.ForceEquip(weapon);
            weapon.SetAmmoClamping(true);
            if (difficulty == TNHModifier_AIDifficulty.Arcade)
                weapon.FlightVelocityMultiplier = 0.3f;
        }

        public static int EquipSosigClothing(List<string> options, SosigLink link, int index, bool wearAll)
        {
            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = Object.Instantiate(IM.OD[clothing].GetGameObject(), link.transform.position, link.transform.rotation);
                    clothingObject.transform.SetParent(link.transform);
                    clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
                }
            }
            else
            {
                if (index < 0 || index >= options.Count)
                    index = Random.Range(0, options.Count);

                string clothing = options[index];
                GameObject clothingObject = Object.Instantiate(IM.OD[clothing].GetGameObject(), link.transform.position, link.transform.rotation);
                clothingObject.transform.SetParent(link.transform);
                clothingObject.GetComponent<SosigWearable>().RegisterWearable(link);
            }

            return index;
        }

        public static int EquipSosigClothing(List<string> options, List<GameObject> playerClothing, Transform link, int index, bool wearAll)
        {
            if (!options.Any())
                return -1;

            if (wearAll)
            {
                foreach (string clothing in options)
                {
                    GameObject clothingObject = Object.Instantiate(IM.OD[clothing].GetGameObject(), link.position, link.rotation);

                    Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                    foreach (Component child in children)
                    {
                        child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                        if (child is not Transform && child is not MeshFilter && child is not MeshRenderer)
                            Object.Destroy(child);
                    }

                    playerClothing.Add(clothingObject);
                    clothingObject.transform.SetParent(link);
                }
            }
            else
            {
                if (index < 0 || index >= options.Count)
                    index = Random.Range(0, options.Count);

                string clothing = options[index];
                GameObject clothingObject = Object.Instantiate(IM.OD[clothing].GetGameObject(), link.position, link.rotation);

                Component[] children = clothingObject.GetComponentsInChildren<Component>(true);
                foreach (Component child in children)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("ExternalCamOnly");

                    if (child is not Transform && child is not MeshFilter && child is not MeshRenderer)
                        Object.Destroy(child);
                }

                playerClothing.Add(clothingObject);
                clothingObject.transform.SetParent(link);
            }

            return index;
        }

        [HarmonyPatch(typeof(Sosig), "BuffHealing_Invis")]
        [HarmonyPrefix]
        public static bool OverrideCloaking()
        {
            return !TNHFramework.PreventOutfitFunctionality;
        }
    }
}
