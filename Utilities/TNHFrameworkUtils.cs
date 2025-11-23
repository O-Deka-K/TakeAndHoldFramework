using BepInEx;
using Deli.VFS;
using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TNHFramework.ObjectTemplates;
using UnityEngine;
using EquipmentGroup = TNHFramework.ObjectTemplates.EquipmentGroup;

namespace TNHFramework.Utilities
{
    static class TNHFrameworkUtils
    {
        public static bool IsSpawning = false;
        public static GameObject LastSpawnedGun;

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
                    onError?.Invoke(e);
                    yield break;
                }

                if (more)
                {
                    yield return routine.Current;
                }
            }
        }

        public static void RemoveUnloadedObjectIDs(EquipmentGroup group)
        {
            if (group.IDOverride != null)
                RemoveMissingObjectIDs(group.IDOverride);

            // If IDOverride is now empty, copy IDOverrideBackup
            if ((group.IDOverride == null || !group.IDOverride.Any()) && group.IDOverrideBackup != null)
            {
                RemoveMissingObjectIDs(group.IDOverrideBackup);

                if (group.IDOverrideBackup.Any())
                    group.IDOverride = [.. group.IDOverrideBackup];
            }
        }

        public static void RemoveUnloadedObjectIDs(ObjectTemplates.V1.EquipmentGroup group)
        {
            if (group.IDOverride != null)
                RemoveMissingObjectIDs(group.IDOverride);

            // If IDOverride is now empty, copy IDOverrideBackup
            if ((group.IDOverride == null || !group.IDOverride.Any()) && group.IDOverrideBackup != null)
            {
                RemoveMissingObjectIDs(group.IDOverrideBackup);

                if (group.IDOverrideBackup.Any())
                    group.IDOverride = [.. group.IDOverrideBackup];
            }
        }

        public static void RemoveMissingObjectIDs(List<string> IDOverride)
        {
            for (int i = IDOverride.Count - 1; i >= 0; i--)
            {
                if (!IM.OD.ContainsKey(IDOverride[i]))
                {
                    // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                    if (LoadedTemplateManager.LoadedLegacyVaultFiles.ContainsKey(IDOverride[i]))
                    {
                        if (!LoadedTemplateManager.LoadedLegacyVaultFiles[IDOverride[i]].AllComponentsLoaded())
                            IDOverride.RemoveAt(i);
                    }

                    // If this is a vaulted gun with all it's components loaded, we should still have this in the object list
                    else if (LoadedTemplateManager.LoadedVaultFiles.ContainsKey(IDOverride[i]))
                    {
                        if (!VaultFileComponentsLoaded(LoadedTemplateManager.LoadedVaultFiles[IDOverride[i]]))
                            IDOverride.RemoveAt(i);
                    }

                    // If this is not a vaulted gun, remove it
                    else
                    {
                        TNHFrameworkLogger.LogWarning($"Object in table not loaded, removing it from object table! ObjectID: {IDOverride[i]}");
                        IDOverride.RemoveAt(i);
                    }
                }
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

        public static void RemoveUnloadedObjectIDs(SosigTemplate template)
        {
            // Loop through all outfit configs and remove any clothing objects that don't exist
            foreach (OutfitConfig config in template.OutfitConfigs)
            {
                for (int i = config.Headwear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Headwear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Headwear[i]);
                        config.Headwear.RemoveAt(i);
                    }
                }

                if (!config.Headwear.Any())
                    config.Chance_Headwear = 0;

                for (int i = config.Facewear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Facewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Facewear[i]);
                        config.Facewear.RemoveAt(i);
                    }
                }
                
                if (!config.Facewear.Any())
                    config.Chance_Facewear = 0;

                for (int i = config.Eyewear.Count - 1; i >= 0 ; i--)
                {
                    if (!IM.OD.ContainsKey(config.Eyewear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Eyewear[i]);
                        config.Eyewear.RemoveAt(i);
                    }
                }
                
                if (!config.Eyewear.Any())
                    config.Chance_Eyewear = 0;

                for (int i = config.Torsowear.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Torsowear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Torsowear[i]);
                        config.Torsowear.RemoveAt(i);
                    }
                }
                
                if (!config.Torsowear.Any())
                    config.Chance_Torsowear = 0;

                for (int i = config.Pantswear.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear[i]);
                        config.Pantswear.RemoveAt(i);
                    }
                }
                
                if (!config.Pantswear.Any())
                    config.Chance_Pantswear = 0;

                for (int i = config.Pantswear_Lower.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Pantswear_Lower[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Pantswear_Lower[i]);
                        config.Pantswear_Lower.RemoveAt(i);
                    }
                }
                
                if (!config.Pantswear_Lower.Any())
                    config.Chance_Pantswear_Lower = 0;

                for (int i = config.Backpacks.Count - 1; i >= 0; i--)
                {
                    if (!IM.OD.ContainsKey(config.Backpacks[i]))
                    {
                        TNHFrameworkLogger.LogWarning("Clothing item not loaded, removing it from clothing config! ObjectID : " + config.Backpacks[i]);
                        config.Backpacks.RemoveAt(i);
                    }
                }
                
                if (!config.Backpacks.Any())
                    config.Chance_Backpacks = 0;
            }
        }

        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(FileInfo file)
        {
            Texture2D spriteTexture = LoadTexture(file);

            if (spriteTexture == null)
                return null;
            
            Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0, 0), 100f);
            sprite.name = file.Name;
            return sprite;
        }

        /// <summary>
        /// Loads a sprite from a file path. Solution found here: https://forum.unity.com/threads/generating-sprites-dynamically-from-png-or-jpeg-files-in-c.343735/
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Sprite LoadSprite(IFileHandle file)
        {
            Texture2D spriteTexture = LoadTexture(file);
            
            if (spriteTexture == null)
                return null;
            
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
            if (tex2D.LoadImage(fileData))
                return tex2D;

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
            if (tex2D.LoadImage(fileData))
                return tex2D;

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
            {
                output.Write(b, 0, r);
            }
        }

        // This is pretty much a manual process of tagging mods that are incorrectly tagged or missing tags
        public static void FixModAttachmentTags()
        {
            List<FVRObject> attachments = [.. ManagerSingleton<IM>.Instance.odicTagCategory[FVRObject.ObjectCategory.Attachment]];
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

        // ODK - Check for premades and fix them. They already have attachments, but they may not be registered properly.
        //       Attachments may also be parented to the main object instead of the mount points.
        //       Set 'removeSights' to true to get more variety when using 'AddRandomAttachments' feature.
        public static void FixPremadeFirearm(GameObject spawnedGun, bool removeExtras = false)
        {
            FVRFireArm firearm = spawnedGun?.GetComponent<FVRFireArm>();
            FVRObject gunItem = firearm?.ObjectWrapper;

            if (gunItem == null)
                return;

            List<FVRFireArmAttachment> attached = spawnedGun.GetComponentsInChildren<FVRFireArmAttachment>(true).ToList();

            if (!attached.Any())
                return;

            TNHFrameworkLogger.Log($"FixPremadeFirearms: Found ({attached.Count}) attachments on [{spawnedGun.name}]", TNHFrameworkLogger.LogType.TNH);

            Regex regexModulAK = new(@"Modul(AK|RD|PP|SAG|Saiga9)");
            Regex regexModulAR10 = new(@"Modul(AR10|AER10)");
            Regex regexModulAR = new(@"Modul(AR|ADAR|DD|M16|Saint|SR25|TX15|Aero|V7)");
            Regex regexModulSIG = new(@"^MCX(Bllk|Coyote|FDE|Gry|Raptor)");
            //Regex regexModulShotguns = new(@"Modul(133|153|155|590|870|Auto5|Ithaca|M3|M3|M4)");  // Seems to work fine as-is

            bool isModulAK = regexModulAK.IsMatch(gunItem.ItemID);
            bool isModulAR10 = regexModulAR10.IsMatch(gunItem.ItemID);
            bool isModulAR = !isModulAR10 && regexModulAR.IsMatch(gunItem.ItemID);
            bool isModulSIG = regexModulSIG.IsMatch(gunItem.ItemID);

            // List all mount points
            List<FVRFireArmAttachmentMount> attachmentMounts = spawnedGun.GetComponentsInChildren<FVRFireArmAttachmentMount>(true).ToList();
            TNHFrameworkLogger.Log($"FixPremadeFirearms: Found ({attachmentMounts.Count}) mount point(s)", TNHFrameworkLogger.LogType.TNH);

            foreach (FVRFireArmAttachmentMount mount in attachmentMounts)
            {
                if (mount == null)
                    continue;

                string parentID = mount.Parent?.ObjectWrapper?.ItemID ?? "(unknown)";
                string mountName = mount.name.IsNullOrWhiteSpace() ? "(unknown-mount)" : mount.name;

                TNHFrameworkLogger.Log($"FixPremadeFirearms: Mount point [{parentID}/{mountName}] found", TNHFrameworkLogger.LogType.TNH);
            }

            foreach (FVRFireArmAttachment attachment in attached)
            {
                if (attachment == null)
                    continue;

                attachment.gameObject?.SetActive(true);

                FVRObject attachmentObject = attachment.ObjectWrapper;

                if (attachmentObject == null)
                    continue;

                // Check if its parent is a mount point or not
                //FVRFireArmAttachmentMount mount = attachment.GetComponentInParent<FVRFireArmAttachmentMount>();
                FVRFireArmAttachmentMount mount = attachment.transform.parent.GetComponent<FVRFireArmAttachmentMount>();
                string id = attachmentObject.ItemID ?? "(null)";

                if (mount == null)  // Not mounted correctly
                {
                    TNHFrameworkLogger.Log($"FixPremadeFirearms: Attachment ID [{id}]", TNHFrameworkLogger.LogType.TNH);

                    if (isModulAK)
                    {
                        if (id.ToLower().Contains("dustcover"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_dustcover"));
                        }
                        else if (id.ToLower().Contains("fore"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foregrip"));
                        }
                        else if (id.ToLower().Contains("grip"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_grip"));
                        }
                    }
                    else if (isModulAR10)
                    {
                        if (id.ToLower().Contains("barrel"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_suppresormount"));
                        }
                        else if ((id.ToLower().Contains("sight") || id.ToLower().Contains("30mm")) && removeExtras)
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Removing sight [{id}]", TNHFrameworkLogger.LogType.TNH);
                            UnityEngine.Object.Destroy(attachment.gameObject);
                        }
                        else if ((id.ToLower().Contains("sightspike") || attachmentObject.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.IronSight) && id.ToLower().Contains("front"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if ((id.ToLower().Contains("sightspike") || attachmentObject.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.IronSight) && id.ToLower().Contains("rear"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                        }
                        else if (id.ToLower().Contains("30mm"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                        }
                        else if (id.ToLower().Contains("sight"))  // Rear ironsight or reflex/scope
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("ar10fore"))  // Make sure we don't match with "forest"
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                        }
                        else if (id.ToLower().Contains("grip"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().EndsWith("_bottom"));
                        }
                        else if (id.ToLower().Contains("piccover"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("tube"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_stockmount"));
                        }
                        else if (id.ToLower().Contains("stock"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("gasblock"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (attachmentObject.TagAttachmentMount == FVRObject.OTagFirearmMount.Muzzle)
                        {
                            if (removeExtras)
                            {
                                TNHFrameworkLogger.Log($"FixPremadeFirearms: Removing sight [{id}]", TNHFrameworkLogger.LogType.TNH);
                                UnityEngine.Object.Destroy(attachment.gameObject);
                            }
                            else
                            {
                                TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                                attachment.SetParentage(null);
                            }
                        }
                    }
                    else if (isModulAR || isModulSIG)
                    {
                        if (id.ToLower().Contains("barrel"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_suppresormount"));
                        }
                        else if (id.ToLower().Contains("charge"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_upper (1)"));
                        }
                        else if (id.ToLower().Contains("sight") && removeExtras)
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Removing sight [{id}]", TNHFrameworkLogger.LogType.TNH);
                            UnityEngine.Object.Destroy(attachment.gameObject);
                        }
                        else if ((id.ToLower().Contains("ironsight") || attachmentObject.TagAttachmentFeature == FVRObject.OTagAttachmentFeature.IronSight) && id.ToLower().Contains("front"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("sight"))  // Rear ironsight or reflex/scope
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("foregrip"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("fore") || id.ToLower().Contains("handguard") || id.Contains("MFR"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                        }
                        else if (id.ToLower().Contains("grip"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().EndsWith("_bottom"));
                        }
                        else if (id.ToLower().Contains("tube"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_stockmount"));
                        }
                        else if (id.ToLower().Contains("stock"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (id.ToLower().Contains("upper"))
                        {
                            mount = firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().EndsWith("_upper"));
                        }
                        else if (id.ToLower().Contains("gasblock"))
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                            attachment.SetParentage(null);
                        }
                        else if (attachmentObject.TagAttachmentMount == FVRObject.OTagFirearmMount.Muzzle && removeExtras)
                        {
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Removing sight [{id}]", TNHFrameworkLogger.LogType.TNH);
                            UnityEngine.Object.Destroy(attachment.gameObject);
                        }
                        else if (id.ToLower().Contains("ar15sup") || id.ToLower().Contains("muzzle") || attachmentObject.TagAttachmentMount == FVRObject.OTagFirearmMount.Muzzle)
                        {
                            if (removeExtras)
                            {
                                TNHFrameworkLogger.Log($"FixPremadeFirearms: Removing sight [{id}]", TNHFrameworkLogger.LogType.TNH);
                                UnityEngine.Object.Destroy(attachment.gameObject);
                            }
                            else
                            {
                                TNHFrameworkLogger.Log($"FixPremadeFirearms: Deferring [{id}] to second pass", TNHFrameworkLogger.LogType.TNH);
                                attachment.SetParentage(null);
                            }
                        }
                    }

                    if (mount == null)
                    {
                        if (!isModulAR && !isModulSIG && !isModulAR10)
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Unknown attachment [{id}]", TNHFrameworkLogger.LogType.TNH);
                    }
                    else
                    {
                        string parentID = mount.Parent?.ObjectWrapper?.ItemID ?? "(unknown)";
                        string mountName = mount.name.IsNullOrWhiteSpace() ? "(unknown-mount)" : mount.name;
                        TNHFrameworkLogger.Log($"FixPremadeFirearms: Registering [{id}] on [{parentID}/{mountName}]", TNHFrameworkLogger.LogType.TNH);
                        mount.DeRegisterAttachment(attachment);
                        AttachToMount(attachment, mount);
                    }
                }
                else  // Mounted correctly - Remount in order to fix physics
                {
                    string parentID = mount.Parent?.ObjectWrapper?.ItemID ?? "(unknown)";
                    string mountName = mount.name.IsNullOrWhiteSpace() ? "(unknown-mount)" : mount.name;
                    TNHFrameworkLogger.Log($"FixPremadeFirearms: Registering [{id}] on [{parentID}/{mountName}] (remount)", TNHFrameworkLogger.LogType.TNH);
                    mount.DeRegisterAttachment(attachment);
                    AttachToMount(attachment, mount);
                }
            }

            TNHFrameworkLogger.Log($"FixPremadeFirearms: Second pass", TNHFrameworkLogger.LogType.TNH);

            // Second pass
            foreach (FVRFireArmAttachment attachment in attached)
            {
                if (attachment == null)
                    continue;

                if (attachment.transform?.parent == null)  // Detached
                {
                    FVRObject attachmentObject = attachment.ObjectWrapper;

                    if (attachmentObject == null)
                        continue;

                    FVRFireArmAttachmentMount mount = attachment.GetComponentInParent<FVRFireArmAttachmentMount>();
                    string id = attachmentObject.ItemID ?? "(null)";

                    TNHFrameworkLogger.Log($"FixPremadeFirearms: Attachment ID [{id}]", TNHFrameworkLogger.LogType.TNH);

                    bool destroyed = false;

                    if (isModulAR10)
                    {
                        if (id.ToLower().Contains("sightspike") && id.ToLower().Contains("front"))
                        {
                            FVRFireArmAttachment fore = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("fore") && !a.ObjectWrapper.ItemID.ToLower().Contains("grip"));

                            if (fore != null)
                            {
                                mount = fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                                mount ??= fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_bottom (1)"));
                            }

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                        }
                        else if (id.ToLower().Contains("sightspike") && id.ToLower().Contains("rear"))
                        {
                            // Do nothing
                        }
                        else if (id.ToLower().Contains("sight"))
                        {
                            FVRFireArmAttachment rings = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("30mm"));

                            if (rings != null)
                            {
                                mount = rings.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                            }

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                        }
                        else if (id.ToLower().Contains("piccover"))
                        {
                            FVRFireArmAttachment fore = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("fore") && !a.ObjectWrapper.ItemID.ToLower().Contains("grip"));

                            if (fore != null)
                            {
                                mount = fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_right"));
                                mount ??= fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_bottom"));
                            }

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                        }
                        else if (id.ToLower().Contains("stock"))
                        {
                            FVRFireArmAttachment tube = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("tube"));

                            if (tube != null)
                                mount = tube.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_stockmount"));
                        }
                        else if (id.ToLower().Contains("gasblock"))
                        {
                            FVRFireArmAttachment barrel = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("barrel"));
                            mount = barrel.AttachmentMounts.Single(m => m.name.ToLower().Contains("_gasblock"));
                        }
                        else if (attachmentObject.TagAttachmentMount == FVRObject.OTagFirearmMount.Muzzle)
                        {
                            FVRFireArmAttachment barrel = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("barrel"));
                            mount = firearm.AttachmentMounts.First(m => m.name.ToLower().Contains("_suppresormount"));
                        }
                    }
                    else if (isModulAR || isModulSIG)
                    {
                        if (id.ToLower().Contains("ironsight") && removeExtras)
                        {
                            // Do nothing
                        }
                        else if (id.ToLower().Contains("ironsight") && id.ToLower().Contains("front"))
                        {
                            FVRFireArmAttachment fore = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("fore") && !a.ObjectWrapper.ItemID.ToLower().Contains("grip"));
                            FVRFireArmAttachment upper = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("upper"));

                            if (fore != null)
                            {
                                mount = fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                                mount ??= fore.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_bottom (1)"));
                            }

                            if (upper != null)
                                mount ??= upper.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_foremount"));
                        }
                        else if (id.ToLower().Contains("sight"))
                        {
                            FVRFireArmAttachment upper = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("upper"));

                            if (upper != null)
                                mount = upper.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));
                        }
                        else if (id.ToLower().Contains("foregrip"))
                        {
                            FVRFireArmAttachment fore = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("fore") && !a.ObjectWrapper.ItemID.ToLower().Contains("grip"));
                            mount = fore.AttachmentMounts.First(m => m.name.ToLower().Contains("_bottom"));
                        }
                        else if (id.ToLower().Contains("stock"))
                        {
                            FVRFireArmAttachment tube = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("tube"));

                            if (tube != null)
                                mount = tube.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_top"));

                            mount ??= firearm.AttachmentMounts.FirstOrDefault(m => m.name.ToLower().Contains("_stockmount"));
                        }
                        else if (id.ToLower().Contains("gasblock"))
                        {
                            FVRFireArmAttachment barrel = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("barrel"));
                            mount = barrel.AttachmentMounts.Single(m => m.name.ToLower().Contains("_gasblock"));
                        }
                        else if (id.ToLower().Contains("ar15sup") || id.ToLower().Contains("muzzle") || attachmentObject.TagAttachmentMount == FVRObject.OTagFirearmMount.Muzzle)
                        {
                            FVRFireArmAttachment barrel = attached.FirstOrDefault(a => a.ObjectWrapper.ItemID.ToLower().Contains("barrel"));
                            mount = firearm.AttachmentMounts.First(m => m.name.ToLower().Contains("_suppresormount"));

                            if (mount.HasAttachmentsOnIt())
                            {
                                TNHFrameworkLogger.Log($"FixPremadeFirearms: Muzzle device already mounted. Removing [{id}]", TNHFrameworkLogger.LogType.TNH);
                                UnityEngine.Object.Destroy(attachment.gameObject);
                                mount = null;
                                destroyed = true;
                            }
                        }
                    }

                    if (mount == null)
                    {
                        if (!destroyed)
                            TNHFrameworkLogger.Log($"FixPremadeFirearms: Unknown attachment [{id}]", TNHFrameworkLogger.LogType.TNH);
                    }
                    else
                    {
                        string parentID = mount.Parent?.ObjectWrapper?.ItemID ?? "(unknown)";
                        string mountName = mount.name.IsNullOrWhiteSpace() ? "(unknown-mount)" : mount.name;
                        TNHFrameworkLogger.Log($"FixPremadeFirearms: Registering [{id}] on [{parentID}/{mountName}]", TNHFrameworkLogger.LogType.TNH);
                        AttachToMount(attachment, mount);
                    }
                }
            }

            TNHFrameworkLogger.Log($"FixPremadeFirearms: DONE [{spawnedGun.name}]", TNHFrameworkLogger.LogType.TNH);
        }

        public static IEnumerator SpawnLegacyVaultFile(SavedGunSerializable savedGun, Vector3 position, Quaternion rotation, TNH_Manager M, bool addForce = false)
        {
            IsSpawning = true;

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

                    LastSpawnedGun = gameObject;

                    validIndexes.Add(j);
                    gameObject.transform.position = position;
                    gameObject.transform.rotation = Quaternion.identity;

                    if (addForce)
                        AddSpawningForce(gameObject.GetComponent<Rigidbody>());
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
                    
                    if (gameObject.GetComponent<Speedloader>() != null && gun.LoadedRoundsInMag.Any())
                    {
                        Speedloader component = gameObject.GetComponent<Speedloader>();
                        component.ReloadSpeedLoaderWithList(gun.LoadedRoundsInMag);
                    }
                    else if (gameObject.GetComponent<FVRFireArmClip>() != null && gun.LoadedRoundsInMag.Any())
                    {
                        FVRFireArmClip component2 = gameObject.GetComponent<FVRFireArmClip>();
                        component2.ReloadClipWithList(gun.LoadedRoundsInMag);
                    }
                }
               
                gameObject.GetComponent<FVRPhysicalObject>().ConfigureFromFlagDic(gun.Components[j].Flags);
            }
            
            if (myGun.Magazine != null && gun.LoadedRoundsInMag.Any())
            {
                myGun.Magazine.ReloadMagWithList(gun.LoadedRoundsInMag);
                myGun.Magazine.IsInfinite = false;
            }
            
            int BreakIterator = 200;
            
            while (toDealWith.Any() && BreakIterator > 0)
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
                            component3.ScaleToMount(mount);
                        
                        component3.AttachToMount(mount, false);
                        
                        if (component3 is Suppressor)
                            (component3 as Suppressor).AutoMountWell();
                        
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
                    trayIndex = 0;
            }
            
            myGun.SetLoadedChambers(gun.LoadedRoundsInChambers);
            myGun.transform.rotation = rotation;

            IsSpawning = false;
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

        public static IEnumerator SpawnItemRoutine(TNH_Manager M, Vector3 position, Quaternion rotation, FVRObject o, bool addForce = false)
        {
            if (o == null)
                yield break;

            IsSpawning = true;
            yield return o.GetGameObjectAsync();

            TNHFrameworkLogger.Log($"SpawnItemRoutine: Spawning item [{o.ItemID}]", TNHFrameworkLogger.LogType.TNH);
            LastSpawnedGun = UnityEngine.Object.Instantiate(o.GetGameObject(), position, rotation);
            M.AddObjectToTrackedList(LastSpawnedGun);
            LastSpawnedGun.SetActive(true);

            // Add force and torque
            if (addForce)
                AddSpawningForce(o.GetGameObject().GetComponent<Rigidbody>());

            IsSpawning = false;
            yield break;
        }

        private static void AttachToMount(FVRFireArmAttachment attachment, FVRFireArmAttachmentMount mount)
        {
            if (attachment.CanScaleToMount && mount.CanThisRescale() && mount.GetRootMount().ScaleModifier > 0.01f)
                attachment.ScaleToMount(mount);

            attachment.AttachToMount(mount, false);

            if (attachment is Suppressor)
                (attachment as Suppressor).AutoMountWell();
        }

        // Add force and torque
        private static void AddSpawningForce(Rigidbody rigidbody)
        {
            Vector3 velocity = new(UnityEngine.Random.Range(-0.5f, 0.5f), 1f, UnityEngine.Random.Range(-0.5f, 0.5f));
            rigidbody.AddForce(velocity.normalized * UnityEngine.Random.Range(2f, 3f), ForceMode.VelocityChange);

            Vector3 torque = new(UnityEngine.Random.Range(-0.5f, 0.5f), UnityEngine.Random.Range(-0.1f, 0.1f), UnityEngine.Random.Range(-1f, 1f));
            rigidbody.AddRelativeTorque(torque.normalized * UnityEngine.Random.Range(10f, 15f), ForceMode.VelocityChange);
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
                        // First, async get the game object to spawn
                        AnvilCallback<GameObject> objectCallback = selectedFVR.GetGameObjectAsync();
                        yield return objectCallback;

                        // Next calculate the height needed for this item
                        GameObject gameObject = selectedFVR.GetGameObject();
                        float heightNeeded = gameObject.GetMaxBounds().size.y / 2 * tolerance;
                        currentHeight += heightNeeded;

                        // Finally spawn the item and call the callback if it's not null
                        GameObject spawnedObject = UnityEngine.GameObject.Instantiate(gameObject, position + (Vector3.up * currentHeight), rotation);

                        // This is added to the tracked object list after we return
                        callback?.Invoke(spawnedObject);
                        yield return null;
                    }
                }
            }
        }
    }
}
