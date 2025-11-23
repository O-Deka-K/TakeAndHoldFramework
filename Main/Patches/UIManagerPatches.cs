using FistVR;
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
        private static readonly MethodInfo miPlayButtonSound = typeof(TNH_UIManager).GetMethod("PlayButtonSound", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetCharacter = typeof(TNH_UIManager).GetMethod("SetCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiSelectedCategory = typeof(TNH_UIManager).GetField("m_selectedCategory", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSelectedCharacter = typeof(TNH_UIManager).GetField("m_selectedCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static OptionsPanel_ButtonSet OBS_Character = null;
        private static int PageCat = 0;
        private static int PageChar = 0;
        private static int LastPlayedChar;

        // Performs initial setup of the TNH Scene when loaded
        [HarmonyPatch(typeof(TNH_UIManager), "Start")]
        [HarmonyPrefix]
        public static void Start_InitTNH(TNH_UIManager __instance)
        {
            TNHFrameworkLogger.Log("Start method of TNH_UIManager just got called!", TNHFrameworkLogger.LogType.General);

            Text magazineCacheText = CreateMagazineCacheText(__instance);
            Text itemsText = CreateItemsText(__instance);
            ExpandCharacterUI(__instance);

            // Perform first time setup of all files
            if (!TNHMenuInitializer.TNHInitialized)
            {
                SceneLoader sceneHotDog = Object.FindObjectOfType<SceneLoader>();

                if (!TNHMenuInitializer.MagazineCacheFailed)
                {
                    AnvilManager.Run(TNHMenuInitializer.InitializeTNHMenuAsync(TNHFramework.OutputFilePath, magazineCacheText, itemsText, sceneHotDog, __instance.Categories, __instance.CharDatabase, __instance, TNHFramework.BuildCharacterFiles.Value));
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
            TNHFrameworkLogger.Log("Start_InitTNHPost", TNHFrameworkLogger.LogType.General);

            __instance.LBL_CategoryName[0].text = "Loading... Please Wait";
            __instance.LBL_CategoryName[0].gameObject.SetActive(true);

            for (int i = 1; i < __instance.LBL_CategoryName.Count; i++)
                __instance.LBL_CategoryName[i].gameObject.SetActive(false);

            RefreshTNHUI(__instance, __instance.Categories, __instance.CharDatabase);
        }

        /// <summary>
        /// Creates the additional text above the character select screen, and returns that text component
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private static Text CreateMagazineCacheText(TNH_UIManager manager)
        {
            Text magazineCacheText = Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
            magazineCacheText.transform.localPosition = new Vector3(0, 550, 0);
            magazineCacheText.transform.localScale = new Vector3(2, 2, 2);
            magazineCacheText.horizontalOverflow = HorizontalWrapMode.Overflow;
            magazineCacheText.text = "EXAMPLE TEXT";

            return magazineCacheText;
        }

        private static Text CreateItemsText(TNH_UIManager manager)
        {
            Text itemsText = Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
            itemsText.transform.localPosition = new Vector3(-30, 630, 0);
            itemsText.transform.localScale = new Vector3(1, 1, 1);
            itemsText.text = "";
            itemsText.supportRichText = true;
            itemsText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            itemsText.alignment = TextAnchor.LowerLeft;
            itemsText.verticalOverflow = VerticalWrapMode.Overflow;
            itemsText.horizontalOverflow = HorizontalWrapMode.Overflow;

            return itemsText;
        }

        /// <summary>
        /// Adds more space for characters to be displayed in the TNH menu
        /// </summary>
        /// <param name="instance"></param>
        private static void ExpandCharacterUI(TNH_UIManager instance)
        {
            LastPlayedChar = GM.TNHOptions.LastPlayedChar;
            OBS_Character = instance.LBL_CharacterName[0].transform.parent.GetComponent<OptionsPanel_ButtonSet>();
            List<FVRPointableButton> buttonListChar = [.. OBS_Character.ButtonsInSet];
            List<FVRPointableButton> buttonListCat = [.. instance.OBS_CharCategory.ButtonsInSet];

            // Add 3 more category slots and character slots
            for (int i = 0; i < 3; i++)
            {
                Text newLabelChar = Object.Instantiate(instance.LBL_CharacterName[1].gameObject, instance.LBL_CharacterName[1].transform.parent).GetComponent<Text>();

                instance.LBL_CharacterName.Add(newLabelChar);
                buttonListChar.Add(newLabelChar.gameObject.GetComponent<FVRPointableButton>());
                
                Text newLabelCat = Object.Instantiate(instance.LBL_CategoryName[1].gameObject, instance.LBL_CategoryName[1].transform.parent).GetComponent<Text>();

                instance.LBL_CategoryName.Add(newLabelCat);
                buttonListCat.Add(newLabelCat.gameObject.GetComponent<FVRPointableButton>());
            }

            OBS_Character.ButtonsInSet = [.. buttonListChar];
            instance.OBS_CharCategory.ButtonsInSet = [.. buttonListCat];

            // Adjust buttons to be tighter together
            float posXChar = instance.LBL_CharacterName[0].transform.localPosition.x;
            float posYChar = instance.LBL_CharacterName[0].transform.localPosition.y;

            for (int i = 0; i < instance.LBL_CharacterName.Count; i++)
            {
                instance.LBL_CharacterName[i].gameObject.SetActive(false);

                Button buttonChar = instance.LBL_CharacterName[i].gameObject.GetComponent<Button>();
                buttonChar.onClick = new Button.ButtonClickedEvent();

                int index = i;  // Loop optimization fix - do NOT delete
                buttonChar.onClick.AddListener(() => { OBS_Character.SetSelectedButton(index); });
                buttonChar.onClick.AddListener(() => { instance.SetSelectedCharacter(index); });

                instance.LBL_CharacterName[i].transform.localPosition = new Vector3(posXChar, posYChar, 0);
                posYChar -= 45f;
            }

            float posXCat = instance.LBL_CategoryName[0].transform.localPosition.x;
            float posYCat = instance.LBL_CategoryName[0].transform.localPosition.y;

            for (int j = 0; j < instance.LBL_CategoryName.Count; j++)
            {
                Button buttonCat = instance.LBL_CategoryName[j].gameObject.GetComponent<Button>();
                buttonCat.onClick = new Button.ButtonClickedEvent();

                int index2 = j;  // Loop optimization fix - do NOT delete
                buttonCat.onClick.AddListener(() => { instance.OBS_CharCategory.SetSelectedButton(index2); });
                buttonCat.onClick.AddListener(() => { instance.SetSelectedCategory(index2); });

                instance.LBL_CategoryName[j].transform.localPosition = new Vector3(posXCat, posYCat, 0);
                posYCat -= 45f;
            }
        }

        public static void RefreshTNHUI(TNH_UIManager instance, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase)
        {
            if (!TNHMenuInitializer.TNHInitialized)
                return;

            TNHFrameworkLogger.Log("Refreshing TNH UI", TNHFrameworkLogger.LogType.General);

            instance.LBL_CategoryName[0].text = "<<Previous<<";
            instance.LBL_CategoryName[0].gameObject.SetActive(true);

            instance.LBL_CategoryName[11].text = ">>Next>>";
            instance.LBL_CategoryName[11].gameObject.SetActive(true);

            instance.LBL_CharacterName[0].text = "<<Previous<<";
            instance.LBL_CharacterName[0].gameObject.SetActive(true);

            instance.LBL_CharacterName[11].text = ">>Next>>";
            instance.LBL_CharacterName[11].gameObject.SetActive(true);

            // Load all characters into the UI
            foreach (KeyValuePair<TNH_Char, CharacterTemplate> character in LoadedTemplateManager.LoadedCharacterDict)
            {
                // Add new category if it doesn't exist yet
                if (!Categories.Any(o => o.CategoryName == character.Value.Custom.CategoryData.Name))
                {
                    Categories.Insert(character.Value.Custom.CategoryData.Priority, new TNH_UIManager.CharacterCategory()
                    {
                        CategoryName = character.Value.Custom.CategoryData.Name,
                        Characters = []
                    });
                }

                // Add character to category
                if (!Categories[(int)character.Value.Def.Group].Characters.Contains(character.Key))
                {
                    Categories[(int)character.Value.Def.Group].Characters.Add(character.Key);
                    CharDatabase.Characters.Add(character.Value.Def);
                }
            }

            // Refresh categories and characters
            try
            {
                miSetCharacter.Invoke(instance, [(TNH_Char)LastPlayedChar]);
                SetCharacterCategoryFromCharacter(instance, (TNH_Char)GM.TNHOptions.LastPlayedChar);
            }
            catch
            {
                miSetCharacter.Invoke(instance, [TNH_Char.DD_BeginnerBlake]);
                SetCharacterCategoryFromCharacter(instance, TNH_Char.DD_BeginnerBlake);
            }
        }

        public static void SetCharacterCategoryFromCharacter(TNH_UIManager instance, TNH_Char c)
        {
            for (int i = 0; i < instance.Categories.Count; i++)
            {
                for (int j = 0; j < instance.Categories[i].Characters.Count; j++)
                {
                    if (c == instance.Categories[i].Characters[j])
                    {
                        //m_selectedCategory = i;
                        //m_selectedCharacter = j;
                        fiSelectedCategory.SetValue(instance, i);
                        fiSelectedCharacter.SetValue(instance, j);

                        PageCat = i / 10;
                        PageChar = j / 10;

                        DisplayCategories(instance);
                        DisplayCharacters(instance, i);

                        instance.SetSelectedCategory((i % 10) + 1);
                        instance.OBS_CharCategory.SetSelectedButton((i % 10) + 1);

                        instance.SetSelectedCharacter((j % 10) + 1);
                        OBS_Character.SetSelectedButton((j % 10) + 1);

                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCategory")]
        [HarmonyPrefix]
        public static bool SetSelectedCategory_UIPatch(TNH_UIManager __instance, ref int ___m_selectedCategory, int cat)
        {
            if (!TNHMenuInitializer.TNHInitialized)
                return false;

            //TNHFrameworkLogger.Log("SetSelectedCategory: Category number " + cat + ", page " + PageCat + ", max is " + __instance.Categories.Count, TNHFrameworkLogger.LogType.TNH);
            int prevCat = ___m_selectedCategory;

            if (cat == 0)
            {
                if (PageCat > 0)
                {
                    PageCat--;
                    cat = 1;
                }
                else
                {
                    cat = prevCat % 10 + 1;
                }

                __instance.OBS_CharCategory.SetSelectedButton(cat);
            }
            else if (cat == 11)
            {
                if (PageCat < (__instance.Categories.Count - 1) / 10)
                {
                    PageCat++;
                    cat = 1;
                }
                else
                {
                    cat = prevCat % 10 + 1;
                }

                __instance.OBS_CharCategory.SetSelectedButton(cat);
            }

            //__instance.PlayButtonSound(0);
            miPlayButtonSound.Invoke(__instance, [0]);
            DisplayCategories(__instance);

            ___m_selectedCategory = cat - 1 + PageCat * 10;

            if (___m_selectedCategory != prevCat)
            {
                PageChar = 0;
                __instance.SetSelectedCharacter(1);
                OBS_Character.SetSelectedButton(1);
            }

            return false;
        }

        public static void DisplayCategories(TNH_UIManager instance)
        {
            // Adjust category labels according to PageCat
            for (int i = 0; i < instance.LBL_CategoryName.Count - 2; i++)
            {
                if (i + PageCat * 10 < instance.Categories.Count)
                {
                    instance.LBL_CategoryName[i + 1].gameObject.SetActive(true);
                    instance.LBL_CategoryName[i + 1].text = (i + 1 + PageCat * 10).ToString() + ". " + instance.Categories[i + PageCat * 10].CategoryName;
                }
                else
                {
                    instance.LBL_CategoryName[i + 1].gameObject.SetActive(false);
                }
            }
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCharacter")]
        [HarmonyPrefix]
        public static bool SetSelectedCharacter_UIPatch(TNH_UIManager __instance, int ___m_selectedCategory, ref int ___m_selectedCharacter, int i)
        {
            if (!TNHMenuInitializer.TNHInitialized)
                return false;

            //TNHFrameworkLogger.Log("SetSelectedCharacter: Character number " + i + ", page " + PageChar + ", max is " + __instance.Categories[___m_selectedCategory].Characters.Count, TNHFrameworkLogger.LogType.TNH);

            if (i == 0)
            {
                if (PageChar > 0)
                {
                    PageChar--;
                    i = 1;
                }
                else
                {
                    i = ___m_selectedCharacter % 10 + 1;
                }

                OBS_Character.SetSelectedButton(i);
            }
            else if (i == 11)
            {
                if (PageChar < (__instance.Categories[___m_selectedCategory].Characters.Count - 1) / 10)
                {
                    PageChar++;
                    i = 1;
                }
                else
                {
                    i = ___m_selectedCharacter % 10 + 1;
                }

                OBS_Character.SetSelectedButton(i);
            }

            //__instance.PlayButtonSound(1);
            miPlayButtonSound.Invoke(__instance, [1]);
            DisplayCharacters(__instance, ___m_selectedCategory);

            ___m_selectedCharacter = i - 1 + PageChar * 10;

            TNH_Char character = __instance.Categories[___m_selectedCategory].Characters[___m_selectedCharacter];
            if (LoadedTemplateManager.LoadedCharacterDict.ContainsKey(character))
                LoadedTemplateManager.CurrentCharacter = LoadedTemplateManager.LoadedCharacterDict[character].Custom;

            //__instance.SetCharacter(character);
            miSetCharacter.Invoke(__instance, [character]);

            return false;
        }

        public static void DisplayCharacters(TNH_UIManager instance, int selectedCategory)
        {
            // Adjust character labels according to PageChar
            for (int i = 0; i < instance.LBL_CharacterName.Count - 2; i++)
            {
                if (i + PageChar * 10 < instance.Categories[selectedCategory].Characters.Count)
                {
                    instance.LBL_CharacterName[i + 1].gameObject.SetActive(true);
                    TNH_CharacterDef def = instance.CharDatabase.GetDef(instance.Categories[selectedCategory].Characters[i + PageChar * 10]);
                    instance.LBL_CharacterName[i + 1].text = (i + 1 + PageChar * 10).ToString() + ". " + def.DisplayName;
                }
                else
                {
                    instance.LBL_CharacterName[i + 1].gameObject.SetActive(false);
                }
            }
        }
    }
}
