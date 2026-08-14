using FistVR;
using System.Collections.Generic;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;

namespace TNHFramework
{
    public class CharacterTemplate(TNH_CharacterDef def, CustomCharacter custom)
    {
        public TNH_CharacterDef Def = def;
        public CustomCharacter Custom = custom;
    }

    public static class LoadedTemplateManager
    {
        public static CustomCharacter CurrentCharacter;
        public static Level CurrentLevel;
        public static Dictionary<string, CharacterTemplate> LoadedCharacterDict = [];
        public static Dictionary<SosigEnemyTemplate, SosigTemplate> LoadedSosigsDict = [];
        public static Dictionary<EquipmentPoolDef.PoolEntry, EquipmentPool> EquipmentPoolDictionary = [];
        public static Dictionary<string, VaultFile> LoadedVaultFiles = [];
        public static Dictionary<string, SavedGunSerializable> LoadedLegacyVaultFiles = [];
        public static List<CustomCharacter> CustomCharacters = [];
        public static List<CustomCharacter> DefaultCharacters = [];
        public static List<SosigTemplate> CustomSosigs = [];
        public static List<SosigEnemyTemplate> DefaultSosigs = [];
        public static Dictionary<string, int> SosigIDDict = [];

        public static int NewSosigID = 30000;

        public static Dictionary<string, Sprite> DefaultIconSprites = [];

        /// <summary>
        /// Takes a custom SosigTemplate object, and adds it to the necessary dictionaries. This method assumes that you are sending a template for a custom sosig, and that it should be given a new the SosigEnemyID
        /// </summary>
        /// <param name="template">A template for a custom sosig (Loaded at runtime)</param>
        public static void AddSosigTemplate(SosigTemplate template)
        {
            SosigEnemyTemplate realTemplate = template.GetSosigEnemyTemplate();

            // Since this template is for a custom sosig, we should give it a brand new SosigEnemyID
            if (!SosigIDDict.ContainsKey(template.SosigEnemyID))
            {
                SosigIDDict.Add(template.SosigEnemyID, NewSosigID);
                NewSosigID++;
            }
            else
            {
                TNHFrameworkLogger.LogError("Loaded sosig had same SosigEnemyID as another sosig -- SosigEnemyID : " + template.SosigEnemyID);
                return;
            }

            // Now fill out the SosigEnemyIDs values for the real sosig template (These will effectively be ints, but this is ok since enums are just ints in disguise)
            realTemplate.SosigEnemyID = (SosigEnemyID)SosigIDDict[template.SosigEnemyID];

            // Finally, add the templates to our global dictionary
            CustomSosigs.Add(template);
            LoadedSosigsDict.Add(realTemplate, template);

            TNHFrameworkLogger.Log("Sosig added successfully : " + template.DisplayName, TNHFrameworkLogger.LogType.Character);
        }

        public static void AddSosigTemplate(SosigEnemyTemplate realTemplate)
        {
            SosigTemplate template = new(realTemplate);

            // This template is from a sosig that already has a valid SosigEnemyID, so we can just add that to the dictionary casted as an int
            if (!SosigIDDict.ContainsKey(template.SosigEnemyID))
            {
                SosigIDDict.Add(template.SosigEnemyID, (int)realTemplate.SosigEnemyID);
            }
            else
            {
                TNHFrameworkLogger.LogError("Loaded sosig had same SosigEnemyID as another sosig -- SosigEnemyID : " + template.SosigEnemyID);
                return;
            }

            // Since the real template already had a valid SosigEnemyID, we can skip the part where we reassign them
            DefaultSosigs.Add(realTemplate);
            LoadedSosigsDict.Add(realTemplate, template);

            TNHFrameworkLogger.Log("Sosig added successfully : " + template.DisplayName, TNHFrameworkLogger.LogType.Character);
        }

        public static void AddCharacterTemplate(CustomCharacter template, Sprite thumbnail)
        {
            template.isCustom = true;
            template.TableID = "TNHF_" + template.TableID;
            
            TNH_CharacterDef charDef = template.GetCharacter(thumbnail);

            if (LoadedCharacterDict.ContainsKey(charDef.UgcId))
            {
                TNHFrameworkLogger.Log($"Character already exists ({charDef.UgcId} [{template.TableID}]) : " + template.DisplayName, TNHFrameworkLogger.LogType.General);
                return;
            }

            CustomCharacters.Add(template);
            LoadedCharacterDict.Add(charDef.UgcId, new CharacterTemplate(charDef, template));

            for (int i = 0; i < template.EquipmentPools.Count; i++)
            {
                EquipmentPoolDictionary.Add(template.EquipmentPools[i].GetPoolEntry(charDef.UgcId, i, "EquipmentPool"), template.EquipmentPools[i]);
            }

            TNHFrameworkLogger.Log($"Character added successfully ({charDef.UgcId} [{template.TableID}]) : " + template.DisplayName, TNHFrameworkLogger.LogType.General);
        }

        public static void AddCharacterTemplate(TNH_CharacterDef charDef)
        {
            if (LoadedCharacterDict.ContainsKey(charDef.UgcId))
            {
                TNHFrameworkLogger.Log($"Character already exists ({charDef.UgcId} [{charDef.TableID}]) : " + charDef.DisplayName, TNHFrameworkLogger.LogType.General);
                return;
            }

            CustomCharacter template = new(charDef);

            DefaultCharacters.Add(template);
            LoadedCharacterDict.Add(charDef.UgcId, new CharacterTemplate(charDef, template));

            for (int i = 0; i < template.EquipmentPools.Count; i++)
            {
                EquipmentPoolDef.PoolEntry poolEntry = template.EquipmentPools[i].GetPoolEntry(charDef.UgcId, i, "EquipmentPool");

                // Must check for this, since default characters can have references to the same pools
                if (!EquipmentPoolDictionary.ContainsKey(poolEntry))
                {
                    EquipmentPoolDictionary.Add(poolEntry, template.EquipmentPools[i]);
                }
            }

            TNHFrameworkLogger.Log($"Character added successfully ({charDef.UgcId} [{charDef.TableID}]) : " + charDef.DisplayName, TNHFrameworkLogger.LogType.General);
        }

        public static void AddVaultFile(VaultFile template)
        {
            if (!LoadedVaultFiles.ContainsKey(template.FileName))
            {
                LoadedVaultFiles.Add(template.FileName, template);
            }
        }

        public static void AddVaultFile(SavedGunSerializable template)
        {
            if (!LoadedLegacyVaultFiles.ContainsKey(template.FileName))
            {
                LoadedLegacyVaultFiles.Add(template.FileName, template);
            }
        }
    }
}
