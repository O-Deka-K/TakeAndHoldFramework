using ADepIn;
using BepInEx;
using Deli;
using Deli.Immediate;
using Deli.Runtime;
using Deli.Runtime.Yielding;
using Deli.Setup;
using Deli.VFS;
using Deli.Newtonsoft.Json;
using Deli.Newtonsoft.Json.Linq;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TNHFramework.ObjectTemplates.V1;
using TNHFramework.Utilities;
using UnityEngine;
using Stratum;
using Stratum.Extensions;

namespace TNHFramework
{
    public class SosigLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {

            if(handle is not IFileHandle file)
            {
                throw new ArgumentException("Could not load sosig! Make sure you're pointing to a sosig template json file in the manifest");
            }

            try
            {
                ObjectTemplates.SosigTemplate sosig = stage.ImmediateReaders.Get<JToken>()(file).ToObject<ObjectTemplates.SosigTemplate>();
                TNHTweakerLogger.Log("TNHTWEAKER -- Sosig loaded successfuly : " + sosig.DisplayName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddSosigTemplate(sosig);
            }
            catch (Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for sosig file! Caused Error: " + e.ToString());
            }

        }
    }



    public class CharacterLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {
            
            if(handle is not IDirectoryHandle dir)
            {
                throw new ArgumentException("Could not load character! Character should point to a folder holding the character.json and thumb.png");
            }


            try
            {
                CustomCharacter character = null;
                Sprite thumbnail = null;

                foreach (IFileHandle file in dir.GetFiles())
                {
                    if (file.Path.EndsWith("character.json"))
                    {
                        string charString = stage.ImmediateReaders.Get<string>()(file);
                        JsonSerializerSettings settings = new()
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        character = JsonConvert.DeserializeObject<CustomCharacter>(charString, settings);
                    }
                    else if (file.Path.EndsWith("thumb.png"))
                    {
                        thumbnail = TNHFrameworkUtils.LoadSprite(file);
                    }
                }

                if (character == null)
                {
                    TNHTweakerLogger.LogError("TNHTWEAKER -- Failed to load custom character! No character.json file found");
                    return;
                }
                else if (thumbnail == null)
                {
                    TNHTweakerLogger.LogError("TNHTWEAKER -- Failed to load custom character! No thumb.png file found");
                    return;
                }

                // Now we want to load the icons for each pool
                foreach (IFileHandle iconFile in dir.GetFiles())
                {
                    foreach (EquipmentPool pool in character.EquipmentPools)
                    {
                        if (iconFile.Path.Split('/').Last() == pool.IconName)
                        {
                            pool.GetPoolEntry().TableDef.Icon = TNHFrameworkUtils.LoadSprite(iconFile);
                        }
                    }
                }

                TNHTweakerLogger.Log("TNHTWEAKER -- Character loaded successfuly : " + character.DisplayName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddCharacterTemplate(new ObjectTemplates.CustomCharacter(character), thumbnail);
            }
            catch(Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for character! Caused Error: " + e.ToString());
            }
        }
    }



    public class VaultFileLoaderDeli
    {
        public void LoadAsset(SetupStage stage, Mod mod, IHandle handle)
        {

            if (handle is not IFileHandle file)
            {
                throw new ArgumentException("Could not load vault file! Make sure you're pointing to a vault json file in the manifest");
            }

            try
            {
                ObjectTemplates.SavedGunSerializable savedGun = stage.ImmediateReaders.Get<JToken>()(file).ToObject<ObjectTemplates.SavedGunSerializable>();

                TNHTweakerLogger.Log("TNHTWEAKER -- Vault file loaded successfuly : " + savedGun.FileName, TNHTweakerLogger.LogType.File);

                LoadedTemplateManager.AddVaultFile(savedGun);
            }
            catch(Exception e)
            {
                TNHTweakerLogger.LogError("Failed to load setup assets for vault file! Caused Error: " + e.ToString());
            }
        }
    }
}
