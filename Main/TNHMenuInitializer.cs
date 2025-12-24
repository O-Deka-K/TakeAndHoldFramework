using FistVR;
using MagazinePatcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TNHFramework.ObjectTemplates;
using TNHFramework.Patches;
using TNHFramework.Utilities;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;

namespace TNHFramework
{
    public static class TNHMenuInitializer
    {
        public static bool TNHInitialized = false;
        public static bool MagazineCacheFailed = false;
        public static List<TNH_CharacterDef> SavedCharacters;

        private static readonly MethodInfo miGetCatFolderName = typeof(VaultSystem).GetMethod("GetCatFolderName", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetSubcatFolderName = typeof(VaultSystem).GetMethod("GetSubcatFolderName", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo miGetSuffix = typeof(VaultSystem).GetMethod("GetSuffix", BindingFlags.Static | BindingFlags.NonPublic);

        public static IEnumerator InitializeTNHMenuAsync(string path, Text progressText, Text itemsText, SceneLoader hotdog, List<TNH_UIManager.CharacterCategory> Categories, TNH_CharacterDatabase CharDatabase, TNH_UIManager instance, bool outputFiles)
        {
            hotdog?.gameObject.SetActive(false);

            bool isOtherLoaderLoaded;
            bool isMagPatcherLoaded;
            try
            {
                PokeOtherloader();
                isOtherLoaderLoaded = true;
            }
            catch
            {
                isOtherLoaderLoaded = false;
                TNHFrameworkLogger.LogWarning("OtherLoader not found. If you are using OtherLoader, please ensure you have version 0.1.6 or later!");
            }

            // First thing we want to do is wait for all asset bundles to be loaded in
            float itemLoadProgress = 0;
            do
            {
                yield return null;
                itemLoadProgress = AsyncLoadMonitor.GetProgress();

                if (isOtherLoaderLoaded)
                {
                    itemLoadProgress = Mathf.Min(itemLoadProgress, GetOtherLoaderProgress());
                    itemsText.text = GetLoadingItems();
                }

                progressText.text = $"LOADING ITEMS : {itemLoadProgress * 100:0.0}%";
            }
            while (itemLoadProgress < 1);

            try
            {
                PokeMagPatcher();
                isMagPatcherLoaded = true;
                TNHFrameworkLogger.LogWarning("MagazinePatcher is detected.");
            }
            catch
            {
                isMagPatcherLoaded = false;
                TNHFrameworkLogger.LogWarning("MagazinePatcher not found.");
            }

            // Now we wait for magazine caching to be done
            if (isMagPatcherLoaded)
            {
                float cachingProgress;
                do
                {
                    yield return null;

                    cachingProgress = PokeMagPatcher();
                    itemsText.text = GetMagPatcherCacheLog();
                    progressText.text = $"CACHING ITEMS : {cachingProgress * 100:0.0}%";
                }
                while (cachingProgress < 1);
            }
            else if (TNHFramework.InternalMagPatcher.Value) // Honey, we have Magazine Patcher at home.
            {
                TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Internal Mag Patcher started!", TNHFrameworkLogger.LogType.General);
                InternalMagPatcher();
                TNHFrameworkLogger.Log($"[{DateTime.Now:HH:mm:ss}] Internal Mag Patcher finished!", TNHFrameworkLogger.LogType.General);
            }

            if (TNHFramework.FixModAttachmentTags.Value)  // ODK
                TNHFrameworkUtils.FixModAttachmentTags();

            // Now perform final steps of loading characters
            LoadTNHTemplates(CharDatabase);
            SavedCharacters = CharDatabase.Characters;

            if (outputFiles)
            {
                CreateTNHFiles(path);
            }

            TNHInitialized = true;
            UIManagerPatches.RefreshTNHUI(instance, Categories, CharDatabase);

            itemsText.text = "";
            progressText.text = "";
            hotdog?.gameObject.SetActive(true);
        }

        public static void PokeOtherloader()
        {
            OtherLoader.LoaderStatus.GetLoaderProgress();
            List<string> items = OtherLoader.LoaderStatus.LoadingItems;
        }

        public static float GetOtherLoaderProgress()
        {
            return OtherLoader.LoaderStatus.GetLoaderProgress();
        }

        public static string GetLoadingItems()
        {
            List<string> loading = OtherLoader.LoaderStatus.LoadingItems;

            for (int i = 0; i < loading.Count; i++)
            {
                string colorHex = ColorUtility.ToHtmlStringRGBA(new Color(0.5f, 0.5f, 0.5f, Mathf.Clamp(((float)loading.Count - i) / loading.Count, 0, 1)));
                loading[i] = "<color=#" + colorHex + ">Loading Assets (" + loading[i] + ")</color>";
            }

            loading.Reverse();

            return string.Join("\n", [.. loading]);
        }

        public static float PokeMagPatcher()
        {
            return PatcherStatus.PatcherProgress;
        }

        public static string GetMagPatcherCacheLog()
        {
            return PatcherStatus.CacheLog;
        }

        public static void InternalMagPatcher()
        {
            List<FVRObject> gunsToIterate = [];

            foreach (KeyValuePair<string, FVRObject> item in IM.OD)
            {
                if (item.Value.Category == FVRObject.ObjectCategory.Firearm)
                {
                    gunsToIterate.Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Cartridge)
                {
                    if (!TNHFramework.CartridgeDictionary.ContainsKey(item.Value.RoundType))
                        TNHFramework.CartridgeDictionary.Add(item.Value.RoundType, []);

                    TNHFramework.CartridgeDictionary[item.Value.RoundType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Magazine)
                {
                    if (!TNHFramework.MagazineDictionary.ContainsKey(item.Value.MagazineType))
                        TNHFramework.MagazineDictionary.Add(item.Value.MagazineType, []);

                    TNHFramework.MagazineDictionary[item.Value.MagazineType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.Clip)
                {
                    if (!TNHFramework.StripperDictionary.ContainsKey(item.Value.ClipType))
                        TNHFramework.StripperDictionary.Add(item.Value.ClipType, []);

                    TNHFramework.StripperDictionary[item.Value.ClipType].Add(item.Value);
                }
                else if (item.Value.Category == FVRObject.ObjectCategory.SpeedLoader)
                {
                    if (!TNHFramework.SpeedloaderDictionary.ContainsKey(item.Value.RoundType))
                        TNHFramework.SpeedloaderDictionary.Add(item.Value.RoundType, []);

                    // Metadata fix
                    if (item.Value.ItemID == "Speedloader12gauge_5Shot")
                        item.Value.MagazineCapacity = 5;

                    TNHFramework.SpeedloaderDictionary[item.Value.RoundType].Add(item.Value);
                }
            }

            foreach (FVRObject firearm in gunsToIterate)
            {
                FVRFireArm firearmComp = null;
                FireArmClipType clipType = firearm.ClipType;
                FireArmRoundType roundType = firearm.RoundType;
                FireArmMagazineType magazineType = firearm.MagazineType;
                int magazineCapacity = firearm.MagazineCapacity;

                // If the data is mostly zeroes, get the FVRFireArm component
                if (!ValidFireArm(roundType, clipType, magazineType, 0))
                {
                    // Some muzzle loaded vanilla guns should be skipped
                    if (!firearm.IsModContent && firearm.TagFirearmAction == FVRObject.OTagFirearmAction.OpenBreach && firearm.TagFirearmFeedOption.Contains(FVRObject.OTagFirearmFeedOption.BreachLoad))
                        continue;

                    TNHFrameworkLogger.Log($"Loading firearm {firearm.DisplayName} [Mod = {firearm.IsModContent}]", TNHFrameworkLogger.LogType.General);

                    GameObject gameObject = firearm.GetGameObject();
                    if (gameObject != null)
                        firearmComp = gameObject.GetComponent<FVRFireArm>();

                    if (firearmComp != null)
                    {
                        roundType = firearmComp.RoundType;
                        magazineType = firearmComp.MagazineType;
                        clipType = firearmComp.ClipType;
                    }
                }

                // If it's still mostly zeroes, skip it, otherwise stuff like the Graviton Beamer gets .22LR ammo
                if (!ValidFireArm(roundType, clipType, magazineType, magazineCapacity))
                {
                    TNHFrameworkLogger.Log($"Firearm {firearm.DisplayName} skipped!", TNHFrameworkLogger.LogType.General);
                    continue;
                }

                if ((firearm.CompatibleSingleRounds == null || !firearm.CompatibleSingleRounds.Any()) &&
                    TNHFramework.CartridgeDictionary.ContainsKey(roundType))
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new rounds of type {roundType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleSingleRounds = TNHFramework.CartridgeDictionary[roundType];
                }

                if ((firearm.CompatibleMagazines == null || !firearm.CompatibleMagazines.Any()) &&
                    TNHFramework.MagazineDictionary.ContainsKey(magazineType) &&
                    magazineType != FireArmMagazineType.mNone)
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new magazines of type {magazineType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleMagazines = TNHFramework.MagazineDictionary[magazineType];
                }

                if ((firearm.CompatibleClips == null || !firearm.CompatibleClips.Any()) &&
                    TNHFramework.StripperDictionary.ContainsKey(clipType) &&
                    clipType != FireArmClipType.None)
                {
                    TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new clips of type {clipType}", TNHFrameworkLogger.LogType.General);
                    firearm.CompatibleClips = TNHFramework.StripperDictionary[clipType];
                }

                if ((firearm.CompatibleSpeedLoaders == null || !firearm.CompatibleSpeedLoaders.Any()) &&
                    firearm.TagFirearmAction == FVRObject.OTagFirearmAction.Revolver)
                {
                    if (firearmComp == null)
                    {
                        GameObject gameObject = firearm.GetGameObject();

                        if (gameObject != null)
                            firearmComp = gameObject.GetComponent<FVRFireArm>();
                    }

                    if (firearmComp != null)
                    {
                        roundType = firearmComp.RoundType;

                        // The Revolver component has the real chamber capacity
                        Revolver revolverComp = firearmComp.gameObject.GetComponent<Revolver>();
                        if (revolverComp != null)
                            magazineCapacity = revolverComp.Chambers.Length;
                    }

                    if (TNHFramework.SpeedloaderDictionary.ContainsKey(roundType))
                    {
                        foreach (FVRObject speedloader in TNHFramework.SpeedloaderDictionary[roundType])
                        {
                            if (speedloader.MagazineCapacity == magazineCapacity)
                            {
                                TNHFrameworkLogger.Log($"Giving firearm {firearm.DisplayName} new speedloader of type {roundType}", TNHFrameworkLogger.LogType.General);
                                firearm.CompatibleSpeedLoaders.Add(speedloader);
                            }
                        }
                    }
                }
            }
        }

        public static bool ValidFireArm(FireArmRoundType roundType, FireArmClipType clipType, FireArmMagazineType magazineType, int magazineCapacity)
        {
            return roundType != FireArmRoundType.a22_LR || magazineType != FireArmMagazineType.mNone || magazineCapacity != 0 || clipType != FireArmClipType.None;
        }

        public static void LoadTNHTemplates(TNH_CharacterDatabase CharDatabase)
        {
            TNHFrameworkLogger.Log("Performing TNH Initialization", TNHFrameworkLogger.LogType.General);

            // Load all of the default templates into our dictionaries
            TNHFrameworkLogger.Log("Adding default sosigs to template dictionary", TNHFrameworkLogger.LogType.General);
            LoadDefaultSosigs();
            TNHFrameworkLogger.Log("Adding default characters to template dictionary", TNHFrameworkLogger.LogType.General);
            LoadDefaultCharacters(CharDatabase.Characters);

            LoadedTemplateManager.DefaultIconSprites = GetAllIcons(LoadedTemplateManager.DefaultCharacters);

            TNHFrameworkLogger.Log("Delayed Init of default characters", TNHFrameworkLogger.LogType.General);
            InitCharacters(LoadedTemplateManager.DefaultCharacters);

            TNHFrameworkLogger.Log("Delayed Init of custom characters", TNHFrameworkLogger.LogType.General);
            InitCharacters(LoadedTemplateManager.CustomCharacters);

            TNHFrameworkLogger.Log("Delayed Init of custom sosigs", TNHFrameworkLogger.LogType.General);
            InitSosigs(LoadedTemplateManager.CustomSosigs);
        }

        /// <summary>
        /// Loads all default sosigs into the template manager
        /// </summary>
        private static void LoadDefaultSosigs()
        {
            foreach (SosigEnemyTemplate sosig in ManagerSingleton<IM>.Instance.odicSosigObjsByID.Values)
            {
                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
        }

        /// <summary>
        /// Loads all default characters into the template manager
        /// </summary>
        /// <param name="characters">A list of TNH characters</param>
        private static void LoadDefaultCharacters(List<TNH_CharacterDef> characters)
        {
            foreach (TNH_CharacterDef character in characters)
            {
                LoadedTemplateManager.AddCharacterTemplate(character);
            }
        }

        public static Dictionary<string, Sprite> GetAllIcons(List<CustomCharacter> characters)
        {
            Dictionary<string, Sprite> icons = [];

            foreach (CustomCharacter character in characters)
            {
                foreach (EquipmentPoolDef.PoolEntry pool in character.GetCharacter().EquipmentPool.Entries)
                {
                    if (!icons.ContainsKey(pool.TableDef.Icon.name))
                    {
                        TNHFrameworkLogger.Log("Icon found (" + pool.TableDef.Icon.name + ")", TNHFrameworkLogger.LogType.Character);
                        icons.Add(pool.TableDef.Icon.name, pool.TableDef.Icon);
                    }
                }
            }

            return icons;
        }

        /// <summary>
        /// Performs a delayed init on the sent list of custom characters, and removes any characters that failed to init
        /// </summary>
        /// <param name="characters"></param>
        private static void InitCharacters(List<CustomCharacter> characters)
        {
            for (int i = characters.Count - 1; i >= 0; i--)
            {
                CustomCharacter character = characters[i];

                try
                {
                    character.DelayedInit();
                }
                catch (Exception e)
                {
                    TNHFrameworkLogger.LogError("Failed to load character: " + character.DisplayName + ". Error Output:\n" + e.ToString());
                    characters.RemoveAt(i);
                    var item = LoadedTemplateManager.LoadedCharacterDict.Single(o => o.Value.Custom == character).Value;
                    LoadedTemplateManager.LoadedCharacterDict.Remove(item.Def.CharacterID);
                }
            }
        }

        /// <summary>
        /// Performs a delayed init on the sent list of sosigs. If a sosig fails to init, any character using that sosig will be removed
        /// </summary>
        /// <param name="sosigs"></param>
        private static void InitSosigs(List<SosigTemplate> sosigs)
        {
            for (int i = 0; i < sosigs.Count; i++)
            {
                SosigTemplate sosig = sosigs[i];

                try
                {
                    sosig.DelayedInit();
                }
                catch (Exception e)
                {
                    TNHFrameworkLogger.LogError("Failed to load sosig: " + sosig.DisplayName + ". Error Output:\n" + e.ToString());

                    // Find any characters that use this sosig, and remove them
                    KeyValuePair<TNH_Char, CharacterTemplate>[] removeList = [.. LoadedTemplateManager.LoadedCharacterDict.Where(o => o.Value.Custom.CharacterUsesSosig(sosig.SosigEnemyID))];
                    foreach (KeyValuePair<TNH_Char, CharacterTemplate> item in removeList)
                    {
                        TNHFrameworkLogger.LogError("Removing character that used removed sosig: " + item.Value.Custom.DisplayName);
                        LoadedTemplateManager.LoadedCharacterDict.Remove(item.Key);
                    }
                }
            }
        }

        public static void CreateTNHFiles(string path)
        {
            // Create files relevant for character creation
            TNHFrameworkLogger.Log("Creating character creation files", TNHFrameworkLogger.LogType.General);
            CreateSosigTemplateFiles(LoadedTemplateManager.DefaultSosigs, path);
            CreateSosigTemplateFiles(LoadedTemplateManager.CustomSosigs, path);
            CreateCharacterFiles(LoadedTemplateManager.DefaultCharacters, path, false);
            CreateCharacterFiles(LoadedTemplateManager.CustomCharacters, path, true);
            CreateIconIDFile(path, [.. LoadedTemplateManager.DefaultIconSprites.Keys]);
            CreateObjectIDFile(path);
            CreateSosigIDFile(path);
            CreateJsonVaultFiles(path);
            CreateGeneratedTables(path);
            CreatePopulatedCharacterTemplate(path);
        }

        public static string CleanFilename(string filename)
        {
            // Remove illegal characters
            return Regex.Replace(filename, @"(<|>|:|""|/|\\|\||\?|\*)", "");
        }

        public static void CreateSosigTemplateFiles(List<SosigEnemyTemplate> sosigs, string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating default sosig template files", TNHFrameworkLogger.LogType.File);

                path += "/DefaultSosigTemplates";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                foreach (SosigEnemyTemplate template in sosigs)
                {
                    string jsonPath = path + "/" + CleanFilename(template.SosigEnemyID + ".json");

                    if (File.Exists(jsonPath))
                        File.Delete(jsonPath);

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(jsonPath))
                    {
                        SosigTemplate sosig = new(template);
                        string sosigString = JsonConvert.SerializeObject(sosig, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(sosigString);
                        sw.Close();
                    }

                    string yamlPath = path + "/" + CleanFilename(template.SosigEnemyID + ".yaml");

                    if (File.Exists(yamlPath))
                        File.Delete(yamlPath);

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(yamlPath))
                    {
                        var serializer = new SerializerBuilder()
                            .WithIndentedSequences()
                            .Build();

                        SosigTemplate sosig = new(template);
                        string sosigString = serializer.Serialize(sosig);

                        sw.WriteLine(sosigString);
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateSosigTemplateFiles(List<SosigTemplate> sosigs, string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating custom sosig template files", TNHFrameworkLogger.LogType.File);

                path += "/CustomSosigTemplates";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                foreach (SosigTemplate template in sosigs)
                {
                    string jsonPath = path + "/" + CleanFilename(template.SosigEnemyID + ".json");

                    if (File.Exists(jsonPath))
                        File.Delete(jsonPath);

                    // Create a new file
                    using (StreamWriter sw = File.CreateText(jsonPath))
                    {
                        string sosigString = JsonConvert.SerializeObject(template, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(sosigString);
                        sw.Close();
                    }

                    string yamlPath = path + "/" + CleanFilename(template.SosigEnemyID + ".yaml");

                    if (File.Exists(yamlPath))
                        File.Delete(yamlPath);

                    // Create a new file
                    using (StreamWriter sw = File.CreateText(yamlPath))
                    {
                        var serializer = new SerializerBuilder()
                            .WithIndentedSequences()
                            .Build();

                        string sosigString = serializer.Serialize(template);
                        sw.WriteLine(sosigString);
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateCharacterFiles(List<CustomCharacter> characters, string path, bool isCustom)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating " + (isCustom ? "custom" : "default") + " character template files", TNHFrameworkLogger.LogType.File);

                path += isCustom ? "/CustomCharacters" : "/DefaultCharacters";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                foreach (CustomCharacter charDef in characters)
                {
                    string jsonPath = path + "/" + CleanFilename(charDef.DisplayName + ".json");

                    if (File.Exists(jsonPath))
                        File.Delete(jsonPath);

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(jsonPath))
                    {
                        string characterString = JsonConvert.SerializeObject(charDef, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(characterString);
                        sw.Close();
                    }

                    string yamlPath = path + "/" + CleanFilename(charDef.DisplayName + ".yaml");

                    TNHFrameworkLogger.Log("Creating character template file: " + yamlPath, TNHFrameworkLogger.LogType.File);  // ODK - DEBUG
                    if (File.Exists(yamlPath))
                        File.Delete(yamlPath);

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(yamlPath))
                    {
                        var serializer = new SerializerBuilder()
                            .WithIndentedSequences()
                            .Build();

                        string characterString = serializer.Serialize(charDef);
                        sw.WriteLine(characterString);
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateIconIDFile(string path, List<string> icons)
        {
            try
            {
                if (File.Exists(path + "/IconIDs.txt"))
                {
                    File.Delete(path + "/IconIDs.txt");
                }

                // Create a new file     
                using (StreamWriter sw = File.CreateText(path + "/IconIDs.txt"))
                {
                    sw.WriteLine("#Available Icons for equipment pools");
                    foreach (string icon in icons)
                    {
                        sw.WriteLine(icon);
                    }
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateObjectIDFile(string path)
        {
            try
            {
                if (File.Exists(path + "/ObjectIDs.csv"))
                {
                    File.Delete(path + "/ObjectIDs.csv");
                }

                // Create a new file     
                using (StreamWriter sw = File.CreateText(path + "/ObjectIDs.csv"))
                {
                    sw.WriteLine("DisplayName,ObjectID,Mod Content,Category,Era,Set,Country of Origin,Attachment Feature,Firearm Action,Firearm Feed Option,Firing Modes,Firearm Mounts,Attachment Mount,Round Power,Size,Melee Handedness,Melee Style,Powerup Type,Thrown Damage Type,Thrown Type");

                    foreach (FVRObject obj in IM.OD.Values)
                    {
                        sw.WriteLine(
                            obj.DisplayName.Replace(",", ".") + "," +
                            obj.ItemID.Replace(",", ".") + "," +
                            obj.IsModContent.ToString() + "," +
                            obj.Category + "," +
                            obj.TagEra + "," +
                            obj.TagSet + "," +
                            obj.TagFirearmCountryOfOrigin + "," +
                            obj.TagAttachmentFeature + "," +
                            obj.TagFirearmAction + "," +
                            string.Join("+", [.. obj.TagFirearmFeedOption.Select(o => o.ToString())]) + "," +
                            string.Join("+", [.. obj.TagFirearmFiringModes.Select(o => o.ToString())]) + "," +
                            string.Join("+", [.. obj.TagFirearmMounts.Select(o => o.ToString())]) + "," +
                            obj.TagAttachmentMount + "," +
                            obj.TagFirearmRoundPower + "," +
                            obj.TagFirearmSize + "," +
                            obj.TagMeleeHandedness + "," +
                            obj.TagMeleeStyle + "," +
                            obj.TagPowerupType + "," +
                            obj.TagThrownDamageType + "," +
                            obj.TagThrownType);
                    }
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateSosigIDFile(string path)
        {
            try
            {
                if (File.Exists(path + "/SosigIDs.txt"))
                {
                    File.Delete(path + "/SosigIDs.txt");
                }

                // Create a new file     
                using (StreamWriter sw = File.CreateText(path + "/SosigIDs.txt"))
                {
                    sw.WriteLine("#Available Sosig IDs for spawning");

                    List<string> sosigList = [.. LoadedTemplateManager.SosigIDDict.Keys];
                    sosigList.Sort();

                    foreach (string ID in sosigList)
                    {
                        sw.WriteLine(ID);
                    }
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateJsonVaultFiles(string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating JSON vault files", TNHFrameworkLogger.LogType.File);

                path += "/VaultFiles";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string[] vaultFiles = ES2.GetFiles(string.Empty, "*.txt");
                List<SavedGunSerializable> savedGuns = [];
                foreach (string name in vaultFiles)
                {
                    try
                    {
                        if (name.Contains("DONTREMOVETHISPARTOFFILENAMEV02a") && ES2.Exists(name))
                        {
                            using (ES2Reader reader = ES2Reader.Create(name))
                            {
                                SavedGun savedGun = reader.Read<SavedGun>("SavedGun");
                                savedGuns.Add(new SavedGunSerializable(savedGun));
                            }
                        }
                    }
                    catch
                    {
                        TNHFrameworkLogger.LogWarning($"Vault File {name} could not be loaded");
                    }
                }

                foreach (SavedGunSerializable savedGun in savedGuns)
                {
                    string jsonPath = path + "/" + CleanFilename(savedGun.FileName + ".json");

                    if (File.Exists(jsonPath))
                        File.Delete(jsonPath);

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(jsonPath))
                    {
                        string characterString = JsonConvert.SerializeObject(savedGun, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(characterString);
                        sw.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }

        public static void CreateGeneratedTables(string path)
        {
            try
            {
                path += "/GeneratedEquipmentPools";

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var characters = LoadedTemplateManager.LoadedCharacterDict.Select(o => o.Value.Custom);
                foreach (CustomCharacter character in characters)
                {
                    string txtPath = path + "/" + CleanFilename(character.DisplayName + ".txt");

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(txtPath))
                    {
                        sw.WriteLine("Primary Starting Weapon");
                        if (character.PrimaryWeapon != null)
                        {
                            sw.WriteLine(character.PrimaryWeapon.ToString());
                        }

                        sw.WriteLine("\n\nSecondary Starting Weapon");
                        if (character.SecondaryWeapon != null)
                        {
                            sw.WriteLine(character.SecondaryWeapon.ToString());
                        }

                        sw.WriteLine("\n\nTertiary Starting Weapon");
                        if (character.TertiaryWeapon != null)
                        {
                            sw.WriteLine(character.TertiaryWeapon.ToString());
                        }

                        sw.WriteLine("\n\nPrimary Starting Item");
                        if (character.PrimaryItem != null)
                        {
                            sw.WriteLine(character.PrimaryItem.ToString());
                        }

                        sw.WriteLine("\n\nSecondary Starting Item");
                        if (character.SecondaryItem != null)
                        {
                            sw.WriteLine(character.SecondaryItem.ToString());
                        }

                        sw.WriteLine("\n\nTertiary Starting Item");
                        if (character.TertiaryItem != null)
                        {
                            sw.WriteLine(character.TertiaryItem.ToString());
                        }

                        sw.WriteLine("\n\nStarting Shield");
                        if (character.Shield != null)
                        {
                            sw.WriteLine(character.Shield.ToString());
                        }

                        foreach (EquipmentPool pool in character.EquipmentPools)
                        {
                            sw.WriteLine("\n\n" + pool.ToString());
                        }

                        sw.Close();
                    }
                }
            }
            catch
            {
                //Debug.LogError(ex.ToString());
            }
        }

        public static void CreatePopulatedCharacterTemplate(string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating populated character template file", TNHFrameworkLogger.LogType.File);

                path += "/PopulatedCharacterTemplate.json";

                if (!File.Exists(path))
                    File.Delete(path);

                using (StreamWriter sw = File.CreateText(path))
                {
                    CustomCharacter character = new();
                    string characterString = JsonConvert.SerializeObject(character, Formatting.Indented, new StringEnumConverter());
                    sw.WriteLine(characterString);
                    sw.Close();
                }
            }
            catch (Exception ex)
            {
                TNHFrameworkLogger.LogError(ex.ToString());
            }
        }
    }
}
