using FistVR;
using FistVR.Ugc;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.Events;
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

        public static GameObject OptionsPanel;
        public static Text OptionName;
        public static Text OptionDescription;
        public static OptionsPanel_ButtonSet OBS_UpgradeMags;
        public static OptionsPanel_ButtonSet OBS_InjectBackpacks;
        public static OptionsPanel_ButtonSet OBS_UnlimitedTokens;
        public static OptionsPanel_ButtonSet OBS_DropVibrate;
        public static OptionsPanel_ButtonSet OBS_EncrRegenerative;
        public static OptionsPanel_ButtonSet OBS_EncrCascading;
        public static OptionsPanel_ButtonSet OBS_EncrOrthogonal;
        public static OptionsPanel_ButtonSet OBS_ConstructBlister;
        public static OptionsPanel_ButtonSet OBS_ConstructFloater;
        public static OptionsPanel_ButtonSet OBS_ConstructIris;
        public static OptionsPanel_ButtonSet OBS_ConstructSentinel;

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
            OptionsPanel = CreateOptionsPanel(__instance);
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

        private static GameObject CreateOptionsPanel(TNH_UIManager manager)
        {
            // Move the character holo over so we have space
            // Original position is (-2.415, 0.0, 6.6)
            Transform holo = manager.CharacterDisplay.transform;
            holo.position = new Vector3(-1.8f, 0f, 6.95f);

            GameObject panelGO = Object.Instantiate(manager.OBS_GameMode.transform.parent.gameObject, manager.OBS_GameMode.transform.parent.parent);
            panelGO.name = "TNHF_OptionsPanel";
            Transform panel = panelGO.transform;
            panel.position = new Vector3(-2.4396f, 1.046f, 6.331f);  //-2.442f
            panel.eulerAngles = new Vector3(0, 270, 0);

            // Delete everything we don't need, and get references to the Text objects we do need
            foreach (Transform child in panel)
            {
                if (child.name.StartsWith("Option_") || child.name == "Top (2)")
                {
                    Object.Destroy(child.gameObject);
                }
                else if (child.name == "GameModeDescriptions")
                {
                    foreach (Transform grandchild in child)
                    {
                        if (grandchild.name == "ModeName")
                        {
                            OptionName = grandchild.GetComponent<Text>();
                            OptionName.text = "Options";
                            OptionName.rectTransform.sizeDelta = new Vector2(1000, 40);
                        }
                        else if (grandchild.name == "ModeDescription")
                        {
                            OptionDescription = grandchild.GetComponent<Text>();
                            OptionDescription.text = "Click on an option for a description.";
                        }
                        else
                        {
                            Object.Destroy(grandchild.gameObject);
                        }
                    }
                }
            }

            // Create the backboard
            GameObject backboard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backboard.name = "TNHF_Backboard";
            backboard.transform.position = new Vector3(panel.transform.position.x - 0.055f, panel.transform.position.y - 0.025f, panel.transform.position.z);
            backboard.transform.rotation = panel.transform.rotation;
            backboard.transform.localScale = new Vector3(2, 2.05f, 0.09f);
            backboard.GetComponent<Renderer>().material.color = Color.black;

            // Set the heading
            Transform top = panel.Find("Top (1)");
            Text topText = top.GetComponent<Text>();
            topText.text = "TNHFramework Options";
            topText.transform.localPosition = new Vector3(0, 500, 0);

            // Make new headings
            OBS_UpgradeMags = new();
            GameObject upgradeMagsGO = AddText(panel, top, -350, 350, "Always Mag Upgrader:", TextAnchor.LowerRight);
            GameObject upgradeMagsEnabledGO = AddButton(manager, panel, -100, 350, "Enabled", OBS_UpgradeMags, () => SetAlwaysMagUpgrader(true));
            GameObject upgradeMagsDisabledGO = AddButton(manager, panel, 100, 350, "Disabled", OBS_UpgradeMags, () => SetAlwaysMagUpgrader(false));
            OBS_UpgradeMags.SetSelectedButton(TNHFramework.AlwaysMagUpgrader.Value ? 0 : 1);

            OBS_InjectBackpacks = new();
            GameObject injectBackpacksGO = AddText(panel, top, -350, 300, "Inject Mod Backpacks:", TextAnchor.LowerRight);
            GameObject injectBackpacksEnabledGO = AddButton(manager, panel, -100, 300, "Enabled", OBS_InjectBackpacks, () => SetInjectModBackpacks(true));
            GameObject injectBackpacksDisabledGO = AddButton(manager, panel, 100, 300, "Disabled", OBS_InjectBackpacks, () => SetInjectModBackpacks(false));
            OBS_InjectBackpacks.SetSelectedButton(TNHFramework.InjectModBackpacks.Value ? 0 : 1);

            OBS_DropVibrate = new();
            GameObject dropVibrateGO = AddText(panel, top, -350, 250, "Vibrate on Loot Drop:", TextAnchor.LowerRight);
            GameObject dropVibrateEnabledGO = AddButton(manager, panel, -100, 250, "Enabled", OBS_DropVibrate, () => SetVibrateOnLootDrop(true));
            GameObject dropVibrateDisabledGO = AddButton(manager, panel, 100, 250, "Disabled", OBS_DropVibrate, () => SetVibrateOnLootDrop(false));
            OBS_DropVibrate.SetSelectedButton(TNHFramework.SosigItemDropVibrate.Value ? 0 : 1);

            OBS_UnlimitedTokens = new();
            GameObject unlimitedTokensGO = AddText(panel, top, -350, 200, "Unlimited Tokens:", TextAnchor.LowerRight);
            GameObject unlimitedTokensEnabledGO = AddButton(manager, panel, -100, 200, "Enabled", OBS_UnlimitedTokens, () => SetUnlimitedTokens(true));
            GameObject unlimitedTokensDisabledGO = AddButton(manager, panel, 100, 200, "Disabled", OBS_UnlimitedTokens, () => SetUnlimitedTokens(false));
            OBS_UnlimitedTokens.SetSelectedButton(TNHFramework.UnlimitedTokens.Value ? 0 : 1);

            GameObject encryptionsHeadingGO = AddText(panel, top, 0, 50, "Encryptions (Spawnlocking Mode)", TextAnchor.LowerCenter);

            OBS_EncrRegenerative = new();
            GameObject encrRegenerativeGO = AddText(panel, top, -350, 0, "Regenerative:", TextAnchor.LowerRight);
            GameObject encrRegenerativeNormalGO = AddButton(manager, panel, -100, 0, "Normal", OBS_EncrRegenerative, () => SetSimpleRegenerative(false));
            GameObject encrRegenerativeSimpleGO = AddButton(manager, panel, 100, 0, "Simple", OBS_EncrRegenerative, () => SetSimpleRegenerative(true));
            OBS_EncrRegenerative.SetSelectedButton(TNHFramework.SimpleRegenerative.Value ? 1 : 0);

            OBS_EncrCascading = new();
            GameObject encrCascadingGO = AddText(panel, top, -350, -50, "Cascading:", TextAnchor.LowerRight);
            GameObject encrCascadingNormalGO = AddButton(manager, panel, -100, -50, "Normal", OBS_EncrCascading, () => SetSimpleCascading(false));
            GameObject encrCascadingSimpleGO = AddButton(manager, panel, 100, -50, "Simple", OBS_EncrCascading, () => SetSimpleCascading(true));
            OBS_EncrCascading.SetSelectedButton(TNHFramework.SimpleCascading.Value ? 1 : 0);

            OBS_EncrOrthogonal = new();
            GameObject encrOrthogonalGO = AddText(panel, top, -350, -100, "Orthogonal:", TextAnchor.LowerRight);
            GameObject encrOrthogonalNormalGO = AddButton(manager, panel, -100, -100, "Normal", OBS_EncrOrthogonal, () => SetSimpleOrthogonal(false));
            GameObject encrOrthogonalSimpleGO = AddButton(manager, panel, 100, -100, "Simple", OBS_EncrOrthogonal, () => SetSimpleOrthogonal(true));
            OBS_EncrOrthogonal.SetSelectedButton(TNHFramework.SimpleOrthogonal.Value ? 1 : 0);

            GameObject constructsHeadingGO = AddText(panel, top, 0, -200, "Institution Constructs", TextAnchor.LowerCenter);

            OBS_ConstructBlister = new();
            GameObject constructsBlisterGO = AddText(panel, top, -350, -250, "Blister:", TextAnchor.LowerRight);
            GameObject constructsBlisterEnabledGO = AddButton(manager, panel, -100, -250, "Enabled", OBS_ConstructBlister, () => SetEnableBlister(true));
            GameObject constructsBlisterDisabledGO = AddButton(manager, panel, 100, -250, "Disabled", OBS_ConstructBlister, () => SetEnableBlister(false));
            OBS_ConstructBlister.SetSelectedButton(TNHFramework.EnableBlister.Value ? 0 : 1);

            OBS_ConstructFloater = new();
            GameObject constructsFloaterGO = AddText(panel, top, -350, -300, "Floater:", TextAnchor.LowerRight);
            GameObject constructsFloaterEnabledGO = AddButton(manager, panel, -100, -300, "Enabled", OBS_ConstructFloater, () => SetEnableFloater(true));
            GameObject constructsFloaterDisabledGO = AddButton(manager, panel, 100, -300, "Disabled", OBS_ConstructFloater, () => SetEnableFloater(false));
            OBS_ConstructFloater.SetSelectedButton(TNHFramework.EnableFloater.Value ? 0 : 1);

            OBS_ConstructIris = new();
            GameObject constructsIrisGO = AddText(panel, top, -350, -350, "Iris:", TextAnchor.LowerRight);
            GameObject constructsIrisEnabledGO = AddButton(manager, panel, -100, -350, "Enabled", OBS_ConstructIris, () => SetEnableIris(true));
            GameObject constructsIrisDisabledGO = AddButton(manager, panel, 100, -350, "Disabled", OBS_ConstructIris, () => SetEnableIris(false));
            OBS_ConstructIris.SetSelectedButton(TNHFramework.EnableIris.Value ? 0 : 1);

            OBS_ConstructSentinel = new();
            GameObject constructsSentinelGO = AddText(panel, top, -350, -400, "Sentinel:", TextAnchor.LowerRight);
            GameObject constructsSentinelEnabledGO = AddButton(manager, panel, -100, -400, "Enabled", OBS_ConstructSentinel, () => SetEnableSentinel(true));
            GameObject constructsSentinelDisabledGO = AddButton(manager, panel, 100, -400, "Disabled", OBS_ConstructSentinel, () => SetEnableSentinel(false));
            OBS_ConstructSentinel.SetSelectedButton(TNHFramework.EnableSentinel.Value ? 0 : 1);

            return panelGO;
        }

        // Make a copy of the source Text object, set its text, and position it
        private static GameObject AddText(Transform parent, Transform source, int x, int y, string text, TextAnchor alignment = TextAnchor.LowerLeft)
        {
            GameObject headingGO = Object.Instantiate(source.gameObject, parent);
            headingGO.transform.localPosition = new Vector3(x, y, 0);

            Text heading = headingGO.GetComponent<Text>();
            heading.text = text;

            if (alignment == TextAnchor.LowerRight)
                heading.rectTransform.sizeDelta = new Vector2(350, 40);
            else if (alignment == TextAnchor.LowerLeft)
                heading.rectTransform.sizeDelta = new Vector2(90, 40);

            heading.alignment = alignment;

            return headingGO;
        }

        private static GameObject AddButton(TNH_UIManager manager, Transform parent, int x, int y, string text, OptionsPanel_ButtonSet obs, UnityAction call)
        {
            GameObject buttonGO = AddText(parent, manager.LBL_CharacterName[0].transform, x, y, text);

            obs.UsesPointableButtons = true;
            obs.SelectedColor = manager.OBS_Character.SelectedColor;
            obs.UnSelectedColor = manager.OBS_Character.UnSelectedColor;
            obs.HighlightedColor = manager.OBS_Character.HighlightedColor;
            obs.ButtonsInSet ??= [];
            int index = obs.ButtonsInSet.Length;

            // Set the listener events for the button
            Button button = buttonGO.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => { obs.SetSelectedButton(index); });
            button.onClick.AddListener(call);

            // Resize the collider for the pointable button
            BoxCollider collider = button.GetComponent<BoxCollider>();
            collider.size = new Vector3(90, 40, 0);

            obs.ButtonsInSet = [.. obs.ButtonsInSet, buttonGO.GetComponent<FVRPointableButton>()];

            return buttonGO;
        }

        private static void SetAlwaysMagUpgrader(bool enabled)
        {
            OptionName.text = "Always Mag Upgrader";
            OptionDescription.text = "All Mag Duplicators become Mag Upgraders. In addition to duplication, they allow you to buy a new mag for your gun. (Default: Enabled)";
            TNHFramework.AlwaysMagUpgrader.Value = enabled;
        }

        private static void SetInjectModBackpacks(bool enabled)
        {
            OptionName.text = "Inject Mod Backpacks";
            OptionDescription.text = "Add mod backpacks to any pools that only contain the vanilla backpack. Does not add to starting equipment. (Default: Enabled)";
            TNHFramework.InjectModBackpacks.Value = enabled;
        }

        private static void SetVibrateOnLootDrop(bool enabled)
        {
            OptionName.text = "Vibrate on Loot Drop";
            OptionDescription.text = "Vibrate the controllers when a Sosig spawns an item on death. Doesn't apply to health drops. (Default: Enabled)";
            TNHFramework.SosigItemDropVibrate.Value = enabled;
        }

        private static void SetUnlimitedTokens(bool enabled)
        {
            OptionName.text = "Unlimited Tokens";
            OptionDescription.text = "Spawn with 999999 tokens. (Default: Disabled)";
            TNHFramework.UnlimitedTokens.Value = enabled;
        }

        private static void SetSimpleRegenerative(bool enabled)
        {
            OptionName.text = "Simple Regenerative";
            OptionDescription.text = "Make Regenerative encryption easier in Spawnlocking mode.\nIt is 3x3 instead of 5x5. (Default: Normal)";
            TNHFramework.SimpleRegenerative.Value = enabled;
        }

        private static void SetSimpleCascading(bool enabled)
        {
            OptionName.text = "Simple Cascading";
            OptionDescription.text = "Make Cascading encryption easier in Spawnlocking mode.\nIt splits into 3 blocks instead of 6. (Default: Normal)";
            TNHFramework.SimpleCascading.Value = enabled;
        }

        private static void SetSimpleOrthogonal(bool enabled)
        {
            OptionName.text = "Simple Orthogonal";
            OptionDescription.text = "Make Orthogonal encryption easier in Spawnlocking mode.\nIt has 1 target per face instead of 3. (Default: Normal)";
            TNHFramework.SimpleOrthogonal.Value = enabled;
        }

        private static void SetEnableBlister(bool enabled)
        {
            OptionName.text = "Institution Construct: Blister";
            OptionDescription.text = "Allows you to disable the Blister construct on Institution.\nRed lasers that scan and trigger alerts. (Default: Enabled)";
            TNHFramework.EnableBlister.Value = enabled;
        }

        private static void SetEnableFloater(bool enabled)
        {
            OptionName.text = "Institution Construct: Floater";
            OptionDescription.text = "Allows you to disable the Floater construct on Institution.\nFloating mines that follow you. (Default: Enabled)";
            TNHFramework.EnableFloater.Value = enabled;
        }

        private static void SetEnableIris(bool enabled)
        {
            OptionName.text = "Institution Construct: Iris";
            OptionDescription.text = "Allows you to disable the Iris construct on Institution.\nFloating rings that fire a beam. (Default: Enabled)";
            TNHFramework.EnableIris.Value = enabled;
        }

        private static void SetEnableSentinel(bool enabled)
        {
            OptionName.text = "Institution Construct: Sentinel";
            OptionDescription.text = "Allows you to disable the Sentinel construct on Institution.\nLarge monoliths that scan and trigger alerts. (Default: Enabled)";
            TNHFramework.EnableSentinel.Value = enabled;
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
