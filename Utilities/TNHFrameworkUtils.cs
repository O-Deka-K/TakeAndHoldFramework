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

            // If IDOverride is now empty, use IDOverrideBackup
            if ((group.IDOverride == null || !group.IDOverride.Any()) && group.IDOverrideBackup != null)
            {
                RemoveMissingObjectIDs(group.IDOverrideBackup);

                if (group.IDOverrideBackup.Any())
                    group.IDOverride = group.IDOverrideBackup;
            }
        }

        public static void RemoveUnloadedObjectIDs(ObjectTemplates.V1.EquipmentGroup group)
        {
            if (group.IDOverride != null)
                RemoveMissingObjectIDs(group.IDOverride);

            // If IDOverride is now empty, use IDOverrideBackup
            if ((group.IDOverride == null || !group.IDOverride.Any()) && group.IDOverrideBackup != null)
            {
                RemoveMissingObjectIDs(group.IDOverrideBackup);

                if (group.IDOverrideBackup.Any())
                    group.IDOverride = group.IDOverrideBackup;
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
            template.SosigPrefabs.RemoveAll(o => !IM.OD.ContainsKey(o));
            if (!template.SosigPrefabs.Any())
                template.SosigPrefabs.Add("SosigBody_Default");

            template.WeaponOptions.RemoveAll(o => !IM.OD.ContainsKey(o));
            template.WeaponOptionsSecondary.RemoveAll(o => !IM.OD.ContainsKey(o));
            template.WeaponOptionsTertiary.RemoveAll(o => !IM.OD.ContainsKey(o));

            // Loop through all outfit configs and remove any clothing objects that don't exist
            foreach (OutfitConfig config in template.OutfitConfigs)
            {
                config.Headwear.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Headwear.Any())
                    config.Chance_Headwear = 0;

                config.Eyewear.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Eyewear.Any())
                    config.Chance_Eyewear = 0;

                config.Facewear.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Facewear.Any())
                    config.Chance_Facewear = 0;

                config.Torsowear.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Torsowear.Any())
                    config.Chance_Torsowear = 0;

                config.Pantswear.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Pantswear.Any())
                    config.Chance_Pantswear = 0;

                config.Pantswear_Lower.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Pantswear_Lower.Any())
                    config.Chance_Pantswear_Lower = 0;

                config.Backpacks.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Backpacks.Any())
                    config.Chance_Backpacks = 0;

                config.TorosDecoration.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.TorosDecoration.Any())
                    config.Chance_TorosDecoration = 0;

                config.Belt.RemoveAll(o => !IM.OD.ContainsKey(o));
                if (!config.Belt.Any())
                    config.Chance_Belt = 0;
            }
        }

        public static SosigEnemyID ParseEnemyType(string enemyType)
        {
            SosigEnemyID id;

            try
            {
                id = (SosigEnemyID)Enum.Parse(typeof(SosigEnemyID), enemyType, true);
            }
            catch
            {
                if (enemyType.StartsWith("M_Greas", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.M_GreaseGremlins_Guard;
                else if (enemyType.StartsWith("M_Grin", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.M_GrinchyGoobs_Guard;
                else if (enemyType.StartsWith("M_Merc", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.M_MercWiener_Guard;
                else if (enemyType.StartsWith("M_Pops", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.M_Popsicles_Guard;
                else if (enemyType.StartsWith("M_Veggi", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.M_VeggieDawgs_Guard;
                else if (enemyType.StartsWith("W_Tan", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.W_Tan_Guard;
                else if (enemyType.StartsWith("W_Brown", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.W_Brown_Guard;
                else if (enemyType.StartsWith("W_Grey", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.W_Grey_Guard;
                else if (enemyType.StartsWith("W_Gray", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.W_Grey_Guard;
                else if (enemyType.StartsWith("W_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.W_Green_Guard;
                else if (enemyType.StartsWith("D_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.D_Gunfighter;
                else if (enemyType.StartsWith("J_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.J_Guard;
                else if (enemyType.StartsWith("H_Bre", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.H_BreadCrabZombie_Standard;
                else if (enemyType.StartsWith("H_Ober", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.H_OberwurstSoldier_Shotgun;
                else if (enemyType.StartsWith("H_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.H_CivicErection_Pistol;
                else if (enemyType.StartsWith("MF_Blue", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.MF_BlueFranks_Scout;
                else if (enemyType.StartsWith("MF_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.MF_RedHots_Scout;
                else if (enemyType.StartsWith("Mountain", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.MountainMeat_Pistol;
                else if (enemyType.StartsWith("RW_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.RW_Rot;
                else if (enemyType.StartsWith("RWP_Pac", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.RWP_PacSquad_Trooper;
                else if (enemyType.StartsWith("RWP_Pros", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.RWP_Prospector_Pistol;
                else if (enemyType.StartsWith("RWP_Skul", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.RWP_Skulker_Pistol;
                else if (enemyType.StartsWith("RWP_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.RWP_Cultist;
                else if (enemyType.StartsWith("Junkbot", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Junkbot_Patrol;
                else if (enemyType.StartsWith("MG_Spec", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.MG_Special_Duelist;
                else if (enemyType.StartsWith("MG_", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.MG_Soldier_LInfantry_Rifle;
                else if (enemyType.StartsWith("Kolbasa_PMC", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Kolbasa_PMC_Pistols;
                else if (enemyType.StartsWith("Kolbasa_Swe", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Kolbasa_SweatyPMC_Rifle;
                else if (enemyType.StartsWith("Kolbasa_Bos", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Kolbasa_Boss_Kotleta;
                else if (enemyType.StartsWith("Kolbas", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Kolbasa_Scavenger_Pistols;
                else if (enemyType.StartsWith("Comperator_Heavy_Tier1", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Heavy_Tier1_DoubleBarrels;
                else if (enemyType.StartsWith("Comperator_Heavy_Tier2", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Heavy_Tier2_Shotgun;
                else if (enemyType.StartsWith("Comperator_Heavy_Tier4", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Heavy_Tier4_Rifle;
                else if (enemyType.StartsWith("Comperator_Heavy_Tier5", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Heavy_Tier5_LMG;
                else if (enemyType.StartsWith("Comperator_Heavy", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Heavy_Tier3_Pistols;
                else if (enemyType.StartsWith("Comperator_Medium_Tier1", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Medium_Tier1_DoubleBarrels;
                else if (enemyType.StartsWith("Comperator_Medium_Tier2", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Medium_Tier2_BoltAction;
                else if (enemyType.StartsWith("Comperator_Medium_Tier4", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Medium_Tier4_DMR;
                else if (enemyType.StartsWith("Comperator_Medium_Tier5", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Medium_Tier5_LMG;
                else if (enemyType.StartsWith("Comperator_Medium", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Medium_Tier3_Pistols;
                else if (enemyType.StartsWith("Comperator_Light_Tier1", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Light_Tier1_Melee;
                else if (enemyType.StartsWith("Comperator_Light_Tier2", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Light_Tier2_Shotgun;
                else if (enemyType.StartsWith("Comperator_Light_Tier4", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Light_Tier4_SMG;
                else if (enemyType.StartsWith("Comperator_Light_Tier5", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Light_Tier5_PDW;
                else if (enemyType.StartsWith("Comp", StringComparison.OrdinalIgnoreCase))
                    id = SosigEnemyID.Comperator_Light_Tier3_Pistols;
                else
                    id = SosigEnemyID.M_Swat_Guard;

                TNHFrameworkLogger.Log($"Sosig ID [{enemyType}] does not exist! Substituting with [{id}]", TNHFrameworkLogger.LogType.General);
            }

            return id;
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

            GameObject rootObj = null;
            List<GameObject> toDealWith = [];
            FVRFireArm myGun = null;
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

                if (j == 0)
                {
                    rootObj = gameObject;
                    validIndexes.Add(j);
                }

                FVRPhysicalObject component = gameObject.GetComponent<FVRPhysicalObject>();
                dicGO.Add(gameObject, gun.Components[j]);
                dicByIndex.Add(gun.Components[j].Index, gameObject);
                component.ConfigureFromFlagDic(gun.Components[j].Flags);

                if (gun.Components[j].isFirearm)
                {
                    TNHFrameworkLogger.Log($"Firearm [{gun.Components[j].ObjectID}] is index ({j})", TNHFrameworkLogger.LogType.General);
                    myGun = gameObject.GetComponent<FVRFireArm>();
                    savedGun.ApplyFirearmProperties(myGun);

                    LastSpawnedGun = gameObject;

                    gameObject.transform.position = position;
                    gameObject.transform.rotation = Quaternion.identity;

                    if (myGun.Magazine != null && myGun.Magazine.IsIntegrated)
                    {
                        myGun.Magazine.ReloadMagWithList(gun.LoadedRoundsInMag);
                        myGun.Magazine.IsInfinite = false;
                    }

                    myGun.SetLoadedChambers(gun.LoadedRoundsInChambers);
                }
                else if (gun.Components[j].isMagazine)
                {
                    TNHFrameworkLogger.Log($"Reloading magazine [{gun.Components[j].ObjectID}] attached to ({gun.Components[j].ObjectAttachedTo})", TNHFrameworkLogger.LogType.General);
                    toDealWith.Add(gameObject);
                    FVRFireArmMagazine component2 = gameObject.GetComponent<FVRFireArmMagazine>();
                    component2.ReloadMagWithList(gun.LoadedRoundsInMag);
                    component2.IsInfinite = false;
                }
                else if (gun.Components[j].isAttachment)
                {
                    toDealWith.Add(gameObject);
                }
                else if (gameObject.GetComponent<FVRFireArmRound>() != null && gun.LoadedRoundsInMag.Any())
                {
                    FVRFireArmRound component3 = gameObject.GetComponent<FVRFireArmRound>();

                    for (int k = 0; k < gun.LoadedRoundsInMag.Count; k++)
                    {
                        component3.AddProxy(gun.LoadedRoundsInMag[k], AM.GetRoundSelfPrefab(component3.RoundType, gun.LoadedRoundsInMag[k]));
                    }

                    component3.UpdateProxyDisplay();
                }
                else if (gameObject.GetComponent<Speedloader>() != null && gun.LoadedRoundsInMag.Any())
                {
                    Speedloader component4 = gameObject.GetComponent<Speedloader>();
                    component4.ReloadSpeedLoaderWithList(gun.LoadedRoundsInMag);
                }
                else if (gameObject.GetComponent<FVRFireArmClip>() != null && gun.LoadedRoundsInMag.Any())
                {
                    FVRFireArmClip component5 = gameObject.GetComponent<FVRFireArmClip>();
                    component5.ReloadClipWithList(gun.LoadedRoundsInMag);
                }
            }
            
            int BreakIterator = 400;
            while (toDealWith.Any() && BreakIterator > 0)
            {
                BreakIterator--;
                
                for (int l = toDealWith.Count - 1; l >= 0; l--)
                {
                    SavedGunComponent savedGunComponent = dicGO[toDealWith[l]];

                    // Correction for older vault files
                    if (savedGunComponent.ObjectAttachedTo == -1)
                        savedGunComponent.ObjectAttachedTo = 0;

                    if (validIndexes.Contains(savedGunComponent.ObjectAttachedTo))
                    {
                        GameObject gameObject2 = toDealWith[l];
                        if (gameObject2.GetComponent<FVRFireArmAttachment>() != null)
                        {
                            FVRFireArmAttachment component6 = gameObject2.GetComponent<FVRFireArmAttachment>();
                            FVRFireArmAttachmentMount mount = GetMount(dicByIndex[savedGunComponent.ObjectAttachedTo], savedGunComponent.MountAttachedTo);
                            gameObject2.transform.rotation = Quaternion.LookRotation(savedGunComponent.OrientationForward, savedGunComponent.OrientationUp);
                            gameObject2.transform.position = GetPositionRelativeToGun(savedGunComponent, myGun.transform);

                            if (component6.CanScaleToMount && mount.CanThisRescale())
                                component6.ScaleToMount(mount);

                            component6.AttachToMount(mount, false);

                            if (component6 is Suppressor)
                                (component6 as Suppressor).AutoMountWell();

                            validIndexes.Add(savedGunComponent.Index);
                            toDealWith.RemoveAt(l);
                        }
                        else if (gameObject2.GetComponent<FVRFireArmMagazine>() != null)
                        {
                            FVRFireArmMagazine component7 = gameObject2.GetComponent<FVRFireArmMagazine>();

                            GameObject gameObject3 = dicByIndex[savedGunComponent.ObjectAttachedTo];
                            FVRFireArm component8 = gameObject3.GetComponent<FVRFireArm>();
                            AttachableFirearmPhysicalObject component9 = gameObject3.GetComponent<AttachableFirearmPhysicalObject>();

                            SavedGunComponent savedGunComponent2 = dicGO[gameObject2];
                            TNHFrameworkLogger.Log($"Attaching magazine [{savedGunComponent.ObjectID}] to [{savedGunComponent2.ObjectID}] ({savedGunComponent2.MountAttachedTo})", TNHFrameworkLogger.LogType.General);

                            if (savedGunComponent2.MountAttachedTo < 0)
                            {
                                if (component8 != null)
                                {
                                    TNHFrameworkLogger.Log($"  Regular magazine", TNHFrameworkLogger.LogType.General);
                                    component7.transform.position = component8.GetMagMountPos(component7.IsBeltBox).position;
                                    component7.transform.rotation = component8.GetMagMountPos(component7.IsBeltBox).rotation;
                                    component7.Load(component8);
                                }

                                if (component9 != null)
                                {
                                    TNHFrameworkLogger.Log($"  Attachable firearm magazine", TNHFrameworkLogger.LogType.General);
                                    component7.transform.position = component9.FA.MagazineMountPos.position;
                                    component7.transform.rotation = component9.FA.MagazineMountPos.rotation;
                                    component7.Load(component9.FA);
                                }
                            }
                            else if (component8 != null)
                            {
                                TNHFrameworkLogger.Log($"  Secondary magazine slot", TNHFrameworkLogger.LogType.General);
                                component7.transform.position = component8.SecondaryMagazineSlots[savedGunComponent2.MountAttachedTo].MagazineMountPos.position;
                                component7.transform.rotation = component8.SecondaryMagazineSlots[savedGunComponent2.MountAttachedTo].MagazineMountPos.rotation;
                                component7.LoadIntoSecondary(component8, savedGunComponent2.MountAttachedTo);
                            }

                            toDealWith.RemoveAt(l);
                        }
                    }
                }
            }
            
            rootObj.transform.position = position;
            rootObj.transform.rotation = rotation;

            if (addForce)
                AddSpawningForce(rootObj.GetComponent<Rigidbody>());

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

            // ODK - Check for premades and fix them. They already have attachments, but they aren't registered properly.
            if (TNHFramework.FixLegacyModulGuns.Value)
                FixPremadeFirearm(LastSpawnedGun, true);

            // Add force and torque
            if (addForce)
                AddSpawningForce(o.GetGameObject().GetComponent<Rigidbody>());

            IsSpawning = false;
            yield break;
        }

        private static void AttachToMount(FVRFireArmAttachment attachment, FVRFireArmAttachmentMount mount)
        {
            if (attachment == null || mount == null)
                return;

            if (attachment.CanScaleToMount && mount.CanThisRescale() && mount.GetRootMount().ScaleModifier > 0.01f)
                attachment.ScaleToMount(mount);

            attachment.AttachToMount(mount, false);

            if (attachment is Suppressor)
                (attachment as Suppressor).AutoMountWell();
        }

        // Add force and torque
        private static void AddSpawningForce(Rigidbody rigidbody)
        {
            if (rigidbody == null)
                return;

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
