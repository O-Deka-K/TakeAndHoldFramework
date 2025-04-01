using FistVR;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;
using static FistVR.VaultSystem;

namespace TNHFramework.Patches
{
    public class TNHPatches
    {
        private static readonly MethodInfo miPlayButtonSound = typeof(TNH_UIManager).GetMethod("PlayButtonSound", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetCharacter = typeof(TNH_UIManager).GetMethod("SetCharacter", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo miGenerateValidPatrol = typeof(TNH_Manager).GetMethod("GenerateValidPatrol", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miGenerateInitialTakeSentryPatrols = typeof(TNH_Manager).GetMethod("GenerateInitialTakeSentryPatrols", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiCurLevel = typeof(TNH_Manager).GetField("m_curLevel", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiLevel = typeof(TNH_Manager).GetField("m_level", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiWeaponCases = typeof(TNH_Manager).GetField("m_weaponCases", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo miCompletePhase = typeof(TNH_HoldPoint).GetMethod("CompletePhase", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miDeleteAllActiveWarpIns = typeof(TNH_HoldPoint).GetMethod("DeleteAllActiveWarpIns", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiValidSpawnPoints = typeof(TNH_HoldPoint).GetField("m_validSpawnPoints", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo fiIsConfigured = typeof(TNH_SupplyPoint).GetField("m_isconfigured", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiHasBeenVisited = typeof(TNH_SupplyPoint).GetField("m_hasBeenVisited", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveSosigs = typeof(TNH_SupplyPoint).GetField("m_activeSosigs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiActiveTurrets = typeof(TNH_SupplyPoint).GetField("m_activeTurrets", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSpawnBoxes = typeof(TNH_SupplyPoint).GetField("m_spawnBoxes", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo miUpdateLockUnlockButtonState = typeof(TNH_ObjectConstructor).GetMethod("UpdateLockUnlockButtonState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miSetState = typeof(TNH_ObjectConstructor).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miUpdateRerollButtonState = typeof(TNH_ObjectConstructor).GetMethod("UpdateRerollButtonState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiAllowEntry = typeof(TNH_ObjectConstructor).GetField("allowEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo fiSpawnedCase = typeof(TNH_ObjectConstructor).GetField("m_spawnedCase", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo miUpdateTokenDisplay = typeof(TNH_AmmoReloader2).GetMethod("UpdateTokenDisplay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miUpdateSafetyGeo = typeof(TubeFedShotgun).GetMethod("UpdateSafetyGeo", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo miToggleFireSelector = typeof(OpenBoltReceiver).GetMethod("ToggleFireSelector", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool InitTNH(TNH_Manager __instance, bool ___m_hasInit)
        {
            if (!___m_hasInit)
            {
                __instance.CharDB.Characters = TNHMenuInitializer.SavedCharacters;

                /*
                TNHFrameworkLogger.Log("Delayed Init!", TNHFrameworkLogger.LogType.General);
                TNHFrameworkLogger.Log("Last Played Character: " + ((TNH_Char)GM.TNHOptions.LastPlayedChar).ToString(), TNHFrameworkLogger.LogType.General);
                TNHFrameworkLogger.Log("Is CharDB null? " + (__instance.CharDB == null), TNHFrameworkLogger.LogType.General);
                TNHFrameworkLogger.Log("Is character null? " + (__instance.CharDB.GetDef((TNH_Char)GM.TNHOptions.LastPlayedChar) == null), TNHFrameworkLogger.LogType.General);

                TNH_CharacterDef C = __instance.CharDB.GetDef((TNH_Char)GM.TNHOptions.LastPlayedChar);
                TNHFrameworkLogger.Log("Is progression null? " + (C.Progressions == null), TNHFrameworkLogger.LogType.General);
                TNHFrameworkLogger.Log("Is endless progression null? " + (C.Progressions_Endless == null), TNHFrameworkLogger.LogType.General);
                */
            }

            return true;
        }

        #region Initializing TNH

        //////////////////////////////////
        //INITIALIZING TAKE AND HOLD SCENE
        //////////////////////////////////

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
                TNHMenuInitializer.RefreshTNHUI(__instance, __instance.Categories, __instance.CharDatabase);
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


        public static int OffsetCat = 0;
        public static int OffsetChar = 0;


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
            {
                adjust = Math.Max(-2, 0 - OffsetCat);
            }
            else if (cat == OffsetCat + 1)
            {
                adjust = Math.Max(-1, 0 - OffsetCat);
            }
            else if (cat == 6 && __instance.Categories.Count > 8)
            {
                adjust = Math.Min(1, __instance.Categories.Count - OffsetCat - 8);
            }
            else if (cat == 7 && __instance.Categories.Count > 8)
            {
                adjust = Math.Min(2, __instance.Categories.Count - OffsetCat - 8);
            }

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
        // ANTON
        // WHY IS IT INT I
        // INT INDEX
        // PLEASE.
        // I WILL SACRIFICE AN F2000 FOR YOU TO CHANGE THIS.
        public static bool SetCharacterUIPatch(TNH_UIManager __instance, int ___m_selectedCategory, int ___m_selectedCharacter, int i)
        {
            //TNHFrameworkLogger.Log("Character number " + i + ", offset " + OffsetChar + ", max is " + __instance.Categories[___m_selectedCategory].Characters.Count, TNHFrameworkLogger.LogType.TNH);

            ___m_selectedCharacter = i + OffsetChar;
            OptionsPanel_ButtonSet buttonSet = __instance.LBL_CharacterName[1].transform.parent.GetComponent<OptionsPanel_ButtonSet>();

            int adjust = 0;
            if (i == OffsetChar)
            {
                adjust = Math.Max(-2, 0 - OffsetChar);
            }
            else if (i == OffsetChar + 1)
            {
                adjust = Math.Max(-1, 0 - OffsetChar);
            }
            else if (i == 10 && __instance.Categories[___m_selectedCategory].Characters.Count > 12)
            {
                adjust = Math.Min(1, __instance.Categories[___m_selectedCategory].Characters.Count - OffsetChar - 12);
            }
            else if (i == 11 && __instance.Categories[___m_selectedCategory].Characters.Count > 12)
            {
                adjust = Math.Min(2, __instance.Categories[___m_selectedCategory].Characters.Count - OffsetChar - 12);
            }

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

            // now i don't know what to name this. fuck this, it's getting a j. you did this to me, anton.
            // ...who am i kidding anton isn't reading this-
            // ...either that or i'm probably dead.
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

        #endregion


        #region Supply and Take Points

        ///////////////////////////////////////////
        //PATCHES FOR SUPPLY POINTS AND TAKE POINTS
        ///////////////////////////////////////////

        [HarmonyPatch(typeof(TNH_SupplyPoint), "ConfigureAtBeginning")]
        [HarmonyPrefix]
        public static bool SpawnStartingEquipment(TNH_SupplyPoint __instance, ref List<GameObject> ___m_trackedObjects)
        {
            ___m_trackedObjects.Clear();
            if (__instance.M.ItemSpawnerMode == TNH_ItemSpawnerMode.On)
            {
                __instance.M.ItemSpawner.transform.position = __instance.SpawnPoints_Panels[0].position + Vector3.up * 0.8f;
                __instance.M.ItemSpawner.transform.rotation = __instance.SpawnPoints_Panels[0].rotation;
                __instance.M.ItemSpawner.SetActive(true);
            }

            for (int i = 0; i < __instance.SpawnPoint_Tables.Count; i++)
            {
                GameObject item = UnityEngine.Object.Instantiate(__instance.M.Prefab_MetalTable, __instance.SpawnPoint_Tables[i].position, __instance.SpawnPoint_Tables[i].rotation);
                ___m_trackedObjects.Add(item);
            }

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            if (character.PrimaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.PrimaryWeapon.PrimaryGroup ?? character.PrimaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();
                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = SpawnWeaponCase(__instance.M, selectedGroup.BespokeAttachmentChance, __instance.M.Prefab_WeaponCaseLarge, __instance.SpawnPoint_CaseLarge.position, __instance.SpawnPoint_CaseLarge.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    ___m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.SecondaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.SecondaryWeapon.PrimaryGroup ?? character.SecondaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();
                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = SpawnWeaponCase(__instance.M, selectedGroup.BespokeAttachmentChance, __instance.M.Prefab_WeaponCaseSmall, __instance.SpawnPoint_CaseSmall.position, __instance.SpawnPoint_CaseSmall.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    ___m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.TertiaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryWeapon.PrimaryGroup ?? character.TertiaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoint_Melee.position, __instance.SpawnPoint_Melee.rotation, o => 
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.PrimaryItem != null)
            {
                EquipmentGroup selectedGroup = character.PrimaryItem.PrimaryGroup ?? character.PrimaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoints_SmallItem[0].position, __instance.SpawnPoints_SmallItem[0].rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.SecondaryItem != null)
            {
                EquipmentGroup selectedGroup = character.SecondaryItem.PrimaryGroup ?? character.SecondaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    Transform spawnPoint = __instance.SpawnPoints_SmallItem.Count >= 2 ? __instance.SpawnPoints_SmallItem[1] : __instance.SpawnPoints_SmallItem[0];

                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, spawnPoint.position, spawnPoint.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.TertiaryItem != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryItem.PrimaryGroup ?? character.TertiaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    Transform spawnPoint = __instance.SpawnPoints_SmallItem.Count >= 3 ? __instance.SpawnPoints_SmallItem[2] : __instance.SpawnPoints_SmallItem[__instance.SpawnPoints_SmallItem.Count - 1];

                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, spawnPoint.position, spawnPoint.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.Shield != null)
            {
                EquipmentGroup selectedGroup = character.Shield.PrimaryGroup ?? character.Shield.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoint_Shield.position, __instance.SpawnPoint_Shield.rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (TNHFramework.UnlimitedTokens.Value) __instance.M.AddTokens(999999, false);

            return false;
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")]
        [HarmonyPrefix]
        public static bool SetPhase_Take_Replacement(TNH_Manager __instance, ref List<int> ___m_activeSupplyPointIndicies, ref TNH_Progression.Level ___m_curLevel,
            ref int ___m_lastHoldIndex, ref int ___m_curHoldIndex, ref TNH_HoldPoint ___m_curHoldPoint, TNH_PointSequence ___m_curPointSequence, int ___m_level)
        {
            __instance.ResetAlertedThisPhase();
            __instance.ResetPlayerTookDamageThisPhase();
            __instance.ResetHasGuardBeenKilledThatWasAltered();
            ___m_activeSupplyPointIndicies.Clear();

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            Level level = character.GetCurrentLevel(___m_curLevel);

            TNHFramework.SpawnedBossIndexes.Clear();
            TNHFramework.PatrolIndexPool.Clear();
            TNHFramework.PreventOutfitFunctionality = character.ForceDisableOutfitFunctionality;

            // Reset the TNH radar
            if (__instance.RadarMode == TNHModifier_RadarMode.Standard)
            {
                __instance.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = __instance.ReticleMask_Take;
            }
            else if (__instance.RadarMode == TNHModifier_RadarMode.Omnipresent)
            {
                __instance.TAHReticle.GetComponent<AIEntity>().LM_VisualOcclusionCheck = __instance.ReticleMask_Hold;
            }
            
            __instance.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Hold);
            __instance.TAHReticle.DeRegisterTrackedType(TAH_ReticleContact.ContactType.Supply);

            ___m_lastHoldIndex = ___m_curHoldIndex;

            // Get the next hold point and configure it
            ___m_curHoldIndex = GetNextHoldPointIndex(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
            ___m_curHoldPoint = __instance.HoldPoints[___m_curHoldIndex];
            ___m_curHoldPoint.ConfigureAsSystemNode(___m_curLevel.TakeChallenge, ___m_curLevel.HoldChallenge, ___m_curLevel.NumOverrideTokensForHold);
            __instance.TAHReticle.RegisterTrackedObject(___m_curHoldPoint.SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);

            // Shuffle panel types
            level.PossiblePanelTypes.Shuffle();
            TNHFrameworkLogger.Log("Panel types for this hold:", TNHFrameworkLogger.LogType.TNH);
            level.PossiblePanelTypes.ForEach(o => TNHFrameworkLogger.Log(o.ToString(), TNHFrameworkLogger.LogType.TNH));

            // Ensure ammo reloaders spawn first if this is limited ammo
            if (level.PossiblePanelTypes.Contains(PanelType.AmmoReloader) && __instance.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo)
            {
                level.PossiblePanelTypes.Remove(PanelType.AmmoReloader);
                level.PossiblePanelTypes.Insert(0, PanelType.AmmoReloader);
            }

            // For default characters, only a single supply point spawns in each level of Institution
            // We will allow multiple supply points for custom characters
            bool allowExplicitSingleSupplyPoints = !character.isCustom;

            // Now spawn and set up all of the supply points
            int panelIndex = 0;
            if (allowExplicitSingleSupplyPoints && ___m_curPointSequence.UsesExplicitSingleSupplyPoints && ___m_level < 5)
            {
                TNHFrameworkLogger.Log("Spawning explicit single supply point", TNHFrameworkLogger.LogType.TNH);

                int supplyPointIndex = ___m_curPointSequence.SupplyPoints[___m_level];
                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointIndex];
                //supplyPoint.Configure(___m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                ConfigureSupplyPoint(supplyPoint, level, ref panelIndex, 2, 3, true);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                ___m_activeSupplyPointIndicies.Add(supplyPointIndex);
            }
            else if (allowExplicitSingleSupplyPoints && ___m_curPointSequence.UsesExplicitSingleSupplyPoints && ___m_level >= 5)
            {
                List<int> supplyPointsIndexes = GetNextSupplyPointIndexes(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
                int supplyPointIndex = supplyPointsIndexes[0];

                TNHFrameworkLogger.Log($"Spawning explicit single supply point", TNHFrameworkLogger.LogType.TNH);

                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointIndex];
                //supplyPoint.Configure(___m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                ConfigureSupplyPoint(supplyPoint, level, ref panelIndex, 2, 3, true);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                ___m_activeSupplyPointIndicies.Add(supplyPointIndex);
            }
            else
            {
                // Generate all of the supply points for this level
                List<int> supplyPointsIndexes = GetNextSupplyPointIndexes(__instance, ___m_curPointSequence, ___m_level, ___m_curHoldIndex);
                supplyPointsIndexes.Shuffle<int>();

                int numSupplyPoints = UnityEngine.Random.Range(level.MinSupplyPoints, level.MaxSupplyPoints + 1);
                numSupplyPoints = Mathf.Clamp(numSupplyPoints, 0, supplyPointsIndexes.Count);

                TNHFrameworkLogger.Log($"Spawning {numSupplyPoints} supply points", TNHFrameworkLogger.LogType.TNH);

                bool spawnToken = true;
                for (int i = 0; i < numSupplyPoints; i++)
                {
                    TNHFrameworkLogger.Log($"Configuring supply point : {i}", TNHFrameworkLogger.LogType.TNH);

                    TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointsIndexes[i]];
                    ConfigureSupplyPoint(supplyPoint, level, ref panelIndex, 1, 2, spawnToken);
                    spawnToken = false;
                    TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                    supplyPoint.SetContact(contact);
                    ___m_activeSupplyPointIndicies.Add(supplyPointsIndexes[i]);
                }
            }

            // Spawn the initial patrol
            if (__instance.UsesClassicPatrolBehavior)
            {
                if (___m_level == 0)
                {
                    //__instance.GenerateValidPatrol(___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, ___m_curHoldIndex, true);
                    miGenerateValidPatrol.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, ___m_curHoldIndex, true]);
                }
                else
                {
                    //__instance.GenerateValidPatrol(___m_curLevel.PatrolChallenge, ___m_curHoldIndex, ___m_curHoldIndex, false);
                    miGenerateValidPatrol.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curHoldIndex, ___m_curHoldIndex, false]);
                }
            }
            else
            {
                if (___m_level == 0)
                {
                    //__instance.GenerateInitialTakeSentryPatrols(___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, -1, ___m_curHoldIndex, true);
                    miGenerateInitialTakeSentryPatrols.Invoke(__instance, [___m_curLevel.PatrolChallenge, ___m_curPointSequence.StartSupplyPointIndex, -1, ___m_curHoldIndex, true]);
                }
                else
                {
                    //__instance.GenerateInitialTakeSentryPatrols(___m_curLevel.PatrolChallenge, -1, ___m_curHoldIndex, ___m_curHoldIndex, false);
                    miGenerateInitialTakeSentryPatrols.Invoke(__instance, [___m_curLevel.PatrolChallenge, -1, ___m_curHoldIndex, ___m_curHoldIndex, false]);
                }
            }

            // Spawn the constructor panels
            for (int i = 0; i < __instance.ConstructSpawners.Count; i++)
            {
                if (___m_lastHoldIndex >= 0)
                {
                    TNH_HoldPoint holdPoint = __instance.HoldPoints[___m_lastHoldIndex];
                    
                    if (!holdPoint.ExcludeConstructVolumes.Contains(__instance.ConstructSpawners[i]))
                    {
                        __instance.ConstructSpawners[i].SpawnConstructs(___m_level);
                    }
                }
                else
                {
                    __instance.ConstructSpawners[i].SpawnConstructs(___m_level);
                }
            }

            if (__instance.BGAudioMode == TNH_BGAudioMode.Default)
            {
                __instance.FMODController.SwitchTo(0, 2f, false, false);
            }

            return false;
        }

        public static void ConfigureSupplyPoint(TNH_SupplyPoint supplyPoint, Level level, ref int panelIndex, int minBoxPiles, int maxBoxPiles, bool spawnToken)
        {

            supplyPoint.T = level.SupplyChallenge.GetTakeChallenge();
            //supplyPoint.m_isconfigured = true;
            fiIsConfigured.SetValue(supplyPoint, true);

            SpawnSupplyGroup(supplyPoint, level);

            SpawnSupplyTurrets(supplyPoint, level);

            int numConstructors = UnityEngine.Random.Range(level.MinConstructors, level.MaxConstructors + 1);

            SpawnSupplyConstructor(supplyPoint, numConstructors);

            SpawnSecondarySupplyPanel(supplyPoint, level, numConstructors, ref panelIndex);

            SpawnSupplyBoxes(supplyPoint, level, minBoxPiles, maxBoxPiles, spawnToken);

            //supplyPoint.m_hasBeenVisited = false;
            fiHasBeenVisited.SetValue(supplyPoint, false);
        }


        public static void SpawnSupplyConstructor(TNH_SupplyPoint point, int toSpawn)
        {
            TNHFrameworkLogger.Log("Spawning constructor panel", TNHFrameworkLogger.LogType.TNH);

            point.SpawnPoints_Panels.Shuffle();

            for (int i = 0; i < toSpawn && i < point.SpawnPoints_Panels.Count; i++)
            {
                GameObject constructor = point.M.SpawnObjectConstructor(point.SpawnPoints_Panels[i]);
                TNHFramework.SpawnedConstructors.Add(constructor);
            }
        }

        public static void SpawnSecondarySupplyPanel(TNH_SupplyPoint point, Level level, int startingPanelIndex, ref int panelIndex)
        {
            TNHFrameworkLogger.Log("Spawning secondary panels", TNHFrameworkLogger.LogType.TNH);

            bool isCustomCharacter = ((int)point.M.C.CharacterID >= 1000);
            int numPanels = UnityEngine.Random.Range(level.MinPanels, level.MaxPanels + 1);

            if (point.M.LevelName == "Institution" && !isCustomCharacter)
            {
                numPanels = 3;
            }

            for (int i = startingPanelIndex; i < startingPanelIndex + numPanels && i < point.SpawnPoints_Panels.Count && level.PossiblePanelTypes.Count > 0; i++)
            {
                TNHFrameworkLogger.Log("Panel index : " + i, TNHFrameworkLogger.LogType.TNH);

                // Go through the panels, and loop if we have gone too far 
                if (panelIndex >= level.PossiblePanelTypes.Count) panelIndex = 0;
                PanelType panelType = level.PossiblePanelTypes[panelIndex];
                panelIndex += 1;

                TNHFrameworkLogger.Log("Panel type selected : " + panelType, TNHFrameworkLogger.LogType.TNH);
                
                GameObject panel = null;

                if (panelType == PanelType.AmmoReloader)
                {
                    panel = point.M.SpawnAmmoReloader(point.SpawnPoints_Panels[i]);
                }

                else if (panelType == PanelType.MagDuplicator)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);

                    if (TNHFramework.AlwaysMagUpgrader.Value)
                        panel.AddComponent(typeof(MagazinePanel));
                }

                else if (panelType == PanelType.MagUpgrader || panelType == PanelType.MagPurchase)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(MagazinePanel));
                }

                else if (panelType == PanelType.Recycler)
                {
                    panel = point.M.SpawnGunRecycler(point.SpawnPoints_Panels[i]);
                }

                else if (panelType == PanelType.AmmoPurchase)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(AmmoPurchasePanel));
                }

                else if (panelType == PanelType.AddFullAuto)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FullAutoPanel));
                }

                else if (panelType == PanelType.FireRateUp || panelType == PanelType.FireRateDown)
                {
                    panel = point.M.SpawnMagDuplicator(point.SpawnPoints_Panels[i]);
                    panel.AddComponent(typeof(FireRatePanel));
                }

                else
                {
                    panel = point.M.SpawnAmmoReloader(point.SpawnPoints_Panels[i]);
                }

                // If we spawned a panel, add it to the global list
                if (panel != null)
                {
                    TNHFrameworkLogger.Log("Panel spawned successfully", TNHFrameworkLogger.LogType.TNH);
                    TNHFramework.SpawnedPanels.Add(panel);
                }
                else
                {
                    TNHFrameworkLogger.LogWarning("Failed to spawn secondary panel!");
                }
            }
        }


        public static void SpawnSupplyGroup(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Sosigs_Defense.Shuffle<Transform>();

            TNHFrameworkLogger.Log($"Spawning {level.SupplyChallenge.NumGuards} supply guards", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < level.SupplyChallenge.NumGuards && i < point.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = point.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[level.SupplyChallenge.GetTakeChallenge().GID];

                Sosig enemy = PatrolPatches.SpawnEnemy(template, transform, point.M, level.SupplyChallenge.IFFUsed, false, transform.position, true);

                //point.m_activeSosigs.Add(enemy);
                var activeSosigs = (List<Sosig>)fiActiveSosigs.GetValue(point);
                activeSosigs.Add(enemy);
            }
        }


        public static void SpawnSupplyTurrets(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Turrets.Shuffle<Transform>();
            FVRObject turretPrefab = point.M.GetTurretPrefab(level.SupplyChallenge.TurretType);

            for (int i = 0; i < level.SupplyChallenge.NumTurrets && i < point.SpawnPoints_Turrets.Count; i++)
            {
                Vector3 pos = point.SpawnPoints_Turrets[i].position + Vector3.up * 0.25f;
                AutoMeater turret = UnityEngine.Object.Instantiate<GameObject>(turretPrefab.GetGameObject(), pos, point.SpawnPoints_Turrets[i].rotation).GetComponent<AutoMeater>();

                //point.m_activeTurrets.Add(turret);
                var activeTurrets = (List<AutoMeater>)fiActiveTurrets.GetValue(point);
                activeTurrets.Add(turret);
            }

        }


        public static void SpawnSupplyBoxes(TNH_SupplyPoint point, Level level, int minBoxPiles, int maxBoxPiles, bool spawnToken)
        {
            point.SpawnPoints_Boxes.Shuffle();

            bool isCustomCharacter = ((int)point.M.C.CharacterID >= 1000);
            var spawnBoxes = (List<GameObject>)fiSpawnBoxes.GetValue(point);

            // Custom Character behavior:
            // - Every supply point has the same min and max number of boxes
            // - Every supply point has the same min and max number of tokens
            // - Every box that doesn't have a token has the same probability of having health
            if (isCustomCharacter)
            {
                int minTokens = level.MinTokensPerSupply;
                int maxTokens = level.MaxTokensPerSupply;

                int minBoxes = level.MinBoxesSpawned;
                int maxBoxes = level.MaxBoxesSpawned;
                int boxesToSpawn = UnityEngine.Random.Range(minBoxes, maxBoxes + 1);

                TNHFrameworkLogger.Log($"Going to spawn {boxesToSpawn} boxes at this point -- Min ({minBoxes}), Max ({maxBoxes})", TNHFrameworkLogger.LogType.TNH);

                for (int i = 0; i < boxesToSpawn; i++)
                {
                    Transform spawnTransform = point.SpawnPoints_Boxes[UnityEngine.Random.Range(0, point.SpawnPoints_Boxes.Count)];
                    Vector3 position = spawnTransform.position + Vector3.up * 0.1f + Vector3.right * UnityEngine.Random.Range(-0.5f, 0.5f) + Vector3.forward * UnityEngine.Random.Range(-0.5f, 0.5f);
                    Quaternion rotation = Quaternion.Slerp(spawnTransform.rotation, UnityEngine.Random.rotation, 0.1f);

                    GameObject box = UnityEngine.Object.Instantiate(point.M.Prefabs_ShatterableCrates[UnityEngine.Random.Range(0, point.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                    //point.m_spawnBoxes.Add(box);
                    spawnBoxes.Add(box);
                }

                int tokensSpawned = 0;

                // J: If you're asking "why is this an if/elseif check if it's a boolean value?", I... I don't know. I don't know why Anton does this. It's not a big deal but I don't know why.
                if (!point.M.UsesUberShatterableCrates)
                {
                    foreach (GameObject boxObj in spawnBoxes)
                    {

                        if (tokensSpawned < minTokens)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(point.M);
                            tokensSpawned += 1;
                        }

                        else if (tokensSpawned < maxTokens && UnityEngine.Random.value < level.BoxTokenChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(point.M);
                            tokensSpawned += 1;
                        }

                        else if (UnityEngine.Random.value < level.BoxHealthChance)
                        {
                            boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingHealth(point.M);
                        }
                    }
                }

                else if (point.M.UsesUberShatterableCrates)
                {
                    for (int k = 0; k < spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = spawnBoxes[k].GetComponent<UberShatterable>();
                        if (tokensSpawned < minTokens)
                        {
                            spawnBoxWithToken(point, boxComp);
                            tokensSpawned += 1;
                        }
                        else if (tokensSpawned < maxTokens && UnityEngine.Random.value < level.BoxTokenChance)
                        {
                            spawnBoxWithToken(point, boxComp);
                            tokensSpawned += 1;
                        }
                        else if (UnityEngine.Random.value < level.BoxHealthChance)
                        {
                            spawnBoxWithHealth(point, boxComp);
                        }
                        else
                        {
                            spawnBoxEmpty(point, boxComp);
                        }
                    }
                }
            }

            // Vanilla character behavior:
            // - Only one box per Take phase has a token (spawnToken is only true for one supply point)
            // - Hallways has 1-2 piles of 1-3 boxes per supply point; large maps have only 1 supply point with 2-3 piles of 1-3 boxes
            // - Each supply point has up to 3 health, and each of these has a different probability of spawning
            else
            {
                bool spawnHealth1 = (UnityEngine.Random.Range(0f, 1f) > 0.1f);
                bool spawnHealth2 = (UnityEngine.Random.Range(0f, 1f) > 0.4f);
                bool spawnHealth3 = (UnityEngine.Random.Range(0f, 1f) > 0.8f);

                point.SpawnPoints_Boxes.Shuffle<Transform>();

                int boxPiles = UnityEngine.Random.Range(minBoxPiles, maxBoxPiles + 1);
                if (boxPiles < 1)
                    return;

                for (int i = 0; i < boxPiles; i++)
                {
                    Transform transform = point.SpawnPoints_Boxes[i];

                    int boxesPerPile = UnityEngine.Random.Range(1, 3);
                    for (int j = 0; j < boxesPerPile; j++)
                    {
                        Vector3 position = transform.position + Vector3.up * 0.1f + Vector3.up * 0.85f * (float)j;
                        Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
                        onUnitSphere.y = 0f;
                        onUnitSphere.Normalize();
                        Quaternion rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(onUnitSphere, Vector3.up), 0.1f);

                        GameObject item = UnityEngine.Object.Instantiate<GameObject>(point.M.Prefabs_ShatterableCrates[UnityEngine.Random.Range(0, point.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                        //point.m_spawnBoxes.Add(item);
                        spawnBoxes.Add(item);
                    }
                }

                //point.m_spawnBoxes.Shuffle();
                spawnBoxes.Shuffle();
                //miShuffle.Invoke(spawnBoxes, []);

                if (!point.M.UsesUberShatterableCrates)
                {
                    int spawnIndex = 0;
                    TNH_ShatterableCrate boxComp;

                    if (spawnToken && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingToken(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth1 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth2 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        spawnIndex++;
                    }

                    if (spawnHealth3 && spawnBoxes.Count > spawnIndex)
                    {
                        boxComp = spawnBoxes[spawnIndex].GetComponent<TNH_ShatterableCrate>();
                        boxComp.SetHoldingHealth(point.M);
                        //spawnIndex++;
                    }
                }
                else
                {
                    for (int k = 0; k < spawnBoxes.Count; k++)
                    {
                        UberShatterable boxComp = spawnBoxes[k].GetComponent<UberShatterable>();

                        if (spawnToken)
                        {
                            spawnToken = false;
                            spawnBoxWithToken(point, boxComp);
                        }
                        else if (spawnHealth1)
                        {
                            spawnHealth1 = false;
                            spawnBoxWithHealth(point, boxComp);
                        }
                        else if (spawnHealth2)
                        {
                            spawnHealth2 = false;
                            spawnBoxWithHealth(point, boxComp);
                        }
                        else if (spawnHealth3)
                        {
                            spawnHealth3 = false;
                            spawnBoxWithHealth(point, boxComp);
                        }
                        else
                        {
                            spawnBoxEmpty(point, boxComp);
                        }
                    }
                }
            }
        }

        private static void spawnBoxWithToken(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Token);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
        }

        private static void spawnBoxWithHealth(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_HealthMinor);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
        }

        private static void spawnBoxEmpty(TNH_SupplyPoint point, UberShatterable boxComp)
        {
            boxComp.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Empty);
            boxComp.SpawnOnShatterPoints.Add(boxComp.transform);
            boxComp.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
        }


        public static int GetNextHoldPointIndex(TNH_Manager M, TNH_PointSequence pointSequence, int currLevel, int currHoldIndex)
        {
            int index;

            // If we haven't gone through all the hold points, we just select the next one we haven't been to
            if (currLevel < pointSequence.HoldPoints.Count)
            {
                index = pointSequence.HoldPoints[currLevel];
            }

            // If we have been to all the points, then we just select a random safe one
            else
            {
                List<int> pointIndexes = [];
                for (int i = 0; i < M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_HoldPoints.Count; i++)
                {
                    if (i != currHoldIndex && M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_HoldPoints[i])
                    {
                        pointIndexes.Add(i);
                    }
                }

                pointIndexes.Shuffle();
                index = pointIndexes[0];
            }

            return index;
        }


        public static List<int> GetNextSupplyPointIndexes(TNH_Manager M, TNH_PointSequence pointSequence, int currLevel, int currHoldIndex)
        {
            List<int> indexes = [];

            if (currLevel == 0)
            {
                for (int i = 0; i < M.SafePosMatrix.Entries_SupplyPoints[pointSequence.StartSupplyPointIndex].SafePositions_SupplyPoints.Count; i++)
                {
                    if (M.SafePosMatrix.Entries_SupplyPoints[pointSequence.StartSupplyPointIndex].SafePositions_SupplyPoints[i])
                    {
                        indexes.Add(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_SupplyPoints.Count; i++)
                {
                    if (M.SafePosMatrix.Entries_HoldPoints[currHoldIndex].SafePositions_SupplyPoints[i])
                    {
                        indexes.Add(i);
                    }
                }
            }

            indexes.Shuffle();
            return indexes;
        }


        [HarmonyPatch(typeof(TNH_SupplyPoint), "SpawnTakeEnemyGroup")]
        [HarmonyPrefix]
        public static bool SpawnTakeEnemyGroupReplacement(TNH_SupplyPoint __instance, ref int ___numSpawnBonus, ref List<Sosig> ___m_activeSosigs)
        {
            __instance.SpawnPoints_Sosigs_Defense.Shuffle();
            //__instance.SpawnPoints_Sosigs_Defense.Shuffle();

            int numGuards;
            if (LoadedTemplateManager.CurrentCharacter.isCustom)
            {
                numGuards = __instance.T.NumGuards;
            }
            else
            {
                numGuards = UnityEngine.Random.Range(__instance.T.NumGuards - 1, __instance.T.NumGuards + 1);
                numGuards += ___numSpawnBonus;
                numGuards = Mathf.Clamp(numGuards, 0, 5);
                ___numSpawnBonus++;
            }

            TNHFrameworkLogger.Log($"Spawning {__instance.T.NumGuards} supply guards via SpawnTakeEnemyGroup()", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < numGuards && i < __instance.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = __instance.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[__instance.T.GID];

                Sosig enemy = PatrolPatches.SpawnEnemy(template, transform, __instance.M, __instance.T.IFFUsed, false, transform.position, true);
                ___m_activeSosigs.Add(enemy);
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

                Sosig enemy = PatrolPatches.SpawnEnemy(template, transform, __instance.M, __instance.T.IFFUsed, false, transform.position, true);

                ___m_activeSosigs.Add(enemy);
                __instance.M.RegisterGuard(enemy);
            }

            return false;
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


        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnHoldEnemyGroup")]
        [HarmonyPrefix]
        private static bool SpawnHoldEnemyGroupStub()
        {
            // We've replaced all calls to SpawnHoldEnemyGroup() with our own, so stub this out
            TNHFrameworkLogger.LogWarning("SpawnHoldEnemyGroupStub() called! This should have been overridden!");
            return false;
        }


        #endregion


        #region During Hold Point

        ///////////////////////////////
        //PATCHES FOR DURING HOLD POINT
        ///////////////////////////////

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

                if (AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack.Count <= vectorSpawnPoint) break;

                // Set the sosig's target position
                if (currPhase.SwarmPlayer)
                {
                    targetVector = GM.CurrentPlayerBody.TorsoTransform.position;
                }
                else
                {
                    targetVector = SpawnPoints_Turrets[UnityEngine.Random.Range(0, SpawnPoints_Turrets.Count)].position;
                }

                Sosig enemy = PatrolPatches.SpawnEnemy(enemyTemplate, AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack[vectorSpawnPoint], M, curPhase.IFFUsed, true, targetVector, true);

                ActiveSosigs.Add(enemy);

                // At this point, the leader has been spawned, so always set enemy to be regulars
                enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.EnemyType.GetRandom<string>()]];
                sosigsSpawned += 1;

                vectorIndex += 1;
                if (vectorIndex >= numAttackVectors)
                {
                    vectorIndex = 0;
                    vectorSpawnPoint += 1;
                }


            }
            
            isFirstWave = false;
        }



        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawningRoutineUpdate")]
        [HarmonyPrefix]
        public static bool SpawningUpdateReplacement(TNH_HoldPoint __instance, ref float ___m_tickDownToNextGroupSpawn, ref List<Sosig> ___m_activeSosigs,
            TNH_HoldPoint.HoldState ___m_state, ref bool ___m_hasThrownNadesInWave, bool ___m_isFirstWave, int ___m_phaseIndex, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            ___m_tickDownToNextGroupSpawn -= Time.deltaTime;

            if (___m_activeSosigs.Count < 1 && ___m_state == TNH_HoldPoint.HoldState.Analyzing)
            {
                ___m_tickDownToNextGroupSpawn -= Time.deltaTime;
            }

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


        // Anton pls fix - When you click the unlock button, it should unlock the category on ALL spawned constructors, not just one
        [HarmonyPatch(typeof(TNH_ObjectConstructor), "ButtonClicked_Unlock")]
        [HarmonyPostfix]
        public static void ButtonClicked_UnlockOnAll()
        {
            foreach (GameObject constructorObject in TNHFramework.SpawnedConstructors)
            {
                //constructorObject?.GetComponent<TNH_ObjectConstructor>()?.UpdateLockUnlockButtonState(false);
                var constructor = constructorObject?.GetComponent<TNH_ObjectConstructor>();
                if (constructor != null)
                    miUpdateLockUnlockButtonState.Invoke(constructor, [false]);
            }
        }

        // Anton pls fix - Don't play line to advance to next node when completing last hold
        [HarmonyPatch(typeof(TNH_HoldPoint), "CompleteHold")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CompleteHold_LineFix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);

            for (int i = 0; i < code.Count - 3; i++)
            {
                // Search for "EnqueueLine(TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt)" and remove it
                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i + 1].opcode == OpCodes.Ldfld &&
                    code[i + 2].opcode == OpCodes.Ldc_I4_S && code[i + 2].operand.Equals((sbyte)90) &&
                    code[i + 3].opcode == OpCodes.Callvirt)
                {
                    code[i].opcode = OpCodes.Nop;
                    code[i + 1].opcode = OpCodes.Nop;
                    code[i + 2].opcode = OpCodes.Nop;
                    code[i + 3].opcode = OpCodes.Nop;
                    break;
                }
            }

            return code;
        }

        // Anton pls fix - Don't play line to advance to next node when completing last hold
        [HarmonyPatch(typeof(TNH_Manager), "HoldPointCompleted")]
        [HarmonyPostfix]
        public static void HoldPointCompleted_LineFix(TNH_Manager __instance, int ___m_level, int ___m_maxLevels)
        {
            // Play this only if it's NOT the last level
            if (___m_level < ___m_maxLevels)
            {
                __instance.EnqueueLine(TNH_VoiceLineID.AI_AdvanceToNextSystemNodeAndTakeIt);
            }
        }


        #endregion


        #region Constructor and Secondary Panels

        //////////////////////////////////////////////
        //PATCHES FOR CONSTRUCTOR AND SECONDARY PANELS
        //////////////////////////////////////////////


        /// <summary>
        /// This is a patch for using a character's global ammo blacklist in an ammo reloader
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(TNH_AmmoReloader), "GetClassFromType")]
        [HarmonyPrefix]
        public static bool AmmoReloaderGetAmmo(ref FireArmRoundClass __result, Dictionary<FireArmRoundType, FireArmRoundClass> ___m_decidedTypes,
            List<FVRObject.OTagEra> ___m_validEras, List<FVRObject.OTagSet> ___m_validSets, FireArmRoundType t)
        {
            if (!___m_decidedTypes.ContainsKey(t))
            {
                List<FireArmRoundClass> list = [];
                CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

                for (int i = 0; i < AM.SRoundDisplayDataDic[t].Classes.Length; i++)
                {
                    FVRObject objectID = AM.SRoundDisplayDataDic[t].Classes[i].ObjectID;
                    if (___m_validEras.Contains(objectID.TagEra) && ___m_validSets.Contains(objectID.TagSet))
                    {
                        if (character.GlobalAmmoBlacklist == null || !character.GlobalAmmoBlacklist.Contains(objectID.ItemID))
                        {
                            list.Add(AM.SRoundDisplayDataDic[t].Classes[i].Class);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    ___m_decidedTypes.Add(t, list[UnityEngine.Random.Range(0, list.Count)]);
                }
                else
                {
                    ___m_decidedTypes.Add(t, AM.GetRandomValidRoundClass(t));
                }
            }

            __result = ___m_decidedTypes[t];
            return false;
        }

        // This is a patch for using a character's global ammo blacklist in the new ammo reloader
        [HarmonyPatch(typeof(TNH_AmmoReloader2), "RefreshDisplayWithType")]
        [HarmonyPrefix]
        public static bool RefreshDisplayWithTypeBlacklist(TNH_AmmoReloader2 __instance, List<FireArmRoundType> ___m_detectedTypes, ref bool ___m_isConfirmingPurchase,
            ref bool ___hasDisplayedType, ref FireArmRoundType ___m_displayedType, ref List<FireArmRoundClass> ___m_displayedClasses, int ___m_selectedClass,
            int ___m_confirmingClass, List<FVRObject.OTagEra> ___m_validEras, List<FVRObject.OTagSet> ___m_validSets, FireArmRoundType t, int selectedEntry, bool confirmPurchase)
        {
            __instance.AmmoTypeField.text = AM.SRoundDisplayDataDic[t].DisplayName;
            
            if (___m_detectedTypes.Count > 1)
            {
                __instance.DisplayedTypeNext.enabled = true;
                __instance.DisplayedTypePrevious.enabled = true;
            }
            else
            {
                __instance.DisplayedTypeNext.enabled = false;
                __instance.DisplayedTypePrevious.enabled = false;
            }

            ___m_isConfirmingPurchase = confirmPurchase;
            ___hasDisplayedType = true;
            ___m_displayedType = t;
            ___m_displayedClasses.Clear();

            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;

            for (int i = 0; i < AM.SRoundDisplayDataDic[t].Classes.Length; i++)
            {
                FVRObject objectID = AM.SRoundDisplayDataDic[t].Classes[i].ObjectID;

                if (___m_validEras.Contains(objectID.TagEra) && ___m_validSets.Contains(objectID.TagSet))
                {
                    if (character.GlobalAmmoBlacklist == null || !character.GlobalAmmoBlacklist.Contains(objectID.ItemID))
                    {
                        ___m_displayedClasses.Add(AM.SRoundDisplayDataDic[t].Classes[i].Class);
                    }
                }
            }

            if (___m_displayedClasses.Count == 0)
            {
                ___m_displayedClasses.Add(AM.SRoundDisplayDataDic[t].Classes[0].Class);
            }

            if (!__instance.M.UnlockedClassesByType.ContainsKey(t))
            {
                List<FireArmRoundClass> list = new List<FireArmRoundClass>();
                list.Add(___m_displayedClasses[0]);
                __instance.M.UnlockedClassesByType.Add(t, list);
            }

            for (int j = 0; j < __instance.AmmoTokenFields.Count; j++)
            {
                if (j < ___m_displayedClasses.Count)
                {
                    __instance.AmmoTokenButtons[j].enabled = true;
                    int costByClass = AM.GetCostByClass(___m_displayedType, ___m_displayedClasses[j]);

                    if (__instance.M.UnlockedClassesByType[___m_displayedType].Contains(___m_displayedClasses[j]) || costByClass < 1)
                    {
                        if (___m_selectedClass == j)
                        {
                            __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Arrow;
                            __instance.AmmoTokenFields[j].text = "[Selected] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                        }
                        else
                        {
                            __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Select;
                            __instance.AmmoTokenFields[j].text = AM.STypeDic[t][___m_displayedClasses[j]].Name;
                        }
                    }
                    else if (___m_isConfirmingPurchase && j == ___m_confirmingClass)
                    {
                        __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Check;
                        __instance.AmmoTokenFields[j].text = "[Confirm Purchase?] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                    }
                    else
                    {
                        __instance.AmmoTokenButtons[j].sprite = __instance.Sprite_Token;
                        __instance.AmmoTokenFields[j].text = "[Buy (" + costByClass.ToString() + ")] " + AM.STypeDic[t][___m_displayedClasses[j]].Name;
                    }
                }
                else
                {
                    __instance.AmmoTokenButtons[j].enabled = false;
                    __instance.AmmoTokenFields[j].text = string.Empty;
                }
            }

            //__instance.UpdateTokenDisplay(__instance.M.GetNumTokens());
            miUpdateTokenDisplay.Invoke(__instance, [__instance.M.GetNumTokens()]);
            return false;
        }


        // Anton pls fix - Wrong sound plays when purchasing a clip at the new ammo reloader panel
        [HarmonyPatch(typeof(TNH_AmmoReloader2), "Button_SpawnClip")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Button_SpawnClip_AudioFix(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new(instructions);

            // Find the insertion index
            int insertIndex = -1;
            for (int i = 0; i < code.Count - 2; i++)
            {
                // Search for "if (obj.CompatibleClips.Count > 0)"
                if (code[i].opcode == OpCodes.Ldfld &&
                    code[i + 1].opcode == OpCodes.Ldc_I4_0 &&
                    code[i + 2].opcode == OpCodes.Ble)
                {
                    insertIndex = i + 3;
                    break;
                }
            }

            // If that failed, then just look for the first branch instruction
            if (insertIndex == -1)
            {
                for (int i = 0; i < code.Count; i++)
                {
                    // Search for ble
                    if (code[i].opcode == OpCodes.Ble)
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
            }

            // Set flag = true so that AudEvent_Spawn is played instead of AudEvent_Fail
            List<CodeInstruction> codeToInsert =
            [
                new(OpCodes.Ldc_I4_1),
                new(OpCodes.Stloc_0),
            ];

            // Insert the code
            if (insertIndex > -1)
            {
                code.InsertRange(insertIndex, codeToInsert);
            }

            return code;
        }

        [HarmonyPatch(typeof(TNH_ObjectConstructor), "GetPoolEntry")]
        [HarmonyPrefix]
        public static bool GetPoolEntryPatch(ref EquipmentPoolDef.PoolEntry __result, int level, EquipmentPoolDef poolDef, EquipmentPoolDef.PoolEntry.PoolEntryType t, EquipmentPoolDef.PoolEntry prior)
        {
            // Collect all pools that could spawn based on level and type, and sum up their rarities
            List<EquipmentPoolDef.PoolEntry> validPools = [];
            float summedRarity = 0;
            foreach (EquipmentPoolDef.PoolEntry entry in poolDef.Entries)
            {
                if (entry.Type == t && entry.MinLevelAppears <= level && entry.MaxLevelAppears >= level)
                {
                    validPools.Add(entry);
                    summedRarity += entry.Rarity;
                }
            }

            // If we didn't find a single pool, we cry about it
            if (validPools.Count == 0)
            {
                TNHFrameworkLogger.LogWarning("No valid pool could spawn at constructor for type (" + t + ")");
                __result = null;
                return false;
            }

            // Go back through and remove pools that have already spawned, unless there is only one entry left
            validPools.Shuffle();
            for (int i = validPools.Count - 1; i >= 0 && validPools.Count > 1; i--)
            {
                if (TNHFramework.SpawnedPools.Contains(validPools[i]))
                {
                    summedRarity -= validPools[i].Rarity;
                    validPools.RemoveAt(i);
                }
            }

            // Select a random value within the summed rarity, and select a pool based on that value
            float selectValue = UnityEngine.Random.Range(0, summedRarity);
            float currentSum = 0;
            foreach (EquipmentPoolDef.PoolEntry entry in validPools)
            {
                currentSum += entry.Rarity;
                if (selectValue <= currentSum)
                {
                    __result = entry;
                    TNHFramework.SpawnedPools.Add(entry);
                    return false;
                }
            }


            TNHFrameworkLogger.LogError("Somehow escaped pool entry rarity selection! This is not good!");
            __result = poolDef.Entries[0];
            return false;
        }

        [HarmonyPatch(typeof(TNH_ObjectConstructor), "ButtonClicked")]
        [HarmonyPriority(800)]
        [HarmonyPrefix]
        public static bool ButtonClickedReplacement(TNH_ObjectConstructor __instance, bool ___allowEntry, List<EquipmentPoolDef.PoolEntry> ___m_poolEntries,
            ref int ___m_selectedEntry, GameObject ___m_spawnedCase, ref int ___m_numTokensSelected, ref List<int> ___m_poolAddedCost, int i)
        {
            //__instance.UpdateRerollButtonState(false);
            miUpdateRerollButtonState.Invoke(__instance, [false]);

            if (!___allowEntry)
                return false;

            if (__instance.State == TNH_ObjectConstructor.ConstructorState.EntryList)
            {
                int cost = ___m_poolEntries[i].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[i];

                if (__instance.M.GetNumTokens() >= cost)
                {
                    //__instance.SetState(TNH_ObjectConstructor.ConstructorState.Confirm, i);
                    miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.Confirm, i]);
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Select, __instance.transform.position);
                }
                else
                {
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                }
            }
            else if (__instance.State == TNH_ObjectConstructor.ConstructorState.Confirm)
            {
                if (i == 1)
                {
                    //__instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
                    miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.EntryList, 0]);
                    ___m_selectedEntry = -1;
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Back, __instance.transform.position);
                }
                else if (i == 3)
                {
                    int cost = ___m_poolEntries[___m_selectedEntry].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[___m_selectedEntry];

                    if (__instance.M.GetNumTokens() >= cost)
                    {
                        if ((!___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInSmallCase && !___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInLargeCase) || ___m_spawnedCase == null)
                        {
                            AnvilManager.Run(SpawnObjectAtConstructor(___m_poolEntries[___m_selectedEntry], __instance));
                            ___m_numTokensSelected = 0;
                            __instance.M.SubtractTokens(cost);
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Spawn, __instance.transform.position);

                            if (__instance.M.C.UsesPurchasePriceIncrement)
                            {
                                ___m_poolAddedCost[___m_selectedEntry] += 1;
                            }

                            //__instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
                            miSetState.Invoke(__instance, [TNH_ObjectConstructor.ConstructorState.EntryList, 0]);
                            ___m_selectedEntry = -1;
                        }
                        else
                        {
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                        }
                    }
                    else
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Fail, __instance.transform.position);
                    }
                }
            }

            return false;
        }


