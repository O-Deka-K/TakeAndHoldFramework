﻿using ADepIn;
using Deli.VFS;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TNHFramework.ObjectTemplates;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;

namespace TNHFramework.Utilities
{
    static class TNHFrameworkUtils
    {
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
                            obj.DisplayName.Replace(",", ".") + "," +  // ODK - Added
                            obj.ItemID.Replace(",", ".") + "," + 
                            obj.IsModContent.ToString() + "," +  // ODK - Added
                            obj.Category + "," +
                            obj.TagEra + "," +
                            obj.TagSet + "," +
                            obj.TagFirearmCountryOfOrigin + "," +
                            obj.TagAttachmentFeature + "," +
                            obj.TagFirearmAction + "," +
                            string.Join("+", obj.TagFirearmFeedOption.Select(o => o.ToString()).ToArray()) + "," +
                            string.Join("+", obj.TagFirearmFiringModes.Select(o => o.ToString()).ToArray()) + "," +
                            string.Join("+", obj.TagFirearmMounts.Select(o => o.ToString()).ToArray()) + "," +
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

        public static void RunCoroutine(IEnumerator routine, Action<Exception> onError = null)
        {
            AnvilManager.Run(RunAndCatch(routine, onError));
        }

        public static IEnumerator RunAndCatch(IEnumerator routine, Action<Exception> onError = null)
        {
            bool more = true;
            while (more)
            {
                try
                {
                    more = routine.MoveNext();
                }
                catch (Exception e)
                {
                    if (onError != null)
                    {
                        onError(e);
                    }

                    yield break;
                }

                if (more)
                {
                    yield return routine.Current;
                }
            }
        }


