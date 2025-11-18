using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace TNHFramework.Patches
{
    static class UIManagerPatches
    {
        private static readonly MethodInfo miPlayButtonSound = typeof(TNH_UIManager).GetMethod("PlayButtonSound", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetCharacter = typeof(TNH_UIManager).GetMethod("SetCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        public static int OffsetCat = 0;
        public static int OffsetChar = 0;

        // Performs initial setup of the TNH Scene when loaded
        [HarmonyPatch(typeof(TNH_UIManager), "Start")]
        [HarmonyPrefix]
        public static bool InitTNH(TNH_UIManager __instance)
        {
            TNHFrameworkLogger.Log("Start method of TNH_UIManager just got called!", TNHFrameworkLogger.LogType.General);

            Text magazineCacheText = CreateMagazineCacheText(__instance);
            Text itemsText = CreateItemsText(__instance);
            ExpandCharacterUI(__instance);
            FixModAttachmentTags();

            // Perform first time setup of all files
            if (!TNHMenuInitializer.TNHInitialized)
            {
                SceneLoader sceneHotDog = UnityEngine.Object.FindObjectOfType<SceneLoader>();

                if (!TNHMenuInitializer.MagazineCacheFailed)
                {
                    AnvilManager.Run(TNHMenuInitializer.InitializeTNHMenuAsync(TNHFramework.OutputFilePath, magazineCacheText, itemsText, sceneHotDog, __instance.Categories, __instance.CharDatabase, __instance, TNHFramework.BuildCharacterFiles.Value));
                }

                // If the magazine cache has previously failed, we shouldn't let the player continue
                else
                {
                    sceneHotDog.gameObject.SetActive(false);
                    magazineCacheText.text = "FAILED! SEE LOG!";
                }

            }
            else
            {
                RefreshTNHUI(__instance, __instance.Categories, __instance.CharDatabase);
                magazineCacheText.text = "CACHE BUILT";
            }

            return true;
        }

        /// <summary>
        /// Creates the additional text above the character select screen, and returns that text component
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private static Text CreateMagazineCacheText(TNH_UIManager manager)
        {
            Text magazineCacheText = UnityEngine.Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
            magazineCacheText.transform.localPosition = new Vector3(0, 550, 0);
            magazineCacheText.transform.localScale = new Vector3(2, 2, 2);
            magazineCacheText.horizontalOverflow = HorizontalWrapMode.Overflow;
            magazineCacheText.text = "EXAMPLE TEXT";

            return magazineCacheText;
        }

        private static Text CreateItemsText(TNH_UIManager manager)
        {
            Text itemsText = UnityEngine.Object.Instantiate(manager.SelectedCharacter_Title.gameObject, manager.SelectedCharacter_Title.transform.parent).GetComponent<Text>();
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
        /// <param name="manager"></param>
        private static void ExpandCharacterUI(TNH_UIManager manager)
        {
            // Add additional character buttons
            OptionsPanel_ButtonSet buttonSet = manager.LBL_CharacterName[1].transform.parent.GetComponent<OptionsPanel_ButtonSet>();
            List<FVRPointableButton> buttonList = new(buttonSet.ButtonsInSet);
            for (int i = 0; i < 3; i++)
            {
                Text newCharacterLabel = UnityEngine.Object.Instantiate(manager.LBL_CharacterName[1].gameObject, manager.LBL_CharacterName[1].transform.parent).GetComponent<Text>();

                manager.LBL_CharacterName.Add(newCharacterLabel);
                buttonList.Add(newCharacterLabel.gameObject.GetComponent<FVRPointableButton>());
            }
            buttonSet.ButtonsInSet = buttonList.ToArray();

            // Adjust buttons to be tighter together
            float prevY = manager.LBL_CharacterName[0].transform.localPosition.y;
            for (int i = 0; i < manager.LBL_CharacterName.Count; i++)
            {
                Button button = manager.LBL_CharacterName[i].gameObject.GetComponent<Button>();

                button.onClick = new Button.ButtonClickedEvent();

                int whatTheFuck = i;
                button.onClick.AddListener(() => { buttonSet.SetSelectedButton(whatTheFuck); });
                button.onClick.AddListener(() => { manager.SetSelectedCharacter(whatTheFuck); });

                if (i > 0)
                {
                    prevY -= 35f;
                    manager.LBL_CharacterName[i].transform.localPosition = new Vector3(250, prevY, 0);
                }
            }
        }

        // This is pretty much a manual process of tagging mods that are incorrectly tagged or missing tags
        private static void FixModAttachmentTags()
        {
            List<FVRObject> attachments = new(ManagerSingleton<IM>.Instance.odicTagCategory[FVRObject.ObjectCategory.Attachment]);
            Regex regexSup = new(@"^Sup(SLX|SRD|TI|AA|DD|DeadAir|Gemtech|HUXWRX|KAC|Surefire|Hexagon|Silencer)");
            Regex regexMWORus = new(@"^DotRusSight(NPZPK1|SurplusOKP7D|AxionKobraEKPD)");
            Regex regexMWOMicroMount = new(@"^Dot(Geissele|Micro)(GBRS|Leap|Mounts|Offset|Shim|Short|SIGShort|SIGTall|Tall|Unity)");
            Regex regexMWOMicroSight = new(@"^DotMicro(Aimpoint|Holosun|SIGRomeo|Vortex)");

            foreach (FVRObject attachment in attachments)
            {
                if (attachment == null)
                    continue;

                if (attachment.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.None || attachment.TagAttachmentMount == FVRObject.OTagFirearmMount.None)
                {
                    // Meats ModulShotguns chokes
                    if (attachment.ItemID.StartsWith("AttSuppChoke") || attachment.ItemID.StartsWith("Choke"))
                    {
                        // Don't tag these as suppressors
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulShotguns barrels
                    else if (attachment.ItemID.ToLower().StartsWith("attsuppbar") || attachment.ItemID.StartsWith("AttSuppressorBarrel"))
                    {
                        // Don't tag these as suppressors
                        attachment.OSple = false;
                    }
                    // Meats ModulAK suppressors
                    else if (attachment.ItemID.StartsWith("AttSuppressor"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Suppression;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulAR muzzle brakes
                    else if (attachment.ItemID.StartsWith("AR15Muzzle") || attachment.ItemID == "AttSuppCookie")
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.RecoilMitigation;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulAR suppressors
                    else if (attachment.ItemID.StartsWith("AR15Sup") || attachment.ItemID.StartsWith("AttSupp"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Suppression;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulAR iron sights
                    else if (attachment.ItemID.Contains("IronSight") && (attachment.ItemID.Contains("Front") || attachment.ItemID.Contains("Rear")))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.IronSight;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;

                        if (attachment.ItemID.Contains("Front"))
                        {
                            // Spawn the rear sight together with the front sight
                            string rearSight = attachment.ItemID.Replace("Front", "Rear");

                            if (IM.OD.ContainsKey(rearSight))
                                attachment.RequiredSecondaryPieces.Add(IM.OD[rearSight]);
                        }
                        else if (attachment.ItemID.Contains("Rear"))
                        {
                            // Don't allow the rear sight to autopopulate into equipment pools
                            attachment.OSple = false;
                        }
                    }
                    // Meats ModulAR handle sights
                    else if (attachment.ItemID.StartsWith("IronSightGooseneck") || attachment.ItemID.EndsWith("HandleSight"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.IronSight;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModulAero muzzle brakes
                    else if (attachment.ItemID.StartsWith("Aero_Muzzle"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.RecoilMitigation;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulSIG muzzle brakes
                    else if (attachment.ItemID.StartsWith("MCXMB") || attachment.ItemID.StartsWith("MuzzleMPX"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.RecoilMitigation;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulSIG suppressors
                    else if (attachment.ItemID.StartsWith("MCXSRD") || attachment.ItemID.StartsWith("MCXSup"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Suppression;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulMCX2/ModulMCXSpear/ModulAR2 muzzle brakes
                    else if (attachment.ItemID.StartsWith("MuzzleBrake_"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.RecoilMitigation;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulMCX2/ModulMCXSpear/ModulAR2/ModulShotguns suppressors
                    else if (regexSup.IsMatch(attachment.ItemID))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Suppression;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulMCXSpear scopes
                    else if (attachment.ItemID.StartsWith("Scope_"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Magnification;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModulSIG/ModulMCX2/ModulSCAR iron sights
                    else if ((attachment.ItemID.StartsWith("MCX") || attachment.ItemID.StartsWith("SIG") || attachment.ItemID.StartsWith("SCAR")) && attachment.ItemID.Contains("Sight"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.IronSight;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;

                        if (attachment.ItemID.Contains("Front"))
                        {
                            // Spawn the rear sight together with the front sight
                            string rearSight = attachment.ItemID.Replace("Front", "Rear");

                            if (IM.OD.ContainsKey(rearSight))
                                attachment.RequiredSecondaryPieces.Add(IM.OD[rearSight]);
                        }
                        else if (attachment.ItemID.Contains("Rear"))
                        {
                            // Don't allow the rear sight to autopopulate into equipment pools
                            attachment.OSple = false;
                        }
                    }
                    // Meats ModulSCAR muzzle brakes
                    else if (attachment.ItemID.StartsWith("MuzzSCAR"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.RecoilMitigation;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModulSCAR underbarrel grenade launchers
                    else if (attachment.ItemID.StartsWith("EGLM"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.ProjectileWeapon;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats NGSW suppressors
                    else if (attachment.ItemID.StartsWith("NGSWSpearSup"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Suppression;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Muzzle;
                    }
                    // Meats ModernWarfighterOptics magnifiers
                    else if (attachment.ItemID.StartsWith("DotPicSight") && attachment.name.Contains("Magnifier"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Magnification;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics red dot
                    else if (attachment.ItemID.StartsWith("DotPicSight"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Reflex;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics Russian
                    else if (attachment.ItemID.StartsWith("DotRusSight"))
                    {
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Reflex;

                        // Only some of the Russian-made sights use the Russian mount; the rest are Picatinny
                        if (regexMWORus.IsMatch(attachment.ItemID))
                            attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Russian;
                        else
                            attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics ACRO mounts
                    else if (attachment.ItemID.StartsWith("DotACROMount"))
                    {
                        attachment.OSple = false;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Adapter;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics ACRO sights
                    else if (attachment.ItemID.StartsWith("DotACROSight"))
                    {
                        attachment.OSple = true;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Reflex;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                        attachment.RequiredSecondaryPieces ??= [];
                    }
                    // Meats ModernWarfighterOptics Micro mounts
                    else if (regexMWOMicroMount.IsMatch(attachment.ItemID))
                    {
                        attachment.OSple = false;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Adapter;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics Micro sights
                    else if (regexMWOMicroSight.IsMatch(attachment.ItemID))
                    {
                        attachment.OSple = true;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Reflex;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                        attachment.RequiredSecondaryPieces ??= [];
                    }
                    // Meats ModernWarfighterOptics MRD mounts
                    else if (attachment.ItemID.StartsWith("DotMRDMount"))
                    {
                        attachment.OSple = false;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Adapter;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                    // Meats ModernWarfighterOptics MRD sights
                    else if (attachment.ItemID.StartsWith("DotMRDSight"))
                    {
                        attachment.OSple = true;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Reflex;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                        attachment.RequiredSecondaryPieces ??= [];
                    }
                }

                if (attachment.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.Magnification && attachment.TagAttachmentMount == FVRObject.OTagFirearmMount.Picatinny)
                {
                    // FSCE ModernWarfighterRemake Optics
                    if (attachment.ItemID.StartsWith("FSCE") && (attachment.name.Contains("30mm Mount") || attachment.name.Contains("Adapter")))
                    {
                        attachment.OSple = false;
                        attachment.TagAttachmentFeature = FVRObject.OTagAttachmentFeature.Adapter;
                        attachment.TagAttachmentMount = FVRObject.OTagFirearmMount.Picatinny;
                    }
                }
                else if (attachment.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.Adapter && attachment.TagAttachmentMount == FVRObject.OTagFirearmMount.Picatinny)
                {
                    // FSCE 30mm mounts
                    if (attachment.ItemID.StartsWith("FSCE.LPVO") || attachment.name.Contains("30mm Mount"))
                    {
                        attachment.OSple = false;
                    }
                }
            }
        }

        public static void RefreshTNHUI(TNH_UIManager instance, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase)
        {
            TNHFrameworkLogger.Log("Refreshing TNH UI", TNHFrameworkLogger.LogType.General);

            // Load all characters into the UI
            foreach (KeyValuePair<TNH_Char, CharacterTemplate> character in LoadedTemplateManager.LoadedCharacterDict)
            {
                bool flag = false;
                foreach (TNH_UIManager.CharacterCategory category in Categories)
                {
                    if (category.CategoryName == character.Value.Custom.CategoryData.Name)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    Categories.Insert(character.Value.Custom.CategoryData.Priority, new TNH_UIManager.CharacterCategory()
                    {
                        CategoryName = character.Value.Custom.CategoryData.Name,
                        Characters = []
                    });
                }

                if (!Categories[(int)character.Value.Def.Group].Characters.Contains(character.Key))
                {
                    Categories[(int)character.Value.Def.Group].Characters.Add(character.Key);
                    CharDatabase.Characters.Add(character.Value.Def);
                }
            }

            // Update the UI
            Traverse instanceTraverse = Traverse.Create(instance);
            int selectedCategory = (int)instanceTraverse.Field("m_selectedCategory").GetValue();
            int selectedCharacter = (int)instanceTraverse.Field("m_selectedCharacter").GetValue();

            instanceTraverse.Method("SetSelectedCategory", selectedCategory).GetValue();
            instance.OBS_CharCategory.SetSelectedButton(selectedCharacter);
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCategory")]
        [HarmonyPrefix]
        public static bool SetCategoryUIPatch(TNH_UIManager __instance, ref int ___m_selectedCategory, int cat)
        {
            //TNHFrameworkLogger.Log("Category number " + cat + ", offset " + OffsetCat + ", max is " + __instance.Categories.Count, TNHFrameworkLogger.LogType.TNH);

            ___m_selectedCategory = cat + OffsetCat;
            OptionsPanel_ButtonSet buttonSet = __instance.LBL_CharacterName[0].transform.parent.GetComponent<OptionsPanel_ButtonSet>();

            // Probably better done with a switch statement and a single int, but i just wanna get this done first
            int adjust = 0;
            if (cat == OffsetCat)
                adjust = Math.Max(-2, 0 - OffsetCat);
            else if (cat == OffsetCat + 1)
                adjust = Math.Max(-1, 0 - OffsetCat);
            else if (cat == 6 && __instance.Categories.Count > 8)
                adjust = Math.Min(1, __instance.Categories.Count - OffsetCat - 8);
            else if (cat == 7 && __instance.Categories.Count > 8)
                adjust = Math.Min(2, __instance.Categories.Count - OffsetCat - 8);

            //TNHFrameworkLogger.Log("Adjust is " + adjust, TNHFrameworkLogger.LogType.TNH);

            OffsetCat += adjust;
            buttonSet.SetSelectedButton(buttonSet.selectedButton - adjust);

            //__instance.PlayButtonSound(0);
            miPlayButtonSound.Invoke(__instance, [0]);

            for (int i = 0; i < __instance.LBL_CategoryName.Count; i++)
            {
                //TNHFrameworkLogger.Log("Category iterator is " + i, TNHFrameworkLogger.LogType.TNH);

                if (i + OffsetCat < __instance.Categories.Count)
                {
                    __instance.LBL_CategoryName[i].gameObject.SetActive(true);
                    __instance.LBL_CategoryName[i].text = (i + OffsetCat + 1).ToString() + ". " + __instance.Categories[i + OffsetCat].CategoryName;
                }
                else
                {
                    __instance.LBL_CategoryName[i].gameObject.SetActive(false);
                }
            }

            OffsetChar = 0;
            __instance.SetSelectedCharacter(0);

            return false;
        }

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCharacter")]
        [HarmonyPrefix]
        public static bool SetCharacterUIPatch(TNH_UIManager __instance, int ___m_selectedCategory, int ___m_selectedCharacter, int i)
        {
            //TNHFrameworkLogger.Log("Character number " + i + ", offset " + OffsetChar + ", max is " + __instance.Categories[___m_selectedCategory].Characters.Count, TNHFrameworkLogger.LogType.TNH);

            ___m_selectedCharacter = i + OffsetChar;
            OptionsPanel_ButtonSet buttonSet = __instance.LBL_CharacterName[1].transform.parent.GetComponent<OptionsPanel_ButtonSet>();

            int adjust = 0;
            if (i == OffsetChar)
                adjust = Math.Max(-2, 0 - OffsetChar);
            else if (i == OffsetChar + 1)
                adjust = Math.Max(-1, 0 - OffsetChar);
            else if (i == 10 && __instance.Categories[___m_selectedCategory].Characters.Count > 12)
                adjust = Math.Min(1, __instance.Categories[___m_selectedCategory].Characters.Count - OffsetChar - 12);
            else if (i == 11 && __instance.Categories[___m_selectedCategory].Characters.Count > 12)
                adjust = Math.Min(2, __instance.Categories[___m_selectedCategory].Characters.Count - OffsetChar - 12);

            //TNHFrameworkLogger.Log("Adjust is " + adjust, TNHFrameworkLogger.LogType.TNH);

            OffsetChar += adjust;
            buttonSet.SetSelectedButton(buttonSet.selectedButton - adjust);

            TNH_Char character = __instance.Categories[___m_selectedCategory].Characters[___m_selectedCharacter];
            if (LoadedTemplateManager.LoadedCharacterDict.ContainsKey(character))
                LoadedTemplateManager.CurrentCharacter = LoadedTemplateManager.LoadedCharacterDict[character].Custom;

            //__instance.SetCharacter(character);
            //__instance.PlayButtonSound(1);
            miSetCharacter.Invoke(__instance, [character]);
            miPlayButtonSound.Invoke(__instance, [1]);

            for (int j = 0; j < __instance.LBL_CharacterName.Count; j++)
            {
                //TNHFrameworkLogger.Log("Char iterator is " + j, TNHFrameworkLogger.LogType.TNH);

                if (j + OffsetChar < __instance.Categories[___m_selectedCategory].Characters.Count)
                {
                    __instance.LBL_CharacterName[j].gameObject.SetActive(true);

                    TNH_CharacterDef def = __instance.CharDatabase.GetDef(__instance.Categories[___m_selectedCategory].Characters[j + OffsetChar]);
                    __instance.LBL_CharacterName[j].text = (j + OffsetChar + 1).ToString() + ". " + def.DisplayName;
                }
                else
                {
                    __instance.LBL_CharacterName[j].gameObject.SetActive(false);
                }
            }

            return false;
        }
    }
}