        private static IEnumerator SpawnObjectAtConstructor(EquipmentPoolDef.PoolEntry entry, TNH_ObjectConstructor constructor)
        {
            TNHFrameworkLogger.Log("Spawning item at constructor", TNHFrameworkLogger.LogType.TNH);

            //constructor.allowEntry = false;
            fiAllowEntry.SetValue(constructor, false);
            EquipmentPool pool = LoadedTemplateManager.EquipmentPoolDictionary[entry];
            CustomCharacter character = LoadedTemplateManager.CurrentCharacter;
            List<EquipmentGroup> selectedGroups = pool.GetSpawnedEquipmentGroups();
            AnvilCallback<GameObject> gameObjectCallback;

            if (pool.SpawnsInLargeCase || pool.SpawnsInSmallCase)
            {
                TNHFrameworkLogger.Log("Item will spawn in a container", TNHFrameworkLogger.LogType.TNH);

                GameObject caseFab = constructor.M.Prefab_WeaponCaseLarge;
                if (pool.SpawnsInSmallCase) caseFab = constructor.M.Prefab_WeaponCaseSmall;

                FVRObject item = IM.OD[selectedGroups[0].GetObjects().GetRandom()];
                GameObject itemCase = SpawnWeaponCase(constructor.M, selectedGroups[0].BespokeAttachmentChance, caseFab, constructor.SpawnPoint_Case.position, constructor.SpawnPoint_Case.forward, item, selectedGroups[0].NumMagsSpawned, selectedGroups[0].NumRoundsSpawned, selectedGroups[0].MinAmmoCapacity, selectedGroups[0].MaxAmmoCapacity);

                //constructor.m_spawnedCase = itemCase;
                fiSpawnedCase.SetValue(constructor, itemCase);
                itemCase.GetComponent<TNH_WeaponCrate>().M = constructor.M;
            }

            else
            {
                TNHFrameworkLogger.Log("Item will spawn without a container", TNHFrameworkLogger.LogType.TNH);

                int mainSpawnCount = 0;
                int requiredSpawnCount = 0;
                int ammoSpawnCount = 0;
                int objectSpawnCount = 0;

                TNHFrameworkLogger.Log("Pool has " + selectedGroups.Count + " groups to spawn from", TNHFrameworkLogger.LogType.TNH);
                for (int groupIndex = 0; groupIndex < selectedGroups.Count; groupIndex++)
                {
                    EquipmentGroup group = selectedGroups[groupIndex];

                    TNHFrameworkLogger.Log("Group will spawn " + group.ItemsToSpawn + " items from it", TNHFrameworkLogger.LogType.TNH);
                    for (int itemIndex = 0; itemIndex < group.ItemsToSpawn; itemIndex++)
                    {
                        FVRObject mainObject;
                        VaultFile vaultFile = null;
                        SavedGunSerializable vaultFileLegacy = null;

                        Transform primarySpawn = constructor.SpawnPoint_Object;
                        Transform requiredSpawn = constructor.SpawnPoint_Object;
                        Transform ammoSpawn = constructor.SpawnPoint_Mag;
                        float objectDistancing = 0.2f;

                        if (group.IsCompatibleMagazine)
                        {
                            TNHFrameworkLogger.Log("Item will be a compatible magazine", TNHFrameworkLogger.LogType.TNH);
                            mainObject = FirearmUtils.GetAmmoContainerForEquipped(group.MinAmmoCapacity, group.MaxAmmoCapacity, character.GlobalObjectBlacklist, character.GetMagazineBlacklist());
                            if (mainObject == null)
                            {
                                TNHFrameworkLogger.LogWarning("Failed to spawn a compatible magazine!");
                                break;
                            }
                        }

                        else
                        {
                            string item = group.GetObjects().GetRandom();
                            TNHFrameworkLogger.Log("Item selected: " + item, TNHFrameworkLogger.LogType.TNH);

                            if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(item))
                            {
                                TNHFrameworkLogger.Log("Item is a vaulted gun", TNHFrameworkLogger.LogType.TNH);
                                vaultFile = LoadedTemplateManager.LoadedVaultFiles[item];
                                mainObject = IM.OD[vaultFile.Objects[0].Elements[0].ObjectID];
                            }

                            else if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(item))
                            {
                                TNHFrameworkLogger.Log("Item is a legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                                vaultFileLegacy = LoadedTemplateManager.LoadedLegacyVaultFiles[item];
                                mainObject = vaultFileLegacy.GetGunObject();
                            }

                            else
                            {
                                TNHFrameworkLogger.Log("Item is a normal object", TNHFrameworkLogger.LogType.TNH);
                                mainObject = IM.OD[item];
                            }
                        }

                        // Assign spawn points based on the type of item we are spawning
                        if (mainObject.Category == FVRObject.ObjectCategory.Firearm)
                        {
                            primarySpawn = constructor.SpawnPoints_GunsSize[Mathf.Clamp(mainObject.TagFirearmSize - FVRObject.OTagFirearmSize.Pocket, 0, constructor.SpawnPoints_GunsSize.Count - 1)];
                            requiredSpawn = constructor.SpawnPoint_Grenade;
                            mainSpawnCount += 1;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.Explosive || mainObject.Category == FVRObject.ObjectCategory.Thrown)
                        {
                            primarySpawn = constructor.SpawnPoint_Grenade;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.MeleeWeapon)
                        {
                            primarySpawn = constructor.SpawnPoint_Melee;
                        }
                        else if (mainObject.Category == FVRObject.ObjectCategory.Cartridge)
                        {
                            primarySpawn = constructor.SpawnPoint_Ammo;
                            objectDistancing = 0.05f;
                            mainSpawnCount += 1;
                        }

                        int level = (int)fiLevel.GetValue(constructor.M);
                        TNHFrameworkLogger.Log("Level: " + level, TNHFrameworkLogger.LogType.TNH);

                        // J: New vault files have a method for spawning them. Thank god. Or, y'know, thank Anton.
                        if (vaultFile != null)
                        {
                            VaultSystem.ReturnObjectListDelegate del = new((objs) => TrackVaultObjects(constructor.M, objs));
                            TNHFrameworkLogger.Log("Spawning vault gun", TNHFrameworkLogger.LogType.TNH);
                            SpawnVaultFile(vaultFile, primarySpawn, true, false, false, out _, Vector3.zero, del, false);
                        }
                        // If this is a vault file, we have to spawn it through a routine. Otherwise we just instantiate it
                        else if (vaultFileLegacy != null)
                        {
                            TNHFrameworkLogger.Log("Spawning legacy vaulted gun", TNHFrameworkLogger.LogType.TNH);
                            AnvilManager.Run(TNHFrameworkUtils.SpawnFirearm(vaultFileLegacy, primarySpawn.position, primarySpawn.rotation, constructor.M));
                            // SpawnFirearm adds the objects to the tracked objects list
                        }
                        else
                        {
                            TNHFrameworkLogger.Log("Spawning normal item", TNHFrameworkLogger.LogType.TNH);
                            gameObjectCallback = mainObject.GetGameObjectAsync();
                            yield return gameObjectCallback;

                            GameObject spawnedObject = UnityEngine.Object.Instantiate(mainObject.GetGameObject(), primarySpawn.position + Vector3.up * objectDistancing * mainSpawnCount, primarySpawn.rotation);
                            constructor.M.AddObjectToTrackedList(spawnedObject);
                            TNHFrameworkLogger.Log("Normal item spawned", TNHFrameworkLogger.LogType.TNH);
                        }

                        
                        // Spawn any required objects
                        if (mainObject.RequiredSecondaryPieces != null)
                        {
                            for (int j = 0; j < mainObject.RequiredSecondaryPieces.Count; j++)
                            {
                                if (mainObject.RequiredSecondaryPieces[j] == null)
                                {
                                    TNHFrameworkLogger.Log("Null required object! Skipping", TNHFrameworkLogger.LogType.TNH);
                                    continue;
                                }

                                FVRObject requiredObject = mainObject.RequiredSecondaryPieces[j];
                                gameObjectCallback = requiredObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning required secondary item ({requiredObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                GameObject requiredItem = UnityEngine.Object.Instantiate(requiredObject.GetGameObject(), requiredSpawn.position + -requiredSpawn.right * 0.2f * requiredSpawnCount + Vector3.up * 0.2f * j, requiredSpawn.rotation);
                                constructor.M.AddObjectToTrackedList(requiredItem);
                                requiredSpawnCount += 1;
                            }
                        }
                        

                        // Handle spawning for ammo objects if the main object has any
                        if (FirearmUtils.FVRObjectHasAmmoObject(mainObject))
                        {
                            Dictionary<string, MagazineBlacklistEntry> blacklist = character.GetMagazineBlacklist();
                            MagazineBlacklistEntry blacklistEntry = null;
                            if (blacklist.ContainsKey(mainObject.ItemID)) blacklistEntry = blacklist[mainObject.ItemID];

                            // Get lists of ammo objects for this firearm with filters and blacklists applied
                            List<FVRObject> compatibleMagazines = FirearmUtils.GetCompatibleMagazines(mainObject, group.MinAmmoCapacity, group.MaxAmmoCapacity, true, character.GlobalObjectBlacklist, blacklistEntry);
                            List<FVRObject> compatibleRounds = FirearmUtils.GetCompatibleRounds(mainObject, character.ValidAmmoEras, character.ValidAmmoSets, character.GlobalAmmoBlacklist, character.GlobalObjectBlacklist, blacklistEntry);
                            List<FVRObject> compatibleClips = mainObject.CompatibleClips;

                            // If we are supposed to spawn magazines and clips, perform special logic for that
                            if (group.SpawnMagAndClip && compatibleMagazines.Count > 0 && compatibleClips.Count > 0 && group.NumMagsSpawned > 0 && group.NumClipsSpawned > 0)
                            {
                                TNHFrameworkLogger.Log("Spawning with both magazine and clips", TNHFrameworkLogger.LogType.TNH);

                                FVRObject magazineObject = compatibleMagazines.GetRandom();
                                FVRObject clipObject = compatibleClips.GetRandom();
                                ammoSpawn = constructor.SpawnPoint_Mag;

                                gameObjectCallback = magazineObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning magazine ({magazineObject.ItemID})", TNHFrameworkLogger.LogType.TNH);
                                GameObject spawnedMag = UnityEngine.Object.Instantiate(magazineObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedMag);
                                ammoSpawnCount += 1;

                                gameObjectCallback = clipObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                TNHFrameworkLogger.Log($"Spawning clip ({clipObject.ItemID}), Count = {group.NumClipsSpawned}", TNHFrameworkLogger.LogType.TNH);
                                for (int i = 0; i < group.NumClipsSpawned; i++)
                                {
                                    GameObject spawnedClip = UnityEngine.Object.Instantiate(clipObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(spawnedClip);
                                    ammoSpawnCount += 1;
                                }
                            }
                            // Otherwise, perform normal logic for spawning ammo objects from current group
                            else
                            {
                                FVRObject ammoObject;
                                int numSpawned = 0;

                                if (compatibleMagazines.Count > 0 && group.NumMagsSpawned > 0)
                                {
                                    ammoObject = compatibleMagazines.GetRandom();
                                    numSpawned = group.NumMagsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (compatibleClips.Count > 0 && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = compatibleClips.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else if (mainObject.CompatibleSpeedLoaders != null && mainObject.CompatibleSpeedLoaders.Count > 0 && group.NumClipsSpawned > 0)
                                {
                                    ammoObject = mainObject.CompatibleSpeedLoaders.GetRandom();
                                    numSpawned = group.NumClipsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Mag;
                                }
                                else
                                {
                                    ammoObject = compatibleRounds.GetRandom();
                                    numSpawned = group.NumRoundsSpawned;
                                    ammoSpawn = constructor.SpawnPoint_Ammo;
                                }

                                TNHFrameworkLogger.Log($"Spawning ammo object normally ({ammoObject.ItemID}), Count = {numSpawned}", TNHFrameworkLogger.LogType.TNH);

                                gameObjectCallback = ammoObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                for (int i = 0; i < numSpawned; i++)
                                {
                                    GameObject spawned = UnityEngine.Object.Instantiate(ammoObject.GetGameObject(), ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    constructor.M.AddObjectToTrackedList(spawned);
                                    ammoSpawnCount += 1;
                                }
                            }
                        }

                        // If this object requires picatinny sights, we should try to spawn one
                        if (mainObject.RequiresPicatinnySight && character.RequireSightTable != null)
                        {
                            TNHFrameworkLogger.Log("Spawning required sights", TNHFrameworkLogger.LogType.TNH);

                            FVRObject sight = IM.OD[character.RequireSightTable.GetSpawnedEquipmentGroups().GetRandom().GetObjects().GetRandom()];
                            gameObjectCallback = sight.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject spawnedSight = UnityEngine.Object.Instantiate(sight.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                            constructor.M.AddObjectToTrackedList(spawnedSight);

                            TNHFrameworkLogger.Log($"Required sight spawned ({sight.ItemID})", TNHFrameworkLogger.LogType.TNH);

                            for (int j = 0; j < sight.RequiredSecondaryPieces.Count; j++)
                            {
                                FVRObject objectRequired = sight.RequiredSecondaryPieces[j];
                                gameObjectCallback = objectRequired.GetGameObjectAsync();
                                yield return gameObjectCallback;
                                GameObject spawnedRequired = UnityEngine.Object.Instantiate(objectRequired.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount + Vector3.up * 0.15f * j, constructor.SpawnPoint_Object.rotation);
                                constructor.M.AddObjectToTrackedList(spawnedRequired);
                                TNHFrameworkLogger.Log($"Required secondary item for sight spawned ({objectRequired.ItemID})", TNHFrameworkLogger.LogType.TNH);
                            }

                            objectSpawnCount += 1;
                        }

                        // If this object has bespoke attachments we'll try to spawn one
                        else if (mainObject.BespokeAttachments.Count > 0 && UnityEngine.Random.value < group.BespokeAttachmentChance)
                        {
                            TNHFrameworkLogger.Log("Spawning bespoke attachment", TNHFrameworkLogger.LogType.TNH);
                            FVRObject bespoke = mainObject.BespokeAttachments.GetRandom();
                            gameObjectCallback = bespoke.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject bespokeObject = UnityEngine.Object.Instantiate(bespoke.GetGameObject(), constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                            constructor.M.AddObjectToTrackedList(bespokeObject);
                            objectSpawnCount += 1;

                            TNHFrameworkLogger.Log($"Bespoke item spawned ({bespoke.ItemID})", TNHFrameworkLogger.LogType.TNH);
                        }
                    }
                }
            }

            //constructor.allowEntry = true;
            fiAllowEntry.SetValue(constructor, true);
            yield break;
        }

        /// <summary>
        /// Delegate for tracking all GameObjects created by a vault gun spawn
        /// </summary>
        /// <param name="objs"></param>
        private static void TrackVaultObjects(TNH_Manager M, List<FVRPhysicalObject> objs)
        {
            foreach (FVRPhysicalObject obj in objs)
            {
                if (obj != null)
                    M.AddObjectToTrackedList(obj.GameObject);
            }
        }

        public static GameObject SpawnWeaponCase(TNH_Manager M, float bespokeAttachmentChance, GameObject caseFab, Vector3 position, Vector3 forward,
            FVRObject weapon, int numMag, int numRound, int minAmmo, int maxAmmo, FVRObject ammoObjOverride = null)
        {
            GameObject caseObj = UnityEngine.Object.Instantiate<GameObject>(caseFab, position, Quaternion.LookRotation(forward, Vector3.up));

            //M.m_weaponCases.Add(caseObj);
            var weaponCases = (List<GameObject>)fiWeaponCases.GetValue(M);
            weaponCases.Add(caseObj);

            TNH_WeaponCrate createComp = caseObj.GetComponent<TNH_WeaponCrate>();

            FVRObject ammoObj = ammoObjOverride ?? weapon.GetRandomAmmoObject(weapon, M.C.ValidAmmoEras, minAmmo, maxAmmo, M.C.ValidAmmoSets);
            int numClipSpeedLoaderRound = (ammoObj != null && ammoObj.Category == FVRObject.ObjectCategory.Cartridge) ? numRound : numMag;

            FVRObject sightObj = null;
            FVRObject requiredAttachment_B = null;
            if (weapon.RequiresPicatinnySight)
            {
                sightObj = M.GetObjectTable(M.C.RequireSightTable).GetRandomObject();

                if (sightObj.RequiredSecondaryPieces.Count > 0)
                {
                    requiredAttachment_B = sightObj.RequiredSecondaryPieces[0];
                }
            }
            // Check the bespoke attachment chance here
            // In vanilla TNH, it ALWAYS spawns a bespoke attachment if there is one
            else if (weapon.BespokeAttachments.Count > 0 && UnityEngine.Random.value < bespokeAttachmentChance)
            {
                sightObj = weapon.BespokeAttachments[UnityEngine.Random.Range(0, weapon.BespokeAttachments.Count)];
            }

            if (weapon.RequiredSecondaryPieces.Count > 0)
            {
                requiredAttachment_B = weapon.RequiredSecondaryPieces[0];
            }

            createComp.PlaceWeaponInContainer(weapon, sightObj, requiredAttachment_B, ammoObj, numClipSpeedLoaderRound);
            return caseObj;
        }


        #endregion


        #region Misc Patches

        //////////////////////////
        //MISC PATCHES AND METHODS
        //////////////////////////


        // Clean up references so they can be garbage collected. This normally happens during the Hold phase,
        // but we should do this during the Take phase too. It won't delete existing any objects.
        [HarmonyPatch(typeof(TNH_Manager), "Update_Take")]
        [HarmonyPostfix]
        public static void TakeCleanup(TNH_Manager __instance, ref HashSet<FVRPhysicalObject> ___m_knownObjsHash, ref List<FVRPhysicalObject> ___m_knownObjs,
            ref int ___knownObjectCheckIndex)
        {
            if (___m_knownObjs.Count <= 0)
                return;

            ___knownObjectCheckIndex++;
            if (___knownObjectCheckIndex >= ___m_knownObjs.Count)
                ___knownObjectCheckIndex = 0;

            if (___m_knownObjs[___knownObjectCheckIndex] == null)
            {
                ___m_knownObjsHash.Remove(___m_knownObjs[___knownObjectCheckIndex]);
                ___m_knownObjs.RemoveAt(___knownObjectCheckIndex);
            }
        }


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Hold")]
        [HarmonyPostfix]
        public static void AfterSetHold()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")]
        [HarmonyPostfix]
        public static void AfterSetDead()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")]
        [HarmonyPostfix]
        public static void AfterSetComplete()
        {
            ClearAllPanels();
        }

        public static void ClearAllPanels()
        {
            TNHFramework.SpawnedPools.Clear();

            for (int i = TNHFramework.SpawnedConstructors.Count - 1; i >= 0; i--)
            {
                try
                {
                    TNH_ObjectConstructor constructor = TNHFramework.SpawnedConstructors[i].GetComponent<TNH_ObjectConstructor>();

                    if (constructor != null)
                    {
                        constructor.ClearCase();
                    }

                    UnityEngine.Object.Destroy(TNHFramework.SpawnedConstructors[i]);
                }
                catch
                {
                    TNHFrameworkLogger.LogWarning("Failed to destroy constructor! It's likely that the constructor is already destroyed, so everything is probably just fine :)");
                }

                TNHFramework.SpawnedConstructors.RemoveAt(i);
            }

            for (int i = TNHFramework.SpawnedPanels.Count - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(TNHFramework.SpawnedPanels[i]);
                TNHFramework.SpawnedPanels.RemoveAt(i);
            }
        }

        // Anton pls fix - Pump action shotgun config not working
        [HarmonyPatch(typeof(TubeFedShotgun), "SetLoadedChambers")]
        [HarmonyPostfix]
        public static void SetLoadedChambers_SetExtractor(TubeFedShotgun __instance, ref bool ___m_isChamberRoundOnExtractor, ref FVRFirearmMovingProxyRound ___m_proxy)
        {
            if (__instance.Chamber.IsFull)
            {
                ___m_isChamberRoundOnExtractor = true;
                ___m_proxy.ClearProxy();
            }

        }

        // Anton pls fix - Pump action shotgun config not working
        [HarmonyPatch(typeof(TubeFedShotgun), "ConfigureFromFlagDic")]
        [HarmonyPostfix]
        public static void ConfigureFromFlagDic_CheckLock(TubeFedShotgun __instance, bool ___m_isHammerCocked, ref bool ___m_isSafetyEngaged, Dictionary<string, string> f)
        {
            if (__instance.Mode == TubeFedShotgun.ShotgunMode.PumpMode)
            {
                if (___m_isHammerCocked)
                {
                    if (__instance.HasHandle)
                        __instance.Handle.LockHandle();
                }
            }

            if (__instance.HasSafety)
            {
                if (f.ContainsKey("SafetyState"))
                {
                    if (f["SafetyState"] == "Off")
                        ___m_isSafetyEngaged = false;

                    //__instance.UpdateSafetyGeo();
                    miUpdateSafetyGeo.Invoke(__instance, []);
                }
            }
        }

        // Anton pls fix - OpenBoltReceiver doesn't even HAVE an override for ConfigureFromFlagDic(), so fire selector and bolt state can't be set there
        [HarmonyPatch(typeof(OpenBoltReceiver), "SetLoadedChambers")]
        [HarmonyPrefix]
        public static bool SetLoadedChambers_FireSelect(OpenBoltReceiver __instance, List<FireArmRoundClass> rounds)
        {
            // Kludge. Since open bolt guns are never saved with chambered rounds, we can edit the vault file to add one to trigger this.
            // Note that a round will be taken from the magazine, so there's no actual +1 round.
            if (rounds.Count > 0)
            {
                //__instance.ToggleFireSelector();
                miToggleFireSelector.Invoke(__instance, []);
                __instance.Bolt.SetBoltToRear();
                __instance.BeginChamberingRound();
                __instance.ChamberRound();
            }

            return false;
        }



        #endregion

    }
}