        public static Dictionary<string, Sprite> GetAllIcons(List<CustomCharacter> characters)
        {
            Dictionary<string, Sprite> icons = [];

            foreach(CustomCharacter character in characters)
            {
                foreach(EquipmentPoolDef.PoolEntry pool in character.GetCharacter().EquipmentPool.Entries)
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


        public static void CreateDefaultCharacterFiles(List<CustomCharacter> characters, string path)
        {

            try
            {
                TNHFrameworkLogger.Log("Creating default character template files", TNHFrameworkLogger.LogType.File);

                path = path + "/DefaultCharacters";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                
                foreach (CustomCharacter charDef in characters)
                {
                    if (File.Exists(path + "/" + charDef.DisplayName + ".json"))
                    {
                        File.Delete(path + "/" + charDef.DisplayName + ".json");
                    }

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(path + "/" + charDef.DisplayName + ".json"))
                    {
                        string characterString = JsonConvert.SerializeObject(charDef, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(characterString);
                        sw.Close();
                    }

                    if (File.Exists(path + "/" + charDef.DisplayName + ".yaml"))
                    {
                        File.Delete(path + "/" + charDef.DisplayName + ".yaml");
                    }

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(path + "/" + charDef.DisplayName + ".yaml"))
                    {
                        // i am learning this yaml stuff. it is goofy.
                        var serializerBuilder = new SerializerBuilder();

                        serializerBuilder.WithIndentedSequences();
                        foreach (KeyValuePair<string, Type> thing in TNHFramework.Serializables)
                        {
                            serializerBuilder.WithTagMapping(thing.Key, thing.Value);
                        }
                        var serializer = serializerBuilder.Build();
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


        public static void CreatePopulatedCharacterTemplate(string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating populated character template file", TNHFrameworkLogger.LogType.File);

                path = path + "/PopulatedCharacterTemplate.json";

                if (!File.Exists(path))
                {
                    File.Delete(path);
                }

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


        public static void CreateDefaultSosigTemplateFiles(List<SosigEnemyTemplate> sosigs, string path)
        {
            try
            {
                TNHFrameworkLogger.Log("Creating default sosig template files", TNHFrameworkLogger.LogType.File);

                path = path + "/DefaultSosigTemplates";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                foreach (SosigEnemyTemplate template in sosigs)
                {
                    if (File.Exists(path + "/" + template.SosigEnemyID + ".json"))
                    {
                        File.Delete(path + "/" + template.SosigEnemyID + ".json");
                    }

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(path + "/" + template.SosigEnemyID + ".json"))
                    {
                        SosigTemplate sosig = new(template);
                        string characterString = JsonConvert.SerializeObject(sosig, Formatting.Indented, new StringEnumConverter());
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

        public static void CreateJsonVaultFiles(string path)
        {
            try
            {
                path = path + "/VaultFiles";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string[] vaultFiles = ES2.GetFiles(string.Empty, "*.txt");
                List<SavedGunSerializable> savedGuns = [];
                foreach(string name in vaultFiles)
                {
                    try
                    {
                        if (name.Contains("DONTREMOVETHISPARTOFFILENAMEV02a"))
                        {
                            if (ES2.Exists(name))
                            {
                                using (ES2Reader reader = ES2Reader.Create(name))
                                {
                                    savedGuns.Add(new SavedGunSerializable(reader.Read<SavedGun>("SavedGun")));
                                }
                            }
                        }
                    }
                    catch
                    {
                        TNHFrameworkLogger.LogError("Vault File could not be loaded");
                    }
                }
                
                foreach (SavedGunSerializable savedGun in savedGuns)
                {
                    if (File.Exists(path + "/" + savedGun.FileName + ".json"))
                    {
                        File.Delete(path + "/" + savedGun.FileName + ".json");
                    }

                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(path + "/" + savedGun.FileName + ".json"))
                    {
                        string characterString = JsonConvert.SerializeObject(savedGun, Formatting.Indented, new StringEnumConverter());
                        sw.WriteLine(characterString);
                        sw.Close();
                    }
                }

                var mode = ItemSpawnerV2.VaultFileDisplayMode.SingleObjects;
                string[] vaultFileList = VaultSystem.GetFileListForDisplayMode(mode, CynJsonSortingMode.Alphabetical);

                string vaultPath = Path.Combine(CynJson.GetOrCreateH3VRDataPath(), VaultSystem.rootFolderName);
                vaultPath = Path.Combine(vaultPath, VaultSystem.GetCatFolderName(mode));
                vaultPath = Path.Combine(vaultPath, VaultSystem.GetSubcatFolderName(mode));

                foreach (string vaultFileName in vaultFileList)
                {
                    string filename = vaultFileName + VaultSystem.GetSuffix(mode);

                    try
                    {
                        File.Copy(Path.Combine(vaultPath, filename), Path.Combine(path, filename), true);
                    }

                    catch (Exception ex)
                    {
                        TNHFrameworkLogger.LogError($"Vault File {filename} could not be copied: {ex}");
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
                path = path + "/GeneratedEquipmentPools";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }


                foreach(CustomCharacter character in LoadedTemplateManager.LoadedCharactersDict.Values)
                {
                    // Create a new file     
                    using (StreamWriter sw = File.CreateText(path + "/" + character.DisplayName + ".txt"))
                    {
                        sw.WriteLine("Primary Starting Weapon");
                        if(character.PrimaryWeapon != null)
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

                        foreach(EquipmentPool pool in character.EquipmentPools)
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


        public static bool VaultFileComponentsLoaded(VaultFile template)
        {
            bool result = true;
            List<string> missing = [];

            foreach (VaultObject vaultObject in template.Objects)
            {
                foreach (VaultElement vaultElement in vaultObject.Elements)
                {
                    if (!IM.OD.ContainsKey(vaultElement.ObjectID))
                    {
                        missing.Add(vaultElement.ObjectID);
                        result = false;
                    }
                }
            }

            if (!result)
                TNHFrameworkLogger.LogWarning($"Vaulted gun in table does not have all components loaded, removing it! VaultID: {template.FileName}, Missing ID(s): {string.Join(", ", [.. missing])}");

            return result;
        }


        public static void RemoveUnloadedObjectIDs(EquipmentGroup group)
        {
            if (group.IDOverride != null)
            {
                for (int i = 0; i < group.IDOverride.Count; i++)
                {
                    if (!IM.OD.ContainsKey(group.IDOverride[i]))
                    {
                        //If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                        if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(group.IDOverride[i]))
                        {
                            if (!LoadedTemplateManager.LoadedLegacyVaultFiles[group.IDOverride[i]].AllComponentsLoaded())
                            {
                                group.IDOverride.RemoveAt(i);
                                i--;
                            }
                        }

                        //If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                        else if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(group.IDOverride[i]))
                        {
                            if (!VaultFileComponentsLoaded(LoadedTemplateManager.LoadedVaultFiles[group.IDOverride[i]]))
                            {
                                group.IDOverride.RemoveAt(i);
                                i--;
                            }
                        }

                        //If this is not a vaulted gun, remove it
                        else
                        {
                            TNHFrameworkLogger.LogWarning($"Object in table not loaded, removing it from object table! ObjectID: {group.IDOverride[i]}");
                            group.IDOverride.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }


        // Necessary? No. I'm just lazy and want the errors to go away.
        public static void RemoveUnloadedObjectIDs(ObjectTemplates.V1.EquipmentGroup group)
        {
            if (group.IDOverride != null)
            {
                for (int i = 0; i < group.IDOverride.Count; i++)
                {
                    if (!IM.OD.ContainsKey(group.IDOverride[i]))
                    {
                        // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                        if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(group.IDOverride[i]))
                        {
                            if (!LoadedTemplateManager.LoadedLegacyVaultFiles[group.IDOverride[i]].AllComponentsLoaded())
                            {
                                group.IDOverride.RemoveAt(i);
                                i--;
                            }
                        }

                        // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                        else if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(group.IDOverride[i]))
                        {
                            if (!VaultFileComponentsLoaded(LoadedTemplateManager.LoadedVaultFiles[group.IDOverride[i]]))
                            {
                                group.IDOverride.RemoveAt(i);
                                i--;
                            }
                        }

                        // If this is not a vaulted gun, remove it
                        else
                        {
                            TNHFrameworkLogger.LogWarning($"Object in table not loaded, removing it from object table! ObjectID: {group.IDOverride[i]}");
                            group.IDOverride.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }
        }


        public static void RemoveUnloadedObjectIDs(SosigTemplate template)
        {
            
            //Loop through all outfit configs and remove any clothing objects that don't exist
            foreach (OutfitConfig config in template.OutfitConfigs)
            {
                for(int i = 0; i < config.Headwear.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Headwear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Headwear[i]);
                        config.Headwear.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Headwear.Count == 0) config.Chance_Headwear = 0;

                for (int i = 0; i < config.Facewear.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Facewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Facewear[i]);
                        config.Facewear.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Facewear.Count == 0) config.Chance_Facewear = 0;

                for (int i = 0; i < config.Eyewear.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Eyewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Eyewear[i]);
                        config.Eyewear.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Eyewear.Count == 0) config.Chance_Eyewear = 0;

                for (int i = 0; i < config.Torsowear.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Torsowear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Torsowear[i]);
                        config.Torsowear.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Torsowear.Count == 0) config.Chance_Torsowear = 0;

                for (int i = 0; i < config.Pantswear.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear[i]);
                        config.Pantswear.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Pantswear.Count == 0) config.Chance_Pantswear = 0;

                for (int i = 0; i < config.Pantswear_Lower.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear_Lower[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear_Lower[i]);
                        config.Pantswear_Lower.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Pantswear_Lower.Count == 0) config.Chance_Pantswear_Lower = 0;

                for (int i = 0; i < config.Backpacks.Count; i++)
                {
                    if (!IM.OD.ContainsKey(config.Backpacks[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Backpacks[i]);
                        config.Backpacks.RemoveAt(i);
                        i -= 1;
                    }
                }
                if (config.Backpacks.Count == 0) config.Chance_Backpacks = 0;
            }

        }


        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name=""></param>
        /// <param name="pixelsPerUnit"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(FileInfo file)
        {
            Texture2D spriteTexture = LoadTexture(file);
            if (spriteTexture == null) return null;
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100f);
            sprite.name = file.Name;
            return sprite;
        }


        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name=""></param>
        /// <param name="pixelsPerUnit"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(IFileHandle file)
        {
            Texture2D spriteTexture = LoadTexture(file);
            if (spriteTexture == null) return null;
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100f);
            sprite.name = file.Name;
            return sprite;
        }



        public static Sprite LoadSprite(Texture2D spriteTexture, float pixelsPerUnit = 100f)
        {
            return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), pixelsPerUnit);
        }



        /// <summary>
        /// Loads a texture2D from the sent file. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Texture2D LoadTexture(FileInfo file)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            Stream fileStream = file.OpenRead();
            MemoryStream mem = new();

            CopyStream(fileStream, mem);

            byte[] fileData = mem.ToArray();

            Texture2D tex2D = new(2, 2);
            if (tex2D.LoadImage(fileData)) return tex2D;

            return null;
        }



        /// <summary>
        /// Loads a texture2D from the sent file. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Texture2D LoadTexture(IFileHandle file)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails

            Stream fileStream = file.OpenRead();
            MemoryStream mem = new();

            CopyStream(fileStream, mem);

            byte[] fileData = mem.ToArray();

            Texture2D tex2D = new(2, 2);          
            if (tex2D.LoadImage(fileData)) return tex2D;

            return null;                     
        }



        /// <summary>
        /// Copies the input stream into the output stream. Source: https://stackoverflow.com/questions/1080442/how-to-convert-an-stream-into-a-byte-in-c
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void CopyStream(Stream input, Stream output)
        {
            byte[] b = new byte[32768];
            int r;
            while ((r = input.Read(b, 0, b.Length)) > 0)
                output.Write(b, 0, r);
        }



        public static IEnumerator SpawnFirearm(SavedGunSerializable savedGun, Vector3 position, Quaternion rotation, TNH_Manager M)
        {
            List<GameObject> toDealWith = [];
            List<GameObject> toMoveToTrays = [];
            FVRFireArm myGun = null;
            FVRFireArmMagazine myMagazine = null;
            List<int> validIndexes = [];
            Dictionary<GameObject, SavedGunComponent> dicGO = [];
            Dictionary<int, GameObject> dicByIndex = [];
            List<AnvilCallback<GameObject>> callbackList = [];
            SavedGun gun = savedGun.GetSavedGun();

            for (int i = 0; i < gun.Components.Count; i++)
            {
                callbackList.Add(IM.OD[gun.Components[i].ObjectID].GetGameObjectAsync());
                TNHFrameworkLogger.Log($"Loading vault component: {gun.Components[i].ObjectID}", TNHFrameworkLogger.LogType.General);
            }
            yield return callbackList;

            for (int j = 0; j < gun.Components.Count; j++)
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(IM.OD[gun.Components[j].ObjectID].GetGameObject());
                M.AddObjectToTrackedList(gameObject);

                dicGO.Add(gameObject, gun.Components[j]);
                dicByIndex.Add(gun.Components[j].Index, gameObject);
                if (gun.Components[j].isFirearm)
                {
                    myGun = gameObject.GetComponent<FVRFireArm>();
                    savedGun.ApplyFirearmProperties(myGun);
                    
                    validIndexes.Add(j);
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = Quaternion.identity;
                }
                else if (gun.Components[j].isMagazine)
                {
                    myMagazine = gameObject.GetComponent<FVRFireArmMagazine>();
                    validIndexes.Add(j);
                    if (myMagazine != null)
                    {
                        gameObject.transform.position = myGun.GetMagMountPos(myMagazine.IsBeltBox).position;
                        gameObject.transform.rotation = myGun.GetMagMountPos(myMagazine.IsBeltBox).rotation;
                        myMagazine.Load(myGun);
                        myMagazine.IsInfinite = false;
                    }
                }
                else if (gun.Components[j].isAttachment)
                {
                    toDealWith.Add(gameObject);
                }
                else
                {
                    toMoveToTrays.Add(gameObject);
                    if (gameObject.GetComponent<Speedloader>() != null && gun.LoadedRoundsInMag.Count > 0)
                    {
                        Speedloader component = gameObject.GetComponent<Speedloader>();
                        component.ReloadSpeedLoaderWithList(gun.LoadedRoundsInMag);
                    }
                    else if (gameObject.GetComponent<FVRFireArmClip>() != null && gun.LoadedRoundsInMag.Count > 0)
                    {
                        FVRFireArmClip component2 = gameObject.GetComponent<FVRFireArmClip>();
                        component2.ReloadClipWithList(gun.LoadedRoundsInMag);
                    }
                }
                gameObject.GetComponent<FVRPhysicalObject>().ConfigureFromFlagDic(gun.Components[j].Flags);
            }
            if (myGun.Magazine != null && gun.LoadedRoundsInMag.Count > 0)
            {
                myGun.Magazine.ReloadMagWithList(gun.LoadedRoundsInMag);
                myGun.Magazine.IsInfinite = false;
            }
            int BreakIterator = 200;
            while (toDealWith.Count > 0 && BreakIterator > 0)
            {
                BreakIterator--;
                for (int k = toDealWith.Count - 1; k >= 0; k--)
                {
                    SavedGunComponent savedGunComponent = dicGO[toDealWith[k]];
                    if (validIndexes.Contains(savedGunComponent.ObjectAttachedTo))
                    {
                        GameObject gameObject2 = toDealWith[k];
                        FVRFireArmAttachment component3 = gameObject2.GetComponent<FVRFireArmAttachment>();
                        FVRFireArmAttachmentMount mount = GetMount(dicByIndex[savedGunComponent.ObjectAttachedTo], savedGunComponent.MountAttachedTo);
                        gameObject2.transform.rotation = Quaternion.LookRotation(savedGunComponent.OrientationForward, savedGunComponent.OrientationUp);
                        gameObject2.transform.position = GetPositionRelativeToGun(savedGunComponent, myGun.transform);
                        if (component3.CanScaleToMount && mount.CanThisRescale())
                        {
                            component3.ScaleToMount(mount);
                        }
                        component3.AttachToMount(mount, false);
                        if (component3 is Suppressor)
                        {
                            (component3 as Suppressor).AutoMountWell();
                        }
                        validIndexes.Add(savedGunComponent.Index);
                        toDealWith.RemoveAt(k);
                    }
                }
            }
            int trayIndex = 0;
            int itemIndex = 0;
            for (int l = 0; l < toMoveToTrays.Count; l++)
            {
                toMoveToTrays[l].transform.position = position + (float)itemIndex * 0.1f * Vector3.up;
                toMoveToTrays[l].transform.rotation = rotation;
                itemIndex++;
                trayIndex++;
                if (trayIndex > 2)
                {
                    trayIndex = 0;
                }
            }
            myGun.SetLoadedChambers(gun.LoadedRoundsInChambers);
            myGun.transform.rotation = rotation;
            yield break;
        }

        public static FVRFireArmAttachmentMount GetMount(GameObject obj, int index)
        {
            return obj.GetComponent<FVRPhysicalObject>().AttachmentMounts[index];
        }

        public static Vector3 GetPositionRelativeToGun(SavedGunComponent data, Transform gun)
        {
            Vector3 a = gun.position;
            a += gun.up * data.PosOffset.y;
            a += gun.right * data.PosOffset.x;
            return a + gun.forward * data.PosOffset.z;
        }


        /// <summary>
        /// Used to spawn more than one, same objects at a position
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="position"></param>
        /// <param name="count"></param>
        /// <param name="tolerance"></param>
        // Used to spawn more than one, same objects at a position
        public static IEnumerator InstantiateMultiple(TNH_Manager M, GameObject gameObject, Vector3 position, int count, float tolerance = 1.3f)
        {
            float heightNeeded = (gameObject.GetMaxBounds().size.y / 2) * tolerance;
            for (var index = 0; index < count; index++)
            {
                float current = index * heightNeeded;
                UnityEngine.Object.Instantiate(gameObject, position + (Vector3.up * current), new Quaternion());
                M.AddObjectToTrackedList(gameObject);
                yield return null;
            }
        }
        

        /// <summary>
        /// Spawns items from the equipment group
        /// </summary>
        /// <param name="selectedGroup"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="callback"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static IEnumerator InstantiateFromEquipmentGroup(EquipmentGroup selectedGroup, Vector3 position, Quaternion rotation, Action<GameObject> callback = null, float tolerance = 1.3f)
        {
            float currentHeight = 0;

            foreach (EquipmentGroup group in selectedGroup.GetSpawnedEquipmentGroups())
            {
                for (int i = 0; i < group.ItemsToSpawn; i++)
                {
                    if (IM.OD.TryGetValue(group.GetObjects().GetRandom(), out FVRObject selectedFVR))
                    {
                        //First, async get the game object to spawn
                        AnvilCallback<GameObject> objectCallback = selectedFVR.GetGameObjectAsync();
                        yield return objectCallback;

                        //Next calculate the height needed for this item
                        GameObject gameObject = selectedFVR.GetGameObject();
                        float heightNeeded = gameObject.GetMaxBounds().size.y / 2 * tolerance;
                        currentHeight += heightNeeded;

                        //Finally spawn the item and call the callback if it's not null
                        GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, position + (Vector3.up * currentHeight), rotation);
                        // ODK - This is added to the tracked object list after we return
                        if(callback != null) callback.Invoke(spawnedObject);
                        yield return null;
                    }
                }
            }
        }



    }




    public static class Globals
    {
        public static bool DebugFlag { get; set; }
        public static readonly string Accept = "Accept";
        public static readonly string Content_Type = "Content-Type";
        public static readonly string ApplicationJson = "application/json";
        public static readonly string ErrorOccurred = "Error occurred";
        public static readonly string PrincipalID = "x-ms-client-principal-id";
        public static readonly string PrincipalName = "x-ms-client-principal-name";
    }

}
