using FistVR;
using HarmonyLib;
using Stratum;

// using MagazinePatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;
using static FistVR.VaultSystem;
using static RenderHeads.Media.AVProVideo.MediaPlayer.OptionsApple;
using static RootMotion.FinalIK.IKSolver;

namespace TNHFramework.Patches
{
    public class TNHPatches
    {
        static List<string> BaseCharStrings =
        [
            "DD_C00",
            "DD_C01",
            "DD_C02",
            "DD_C03",
            "COMP_C01",
            "WTT_C01",
            "WTT_C02",
            "WTT_C03",
            "WTT_C04",
            "MM_C01",
            "MM_C02",
            "MM_C03",
        ];

        [HarmonyPatch(typeof(TNH_Manager), "DelayedInit")]
        [HarmonyPrefix]
        public static bool InitTNH(TNH_Manager __instance)
        {
            if (!__instance.m_hasInit)
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


        /// <summary>
        /// Performs initial setup of the TNH Scene when loaded
        /// </summary>
        /// <param name="___Categories"></param>
        /// <param name="___CharDatabase"></param>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(TNH_UIManager), "Start")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool InitTNH(List<TNH_UIManager.CharacterCategory> ___Categories, TNH_CharacterDatabase ___CharDatabase, TNH_UIManager __instance)
        {
            TNHFrameworkLogger.Log("Start method of TNH_UIManager just got called!", TNHFrameworkLogger.LogType.General);

            GM.TNHOptions.Char = TNH_Char.DD_ClassicLoudoutLouis;

            Text magazineCacheText = CreateMagazineCacheText(__instance);
            Text itemsText = CreateItemsText(__instance);
            ExpandCharacterUI(__instance);

            //Perform first time setup of all files
            if (!TNHMenuInitializer.TNHInitialized)
            {
                SceneLoader sceneHotDog = UnityEngine.Object.FindObjectOfType<SceneLoader>();

                if (!TNHMenuInitializer.MagazineCacheFailed)
                {
                    AnvilManager.Run(TNHMenuInitializer.InitializeTNHMenuAsync(TNHFramework.OutputFilePath, magazineCacheText, itemsText, sceneHotDog, ___Categories, ___CharDatabase, __instance, TNHFramework.BuildCharacterFiles.Value));
                }

                //If the magazine cache has previously failed, we shouldn't let the player continue
                else
                {
                    sceneHotDog.gameObject.SetActive(false);
                    magazineCacheText.text = "FAILED! SEE LOG!";
                }

            }
            else
            {
                TNHMenuInitializer.RefreshTNHUI(__instance, ___Categories, ___CharDatabase);
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
            //Add additional character buttons
            OptionsPanel_ButtonSet buttonSet = manager.LBL_CharacterName[1].transform.parent.GetComponent<OptionsPanel_ButtonSet>();
            List<FVRPointableButton> buttonList = new(buttonSet.ButtonsInSet);
            for (int i = 0; i < 3; i++)
            {
                Text newCharacterLabel = UnityEngine.Object.Instantiate(manager.LBL_CharacterName[1].gameObject, manager.LBL_CharacterName[1].transform.parent).GetComponent<Text>();

                manager.LBL_CharacterName.Add(newCharacterLabel);
                buttonList.Add(newCharacterLabel.gameObject.GetComponent<FVRPointableButton>());
            }
            buttonSet.ButtonsInSet = buttonList.ToArray();

            //Adjust buttons to be tighter together
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

        [HarmonyPatch(typeof(TNH_UIManager), "SetSelectedCategory")]
        [HarmonyPrefix]
        public static bool SetCategoryUIPatch(TNH_UIManager __instance, int cat)
        {
            TNHFrameworkLogger.Log("Category number " + cat + ", offset " + OffsetCat + ", max is " + __instance.Categories.Count, TNHFrameworkLogger.LogType.TNH);

            __instance.m_selectedCategory = cat + OffsetCat;
            OptionsPanel_ButtonSet buttonSet = __instance.LBL_CharacterName[0].transform.parent.GetComponent<OptionsPanel_ButtonSet>();

            // probably better done with a switch statement and a single int, but i just wanna get this done first]
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

            TNHFrameworkLogger.Log("Adjust is " + adjust, TNHFrameworkLogger.LogType.TNH);

            OffsetCat += adjust;
            buttonSet.SetSelectedButton(buttonSet.selectedButton + (adjust * -1));

            __instance.PlayButtonSound(0);

            for (int i = 0; i < __instance.LBL_CategoryName.Count; i++)
            {
                TNHFrameworkLogger.Log("Category iterator is " + i, TNHFrameworkLogger.LogType.TNH);

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
        public static bool SetCharacterUIPatch(TNH_UIManager __instance, int i)
        {
            TNHFrameworkLogger.Log("Character number " + i + ", offset " + OffsetChar + ", max is " + __instance.Categories[__instance.m_selectedCategory].Characters.Count, TNHFrameworkLogger.LogType.TNH);

            __instance.m_selectedCharacter = i + OffsetChar;
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
            else if (i == 10 && __instance.Categories[__instance.m_selectedCategory].Characters.Count > 12)
            {
                adjust = Math.Min(1, __instance.Categories[__instance.m_selectedCategory].Characters.Count - OffsetChar - 12);
            }
            else if (i == 11 && __instance.Categories[__instance.m_selectedCategory].Characters.Count > 12)
            {
                adjust = Math.Min(2, __instance.Categories[__instance.m_selectedCategory].Characters.Count - OffsetChar - 12);
            }

            TNHFrameworkLogger.Log("Adjust is " + adjust, TNHFrameworkLogger.LogType.TNH);

            OffsetChar += adjust;
            buttonSet.SetSelectedButton(buttonSet.selectedButton + (adjust * -1));

            __instance.SetCharacter(__instance.Categories[__instance.m_selectedCategory].Characters[__instance.m_selectedCharacter]);
            __instance.PlayButtonSound(1);

            // now i don't know what to name this. fuck this, it's getting a j. you did this to me, anton.
            // ...who am i kidding anton isn't reading this-
            // ...either that or i'm probably dead.
            for (int j = 0; j < __instance.LBL_CharacterName.Count; j++)
            {
                TNHFrameworkLogger.Log("Char iterator is " + j, TNHFrameworkLogger.LogType.TNH);

                if (j + OffsetChar < __instance.Categories[__instance.m_selectedCategory].Characters.Count)
                {
                    __instance.LBL_CharacterName[j].gameObject.SetActive(true);

                    TNH_CharacterDef def = __instance.CharDatabase.GetDef(__instance.Categories[__instance.m_selectedCategory].Characters[j + OffsetChar]);
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
        public static bool SpawnStartingEquipment(TNH_SupplyPoint __instance)
        {
            __instance.m_trackedObjects.Clear();
            if (__instance.M.ItemSpawnerMode == TNH_ItemSpawnerMode.On)
            {
                __instance.M.ItemSpawner.transform.position = __instance.SpawnPoints_Panels[0].position + Vector3.up * 0.8f;
                __instance.M.ItemSpawner.transform.rotation = __instance.SpawnPoints_Panels[0].rotation;
                __instance.M.ItemSpawner.SetActive(true);
            }

            for (int i = 0; i < __instance.SpawnPoint_Tables.Count; i++)
            {
                GameObject item = UnityEngine.Object.Instantiate(__instance.M.Prefab_MetalTable, __instance.SpawnPoint_Tables[i].position, __instance.SpawnPoint_Tables[i].rotation);
                __instance.m_trackedObjects.Add(item);
            }

            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
            if (character.PrimaryWeapon != null && character.PrimaryWeapon.PrimaryGroup is EquipmentGroup)
            {
                EquipmentGroup selectedGroup = character.PrimaryWeapon.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.PrimaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();
                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = __instance.M.SpawnWeaponCase(__instance.M.Prefab_WeaponCaseLarge, __instance.SpawnPoint_CaseLarge.position, __instance.SpawnPoint_CaseLarge.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    __instance.m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.SecondaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.SecondaryWeapon.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.SecondaryWeapon.BackupGroup;

                if (selectedGroup != null)
                {
                    selectedGroup = selectedGroup.GetSpawnedEquipmentGroups().GetRandom();
                    FVRObject selectedItem = IM.OD[selectedGroup.GetObjects().GetRandom()];
                    if (!IM.CompatMags.TryGetValue(selectedItem.MagazineType, out _) && selectedItem.MagazineType != FireArmMagazineType.mNone)
                    {
                        IM.CompatMags.Add(selectedItem.MagazineType, selectedItem.CompatibleMagazines);
                        TNHFrameworkLogger.Log($"{selectedItem.CompatibleMagazines}", TNHFrameworkLogger.LogType.TNH);
                    }
                    GameObject weaponCase = __instance.M.SpawnWeaponCase(__instance.M.Prefab_WeaponCaseSmall, __instance.SpawnPoint_CaseSmall.position, __instance.SpawnPoint_CaseSmall.forward, selectedItem, selectedGroup.NumMagsSpawned, selectedGroup.NumRoundsSpawned, selectedGroup.MinAmmoCapacity, selectedGroup.MaxAmmoCapacity);
                    __instance.m_trackedObjects.Add(weaponCase);
                    weaponCase.GetComponent<TNH_WeaponCrate>().M = __instance.M;
                }
            }

            if (character.TertiaryWeapon != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryWeapon.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.TertiaryWeapon.BackupGroup;

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
                EquipmentGroup selectedGroup = character.PrimaryItem.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.PrimaryItem.BackupGroup;

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
                EquipmentGroup selectedGroup = character.SecondaryItem.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.SecondaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoints_SmallItem[1].position, __instance.SpawnPoints_SmallItem[1].rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.TertiaryItem != null)
            {
                EquipmentGroup selectedGroup = character.TertiaryItem.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.TertiaryItem.BackupGroup;

                if (selectedGroup != null)
                {
                    AnvilManager.Run(TNHFrameworkUtils.InstantiateFromEquipmentGroup(selectedGroup, __instance.SpawnPoints_SmallItem[2].position, __instance.SpawnPoints_SmallItem[2].rotation, o =>
                    {
                        __instance.M.AddObjectToTrackedList(o);
                    }));
                }
            }

            if (character.Shield != null)
            {
                EquipmentGroup selectedGroup = character.Shield.PrimaryGroup;
                if (selectedGroup == null) selectedGroup = character.Shield.BackupGroup;

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




        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Take")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool SetPhase_Take_Replacement(
            TNH_Manager __instance,
            int ___m_level,
            TNH_Progression.Level ___m_curLevel,
            TNH_PointSequence ___m_curPointSequence)
        {

            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.C];
            Level level = character.GetCurrentLevel(___m_curLevel);

            TNHFramework.SpawnedBossIndexes.Clear();
            __instance.m_activeSupplyPointIndicies.Clear();
            TNHFramework.PreventOutfitFunctionality = LoadedTemplateManager.LoadedCharactersDict[__instance.C].ForceDisableOutfitFunctionality;


            //Clear the TNH radar
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
            // J: Don't know if this is necessary, but I'm still getting to grips with things. This is what the base code does and I'm gonna steal it for now.
            int curHoldIndex2 = __instance.m_curHoldIndex;


            //Get the next hold point and configure it
            __instance.m_lastHoldIndex = __instance.m_curHoldIndex;
            __instance.m_curHoldIndex = GetNextHoldPointIndex(__instance, ___m_curPointSequence, ___m_level, __instance.m_curHoldIndex);
            __instance.m_curHoldPoint = __instance.HoldPoints[__instance.m_curHoldIndex];
            __instance.m_curHoldPoint.ConfigureAsSystemNode(___m_curLevel.TakeChallenge, ___m_curLevel.HoldChallenge, ___m_curLevel.NumOverrideTokensForHold);
            __instance.TAHReticle.RegisterTrackedObject(__instance.m_curHoldPoint.SpawnPoint_SystemNode, TAH_ReticleContact.ContactType.Hold);


            //Generate all of the supply points for this level
            List<int> supplyPointsIndexes = GetNextSupplyPointIndexes(__instance, ___m_curPointSequence, ___m_level, __instance.m_curHoldIndex);
            int numSupplyPoints = UnityEngine.Random.Range(level.MinSupplyPoints, level.MaxSupplyPoints + 1);
            numSupplyPoints = Mathf.Clamp(numSupplyPoints, 0, supplyPointsIndexes.Count);


            //Shuffle panel types
            level.PossiblePanelTypes.Shuffle();
            TNHFrameworkLogger.Log("Panel types for this hold:", TNHFrameworkLogger.LogType.TNH);
            level.PossiblePanelTypes.ForEach(o => TNHFrameworkLogger.Log(o.ToString(), TNHFrameworkLogger.LogType.TNH));

            bool isCustomCharacter = true;

            foreach (string Item in BaseCharStrings)
            {
                if (__instance.C.TableID == Item)
                {
                    isCustomCharacter = false;
                    break;
                }
            }


            //Ensure ammo reloaders spawn first if this is limited ammo
            if (level.PossiblePanelTypes.Contains(PanelType.AmmoReloader) && __instance.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo)
            {
                level.PossiblePanelTypes.Remove(PanelType.AmmoReloader);
                level.PossiblePanelTypes.Insert(0, PanelType.AmmoReloader);
            }


            //Now spawn and setup all of the supply points
            //TODO this is one of the main code blocks for this method that requires it to be a full copy of the original method. This would be better fit as a transpiler patch
            TNHFrameworkLogger.Log("Spawning " + numSupplyPoints + " supply points", TNHFrameworkLogger.LogType.TNH);
            int panelIndex = 0;
            if (__instance.m_curPointSequence.UsesExplicitSingleSupplyPoints && __instance.m_level < 5)
            {
                int num3 = __instance.m_curPointSequence.SupplyPoints[__instance.m_level];
                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[num3];
                // tnh_SupplyPoint.Configure(__instance.m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                ConfigureSupplyPoint(supplyPoint, level, ref panelIndex);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                __instance.m_activeSupplyPointIndicies.Add(num3);

                if (supplyPointsIndexes.Contains(num3))
                {
                    supplyPointsIndexes.Remove(num3);
                }

                // Adds extra supply points if it's a custom character, with one in the standard location, and any extra located elsewhere.
                if (isCustomCharacter == true)
                {
                    // 
                    for (int i = 1; i < numSupplyPoints; i++)
                    {
                        TNHFrameworkLogger.Log("Configuring supply point : " + i, TNHFrameworkLogger.LogType.TNH);

                        TNH_SupplyPoint supplyPoint2 = __instance.SupplyPoints[supplyPointsIndexes[i]];
                        ConfigureSupplyPoint(supplyPoint2, level, ref panelIndex);
                        TAH_ReticleContact contact2 = __instance.TAHReticle.RegisterTrackedObject(supplyPoint2.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                        supplyPoint2.SetContact(contact2);
                        __instance.m_activeSupplyPointIndicies.Add(supplyPointsIndexes[i]);
                    }
                }
                
            }
            else if (__instance.m_curPointSequence.UsesExplicitSingleSupplyPoints && __instance.m_level >= 5)
            {
                List<int> list2 = [];
                int num4 = -1;
                for (int i = 0; i < __instance.SafePosMatrix.Entries_HoldPoints[__instance.m_curHoldIndex].SafePositions_SupplyPoints.Count; i++)
                {
                    if (i != num4 && __instance.SafePosMatrix.Entries_HoldPoints[__instance.m_curHoldIndex].SafePositions_SupplyPoints[i])
                    {
                        list2.Add(i);
                    }
                }
                list2.Shuffle<int>();
                int num5 = list2[0];
                TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[num5];
                // tnh_SupplyPoint2.Configure(__instance.m_curLevel.SupplyChallenge, true, true, true, TNH_SupplyPoint.SupplyPanelType.All, 2, 3, true);
                ConfigureSupplyPoint(supplyPoint, level, ref panelIndex);
                TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                supplyPoint.SetContact(contact);
                __instance.m_activeSupplyPointIndicies.Add(num5);
            }
            else
            {
                for (int i = 0; i < numSupplyPoints; i++)
                {
                    TNHFrameworkLogger.Log("Configuring supply point : " + i, TNHFrameworkLogger.LogType.TNH);

                    TNH_SupplyPoint supplyPoint = __instance.SupplyPoints[supplyPointsIndexes[i]];
                    ConfigureSupplyPoint(supplyPoint, level, ref panelIndex);
                    TAH_ReticleContact contact = __instance.TAHReticle.RegisterTrackedObject(supplyPoint.SpawnPoint_PlayerSpawn, TAH_ReticleContact.ContactType.Supply);
                    supplyPoint.SetContact(contact);
                    __instance.m_activeSupplyPointIndicies.Add(supplyPointsIndexes[i]);
                }
            }


            //Go through and spawn the initial patrol
            if (__instance.UsesClassicPatrolBehavior)
            {
                if (__instance.m_level == 0)
                {
                    __instance.GenerateValidPatrol(__instance.m_curLevel.PatrolChallenge, __instance.m_curPointSequence.StartSupplyPointIndex, __instance.m_curHoldIndex, true);
                }
                else
                {
                    __instance.GenerateValidPatrol(__instance.m_curLevel.PatrolChallenge, __instance.m_curHoldIndex, __instance.m_curHoldIndex, false);
                }
            }
            else if (__instance.m_level == 0)
            {
                // __instance.GenerateInitialTakeSentryPatrols(__instance.m_curLevel.PatrolChallenge, __instance.m_curPointSequence.StartSupplyPointIndex, -1, __instance.m_curHoldIndex, true);
                __instance.GenerateInitialTakeSentryPatrols(__instance.m_curLevel.PatrolChallenge, __instance.m_curPointSequence.StartSupplyPointIndex, -1, __instance.m_curHoldIndex, true);
            }
            else
            {
                // __instance.GenerateInitialTakeSentryPatrols(__instance.m_curLevel.PatrolChallenge, -1, __instance.m_curHoldIndex, __instance.m_curHoldIndex, false);
                __instance.GenerateInitialTakeSentryPatrols(__instance.m_curLevel.PatrolChallenge, -1, __instance.m_curHoldIndex, __instance.m_curHoldIndex, false);
            }

            for (int i = 0; i < __instance.ConstructSpawners.Count; i++)
            {
                if (curHoldIndex2 >= 0)
                {
                    TNH_HoldPoint tnh_HoldPoint = __instance.HoldPoints[curHoldIndex2];
                    if (!tnh_HoldPoint.ExcludeConstructVolumes.Contains(__instance.ConstructSpawners[i]))
                    {
                        __instance.ConstructSpawners[i].SpawnConstructs(__instance.m_level);
                    }
                }
                else
                {
                    __instance.ConstructSpawners[i].SpawnConstructs(__instance.m_level);
                }
            }

            if (__instance.BGAudioMode == TNH_BGAudioMode.Default)
            {
                __instance.FMODController.SwitchTo(0, 2f, false, false);
            }

            return false;
        }

        public static void ConfigureSupplyPoint(TNH_SupplyPoint supplyPoint, Level level, ref int panelIndex)
        {

            supplyPoint.T = level.SupplyChallenge.GetTakeChallenge();
            supplyPoint.m_isconfigured = true;

            SpawnSupplyGroup(supplyPoint, level);

            SpawnSupplyTurrets(supplyPoint, level);

            int numConstructors = UnityEngine.Random.Range(level.MinConstructors, level.MaxConstructors + 1);

            SpawnSupplyConstructor(supplyPoint, numConstructors);

            SpawnSecondarySupplyPanel(supplyPoint, level, numConstructors, ref panelIndex);

            SpawnSupplyBoxes(supplyPoint, level);

            supplyPoint.m_hasBeenVisited = false;
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

            int numPanels = UnityEngine.Random.Range(level.MinPanels, level.MaxPanels + 1);

            // Suboptimal, but the simplest way to implement.
            // Check if the map is Institution. Then check if the character is a base-game character.
            if (point.M.LevelName == "Institution") 
            {
                foreach (string Item in BaseCharStrings)
                {
                    if (point.M.C.TableID == Item)
                    {
                        numPanels = 3;
                        break;
                    }
                }
            }

            for (int i = startingPanelIndex; i < startingPanelIndex + numPanels && i < point.SpawnPoints_Panels.Count && level.PossiblePanelTypes.Count > 0; i++)
            {
                TNHFrameworkLogger.Log("Panel index : " + i, TNHFrameworkLogger.LogType.TNH);

                //Go through the panels, and loop if we have gone too far 
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

                //If we spawned a panel, add it to the global list
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

            for (int i = 0; i < level.SupplyChallenge.NumGuards && i < point.SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = point.SpawnPoints_Sosigs_Defense[i];
                SosigEnemyTemplate template = ManagerSingleton<IM>.Instance.odicSosigObjsByID[level.SupplyChallenge.GetTakeChallenge().GID];
                SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

                Sosig enemy = PatrolPatches.SpawnEnemy(customTemplate, LoadedTemplateManager.LoadedCharactersDict[point.M.C], transform, point.M.AI_Difficulty, level.SupplyChallenge.IFFUsed, false, transform.position, true);

                point.m_activeSosigs.Add(enemy);
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
                point.m_activeTurrets.Add(turret);
            }

        }


        public static void SpawnSupplyBoxes(TNH_SupplyPoint point, Level level)
        {
            point.SpawnPoints_Boxes.Shuffle();

            int boxesToSpawn = UnityEngine.Random.Range(level.MinBoxesSpawned, level.MaxBoxesSpawned + 1);

            TNHFrameworkLogger.Log("Going to spawn " + boxesToSpawn + " boxes at this point -- Min (" + level.MinBoxesSpawned + "), Max (" + level.MaxBoxesSpawned + ")", TNHFrameworkLogger.LogType.TNH);

            for (int i = 0; i < boxesToSpawn; i++)
            {
                Transform spawnTransform = point.SpawnPoints_Boxes[UnityEngine.Random.Range(0, point.SpawnPoints_Boxes.Count)];
                Vector3 position = spawnTransform.position + Vector3.up * 0.1f + Vector3.right * UnityEngine.Random.Range(-0.5f, 0.5f) + Vector3.forward * UnityEngine.Random.Range(-0.5f, 0.5f);
                Quaternion rotation = Quaternion.Slerp(spawnTransform.rotation, UnityEngine.Random.rotation, 0.1f);
                GameObject box = UnityEngine.Object.Instantiate(point.M.Prefabs_ShatterableCrates[UnityEngine.Random.Range(0, point.M.Prefabs_ShatterableCrates.Count)], position, rotation);
                point.m_spawnBoxes.Add(box);
            }

            int tokensSpawned = 0;

            // J: If you're asking "why is this an if/elseif check if it's a boolean value?", I... I don't know. I don't know why Anton does this. It's not a big deal but I don't know why.
            if (!point.M.UsesUberShatterableCrates)
            {
                foreach (GameObject boxObj in point.m_spawnBoxes)
                {

                    if (tokensSpawned < level.MinTokensPerSupply)
                    {
                        boxObj.GetComponent<TNH_ShatterableCrate>().SetHoldingToken(point.M);
                        tokensSpawned += 1;
                    }

                    else if (tokensSpawned < level.MaxTokensPerSupply && UnityEngine.Random.value < level.BoxTokenChance)
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
                for (int k = 0; k < point.m_spawnBoxes.Count; k++)
                {
                    UberShatterable component = point.m_spawnBoxes[k].GetComponent<UberShatterable>();
                    if (tokensSpawned < level.MinTokensPerSupply)
                    {
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Token);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
                        tokensSpawned += 1;
                    }
                    else if (tokensSpawned < level.MaxTokensPerSupply && UnityEngine.Random.value < level.BoxTokenChance)
                    {
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Token);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
                        tokensSpawned += 1;
                    }
                    else if (UnityEngine.Random.value < level.BoxHealthChance)
                    {
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Full);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_HealthMinor);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.Identity);
                    }
                    else
                    {
                        component.SpawnOnShatter.Add(point.M.ResourceLib.Prefab_Crate_Empty);
                        component.SpawnOnShatterPoints.Add(component.transform);
                        component.SpawnOnShatterRotTypes.Add(UberShatterable.SpawnOnShatterRotationType.StrikeDir);
                    }
                }
            }
        }

        public static int GetNextHoldPointIndex(TNH_Manager M, TNH_PointSequence pointSequence, int currLevel, int currHoldIndex)
        {
            int index;

            //If we havn't gone through all the hold points, we just select the next one we havn't been to
            if (currLevel < pointSequence.HoldPoints.Count)
            {
                index = pointSequence.HoldPoints[currLevel];
            }

            //If we have been to all the points, then we just select a random safe one
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
            indexes.Shuffle();

            return indexes;
        }


        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTakeEnemyGroup")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool SpawnTakeGroupReplacement(List<Transform> ___SpawnPoints_Sosigs_Defense, TNH_TakeChallenge ___T, TNH_Manager ___M, List<Sosig> ___m_activeSosigs)
        {
            ___SpawnPoints_Sosigs_Defense.Shuffle();
            ___SpawnPoints_Sosigs_Defense.Shuffle();

            for (int i = 0; i < ___T.NumGuards && i < ___SpawnPoints_Sosigs_Defense.Count; i++)
            {
                Transform transform = ___SpawnPoints_Sosigs_Defense[i];
                //Debug.Log("Take challenge sosig ID : " + ___T.GID);
                SosigEnemyTemplate template = IM.Instance.odicSosigObjsByID[___T.GID];
                SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[template];

                Sosig enemy = PatrolPatches.SpawnEnemy(customTemplate, LoadedTemplateManager.LoadedCharactersDict[___M.C], transform, ___M.AI_Difficulty, ___T.IFFUsed, false, transform.position, true);

                ___m_activeSosigs.Add(enemy);
            }

            return false;
        }



        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawnTurrets")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool SpawnTurretsReplacement(List<Transform> ___SpawnPoints_Turrets, TNH_TakeChallenge ___T, TNH_Manager ___M, List<AutoMeater> ___m_activeTurrets)
        {
            ___SpawnPoints_Turrets.Shuffle<Transform>();
            FVRObject turretPrefab = ___M.GetTurretPrefab(___T.TurretType);

            for (int i = 0; i < ___T.NumTurrets && i < ___SpawnPoints_Turrets.Count; i++)
            {
                Vector3 pos = ___SpawnPoints_Turrets[i].position + Vector3.up * 0.25f;
                AutoMeater turret = UnityEngine.Object.Instantiate<GameObject>(turretPrefab.GetGameObject(), pos, ___SpawnPoints_Turrets[i].rotation).GetComponent<AutoMeater>();
                ___m_activeTurrets.Add(turret);
            }

            return false;
        }


        #endregion


        #region During Hold Point

        ///////////////////////////////
        //PATCHES FOR DURING HOLD POINT
        ///////////////////////////////



        [HarmonyPatch(typeof(TNH_HoldPoint), "IdentifyEncryption")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool IdentifyEncryptionReplacement(TNH_HoldPoint __instance, TNH_HoldChallenge.Phase ___m_curPhase)
        {
            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];
            Phase currentPhase = character.GetCurrentPhase(___m_curPhase);

            //If we shouldnt spawn any targets, we exit out early
            if ((currentPhase.MaxTargets < 1 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.Spawnlocking) ||
                (currentPhase.MaxTargetsLimited < 1 && __instance.M.EquipmentMode == TNHSetting_EquipmentMode.LimitedAmmo))
            {
                __instance.CompletePhase();
                return false;
            }

            __instance.m_state = TNH_HoldPoint.HoldState.Hacking;
            __instance.m_tickDownToFailure = 120f;


            if (__instance.M.TargetMode == TNHSetting_TargetMode.Simple)
            {
                __instance.M.EnqueueEncryptionLine(TNH_EncryptionType.Static);
                __instance.DeleteAllActiveWarpIns();
                SpawnEncryptionReplacement(__instance, currentPhase, true);
            }
            else
            {
                __instance.M.EnqueueEncryptionLine(currentPhase.Encryptions[0]);
                __instance.DeleteAllActiveWarpIns();
                SpawnEncryptionReplacement(__instance, currentPhase, false);
            }

            __instance.m_systemNode.SetNodeMode(TNH_HoldPointSystemNode.SystemNodeMode.Indentified);

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


            for (int i = 0; i < numTargets && i < holdPoint.m_validSpawnPoints.Count; i++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(encryptions[i % encryptions.Count].GetGameObject(), holdPoint.m_validSpawnPoints[i].position, holdPoint.m_validSpawnPoints[i].rotation);
                TNH_EncryptionTarget encryption = gameObject.GetComponent<TNH_EncryptionTarget>();
                encryption.SetHoldPoint(holdPoint);
                holdPoint.RegisterNewTarget(encryption);
            }
        }

        public static void SpawnGrenades(List<TNH_HoldPoint.AttackVector> AttackVectors, TNH_Manager M, int m_phaseIndex)
        {
            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[M.C];
            Level currLevel = character.GetCurrentLevel(M.m_curLevel);
            Phase currPhase = currLevel.HoldPhases[m_phaseIndex];

            float grenadeChance = currPhase.GrenadeChance;
            string grenadeType = currPhase.GrenadeType;

            if (grenadeChance >= UnityEngine.Random.Range(0f, 1f))
            {
                TNHFrameworkLogger.Log("Throwing grenade ", TNHFrameworkLogger.LogType.TNH);

                //Get a random grenade vector to spawn a grenade at
                TNH_HoldPoint.AttackVector randAttackVector = AttackVectors[UnityEngine.Random.Range(0, AttackVectors.Count)];

                //Instantiate the grenade object
                GameObject grenadeObject = UnityEngine.Object.Instantiate(IM.OD[grenadeType].GetGameObject(), randAttackVector.GrenadeVector.position, randAttackVector.GrenadeVector.rotation);

                //Give the grenade an initial velocity based on the grenade vector
                grenadeObject.GetComponent<Rigidbody>().velocity = 15 * randAttackVector.GrenadeVector.forward;
                grenadeObject.GetComponent<SosigWeapon>().FuseGrenade();
            }
        }



        public static void SpawnHoldEnemyGroup(TNH_HoldChallenge.Phase curPhase, int phaseIndex, List<TNH_HoldPoint.AttackVector> AttackVectors, List<Transform> SpawnPoints_Turrets, List<Sosig> ActiveSosigs, TNH_Manager M, ref bool isFirstWave)
        {
            TNHFrameworkLogger.Log("Spawning enemy wave", TNHFrameworkLogger.LogType.TNH);

            //TODO add custom property form MinDirections
            int numAttackVectors = UnityEngine.Random.Range(1, curPhase.MaxDirections + 1);
            numAttackVectors = Mathf.Clamp(numAttackVectors, 1, AttackVectors.Count);

            //Get the custom character data
            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[M.C];
            Level currLevel = character.GetCurrentLevel(M.m_curLevel);
            Phase currPhase = currLevel.HoldPhases[phaseIndex];

            //Set first enemy to be spawned as leader
            SosigEnemyTemplate enemyTemplate = ManagerSingleton<IM>.Instance.odicSosigObjsByID[(SosigEnemyID)LoadedTemplateManager.SosigIDDict[currPhase.LeaderType]];
            int enemiesToSpawn = UnityEngine.Random.Range(curPhase.MinEnemies, curPhase.MaxEnemies + 1);

            int sosigsSpawned = 0;
            int vectorSpawnPoint = 0;
            Vector3 targetVector;
            int vectorIndex = 0;
            while (sosigsSpawned < enemiesToSpawn)
            {
                TNHFrameworkLogger.Log("Spawning at attack vector: " + vectorIndex, TNHFrameworkLogger.LogType.TNH);

                if (AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack.Count <= vectorSpawnPoint) break;

                //Set the sosigs target position
                if (currPhase.SwarmPlayer)
                {
                    targetVector = GM.CurrentPlayerBody.TorsoTransform.position;
                }
                else
                {
                    targetVector = SpawnPoints_Turrets[UnityEngine.Random.Range(0, SpawnPoints_Turrets.Count)].position;
                }

                SosigTemplate customTemplate = LoadedTemplateManager.LoadedSosigsDict[enemyTemplate];

                Sosig enemy = PatrolPatches.SpawnEnemy(customTemplate, character, AttackVectors[vectorIndex].SpawnPoints_Sosigs_Attack[vectorSpawnPoint], M.AI_Difficulty, curPhase.IFFUsed, true, targetVector, true);

                ActiveSosigs.Add(enemy);

                //At this point, the leader has been spawned, so always set enemy to be regulars
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



        [HarmonyPatch(typeof(TNH_HoldPoint), "SpawningRoutineUpdate")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool SpawningUpdateReplacement(
            ref float ___m_tickDownToNextGroupSpawn,
            List<Sosig> ___m_activeSosigs,
            TNH_HoldPoint.HoldState ___m_state,
            ref bool ___m_hasThrownNadesInWave,
            List<TNH_HoldPoint.AttackVector> ___AttackVectors,
            List<Transform> ___SpawnPoints_Turrets,
            TNH_Manager ___M,
            TNH_HoldChallenge.Phase ___m_curPhase,
            int ___m_phaseIndex,
            ref bool ___m_isFirstWave)
        {

            ___m_tickDownToNextGroupSpawn -= Time.deltaTime;


            if (___m_activeSosigs.Count < 1)
            {
                if (___m_state == TNH_HoldPoint.HoldState.Analyzing)
                {
                    ___m_tickDownToNextGroupSpawn -= Time.deltaTime;
                }
            }

            if (!___m_hasThrownNadesInWave && ___m_tickDownToNextGroupSpawn <= 5f && !___m_isFirstWave)
            {
                SpawnGrenades(___AttackVectors, ___M, ___m_phaseIndex);
                ___m_hasThrownNadesInWave = true;
            }

            //Handle spawning of a wave if it is time
            if (___m_tickDownToNextGroupSpawn <= 0 && ___m_activeSosigs.Count + ___m_curPhase.MaxEnemies <= ___m_curPhase.MaxEnemiesAlive)
            {
                ___AttackVectors.Shuffle();

                SpawnHoldEnemyGroup(___m_curPhase, ___m_phaseIndex, ___AttackVectors, ___SpawnPoints_Turrets, ___m_activeSosigs, ___M, ref ___m_isFirstWave);
                ___m_hasThrownNadesInWave = false;
                ___m_tickDownToNextGroupSpawn = ___m_curPhase.SpawnCadence;
            }


            return false;
        }


        #endregion


        #region Constructor and Secondary Panels

        //////////////////////////////////////////////
        //PATCHES FOR CONSTRUCTOR AND SECONDARY PANELS
        //////////////////////////////////////////////


        /// <summary>
        /// This is a patch for using a characters global ammo blacklist in an ammo reloader
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__result"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(TNH_AmmoReloader), "GetClassFromType")]
        [HarmonyPrefix]
        public static bool AmmoReloaderGetAmmo(TNH_AmmoReloader __instance, ref FireArmRoundClass __result, FireArmRoundType t)
        {
            if (!__instance.m_decidedTypes.ContainsKey(t))
            {
                List<FireArmRoundClass> list = [];
                CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[__instance.M.C];

                for (int i = 0; i < AM.SRoundDisplayDataDic[t].Classes.Length; i++)
                {
                    FVRObject objectID = AM.SRoundDisplayDataDic[t].Classes[i].ObjectID;
                    if (__instance.m_validEras.Contains(objectID.TagEra) && __instance.m_validSets.Contains(objectID.TagSet))
                    {
                        if (character.GlobalAmmoBlacklist == null || !character.GlobalAmmoBlacklist.Contains(objectID.ItemID))
                        {
                            list.Add(AM.SRoundDisplayDataDic[t].Classes[i].Class);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    __instance.m_decidedTypes.Add(t, list[UnityEngine.Random.Range(0, list.Count)]);
                }
                else
                {
                    __instance.m_decidedTypes.Add(t, AM.GetRandomValidRoundClass(t));
                }
            }

            __result = __instance.m_decidedTypes[t];
            return false;
        }


        [HarmonyPatch(typeof(TNH_ObjectConstructor), "GetPoolEntry")]
        [HarmonyPrefix]
        public static bool GetPoolEntryPatch(ref EquipmentPoolDef.PoolEntry __result, int level, EquipmentPoolDef poolDef, EquipmentPoolDef.PoolEntry.PoolEntryType t, EquipmentPoolDef.PoolEntry prior)
        {

            //Collect all pools that could spawn based on level and type, and sum up their rarities
            List<EquipmentPoolDef.PoolEntry> validPools = [];
            float summedRarity = 0;
            foreach(EquipmentPoolDef.PoolEntry entry in poolDef.Entries)
            {
                if(entry.Type == t && entry.MinLevelAppears <= level && entry.MaxLevelAppears >= level)
                {
                    validPools.Add(entry);
                    summedRarity += entry.Rarity;
                }
            }

            //If we didn't find a single pool, we cry about it
            if(validPools.Count == 0)
            {
                TNHFrameworkLogger.LogWarning("No valid pool could spawn at constructor for type (" + t + ")");
                __result = null;
                return false;
            }

            //Go back through and remove pools that have already spawned, unless there is only one entry left
            validPools.Shuffle();
            for(int i = validPools.Count - 1; i >= 0 && validPools.Count > 1; i--)
            {
                if (TNHFramework.SpawnedPools.Contains(validPools[i]))
                {
                    summedRarity -= validPools[i].Rarity;
                    validPools.RemoveAt(i);
                }
            }

            //Select a random value within the summed rarity, and select a pool based on that value
            float selectValue = UnityEngine.Random.Range(0, summedRarity);
            float currentSum = 0;
            foreach(EquipmentPoolDef.PoolEntry entry in validPools)
            {
                currentSum += entry.Rarity;
                if(selectValue <= currentSum)
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


        [HarmonyPatch(typeof(TNH_ObjectConstructor), "ButtonClicked")] // Specify target method with HarmonyPatch attribute
        [HarmonyPrefix]
        public static bool ButtonClickedReplacement(int i,
            TNH_ObjectConstructor __instance,
            EquipmentPoolDef ___m_pool,
            int ___m_curLevel,
            ref int ___m_selectedEntry,
            ref int ___m_numTokensSelected,
            bool ___allowEntry,
            List<EquipmentPoolDef.PoolEntry> ___m_poolEntries,
            List<int> ___m_poolAddedCost,
            GameObject ___m_spawnedCase)
        {

            __instance.UpdateRerollButtonState(false);

            if (!___allowEntry)
            {
                return false;
            }

            if (__instance.State == TNH_ObjectConstructor.ConstructorState.EntryList)
            {

                int cost = ___m_poolEntries[i].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[i];
                if (__instance.M.GetNumTokens() >= cost)
                {
                    __instance.SetState(TNH_ObjectConstructor.ConstructorState.Confirm, i);
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
                    __instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
                    ___m_selectedEntry = -1;
                    SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Back, __instance.transform.position);
                }
                else if (i == 3)
                {
                    int cost = ___m_poolEntries[___m_selectedEntry].GetCost(__instance.M.EquipmentMode) + ___m_poolAddedCost[___m_selectedEntry];
                    if (__instance.M.GetNumTokens() >= cost)
                    {

                        if ((!___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInSmallCase && !___m_poolEntries[___m_selectedEntry].TableDef.SpawnsInSmallCase) || ___m_spawnedCase == null)
                        {

                            AnvilManager.Run(SpawnObjectAtConstructor(___m_poolEntries[___m_selectedEntry], __instance));
                            ___m_numTokensSelected = 0;
                            __instance.M.SubtractTokens(cost);
                            SM.PlayCoreSound(FVRPooledAudioType.UIChirp, __instance.AudEvent_Spawn, __instance.transform.position);

                            if (__instance.M.C.UsesPurchasePriceIncrement)
                            {
                                ___m_poolAddedCost[___m_selectedEntry] += 1;
                            }

                            __instance.SetState(TNH_ObjectConstructor.ConstructorState.EntryList, 0);
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

            constructor.allowEntry = false;
            EquipmentPool pool = LoadedTemplateManager.EquipmentPoolDictionary[entry];
            CustomCharacter character = LoadedTemplateManager.LoadedCharactersDict[constructor.M.C];
            List<EquipmentGroup> selectedGroups = pool.GetSpawnedEquipmentGroups();
            AnvilCallback<GameObject> gameObjectCallback;

            if (pool.SpawnsInLargeCase || pool.SpawnsInSmallCase)
            {
                TNHFrameworkLogger.Log("Item will spawn in a container", TNHFrameworkLogger.LogType.TNH);

                GameObject caseFab = constructor.M.Prefab_WeaponCaseLarge;
                if (pool.SpawnsInSmallCase) caseFab = constructor.M.Prefab_WeaponCaseSmall;

                FVRObject item = IM.OD[selectedGroups[0].GetObjects().GetRandom()];
                TNHFramework.HoldActions[constructor.M.m_level].Add($"Purchased {item.DisplayName}");
                GameObject itemCase = constructor.M.SpawnWeaponCase(caseFab, constructor.SpawnPoint_Case.position, constructor.SpawnPoint_Case.forward, item, selectedGroups[0].NumMagsSpawned, selectedGroups[0].NumRoundsSpawned, selectedGroups[0].MinAmmoCapacity, selectedGroups[0].MaxAmmoCapacity);

                constructor.m_spawnedCase = itemCase;
                itemCase.GetComponent<TNH_WeaponCrate>().M = constructor.M;
            }

            else
            {
                TNHFrameworkLogger.Log("Item will spawn without a container", TNHFrameworkLogger.LogType.TNH);

                int mainSpawnCount = 0;
                int requiredSpawnCount = 0;
                int ammoSpawnCount = 0;
                int objectSpawnCount = 0;

                // This gathers all spawn points, so that multiple things can be spawned at the same time, on different spawnpoints.
                //TODO: I dont like this, but it should work.
                Dictionary<Transform, List<GameObject>> itemsToSpawn = new Dictionary<Transform, List<GameObject>>
                {
                    { constructor.SpawnPoint_Mag, [] },
                    { constructor.SpawnPoint_Ammo, [] },
                    { constructor.SpawnPoint_Grenade, [] },
                    { constructor.SpawnPoint_Melee, [] },
                    { constructor.SpawnPoint_Shield, [] },
                    { constructor.SpawnPoint_Object, [] },

                    //This should only have one, and throw when trying to spawn more.
                    { constructor.SpawnPoint_Case, [] }
                };
                
                foreach (var gunSpawnPoint in constructor.SpawnPoints_GunsSize)
                {
                    itemsToSpawn.Add(gunSpawnPoint, []);
                }

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
                            mainObject = FirearmUtils.GetAmmoContainerForEquipped(group.MinAmmoCapacity, group.MaxAmmoCapacity, character.GetMagazineBlacklist());
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

                        //Assign spawn points based on the type of item we are spawning
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

                        TNHFrameworkLogger.Log("Level: " + constructor.M.m_level, TNHFrameworkLogger.LogType.TNH);
                        TNHFrameworkLogger.Log("Hold Actions Length: " + TNHFramework.HoldActions.Count, TNHFrameworkLogger.LogType.TNH);
                        TNHFramework.HoldActions[constructor.M.m_level].Add($"Purchased {mainObject.DisplayName}");

                        // J: New vault files have a method for spawning them. Thank god. Or, y'know, thank Anton.
                        if (vaultFile != null)
                        {
                            SpawnVaultFile(vaultFile, primarySpawn, true, false, false, out _, Vector3.zero);
                            TNHFrameworkLogger.Log("Vaulted gun spawned", TNHFrameworkLogger.LogType.TNH);
                        }
                        //If this is a vault file, we have to spawn it through a routine. Otherwise we just instantiate it
                        else if (vaultFileLegacy != null)
                        {
                            AnvilManager.Run(TNHFrameworkUtils.SpawnFirearm(vaultFileLegacy, primarySpawn.position, primarySpawn.rotation));
                            TNHFrameworkLogger.Log("Legacy vaulted gun spawned", TNHFrameworkLogger.LogType.TNH);
                        }
                        else
                        {
                            gameObjectCallback = mainObject.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject spawnedObject = UnityEngine.Object.Instantiate(gameObjectCallback.Result, primarySpawn.position + Vector3.up * objectDistancing * mainSpawnCount, primarySpawn.rotation);
                            TNHFrameworkLogger.Log("Normal item spawned", TNHFrameworkLogger.LogType.TNH);
                        }

                        
                        //Spawn any required objects
                        if (mainObject.RequiredSecondaryPieces != null)
                        {
                            for (int j = 0; j < mainObject.RequiredSecondaryPieces.Count; j++)
                            {
                                if(mainObject.RequiredSecondaryPieces[j] == null)
                                {
                                    TNHFrameworkLogger.Log("Null required object! Skipping", TNHFrameworkLogger.LogType.TNH);
                                    continue;
                                }

                                TNHFrameworkLogger.Log("Spawning Required item", TNHFrameworkLogger.LogType.TNH);
                                gameObjectCallback = mainObject.RequiredSecondaryPieces[j].GetGameObjectAsync();
                                yield return gameObjectCallback;
                                GameObject requiredItem = UnityEngine.Object.Instantiate(gameObjectCallback.Result, requiredSpawn.position + -requiredSpawn.right * 0.2f * requiredSpawnCount + Vector3.up * 0.2f * j, requiredSpawn.rotation);
                                requiredSpawnCount += 1;
                            }
                        }
                        

                        //Handle spawning for ammo objects if the main object has any
                        if (FirearmUtils.FVRObjectHasAmmoObject(mainObject))
                        {
                            Dictionary<string, MagazineBlacklistEntry> blacklist = character.GetMagazineBlacklist();
                            MagazineBlacklistEntry blacklistEntry = null;
                            if (blacklist.ContainsKey(mainObject.ItemID)) blacklistEntry = blacklist[mainObject.ItemID];

                            //Get lists of ammo objects for this firearm with filters and blacklists applied
                            List<FVRObject> compatibleMagazines = FirearmUtils.GetCompatibleMagazines(mainObject, group.MinAmmoCapacity, group.MaxAmmoCapacity, true, blacklistEntry);
                            List<FVRObject> compatibleRounds = FirearmUtils.GetCompatibleRounds(mainObject, character.ValidAmmoEras, character.ValidAmmoSets, character.GlobalAmmoBlacklist, blacklistEntry);
                            List<FVRObject> compatibleClips = mainObject.CompatibleClips;

                            TNHFrameworkLogger.Log("Compatible Mags: " + string.Join(",", compatibleMagazines.Select(o => o.ItemID).ToArray()), TNHFrameworkLogger.LogType.TNH);
                            TNHFrameworkLogger.Log("Compatible Clips: " + string.Join(",", compatibleClips.Select(o => o.ItemID).ToArray()), TNHFrameworkLogger.LogType.TNH);
                            TNHFrameworkLogger.Log("Compatible Rounds: " + string.Join(",", compatibleRounds.Select(o => o.ItemID).ToArray()), TNHFrameworkLogger.LogType.TNH);

                            //If we are supposed to spawn magazines and clips, perform special logic for that
                            if (group.SpawnMagAndClip && compatibleMagazines.Count > 0 && compatibleClips.Count > 0 && group.NumMagsSpawned > 0 && group.NumClipsSpawned > 0)
                            {
                                TNHFrameworkLogger.Log("Spawning with both magazine and clips", TNHFrameworkLogger.LogType.TNH);

                                FVRObject magazineObject = compatibleMagazines.GetRandom();
                                FVRObject clipObject = compatibleClips.GetRandom();
                                ammoSpawn = constructor.SpawnPoint_Mag;

                                gameObjectCallback = magazineObject.GetGameObjectAsync();
                                yield return gameObjectCallback;
                                GameObject spawnedMag = UnityEngine.Object.Instantiate(gameObjectCallback.Result, ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                ammoSpawnCount += 1;

                                gameObjectCallback = clipObject.GetGameObjectAsync();
                                yield return gameObjectCallback;
                                for (int i = 0; i < group.NumClipsSpawned; i++)
                                {
                                    GameObject spawnedClip = UnityEngine.Object.Instantiate(gameObjectCallback.Result, ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    ammoSpawnCount += 1;
                                }
                            }

                            //Otherwise, perform normal logic for spawning ammo objects from current group
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

                                TNHFrameworkLogger.Log("Spawning ammo object normally (" + ammoObject.ItemID + "), Count = " + numSpawned, TNHFrameworkLogger.LogType.TNH);

                                gameObjectCallback = ammoObject.GetGameObjectAsync();
                                yield return gameObjectCallback;

                                for (int i = 0; i < numSpawned; i++)
                                {
                                    GameObject spawned = UnityEngine.Object.Instantiate(gameObjectCallback.Result, ammoSpawn.position + ammoSpawn.up * 0.05f * ammoSpawnCount, ammoSpawn.rotation);
                                    ammoSpawnCount += 1;
                                }
                            }
                        }


                        //If this object equires picatinny sights, we should try to spawn one
                        if (mainObject.RequiresPicatinnySight && character.RequireSightTable != null)
                        {
                            TNHFrameworkLogger.Log("Spawning required sights", TNHFrameworkLogger.LogType.TNH);

                            FVRObject sight = IM.OD[character.RequireSightTable.GetSpawnedEquipmentGroups().GetRandom().GetObjects().GetRandom()];
                            gameObjectCallback = sight.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject spawnedSight = UnityEngine.Object.Instantiate(gameObjectCallback.Result, constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);

                            TNHFrameworkLogger.Log("Required sight spawned", TNHFrameworkLogger.LogType.TNH);

                            for (int j = 0; j < sight.RequiredSecondaryPieces.Count; j++)
                            {
                                gameObjectCallback = sight.RequiredSecondaryPieces[j].GetGameObjectAsync();
                                yield return gameObjectCallback;
                                GameObject spawnedRequired = UnityEngine.Object.Instantiate(gameObjectCallback.Result, constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount + Vector3.up * 0.15f * j, constructor.SpawnPoint_Object.rotation);
                                TNHFrameworkLogger.Log("Required item for sight spawned", TNHFrameworkLogger.LogType.TNH);
                            }

                            objectSpawnCount += 1;
                        }

                        //If this object has bespoke attachments we'll try to spawn one
                        else if (mainObject.BespokeAttachments.Count > 0 && UnityEngine.Random.value < group.BespokeAttachmentChance)
                        {
                            FVRObject bespoke = mainObject.BespokeAttachments.GetRandom();
                            gameObjectCallback = bespoke.GetGameObjectAsync();
                            yield return gameObjectCallback;
                            GameObject bespokeObject = UnityEngine.Object.Instantiate(gameObjectCallback.Result, constructor.SpawnPoint_Object.position + -constructor.SpawnPoint_Object.right * 0.15f * objectSpawnCount, constructor.SpawnPoint_Object.rotation);
                            objectSpawnCount += 1;

                            TNHFrameworkLogger.Log("Bespoke item spawned", TNHFrameworkLogger.LogType.TNH);
                        }
                    }
                }
            }

            constructor.allowEntry = true;
            yield break;
        }


        #endregion


        #region Misc Patches

        //////////////////////////
        //MISC PATCHES AND METHODS
        //////////////////////////


        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Hold")] // Specify target method with HarmonyPatch attribute
        [HarmonyPostfix]
        public static void AfterSetHold()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Dead")] // Specify target method with HarmonyPatch attribute
        [HarmonyPostfix]
        public static void AfterSetDead()
        {
            ClearAllPanels();
        }

        [HarmonyPatch(typeof(TNH_Manager), "SetPhase_Completed")] // Specify target method with HarmonyPatch attribute
        [HarmonyPostfix]
        public static void AfterSetComplete()
        {
            ClearAllPanels();
        }

        public static void ClearAllPanels()
        {
            TNHFramework.SpawnedPools.Clear();

            while (TNHFramework.SpawnedConstructors.Count > 0)
            {
                try
                {
                    TNH_ObjectConstructor constructor = TNHFramework.SpawnedConstructors[0].GetComponent<TNH_ObjectConstructor>();

                    if (constructor != null)
                    {
                        constructor.ClearCase();
                    }

                    UnityEngine.Object.Destroy(TNHFramework.SpawnedConstructors[0]);
                }
                catch
                {
                    TNHFrameworkLogger.LogWarning("Failed to destroy constructor! It's likely that the constructor is already destroyed, so everything is probably just fine :)");
                }

                TNHFramework.SpawnedConstructors.RemoveAt(0);
            }

            while (TNHFramework.SpawnedPanels.Count > 0)
            {
                UnityEngine.Object.Destroy(TNHFramework.SpawnedPanels[0]);
                TNHFramework.SpawnedPanels.RemoveAt(0);
            }
        }



        

        #endregion

    }
}
