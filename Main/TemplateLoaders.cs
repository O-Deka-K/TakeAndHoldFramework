﻿using FistVR;
using Stratum;
using Stratum.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TNHFramework.ObjectTemplates;
using TNHFramework.Utilities;
using UnityEngine;
using Valve.Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace TNHFramework
{
    public class TNHLoaders
    {
        public Empty LoadSosig(FileSystemInfo handle)
        {
            FileInfo file = handle.ConsumeFile();

            try
            {
                SosigTemplate sosig = null;

                if (file.Name.EndsWith(".yaml"))
                {
                    var deserializerBuilder = new DeserializerBuilder();

                    var deserializer = deserializerBuilder.Build();
                    sosig = deserializer.Deserialize<SosigTemplate>(File.ReadAllText(file.FullName));

                    TNHFrameworkLogger.Log("Sosig loaded successfully : " + sosig.DisplayName, TNHFrameworkLogger.LogType.File);
                }
                else if (file.Name.EndsWith(".json"))
                {
                    JsonSerializerSettings settings = new()
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    sosig = JsonConvert.DeserializeObject<SosigTemplate>(File.ReadAllText(file.FullName));

                    TNHFrameworkLogger.Log("Sosig loaded successfully : " + sosig.DisplayName, TNHFrameworkLogger.LogType.File);

                    if (TNHFramework.ConvertFilesToYAML.Value)
                    {
                        using (StreamWriter sw = File.CreateText(file.FullName.Replace(".json", ".yaml")))
                        {
                            var serializerBuilder = new SerializerBuilder();

                            serializerBuilder.WithIndentedSequences();

                            var serializer = serializerBuilder.Build();
                            string vaultString = serializer.Serialize(sosig);
                            sw.WriteLine(vaultString);
                            sw.Close();
                        }

                        File.Delete(file.FullName);
                    }
                }

                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
            catch (Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for sosig file! Caused Error: " + e.ToString());
            }
            return new Empty();
        }

        public Empty LoadChar(FileSystemInfo handle)
        {
            DirectoryInfo folder = handle.ConsumeDirectory();

            try
            {
                CustomCharacter character = null;
                Sprite thumbnail = null;

                foreach (FileInfo file in folder.GetFiles())
                {
                    if (file.Name.EndsWith("character.yaml"))
                    {
                        var deserializerBuilder = new DeserializerBuilder();

                        foreach (KeyValuePair<string, Type> thing in TNHFramework.Serializables)
                        {
                            deserializerBuilder.WithTagMapping(thing.Key, thing.Value);
                        }
                        var deserializer = deserializerBuilder.Build();
                        character = deserializer.Deserialize<CustomCharacter>(File.ReadAllText(file.FullName));

                        TNHFrameworkLogger.Log("Character partially loaded - loaded character file", TNHFrameworkLogger.LogType.File);
                    }
                    else if (file.Name.EndsWith("character.json"))
                    {
                        JsonSerializerSettings settings = new()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        character = new(JsonConvert.DeserializeObject<ObjectTemplates.V1.CustomCharacter>(File.ReadAllText(file.FullName), settings));

                        TNHFrameworkLogger.Log("Character partially loaded - loaded character file", TNHFrameworkLogger.LogType.File);
                    }
                    else if (file.FullName.EndsWith("thumb.png"))
                    {
                        thumbnail = TNHFrameworkUtils.LoadSprite(file);

                        TNHFrameworkLogger.Log("Character partially loaded - loaded character icon", TNHFrameworkLogger.LogType.File);
                    }
                }

                if (character == null)
                {
                    TNHFrameworkLogger.LogError("Failed to load custom character! No character.json file found");
                    return new Empty();
                }

                else if (thumbnail == null)
                {
                    TNHFrameworkLogger.LogError("Failed to load custom character! No thumb.png file found");
                    return new Empty();
                }

                // Now we want to load the icons for each pool
                foreach (FileInfo iconFile in folder.GetFiles())
                {
                    foreach (EquipmentPool pool in character.EquipmentPools)
                    {
                        if (iconFile.FullName.Split('\\').Last() == pool.IconName)
                        {
                            pool.GetPoolEntry().TableDef.Icon = TNHFrameworkUtils.LoadSprite(iconFile);

                            TNHFrameworkLogger.Log($"Character partially loaded - loaded misc icon {iconFile.Name}", TNHFrameworkLogger.LogType.File);
                        }
                    }
                }

                TNHFrameworkLogger.Log("Character loaded successfully : " + character.DisplayName, TNHFrameworkLogger.LogType.File);

                LoadedTemplateManager.AddCharacterTemplate(character, thumbnail);
            }
            catch (Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for character! Caused Error: " + e.ToString());
            }

            return new Empty();
        }

        public Empty LoadVaultFile(FileSystemInfo handle)
        {
            FileInfo file = handle.ConsumeFile();

            try
            {
                VaultFile savedGun = null;

                if (file.Name.EndsWith(".yaml"))
                {
                    var deserializerBuilder = new DeserializerBuilder();

                    var deserializer = deserializerBuilder.Build();
                    savedGun = deserializer.Deserialize<VaultFile>(File.ReadAllText(file.FullName));

                    TNHFrameworkLogger.Log("Vault file loaded successfully : " + savedGun.FileName, TNHFrameworkLogger.LogType.File);
                }
                else if (file.Name.EndsWith(".json"))
                {
                    JsonSerializerSettings settings = new()
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    };

                    savedGun = JsonConvert.DeserializeObject<VaultFile>(File.ReadAllText(file.FullName));

                    TNHFrameworkLogger.Log("Vault file loaded successfully : " + savedGun.FileName, TNHFrameworkLogger.LogType.File);

                    if (TNHFramework.ConvertFilesToYAML.Value)
                    {
                        using (StreamWriter sw = File.CreateText(file.FullName.Replace(".json", ".yaml")))
                        {
                            var serializerBuilder = new SerializerBuilder();

                            serializerBuilder.WithIndentedSequences();

                            var serializer = serializerBuilder.Build();
                            string vaultString = serializer.Serialize(savedGun);
                            sw.WriteLine(vaultString);
                            sw.Close();
                        }

                        File.Delete(file.FullName);
                    }
                }

                if (savedGun != null)
                {
                    LoadedTemplateManager.AddVaultFile(savedGun);
                }
            }
            catch (Exception e)
            {
                TNHFrameworkLogger.LogError("Failed to load setup assets for vault file! Caused Error: " + e.ToString());
            }

            return new Empty();
        }
    }
}
