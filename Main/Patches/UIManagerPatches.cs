using FistVR;
using FistVR.Ugc;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection; 
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework.Patches
{
    static class UIManagerPatches
    {
        private static readonly MethodInfo miInitCharacterCategories = typeof(TNH_UIManager).GetMethod("InitCharacterCategories", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miConfigureButtonStateFromOptions = typeof(TNH_UIManager).GetMethod("ConfigureButtonStateFromOptions", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miAddItemToTree = typeof(UgcManager).GetMethod("AddItemToTree", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetCharacterCategoryFromCharacter = typeof(TNH_UIManager).GetMethod("SetCharacterCategoryFromCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Image SelectedCharacter_Image;
        private static Vector3 TitlePositionLeft = new(-138, 456, 0);
        private static Vector3 TitlePositionCenter = new(-1, 456, 0);
        private static Vector3 DescriptionPositionLeft = new(-138, 306, 0);
        private static Vector3 DescriptionPositionCenter = new(-1, 306, 0);
        private static string LastPlayedChar;

        // Nice try Anton.
        // Just kidding. It could be useful for disabling TNH for older, incompatible versions of TNHFramework. Or it might just crash.
        [HarmonyPatch(typeof(TNH_UIManager), "TNHTweakerChecker")]
        [HarmonyPrefix]
        public static bool TNHTweakerChecker_Disabler()
        {
            return false;
        }

        // Performs initial setup of the TNH Scene when loaded
        [HarmonyPatch(typeof(TNH_UIManager), "Start")]
        [HarmonyPrefix]
        public static void Start_InitTNH(TNH_UIManager __instance)
        {
            TNHFrameworkLogger.Log("Start method of TNH_UIManager just got called!", TNHFrameworkLogger.LogType.General);

            Text magazineCacheText = CreateMagazineCacheText(__instance);
            Text itemsText = CreateItemsText(__instance);
            SelectedCharacter_Image = CreateCharacterImage(__instance);
            LastPlayedChar = GM.TNHOptions.LastPlayedCharUniversalID;

            // Perform first time setup of all files
            if (!TNHMenuInitializer.TNHInitialized)
            {
                TNH_LevelLoader sceneHotDog = Object.FindObjectOfType<TNH_LevelLoader>();

                if (!TNHMenuInitializer.MagazineCacheFailed)
                {
                    AnvilManager.Run(TNHMenuInitializer.InitializeTNHMenuAsync(TNHFramework.OutputFilePath, magazineCacheText, itemsText, sceneHotDog, __instance.CharDatabase, __instance, TNHFramework.BuildCharacterFiles.Value));
                }
                // If the magazine cache has previously failed, we shouldn't let the player continue
                else
                {
                    sceneHotDog?.gameObject.SetActive(false);
                    magazineCacheText.text = "FAILED! SEE LOG!";
                }
            }
            else
            {
                magazineCacheText.text = "CACHE BUILT";
            }
        }

        [HarmonyPatch(typeof(TNH_UIManager), "Start")]
        [HarmonyPostfix]
        public static void Start_InitTNHPost(TNH_UIManager __instance)
        {
            TNHFrameworkLogger.Log("Initialize TNH UI", TNHFrameworkLogger.LogType.General);

            __instance.SelectedCharacter_Title.text = "";
            __instance.SelectedCharacter_Description.text = "Loading... Please Wait";

            // Anton pls fix. Typo "Encyption"
            Text blitzText = __instance.GO_ModeDescriptions[1].GetComponent<Text>();
            blitzText.text = blitzText.text.Replace("Encyption", "Encryption");

            for (int i = 0; i < __instance.LBL_CategoryName.Count; i++)
                __instance.LBL_CategoryName[i].gameObject.SetActive(false);

            for (int i = 0; i < __instance.LBL_CharacterName.Count; i++)
                __instance.LBL_CharacterName[i].gameObject.SetActive(false);

            RefreshTNHUI(__instance);
        }

        /// <summary>
        /// Creates the additional text above the character select screen, and returns that text component
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private static Text CreateMagazineCacheText(TNH_UIManager manager)
        {
            Text magazineCacheText = Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
            magazineCacheText.transform.localPosition = new Vector3(0, 590, 0);
            magazineCacheText.transform.localScale = new Vector3(2, 2, 2);
            magazineCacheText.horizontalOverflow = HorizontalWrapMode.Overflow;
            magazineCacheText.text = "EXAMPLE TEXT";

            return magazineCacheText;
        }

        private static Text CreateItemsText(TNH_UIManager manager)
        {
            Text itemsText = Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
            itemsText.transform.localPosition = new Vector3(-30, 670, 0);
            itemsText.transform.localScale = new Vector3(1, 1, 1);
            itemsText.text = "";
            itemsText.supportRichText = true;
            itemsText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            itemsText.alignment = TextAnchor.LowerLeft;
            itemsText.verticalOverflow = VerticalWrapMode.Overflow;
            itemsText.horizontalOverflow = HorizontalWrapMode.Overflow;

            return itemsText;
        }

        // Reinstate the character image to the right of the description.
        // Workshop characters get a holo, but vanilla and custom characters get nothing?
        private static Image CreateCharacterImage(TNH_UIManager manager)
        {
            Image charImage = Object.Instantiate(manager.IM_LevelImage.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Image>();
            charImage.SetNativeSize();
            charImage.transform.localPosition = new Vector3(322, 355, 0);
            charImage.transform.localScale = new Vector3(0.4734316f, 0.4734318f, 0.4734318f);
            charImage.rectTransform.sizeDelta = new Vector2(512f, 512f);
            charImage.preserveAspect = false;
            charImage.gameObject.SetActive(false);

            return charImage;
        }

        public static void RefreshTNHUI(TNH_UIManager instance)
        {
            if (!TNHMenuInitializer.TNHInitialized)
                return;

            TNHFrameworkLogger.Log("Refreshing TNH UI", TNHFrameworkLogger.LogType.General);

            //instance.InitCharacterCategories();
            miInitCharacterCategories.Invoke(instance, []);
            miConfigureButtonStateFromOptions.Invoke(instance, []);
        }

        // Anton pls fix. Save current level
        [HarmonyPatch(typeof(TNH_UIManager), "UpdateLevelSelectUi")]
        [HarmonyPostfix]
        public static void UpdateLevelSelectUi_SaveLevel(TNH_UIManager __instance)
        {
            GM.TNHOptions.SavedLevelID = __instance.CurrentLevel.LevelID;
            GM.TNHOptions.SaveToFile();
        }

        [HarmonyPatch(typeof(TNH_UIManager), "InitCharacterCategories")]
        [HarmonyPrefix]
        public static bool InitCharacterCategories_Replacement(TNH_UIManager __instance)
        {
            __instance.Categories.Clear();

            // Add the default categories first
            Dictionary<string, int> catDic = [];
            foreach (string category in __instance.CharDatabase.DefaultGroupNames)
            {
                catDic.Add(category, -1);
            }

            foreach (KeyValuePair<string, CharacterTemplate> character in LoadedTemplateManager.LoadedCharacterDict)
            {
                ObjectTemplates.CategoryInfo catData = character.Value.Custom.CategoryData;

                if (!string.IsNullOrEmpty(catData.Name) && !catDic.ContainsKey(catData.Name))
                    catDic.Add(catData.Name, 0);
            }

            foreach (string cat in catDic.OrderBy(o => o.Value).Select(o => o.Key))
            {
                // Add new category if it doesn't exist yet
                if (!__instance.Categories.Any(o => o.CategoryName == cat))
                {
                    __instance.Categories.Add(new TNH_UIManager.CharacterCategory()
                    {
                        CategoryName = cat,
                        Characters = []
                    });
                }
            }

            // Sort the custom characters by name
            Dictionary<int, List<CharacterTemplate>> sortedDic = [];
            List<string> ugcIds = [];

            foreach (KeyValuePair<string, CharacterTemplate> character in LoadedTemplateManager.LoadedCharacterDict)
            {
                int cat = __instance.Categories.FindIndex(o => o.CategoryName == character.Value.Custom.CategoryData.Name);

                if (cat == -1)
                    continue;

                // Add character to category
                if (!__instance.Categories[cat].Characters.Contains(character.Value.Def))
                {
                    if (character.Value.Custom.isCustom)
                    {
                        if (sortedDic.ContainsKey(cat))
                            sortedDic[cat].Add(character.Value);
                        else
                            sortedDic.Add(cat, [character.Value]);

                        // Add missing characters into UgcManager
                        ItemTreeNode<TNH_CharacterDef> node = UgcManager.GetRootNode<TNH_CharacterDef>();

                        if (!ugcIds.Contains(character.Value.Def.UgcId))
                        {
                            ugcIds.Add(character.Value.Def.UgcId);

                            //UgcManager.AddItemToTree<TNH_CharacterDef>(character.Value.Def, node);
                            MethodInfo miAddItemToTreeTNHChar = miAddItemToTree.MakeGenericMethod(typeof(TNH_CharacterDef));
                            miAddItemToTreeTNHChar.Invoke(null, [character.Value.Def, node]);
                        }
                    }
                    else
                    {
                        __instance.Categories[cat].Characters.Add(character.Value.Def);
                    }
                }
            }

            // Sort the custom characters before adding them
            foreach (KeyValuePair<int, List<CharacterTemplate>> character in sortedDic)
            {
                __instance.Categories[character.Key].Characters.AddRange([.. character.Value.OrderBy(o => o.Def.DisplayName).Select(o => o.Def)]);
            }

            // Refresh categories and characters
            if (!UgcManager.TryGetItem(LastPlayedChar, out TNH_CharacterDef charDef))
                UgcManager.TryGetItem("h3vr:Generic_0_BeginnerBlake", out charDef);

            if (charDef != null)
            {
                //instance.SetCharacterCategoryFromCharacter(charDef);
                miSetCharacterCategoryFromCharacter.Invoke(__instance, [charDef]);
            }

            return false;
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetCharacterCategoryFromCharacter")]
        [HarmonyPostfix]
        public static void SetCharacterCategoryFromCharacter_UpdateChar(TNH_UIManager __instance, int ___m_selectedCategory, int ___m_selectedCharacter)
        {
            UpdateCurrentCharacter(__instance, ___m_selectedCategory, ___m_selectedCharacter);
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCharacterIndex")]
        [HarmonyPostfix]
        public static void SetSelectedCharacterIndex_UpdateChar(TNH_UIManager __instance, int ___m_selectedCategory, int ___m_selectedCharacter)
        {
            UpdateCurrentCharacter(__instance, ___m_selectedCategory, ___m_selectedCharacter);
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCategoryIndex")]
        [HarmonyPostfix]
        public static void SetSelectedCategoryIndex_UpdateChar(TNH_UIManager __instance, int ___m_selectedCategory, int ___m_selectedCharacter)
        {
            UpdateCurrentCharacter(__instance, ___m_selectedCategory, ___m_selectedCharacter);
        }

        public static void UpdateCurrentCharacter(TNH_UIManager manager, int selectedCategory, int selectedCharacter)
        {
            if (manager.Categories.Count > selectedCategory && manager.Categories[selectedCategory].Characters.Count > selectedCharacter)
            {
                TNH_CharacterDef charDef = manager.Categories[selectedCategory].Characters[selectedCharacter];

                if (TNHMenuInitializer.TNHInitialized && LoadedTemplateManager.LoadedCharacterDict.ContainsKey(charDef.UgcId))
                {
                    LoadedTemplateManager.CurrentCharacter = LoadedTemplateManager.LoadedCharacterDict[charDef.UgcId].Custom;

                    if (charDef.Picture != null)
                    {
                        SelectedCharacter_Image.sprite = charDef.Picture;
                        SelectedCharacter_Image.gameObject.SetActive(true);

                        manager.SelectedCharacter_Title.transform.localPosition = TitlePositionLeft;
                        manager.SelectedCharacter_Description.transform.localPosition = DescriptionPositionLeft;
                    }
                    else
                    {
                        SelectedCharacter_Image.gameObject.SetActive(false);

                        manager.SelectedCharacter_Title.transform.localPosition = TitlePositionCenter;
                        manager.SelectedCharacter_Description.transform.localPosition = DescriptionPositionCenter;
                    }
                }
            }
        }

        // This goes up by a full page instead of one at a time
        [HarmonyPatch(typeof(TNH_UIManager), "PreviousCategoryButton")]
        [HarmonyPrefix]
        public static void PreviousCategoryButton_SkipPage(TNH_UIManager __instance, ref int ____charCategoryListOffset)
        {
            ____charCategoryListOffset -= __instance.LBL_CategoryName.Count;
            ____charCategoryListOffset = Mathf.Clamp(____charCategoryListOffset, 0, __instance.Categories.Count - __instance.LBL_CategoryName.Count) + 1;
        }

        // This goes down by a full page instead of one at a time
        [HarmonyPatch(typeof(TNH_UIManager), "NextCategoryButton")]
        [HarmonyPrefix]
        public static void NextCategoryButton_SkipPage(TNH_UIManager __instance, ref int ____charCategoryListOffset)
        {
            ____charCategoryListOffset += __instance.LBL_CategoryName.Count;
            ____charCategoryListOffset = Mathf.Clamp(____charCategoryListOffset, 0, __instance.Categories.Count - __instance.LBL_CategoryName.Count) - 1;
        }

        // This goes up by a full page instead of one at a time
        [HarmonyPatch(typeof(TNH_UIManager), "PreviousCharacterButton")]
        [HarmonyPrefix]
        public static void PreviousCharacterButton_SkipPage(TNH_UIManager __instance, int ___m_selectedCategory, ref int ____charListOffset)
        {
            ____charListOffset -= __instance.LBL_CharacterName.Count;
            ____charListOffset = Mathf.Clamp(____charListOffset, 0, __instance.Categories[___m_selectedCategory].Characters.Count - __instance.LBL_CharacterName.Count) + 1;
        }

        // This goes down by a full page instead of one at a time
        [HarmonyPatch(typeof(TNH_UIManager), "NextCharacterButton")]
        [HarmonyPrefix]
        public static void NextCharacterButton_SkipPage(TNH_UIManager __instance, int ___m_selectedCategory, ref int ____charListOffset)
        {
            ____charListOffset += __instance.LBL_CharacterName.Count;
            ____charListOffset = Mathf.Clamp(____charListOffset, 0, __instance.Categories[___m_selectedCategory].Characters.Count - __instance.LBL_CharacterName.Count) - 1;
        }

        [HarmonyPatch(typeof(TNH_LevelLoader), "ValidateCharacter")]
        [HarmonyPostfix]
        public static void ValidateCharacter_Skip(ref bool __result)
        {
            // Allow error messages to print, but make custom characters always pass validation
            // We don't need YOUR validation
            if (LoadedTemplateManager.CurrentCharacter.isCustom)
                __result = true;
        }
    }
}
