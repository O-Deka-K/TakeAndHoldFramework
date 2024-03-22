﻿using ADepIn;
using BepInEx;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TNHTweaker.ObjectTemplates;
using TNHTweaker.Utilities;
using UnityEngine;
using Stratum;
using Stratum.Extensions;

namespace TNHTweaker
{
    public class TNHLoaders
    {
        public Empty LoadSosig(FileSystemInfo handle)
        {
            FileInfo file = handle.ConsumeFile();

            try
            {
                SosigTemplate sosig = JsonConvert.DeserializeObject<SosigTemplate>(File.ReadAllText(file.FullName));

                TNHTweakerLogger.Log("TNHTweaker -- Sosig loaded successfuly : " + sosig.DisplayName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
            catch (Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for sosig file! Caused Error: " + e.ToString());
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
                    if (file.Name.EndsWith("character.json"))
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        character = JsonConvert.DeserializeObject<CustomCharacter>(File.ReadAllText(file.FullName), settings);

                        TNHTweakerLogger.Log("TNHTweaker -- Character partially loaded - loaded character file", TNHTweakerLogger.LogType.File);
                    }
                    else if (file.FullName.EndsWith("thumb.png"))
                    {
                        thumbnail = TNHTweakerUtils.LoadSprite(file);

                        TNHTweakerLogger.Log("TNHTweaker -- Character partially loaded - loaded character icon", TNHTweakerLogger.LogType.File);
                    }
                }

                if (character == null)
                {
                    TNHTweakerLogger.LogError("TNHTweaker -- Failed to load custom character! No character.json file found");
                    return new Empty();
                }

                else if (thumbnail == null)
                {
                    TNHTweakerLogger.LogError("TNHTweaker -- Failed to load custom character! No thumb.png file found");
                    return new Empty();
                }

                //Now we want to load the icons for each pool
                foreach (FileInfo iconFile in folder.GetFiles())
                {
                    foreach (EquipmentPool pool in character.EquipmentPools)
                    {
                        if (iconFile.FullName.Split('\\').Last() == pool.IconName)
                        {
                            pool.GetPoolEntry().TableDef.Icon = TNHTweakerUtils.LoadSprite(iconFile);

                            TNHTweakerLogger.Log($"TNHTweaker -- Character partially loaded - loaded misc icon {iconFile.Name}", TNHTweakerLogger.LogType.File);
                        }
                    }
                }

                TNHTweakerLogger.Log("TNHTweaker -- Character loaded successfuly : " + character.DisplayName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddCharacterTemplate(character, thumbnail);
            }
            catch(Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for character! Caused Error: " + e.ToString());
            }

            return new Empty();
        }

        public Empty LoadVaultFile(FileSystemInfo handle)
        {
            FileInfo file = handle.ConsumeFile();

            try
            {
                VaultFile savedGun = JsonConvert.DeserializeObject<VaultFile>(File.ReadAllText(file.FullName));

                TNHTweakerLogger.Log("TNHTweaker -- Vault file loaded successfuly : " + savedGun.FileName, TNHTweakerLogger.LogType.File);
                TNHTweakerLogger.Log("TNHTweaker -- Vault file loaded successfuly : " + savedGun.FileName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddVaultFile(savedGun);
            }
            catch(Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for vault file! Caused Error: " + e.ToString());
            }

            return new Empty();
        }
    }
}
