using ADepIn;
using FistVR;
using System.Collections.Generic;
using System.Linq;
using TNHFramework.Utilities;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace TNHFramework.ObjectTemplates
{
    public class CustomCharacter()
    {
        public string DisplayName;
        public string Description;
        public CategoryInfo CategoryData = new();
        public int CharacterGroup;
        public string TableID;
        public int StartingTokens;
        public bool ForceAllAgentWeapons;
        public bool ForceDisableOutfitFunctionality;
        public bool UsesPurchasePriceIncrement;
        public bool DisableCleanupSosigDrops;
        public List<TagEra> ValidAmmoEras = [];
        public List<TagSet> ValidAmmoSets = [];
        public List<string> GlobalObjectBlacklist = [];
        public List<string> GlobalAmmoBlacklist = [];
        public List<MagazineBlacklistEntry> MagazineBlacklist = [];

        public EquipmentGroup RequireSightTable = new();
        public LoadoutEntry PrimaryWeapon = new();
        public LoadoutEntry SecondaryWeapon = new();
        public LoadoutEntry TertiaryWeapon = new();
        public LoadoutEntry PrimaryItem = new();
        public LoadoutEntry SecondaryItem = new();
        public LoadoutEntry TertiaryItem = new();
        public LoadoutEntry Shield = new();
        public List<EquipmentPool> EquipmentPools = [];
        public List<Level> Levels = [];
        public List<Level> LevelsEndless = [];

        [JsonIgnore]
        private TNH_CharacterDef character;

        [JsonIgnore]
        private Dictionary<string, MagazineBlacklistEntry> magazineBlacklistDict;

        [JsonIgnore]
        public bool isCustom = false;

        public CustomCharacter(TNH_CharacterDef character) : this()
        {
            isCustom = false;
            DisplayName = character.DisplayName;
            CategoryData = new CategoryInfo();
            switch (character.Group)
            {
                case TNH_CharacterDef.CharacterGroup.DaringDefaults:
                    CategoryData.Name = "Daring Defaults";
                    break;

                case TNH_CharacterDef.CharacterGroup.WienersThroughTime:
                    CategoryData.Name = "Wieners Through Time";
                    break;

                case TNH_CharacterDef.CharacterGroup.MemetasticMeats:
                    CategoryData.Name = "Memetastic Meats";
                    break;

                case TNH_CharacterDef.CharacterGroup.ChallengingMeals:
                    CategoryData.Name = "Competitive Casings";
                    break;
            }
            CategoryData.Priority = (int)character.Group;
            CharacterGroup = (int)character.Group;

            TableID = character.TableID;
            StartingTokens = character.StartingTokens;
            ForceAllAgentWeapons = character.ForceAllAgentWeapons;
            Description = character.Description;
            UsesPurchasePriceIncrement = character.UsesPurchasePriceIncrement;
            ValidAmmoEras = [.. character.ValidAmmoEras.Select(o => (TagEra)o)];
            ValidAmmoSets = [.. character.ValidAmmoSets.Select(o => (TagSet)o)];
            GlobalObjectBlacklist = [];
            GlobalAmmoBlacklist = [];
            MagazineBlacklist = [];
            PrimaryWeapon = character.Has_Weapon_Primary ? new LoadoutEntry(character.Weapon_Primary) : new();
            SecondaryWeapon = character.Has_Weapon_Secondary ? new LoadoutEntry(character.Weapon_Secondary) : new();
            TertiaryWeapon = character.Has_Weapon_Tertiary ? new LoadoutEntry(character.Weapon_Tertiary) : new();
            PrimaryItem = character.Has_Item_Primary ? new LoadoutEntry(character.Item_Primary) : new();
            SecondaryItem = character.Has_Item_Secondary ? new LoadoutEntry(character.Item_Secondary) : new();
            TertiaryItem = character.Has_Item_Tertiary ? new LoadoutEntry(character.Item_Tertiary) : new();
            Shield = character.Has_Item_Shield ? new LoadoutEntry(character.Item_Shield) : new();

            RequireSightTable = new EquipmentGroup(character.RequireSightTable);

            EquipmentPools = [.. character.EquipmentPool.Entries.Select(o => new EquipmentPool(o))];
            Levels = [.. character.Progressions[0].Levels.Select(o => new Level(o))];
            LevelsEndless = [.. character.Progressions_Endless[0].Levels.Select(o => new Level(o))];

            ForceDisableOutfitFunctionality = false;

            this.character = character;
        }

        public CustomCharacter(V1.CustomCharacter character) : this()
        {
            isCustom = character.isCustom;
            DisplayName = character.DisplayName;
            Description = character.Description;
            CategoryData = new CategoryInfo(character.CategoryData.Name, character.CategoryData.Priority);

            if (CategoryData.Name == "")
            {
                CategoryData.Name = character.CharacterGroup switch
                {
                    0 => "Daring Defaults",
                    1 => "Wieners Through Time",
                    2 => "Memetastic Meats",
                    3 => "Competitive Casings",
                    _ => "Daring Defaults",
                };

                CategoryData.Priority = (character.CharacterGroup < 4) ? character.CharacterGroup : 0;
            }
            else if (CategoryData.Name == "Daring Defaults")
            {
                CategoryData.Priority = 0;
            }
            else if (CategoryData.Name == "Wieners Through Time")
            {
                CategoryData.Priority = 1;
            }
            else if (CategoryData.Name == "Memetastic Meats")
            {
                CategoryData.Priority = 2;
            }
            else if (CategoryData.Name == "Competitive Casings")
            {
                CategoryData.Priority = 3;
            }

            CharacterGroup = CategoryData.Priority;

            TableID = character.TableID;
            StartingTokens = character.StartingTokens;
            ForceAllAgentWeapons = character.ForceAllAgentWeapons;
            ForceDisableOutfitFunctionality = character.ForceDisableOutfitFunctionality;
            UsesPurchasePriceIncrement = character.UsesPurchasePriceIncrement;
            DisableCleanupSosigDrops = character.DisableCleanupSosigDrops;
            ValidAmmoEras = character.ValidAmmoEras ?? [];
            ValidAmmoSets = character.ValidAmmoSets ?? [];
            GlobalObjectBlacklist = character.GlobalObjectBlacklist ?? [];
            GlobalAmmoBlacklist = character.GlobalAmmoBlacklist ?? [];
            MagazineBlacklist = character.MagazineBlacklist ?? [];

            RequireSightTable = new EquipmentGroup(character.RequireSightTable);
            PrimaryWeapon = character.HasPrimaryWeapon? new LoadoutEntry(character.PrimaryWeapon) : new();
            SecondaryWeapon = character.HasSecondaryWeapon ? new LoadoutEntry(character.SecondaryWeapon) : new();
            TertiaryWeapon = character.HasTertiaryWeapon ? new LoadoutEntry(character.TertiaryWeapon) : new();
            PrimaryItem = character.HasPrimaryItem ? new LoadoutEntry(character.PrimaryItem) : new();
            SecondaryItem = character.HasSecondaryItem ? new LoadoutEntry(character.SecondaryItem) : new();
            TertiaryItem = character.HasTertiaryItem ? new LoadoutEntry(character.TertiaryItem) : new();
            Shield = character.HasShield ? new LoadoutEntry(character.Shield) : new();

            EquipmentPools = [];
            foreach (V1.EquipmentPool oldPool in character.EquipmentPools)
            {
                EquipmentPools.Add(new EquipmentPool(oldPool));
            }

            Levels = [];
            LevelsEndless = [];

            foreach (V1.Level oldLevel in character.Levels)
            {
                Levels.Add(new Level(oldLevel));
            }

            foreach (V1.Level oldLevel in character.LevelsEndless)
            {
                LevelsEndless.Add(new Level(oldLevel));
            }
        }

        public TNH_CharacterDef GetCharacter(int ID, Sprite thumbnail)
        {
            if (character == null)
            {
                ValidAmmoSets ??= [];
                ValidAmmoEras ??= [];

                character = (TNH_CharacterDef)ScriptableObject.CreateInstance(typeof(TNH_CharacterDef));
                character.DisplayName = DisplayName;
                character.CharacterID = (TNH_Char)ID;
                character.Group = (TNH_CharacterDef.CharacterGroup)CategoryData.Priority;
                character.TableID = TableID;
                character.StartingTokens = StartingTokens;
                character.ForceAllAgentWeapons = ForceAllAgentWeapons;
                character.Description = Description;
                character.UsesPurchasePriceIncrement = UsesPurchasePriceIncrement;
                character.ValidAmmoEras = [.. ValidAmmoEras.Select(o => (FVRObject.OTagEra)o)];
                character.ValidAmmoSets = [.. ValidAmmoSets.Select(o => (FVRObject.OTagSet)o)];
                character.Picture = thumbnail;
                character.Weapon_Primary = PrimaryWeapon.GetLoadoutEntry();
                character.Weapon_Secondary = SecondaryWeapon.GetLoadoutEntry();
                character.Weapon_Tertiary = TertiaryWeapon.GetLoadoutEntry();
                character.Item_Primary = PrimaryItem.GetLoadoutEntry();
                character.Item_Secondary = SecondaryItem.GetLoadoutEntry();
                character.Item_Tertiary = TertiaryItem.GetLoadoutEntry();
                character.Item_Shield = Shield.GetLoadoutEntry();

                character.Has_Weapon_Primary = PrimaryWeapon.PrimaryGroup != null || PrimaryWeapon.BackupGroup != null;
                character.Has_Weapon_Secondary = SecondaryWeapon.PrimaryGroup != null || SecondaryWeapon.BackupGroup != null;
                character.Has_Weapon_Tertiary = TertiaryWeapon.PrimaryGroup != null || TertiaryWeapon.BackupGroup != null;
                character.Has_Item_Primary = PrimaryItem.PrimaryGroup != null || PrimaryItem.BackupGroup != null;
                character.Has_Item_Secondary = SecondaryItem.PrimaryGroup != null || SecondaryItem.BackupGroup != null;
                character.Has_Item_Tertiary = TertiaryItem.PrimaryGroup != null || TertiaryItem.BackupGroup != null;
                character.Has_Item_Shield = Shield.PrimaryGroup != null || Shield.BackupGroup != null;

                character.RequireSightTable = RequireSightTable.GetObjectTableDef();
                character.EquipmentPool = (EquipmentPoolDef)ScriptableObject.CreateInstance(typeof(EquipmentPoolDef));
                character.EquipmentPool.Entries = [.. EquipmentPools.Select(o => o.GetPoolEntry())];

                character.Progressions = [(TNH_Progression)ScriptableObject.CreateInstance(typeof(TNH_Progression))];
                character.Progressions[0].Levels = [];
                foreach (Level level in Levels)
                {
                    character.Progressions[0].Levels.Add(level.GetLevel());
                }

                character.Progressions_Endless = [(TNH_Progression)ScriptableObject.CreateInstance(typeof(TNH_Progression))];
                character.Progressions_Endless[0].Levels = [];
                foreach (Level level in LevelsEndless)
                {
                    character.Progressions_Endless[0].Levels.Add(level.GetLevel());
                }
            }

            return character;
        }

        public TNH_CharacterDef GetCharacter()
        {
            if (character == null)
            {
                TNHFrameworkLogger.LogError("Tried to get character, but it hasn't been initialized yet! Returning null! Character Name : " + DisplayName);
                return null;
            }

            return character;
        }

        public Dictionary<string, MagazineBlacklistEntry> GetMagazineBlacklist()
        {
            return magazineBlacklistDict;
        }

        public Level GetCurrentLevel(TNH_Progression.Level currLevel)
        {
            foreach (Level level in Levels)
            {
                if (level.GetLevel().Equals(currLevel))
                {
                    return level;
                }
            }

            foreach (Level level in LevelsEndless)
            {
                if (level.GetLevel().Equals(currLevel))
                {
                    return level;
                }
            }

            return null;
        }

        public Phase GetCurrentPhase(TNH_HoldChallenge.Phase currPhase)
        {
            foreach (Level level in Levels)
            {
                foreach (Phase phase in level.HoldPhases)
                {
                    if (phase.GetPhase().Equals(currPhase))
                    {
                        return phase;
                    }
                }
            }

            foreach (Level level in LevelsEndless)
            {
                foreach (Phase phase in level.HoldPhases)
                {
                    if (phase.GetPhase().Equals(currPhase))
                    {
                        return phase;
                    }
                }
            }

            return null;
        }

        public bool CharacterUsesSosig(string id)
        {
            foreach (Level level in Levels)
            {
                if (level.LevelUsesSosig(id))
                    return true;
            }

            foreach (Level level in LevelsEndless)
            {
                if (level.LevelUsesSosig(id))
                    return true;
            }

            return false;
        }

        public void DelayedInit()
        {
            TNHFrameworkLogger.Log("Delayed init of character: " + DisplayName, TNHFrameworkLogger.LogType.Character);

            TNHFrameworkLogger.Log("Init of Primary Weapon", TNHFrameworkLogger.LogType.Character);
            if (PrimaryWeapon != null && !PrimaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Primary = false;
                PrimaryWeapon = null;
            }

            TNHFrameworkLogger.Log("Init of Secondary Weapon", TNHFrameworkLogger.LogType.Character);
            if (SecondaryWeapon != null && !SecondaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Secondary = false;
                SecondaryWeapon = null;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Weapon", TNHFrameworkLogger.LogType.Character);
            if (TertiaryWeapon != null && !TertiaryWeapon.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting weapon had no pools to spawn from, and will not spawn equipment!");
                character.Has_Weapon_Tertiary = false;
                TertiaryWeapon = null;
            }

            TNHFrameworkLogger.Log("Init of Primary Item", TNHFrameworkLogger.LogType.Character);
            if (PrimaryItem != null && !PrimaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Primary = false;
                PrimaryItem = null;
            }

            TNHFrameworkLogger.Log("Init of Secondary Item", TNHFrameworkLogger.LogType.Character);
            if (SecondaryItem != null && !SecondaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Secondary = false;
                SecondaryItem = null;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Item", TNHFrameworkLogger.LogType.Character);
            if (TertiaryItem != null && !TertiaryItem.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Tertiary = false;
                TertiaryItem = null;
            }

            TNHFrameworkLogger.Log("Init of Shield", TNHFrameworkLogger.LogType.Character);
            if (Shield != null && !Shield.DelayedInit(GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Shield starting item had no pools to spawn from, and will not spawn equipment!");
                character.Has_Item_Shield = false;
                Shield = null;
            }

            TNHFrameworkLogger.Log("Init of required sights table", TNHFrameworkLogger.LogType.Character);
            if (RequireSightTable != null && !RequireSightTable.DelayedInit(GlobalObjectBlacklist, false))
            {
                TNHFrameworkLogger.LogWarning("Required sight table was empty, guns will not spawn with required sights");
                RequireSightTable = null;
            }

            TNHFrameworkLogger.Log("Init of equipment pools", TNHFrameworkLogger.LogType.Character);
            magazineBlacklistDict = [];

            if (MagazineBlacklist != null)
            {
                foreach (MagazineBlacklistEntry entry in MagazineBlacklist)
                {
                    magazineBlacklistDict.Add(entry.FirearmID, entry);
                }
            }

            for (int i = EquipmentPools.Count - 1; i >= 0; i--)
            {
                EquipmentPool pool = EquipmentPools[i];
                if (!pool.DelayedInit(GlobalObjectBlacklist))
                {
                    TNHFrameworkLogger.LogWarning("Equipment pool had an empty table! Removing it so that it can't spawn!");
                    EquipmentPools.RemoveAt(i);
                    character.EquipmentPool.Entries.RemoveAt(i);
                }
            }

            TNHFrameworkLogger.Log("Init of levels", TNHFrameworkLogger.LogType.Character);
            for (int i = 0; i < Levels.Count; i++)
            {
                Levels[i].DelayedInit(isCustom, i);
            }

            TNHFrameworkLogger.Log("Init of endless levels", TNHFrameworkLogger.LogType.Character);
            for (int i = 0; i < LevelsEndless.Count; i++)
            {
                LevelsEndless[i].DelayedInit(isCustom, i);
            }
        }
    }


    public class CategoryInfo(string name = "", int priority = 0)
    {
        public string Name = name;
        public int Priority = priority;
    }


    public class MagazineBlacklistEntry()
    {
        public string FirearmID;
        public List<string> MagazineBlacklist = [];
        public List<string> MagazineWhitelist = [];
        public List<string> ClipBlacklist = [];
        public List<string> ClipWhitelist = [];
        public List<string> SpeedLoaderBlacklist = [];
        public List<string> SpeedLoaderWhitelist = [];
        public List<string> RoundBlacklist = [];
        public List<string> RoundWhitelist = [];

        public bool IsItemBlacklisted(string itemID)
        {
            return MagazineBlacklist.Contains(itemID) || ClipBlacklist.Contains(itemID) || RoundBlacklist.Contains(itemID) || SpeedLoaderBlacklist.Contains(itemID);
        }

        public bool IsMagazineAllowed(string itemID)
        {
            if (MagazineWhitelist.Any() && !MagazineWhitelist.Contains(itemID))
            {
                return false;
            }

            if (MagazineBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsClipAllowed(string itemID)
        {
            if (ClipWhitelist.Any() && !ClipWhitelist.Contains(itemID))
            {
                return false;
            }

            if (ClipBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsSpeedloaderAllowed(string itemID)
        {
            if (SpeedLoaderWhitelist.Any() && !SpeedLoaderWhitelist.Contains(itemID))
            {
                return false;
            }

            if (SpeedLoaderBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }

        public bool IsRoundAllowed(string itemID)
        {
            if (RoundWhitelist.Any() && !RoundWhitelist.Contains(itemID))
            {
                return false;
            }

            if (RoundBlacklist.Contains(itemID))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// An equipment pool is an entry that can spawn at a constructor panel
    /// </summary>
    public class EquipmentPool()
    {
        public EquipmentPoolDef.PoolEntry.PoolEntryType Type;
        public string IconName;
        public int TokenCost;
        public int TokenCostLimited;
        public int MinLevelAppears;
        public int MaxLevelAppears;
        public bool SpawnsInSmallCase;
        public bool SpawnsInLargeCase;
        public EquipmentGroup PrimaryGroup = new();
        public EquipmentGroup BackupGroup = new();

        [JsonIgnore]
        private EquipmentPoolDef.PoolEntry pool;

        public EquipmentPool(V1.EquipmentPool oldPool) : this()
        {
            if (oldPool == null)
                return;

            Type = oldPool.Type;
            IconName = oldPool.IconName;
            TokenCost = oldPool.TokenCost;
            TokenCostLimited = oldPool.TokenCostLimited;
            MinLevelAppears = oldPool.MinLevelAppears;
            MaxLevelAppears = oldPool.MaxLevelAppears;
            SpawnsInSmallCase = oldPool.SpawnsInSmallCase;
            SpawnsInLargeCase = oldPool.SpawnsInLargeCase;
            PrimaryGroup = new EquipmentGroup(oldPool.PrimaryGroup);
            BackupGroup = new EquipmentGroup(oldPool.BackupGroup);
            pool = oldPool.GetPoolEntry();
        }

        public EquipmentPool(EquipmentPoolDef.PoolEntry pool) : this()
        {
            Type = pool.Type;
            IconName = pool.TableDef.Icon.name;
            TokenCost = pool.TokenCost;
            TokenCostLimited = pool.TokenCost_Limited;
            MinLevelAppears = pool.MinLevelAppears;
            MaxLevelAppears = pool.MaxLevelAppears;
            PrimaryGroup = new EquipmentGroup(pool.TableDef)
            {
                Rarity = pool.Rarity
            };
            SpawnsInLargeCase = pool.TableDef.SpawnsInLargeCase;
            SpawnsInSmallCase = pool.TableDef.SpawnsInSmallCase;
            BackupGroup = new EquipmentGroup();

            this.pool = pool;
        }

        public EquipmentPoolDef.PoolEntry GetPoolEntry()
        {
            if (pool == null)
            {
                pool = new EquipmentPoolDef.PoolEntry
                {
                    Type = Type,
                    TokenCost = TokenCost,
                    TokenCost_Limited = TokenCostLimited,
                    MinLevelAppears = MinLevelAppears,
                    MaxLevelAppears = MaxLevelAppears
                };

                if (PrimaryGroup != null)
                {
                    pool.Rarity = PrimaryGroup.Rarity;
                }
                else
                {
                    pool.Rarity = 1;
                }

                pool.TableDef = PrimaryGroup.GetObjectTableDef();
                pool.TableDef.SpawnsInLargeCase = SpawnsInLargeCase;
                pool.TableDef.SpawnsInSmallCase = SpawnsInSmallCase;
            }

            return pool;
        }

        public bool DelayedInit(List<string> globalObjectBlacklist)
        {
            if (pool != null)
            {
                if (LoadedTemplateManager.DefaultIconSprites.ContainsKey(IconName))
                {
                    if (pool.TableDef == null)
                    {
                        pool.TableDef = (PrimaryGroup as EquipmentGroup).GetObjectTableDef();
                    }
                    pool.TableDef.Icon = LoadedTemplateManager.DefaultIconSprites[IconName];
                    pool.TableDef.SpawnsInLargeCase = SpawnsInLargeCase;
                    pool.TableDef.SpawnsInSmallCase = SpawnsInSmallCase;
                }

                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(globalObjectBlacklist, true))
                    {
                        TNHFrameworkLogger.Log("Primary group for equipment pool entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(globalObjectBlacklist, true))
                    {
                        if (PrimaryGroup == null)
                            TNHFrameworkLogger.Log("Backup group for equipment pool entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);

                        BackupGroup = null;
                    }
                }

                return PrimaryGroup != null || BackupGroup != null;
            }

            return false;
        }

        public List<EquipmentGroup> GetSpawnedEquipmentGroups()
        {
            if (PrimaryGroup != null)
            {
                return PrimaryGroup.GetSpawnedEquipmentGroups();
            }
            else if (BackupGroup != null)
            {
                return BackupGroup.GetSpawnedEquipmentGroups();
            }

            TNHFrameworkLogger.LogWarning("EquipmentPool had both PrimaryGroup and BackupGroup set to null! Returning an empty list for spawned equipment");
            return [];
        }

        public override string ToString()
    {
            string output = "Equipment Pool : IconName=" + IconName + " : CostLimited=" + TokenCostLimited + " : CostSpawnlock=" + TokenCost;

            if (PrimaryGroup != null)
            {
                output += "\nPrimary Group";
                output += PrimaryGroup.ToString(0);
            }

            if (BackupGroup != null)
            {
                output += "\nBackup Group";
                output += BackupGroup.ToString(0);
            }

            return output;
        }
    }

    public class EquipmentGroup()
    {
        public ObjectCategory Category;
        public float Rarity;
        public int ItemsToSpawn;
        public int MinAmmoCapacity;
        public int MaxAmmoCapacity;
        public int NumMagsSpawned;
        public int NumClipsSpawned;
        public int NumRoundsSpawned;
        public bool SpawnMagAndClip;
        public float BespokeAttachmentChance;
        public bool IsCompatibleMagazine;
        public bool AutoPopulateGroup;
        public bool ForceSpawnAllSubPools;
        public List<string> IDOverride = [];
        public List<string> IDOverrideBackup = [];
        public FVRTags Tags = new();
        public List<EquipmentGroup> SubGroups = [];

        [JsonIgnore]
        private ObjectTableDef objectTableDef;

        [JsonIgnore]
        private List<string> objects = [];

        public EquipmentGroup(V1.EquipmentGroup thing) : this()
        {
            if (thing == null)
                return;

            Category = thing.Category;
            Rarity = thing.Rarity;
            ItemsToSpawn = thing.ItemsToSpawn;
            MinAmmoCapacity = thing.MinAmmoCapacity;
            MaxAmmoCapacity = thing.MaxAmmoCapacity;
            NumMagsSpawned = thing.NumMagsSpawned;
            NumClipsSpawned = thing.NumClipsSpawned;
            NumRoundsSpawned = thing.NumRoundsSpawned;
            SpawnMagAndClip = thing.SpawnMagAndClip;
            BespokeAttachmentChance = thing.BespokeAttachmentChance;
            IsCompatibleMagazine = thing.IsCompatibleMagazine;
            AutoPopulateGroup = thing.AutoPopulateGroup;
            ForceSpawnAllSubPools = thing.ForceSpawnAllSubPools;
            IDOverride = thing.IDOverride ?? [];
            IDOverrideBackup = thing.IDOverrideBackup ?? [];

            thing.CopyTags();

            Tags = new()
            {
                MinYear = thing.MinYear,
                MaxYear = thing.MaxYear,
                Eras = thing.Eras ?? [],
                Sets = thing.Sets ?? [],
                Sizes = thing.Sizes ?? [],
                Actions = thing.Actions ?? [],
                Modes = thing.Modes ?? [],
                ExcludedModes = thing.ExcludedModes ?? [],
                FeedOptions = thing.FeedOptions ?? [],
                MountsAvailable = thing.MountsAvailable ?? [],
                RoundPowers = thing.RoundPowers ?? [],
                Features = thing.Features ?? [],
                MeleeStyles = thing.MeleeStyles ?? [],
                MeleeHandedness = thing.MeleeHandedness ?? [],
                MountTypes = thing.MountTypes ?? [],
                PowerupTypes = thing.PowerupTypes ?? [],
                ThrownTypes = thing.ThrownTypes ?? [],
                ThrownDamageTypes = thing.ThrownDamageTypes ?? []
            };

            SubGroups = [];

            foreach (V1.EquipmentGroup subGroup in thing.SubGroups)
            {
                SubGroups.Add(new EquipmentGroup(subGroup));
            }
        }

        public EquipmentGroup(ObjectTableDef objectTableDef) : this()
        {
            Category = (ObjectCategory)objectTableDef.Category;
            ItemsToSpawn = 1;
            MinAmmoCapacity = objectTableDef.MinAmmoCapacity;
            MaxAmmoCapacity = objectTableDef.MaxAmmoCapacity;
            NumMagsSpawned = 3;
            NumClipsSpawned = 3;
            NumRoundsSpawned = 8;
            BespokeAttachmentChance = 0.5f;
            IsCompatibleMagazine = false;
            AutoPopulateGroup = !objectTableDef.UseIDListOverride;
            IDOverride = [.. objectTableDef.IDOverride];

            Tags = new()
            {
                Eras = [.. objectTableDef.Eras.Select(o => (TagEra)o)],
                Sets = [.. objectTableDef.Sets.Select(o => (TagSet)o)],
                Sizes = [.. objectTableDef.Sizes.Select(o => (TagFirearmSize)o)],
                Actions = [.. objectTableDef.Actions.Select(o => (TagFirearmAction)o)],
                Modes = [.. objectTableDef.Modes.Select(o => (TagFirearmFiringMode)o)],
                ExcludedModes = [.. objectTableDef.ExcludeModes.Select(o => (TagFirearmFiringMode)o)],
                FeedOptions = [.. objectTableDef.Feedoptions.Select(o => (TagFirearmFeedOption)o)],
                MountsAvailable = [.. objectTableDef.MountsAvailable.Select(o => (TagFirearmMount)o)],
                RoundPowers = [.. objectTableDef.RoundPowers.Select(o => (TagFirearmRoundPower)o)],
                Features = [.. objectTableDef.Features.Select(o => (TagAttachmentFeature)o)],
                MeleeHandedness = [.. objectTableDef.MeleeHandedness.Select(o => (TagMeleeHandedness)o)],
                MeleeStyles = [.. objectTableDef.MeleeStyles.Select(o => (TagMeleeStyle)o)],
                MountTypes = [.. objectTableDef.MountTypes.Select(o => (TagFirearmMount)o)],
                PowerupTypes = [.. objectTableDef.PowerupTypes.Select(o => (TagPowerupType)o)],
                ThrownTypes = [.. objectTableDef.ThrownTypes.Select(o => (TagThrownType)o)],
                ThrownDamageTypes = [.. objectTableDef.ThrownDamageTypes.Select(o => (TagThrownDamageType)o)]
            };

            this.objectTableDef = objectTableDef;
        }

        public ObjectTableDef GetObjectTableDef()
        {
            if (objectTableDef == null)
            {
                if (Tags == null) 
                { 
                    Tags = new(); 
                }
                else
                {
                    Tags.Eras ??= [];
                    Tags.Sets ??= [];
                    Tags.Sizes ??= [];
                    Tags.Actions ??= [];
                    Tags.Modes ??= [];
                    Tags.ExcludedModes ??= [];
                    Tags.FeedOptions ??= [];
                    Tags.MountsAvailable ??= [];
                    Tags.RoundPowers ??= [];
                    Tags.Features ??= [];
                    Tags.MeleeHandedness ??= [];
                    Tags.MeleeStyles ??= [];
                    Tags.PowerupTypes ??= [];
                    Tags.ThrownTypes ??= [];
                    Tags.ThrownDamageTypes ??= [];
                }

                objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));
                objectTableDef.Category = (FVRObject.ObjectCategory)Category;
                objectTableDef.MinAmmoCapacity = MinAmmoCapacity;
                objectTableDef.MaxAmmoCapacity = MaxAmmoCapacity;
                objectTableDef.RequiredExactCapacity = -1;
                objectTableDef.IsBlanked = false;
                objectTableDef.SpawnsInSmallCase = false;
                objectTableDef.SpawnsInLargeCase = false;
                objectTableDef.UseIDListOverride = !AutoPopulateGroup;
                objectTableDef.IDOverride = [];
                objectTableDef.Eras = [.. Tags.Eras.Select(o => (FVRObject.OTagEra)o)];
                objectTableDef.Sets = [.. Tags.Sets.Select(o => (FVRObject.OTagSet)o)];
                objectTableDef.Sizes = [.. Tags.Sizes.Select(o => (FVRObject.OTagFirearmSize)o)];
                objectTableDef.Actions = [.. Tags.Actions.Select(o => (FVRObject.OTagFirearmAction)o)];
                objectTableDef.Modes = [.. Tags.Modes.Select(o => (FVRObject.OTagFirearmFiringMode)o)];
                objectTableDef.ExcludeModes = [.. Tags.ExcludedModes.Select(o => (FVRObject.OTagFirearmFiringMode)o)];
                objectTableDef.Feedoptions = [.. Tags.FeedOptions.Select(o => (FVRObject.OTagFirearmFeedOption)o)];
                objectTableDef.MountsAvailable = [.. Tags.MountsAvailable.Select(o => (FVRObject.OTagFirearmMount)o)];
                objectTableDef.RoundPowers = [.. Tags.RoundPowers.Select(o => (FVRObject.OTagFirearmRoundPower)o)];
                objectTableDef.Features = [.. Tags.Features.Select(o => (FVRObject.OTagAttachmentFeature)o)];
                objectTableDef.MeleeHandedness = [.. Tags.MeleeHandedness.Select(o => (FVRObject.OTagMeleeHandedness)o)];
                objectTableDef.MeleeStyles = [.. Tags.MeleeStyles.Select(o => (FVRObject.OTagMeleeStyle)o)];
                objectTableDef.MountTypes = [.. Tags.MountTypes.Select(o => (FVRObject.OTagFirearmMount)o)];
                objectTableDef.PowerupTypes = [.. Tags.PowerupTypes.Select(o => (FVRObject.OTagPowerupType)o)];
                objectTableDef.ThrownTypes = [.. Tags.ThrownTypes.Select(o => (FVRObject.OTagThrownType)o)];
                objectTableDef.ThrownDamageTypes = [.. Tags.ThrownDamageTypes.Select(o => (FVRObject.OTagThrownDamageType)o)];
            }
            return objectTableDef;
        }

        public List<string> GetObjects()
        {
            return objects;
        }

        public List<EquipmentGroup> GetSpawnedEquipmentGroups()
        {
            List<EquipmentGroup> result;

            if (IsCompatibleMagazine || SubGroups == null || !SubGroups.Any())
            {
                result = [this];
                return result;
            }
            else if (ForceSpawnAllSubPools)
            {
                result = objects.Any() ? [this] : [];

                foreach (EquipmentGroup group in SubGroups)
                {
                    result.AddRange(group.GetSpawnedEquipmentGroups());
                }

                return result;
            }
            else
            {
                float thisRarity = objects.Any() ? (float)Rarity : 0;
                float combinedRarity = thisRarity;
                foreach (EquipmentGroup group in SubGroups)
                {
                    combinedRarity += group.Rarity;
                }

                float randomSelection = UnityEngine.Random.Range(0, combinedRarity);

                if (randomSelection < thisRarity)
                {
                    result = [this];
                    return result;
                }
                else
                {
                    float progress = thisRarity;
                    for (int i = 0; i < SubGroups.Count; i++)
                    {
                        progress += SubGroups[i].Rarity;
                        if (randomSelection < progress)
                        {
                            return SubGroups[i].GetSpawnedEquipmentGroups();
                        }
                    }
                }
            }

            return [];
        }

        /// <summary>
        /// Fills out the object table and removes any unloaded items
        /// </summary>
        /// <returns> Returns true if valid, and false if empty </returns>
        public bool DelayedInit(List<string> globalObjectBlacklist, bool allowInjection)
        {
            // Before we add anything from the IDOverride list, remove anything that isn't loaded
            TNHFrameworkUtils.RemoveUnloadedObjectIDs(this);

            // Every item in IDOverride gets added to the list of spawnable objects
            if (IDOverride != null)
            {
                foreach (var objectID in IDOverride)
                {
                    if (!globalObjectBlacklist.Contains(objectID))
                        objects.Add(objectID);
                }

                if (TNHFramework.InjectModBackpacks.Value && allowInjection)
                {
                    if (IDOverride.Contains("BackpackA") && IDOverride.Count == 1 && !ForceSpawnAllSubPools)
                    {
                        List<ItemSpawnerID> spawnList = IM.GetAvailableInSubCategory(ItemSpawnerID.ESubCategory.Backpack);

                        foreach (ItemSpawnerID spawnerID in spawnList)
                        {
                            if (spawnerID.ItemID != null
                                && !IDOverride.Contains(spawnerID.ItemID)
                                && !globalObjectBlacklist.Contains(spawnerID.ItemID)
                                && IM.OD.ContainsKey(spawnerID.ItemID)
                                && spawnerID.ItemID != "BackpackA"
                                && spawnerID.ItemID != "GunCaseSaveable Large"
                                && spawnerID.ItemID != "GunCaseSaveable Small")
                            {
                                TNHFrameworkLogger.Log($"Injecting backpack {spawnerID.ItemID}", TNHFrameworkLogger.LogType.Character);
                                objects.Add(spawnerID.ItemID);
                            }
                        }
                    }
                }
            }

            // If this pool isn't a compatible magazine or manually set, then we need to populate it based on its parameters
            if (!IsCompatibleMagazine && AutoPopulateGroup)
            {
                Initialise(globalObjectBlacklist);
            }

            // Perform delayed init on all subgroups. If they are empty, we remove them
            if (SubGroups != null)
            {
                for (int i = SubGroups.Count - 1; i >= 0; i--)
                {
                    if (!SubGroups[i].DelayedInit(globalObjectBlacklist, allowInjection))
                    {
                        //TNHFrameworkLogger.Log("Subgroup was empty, removing it!", TNHFrameworkLogger.LogType.Character);
                        SubGroups.RemoveAt(i);
                    }
                }
            }

            if (Rarity <= 0)
            {
                //TNHFrameworkLogger.Log("Equipment group had a rarity of 0 or less! Setting rarity to 1", TNHFrameworkLogger.LogType.Character);
                Rarity = 1;
            }

            // The table is valid if it has items in it, or is a compatible magazine
            return objects.Any() || IsCompatibleMagazine || (SubGroups != null && SubGroups.Any());
        }

        public void Initialise(List<string> globalObjectBlacklist)
        {
            List<FVRObject> Objs = [.. ManagerSingleton<IM>.Instance.odicTagCategory[(FVRObject.ObjectCategory)Category]];
            for (int j = Objs.Count - 1; j >= 0; j--)
            {
                FVRObject fvrobject = Objs[j];
 
                if (globalObjectBlacklist.Contains(fvrobject.ItemID))
                {
                    continue;
                }
                else if (!fvrobject.OSple)
                {
                    continue;
                }
                else if (MinAmmoCapacity > -1 && fvrobject.MaxCapacityRelated < MinAmmoCapacity)
                {
                    if (Category != ObjectCategory.MeleeWeapon)
                        continue;
                }
                else if (MaxAmmoCapacity > -1 && fvrobject.MinCapacityRelated > MaxAmmoCapacity)
                {
                    if (Category != ObjectCategory.MeleeWeapon)  // Fix for Meat Fortress melee weapons
                        continue;
                }
                // ????
                // anton, why?
                /*
                else if (requiredExactCapacity > -1 && !this.DoesGunMatchExactCapacity(fvrobject))
                {
                    continue;
                }
                */
                else if (Tags.MinYear != -1 && Tags.MinYear > fvrobject.TagFirearmFirstYear)
                {
                    continue;
                }
                else if (Tags.MaxYear != -1 && Tags.MaxYear < fvrobject.TagFirearmFirstYear)
                {
                    continue;
                }
                else if (Tags.Eras != null && Tags.Eras.Any() && !Tags.Eras.Contains((TagEra)fvrobject.TagEra))
                {
                    continue;
                }
                else if (Tags.Sets != null && Tags.Sets.Any() && !Tags.Sets.Contains((TagSet)fvrobject.TagSet))
                {
                    continue;
                }
                else if (Tags.Sizes != null && Tags.Sizes.Any() && !Tags.Sizes.Contains((TagFirearmSize)fvrobject.TagFirearmSize))
                {
                    continue;
                }
                else if (Tags.Actions != null && Tags.Actions.Any() && !Tags.Actions.Contains((TagFirearmAction)fvrobject.TagFirearmAction))
                {
                    continue;
                }
                else if (Tags.RoundPowers != null && Tags.RoundPowers.Any() && !Tags.RoundPowers.Contains((TagFirearmRoundPower)fvrobject.TagFirearmRoundPower))
                {
                    continue;
                }
                else
                {
                    if (Tags.Modes != null && Tags.Modes.Any())
                    {
                        bool flag = false;
                        for (int k = 0; k < Tags.Modes.Count; k++)
                        {
                            if (!fvrobject.TagFirearmFiringModes.Contains((FVRObject.OTagFirearmFiringMode)Tags.Modes[k]))
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (flag)
                        {
                            continue;
                        }
                    }

                    if (Tags.ExcludedModes != null)
                    {
                        bool flag2 = false;
                        for (int l = 0; l < Tags.ExcludedModes.Count; l++)
                        {
                            if (fvrobject.TagFirearmFiringModes.Contains((FVRObject.OTagFirearmFiringMode)Tags.ExcludedModes[l]))
                            {
                                flag2 = true;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            continue;
                        }
                    }

                    if (Tags.FeedOptions != null && Tags.FeedOptions.Any())
                    {
                        bool flag3 = true;
                        for (int m = 0; m < Tags.FeedOptions.Count; m++)
                        {
                            if (fvrobject.TagFirearmFeedOption.Contains((FVRObject.OTagFirearmFeedOption)Tags.FeedOptions[m]))
                            {
                                flag3 = false;
                                break;
                            }
                        }
                        if (flag3)
                        {
                            continue;
                        }
                    }

                    if (Tags.MountsAvailable != null)
                    {
                        bool flag4 = false;
                        for (int n = 0; n < Tags.MountsAvailable.Count; n++)
                        {
                            if (!fvrobject.TagFirearmMounts.Contains((FVRObject.OTagFirearmMount)Tags.MountsAvailable[n]))
                            {
                                flag4 = true;
                                break;
                            }
                        }
                        if (flag4)
                        {
                            continue;
                        }
                    }
                    
                    if (Tags.PowerupTypes != null && Tags.PowerupTypes.Any() && !Tags.PowerupTypes.Contains((TagPowerupType)fvrobject.TagPowerupType))
                    {
                        continue;
                    }
                    else if (Tags.ThrownTypes != null && Tags.ThrownTypes.Any() && !Tags.ThrownTypes.Contains((TagThrownType)fvrobject.TagThrownType))
                    {
                        continue;
                    }
                    else if (Tags.ThrownDamageTypes != null && Tags.ThrownDamageTypes.Any() && !Tags.ThrownDamageTypes.Contains((TagThrownDamageType)fvrobject.TagThrownDamageType))
                    {
                        continue;
                    }
                    else if (Tags.MeleeStyles != null && Tags.MeleeStyles.Any() && !Tags.MeleeStyles.Contains((TagMeleeStyle)fvrobject.TagMeleeStyle))
                    {
                        continue;
                    }
                    else if (Tags.MeleeHandedness != null && Tags.MeleeHandedness.Any() && !Tags.MeleeHandedness.Contains((TagMeleeHandedness)fvrobject.TagMeleeHandedness))
                    {
                        continue;
                    }
                    else if (Tags.MountTypes != null && Tags.MountTypes.Any() && !Tags.MountTypes.Contains((TagFirearmMount)fvrobject.TagAttachmentMount))
                    {
                        continue;
                    }
                    else if (Tags.Features != null && Tags.Features.Any() && !Tags.Features.Contains((TagAttachmentFeature)fvrobject.TagAttachmentFeature))
                    {
                        continue;
                    }
                    objects.Add(fvrobject.ItemID);
                }
            }
        }

        public string ToString(int level)
        {
            string prefix = "\n-";
            for (int i = 0; i < level; i++) prefix += "-";

            string output = prefix + "Group : Rarity=" + Rarity;

            if (IsCompatibleMagazine)
            {
                output += prefix + "Compatible Magazine";
            }
            else
            {
                foreach (string item in objects)
                {
                    output += prefix + item;
                }

                if (SubGroups != null)
                {
                    foreach (EquipmentGroup group in SubGroups)
                    {
                        output += group.ToString(level + 1);
                    }
                }
            }

            return output;
        }
    }

    public class FVRTags
    {
        public int MinYear = -1;
        public int MaxYear = -1;
        public List<TagEra> Eras = [];
        public List<TagSet> Sets = [];
        public List<TagFirearmSize> Sizes = [];
        public List<TagFirearmAction> Actions = [];
        public List<TagFirearmFiringMode> Modes = [];
        public List<TagFirearmFiringMode> ExcludedModes = [];
        public List<TagFirearmFeedOption> FeedOptions = [];
        public List<TagFirearmMount> MountsAvailable = [];
        public List<TagFirearmRoundPower> RoundPowers = [];
        public List<TagAttachmentFeature> Features = [];
        public List<TagMeleeStyle> MeleeStyles = [];
        public List<TagMeleeHandedness> MeleeHandedness = [];
        public List<TagFirearmMount> MountTypes = [];
        public List<TagPowerupType> PowerupTypes = [];
        public List<TagThrownType> ThrownTypes = [];
        public List<TagThrownDamageType> ThrownDamageTypes = [];
    }

    public class LoadoutEntry()
    {
        public EquipmentGroup PrimaryGroup = new();
        public EquipmentGroup BackupGroup = new();

        [JsonIgnore]
        private TNH_CharacterDef.LoadoutEntry loadout;

        public LoadoutEntry(V1.LoadoutEntry oldEntry) : this()
        {
            if (oldEntry == null)
                return;

            PrimaryGroup = new EquipmentGroup(oldEntry.PrimaryGroup);
            BackupGroup = new EquipmentGroup(oldEntry.BackupGroup);
        }

        public LoadoutEntry(TNH_CharacterDef.LoadoutEntry loadout) : this()
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry
                {
                    TableDefs = [],
                    ListOverride = []
                };
            }
            else if (loadout.ListOverride != null && loadout.ListOverride.Any())
            {
                PrimaryGroup = new EquipmentGroup
                {
                    Rarity = 1,
                    IDOverride = [.. loadout.ListOverride.Select(o => o.ItemID)],
                    ItemsToSpawn = 1,
                    MinAmmoCapacity = -1,
                    MaxAmmoCapacity = 9999,
                    NumMagsSpawned = loadout.Num_Mags_SL_Clips,
                    NumClipsSpawned = loadout.Num_Mags_SL_Clips,
                    NumRoundsSpawned = loadout.Num_Rounds
                };
            }
            else if (loadout.TableDefs != null && loadout.TableDefs.Any())
            {
                // If we have just one pool, then the primary pool becomes that pool
                if (loadout.TableDefs.Count == 1)
                {
                    PrimaryGroup = new EquipmentGroup(loadout.TableDefs[0])
                    {
                        Rarity = 1,
                        NumMagsSpawned = loadout.Num_Mags_SL_Clips,
                        NumClipsSpawned = loadout.Num_Mags_SL_Clips,
                        NumRoundsSpawned = loadout.Num_Rounds
                    };
                }
                else
                {
                    PrimaryGroup = new EquipmentGroup
                    {
                        Rarity = 1,
                        SubGroups = []
                    };
                    foreach (ObjectTableDef table in loadout.TableDefs)
                    {
                        EquipmentGroup group = new(table)
                        {
                            Rarity = 1,
                            NumMagsSpawned = loadout.Num_Mags_SL_Clips,
                            NumClipsSpawned = loadout.Num_Mags_SL_Clips,
                            NumRoundsSpawned = loadout.Num_Rounds
                        };
                        PrimaryGroup.SubGroups.Add(group);
                    }
                }
            }

            this.loadout = loadout;
        }

        public TNH_CharacterDef.LoadoutEntry GetLoadoutEntry()
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry
                {
                    Num_Mags_SL_Clips = 3,
                    Num_Rounds = 9
                };

                if (PrimaryGroup != null)
                {
                    loadout.TableDefs = [PrimaryGroup.GetObjectTableDef()];
                }
            }

            return loadout;
        }

        public bool DelayedInit(List<string> globalObjectBlacklist)
        {
            if (loadout != null)
            {
                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(globalObjectBlacklist, false))
                    {
                        TNHFrameworkLogger.Log("Primary group for loadout entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(globalObjectBlacklist, false))
                    {
                        if (PrimaryGroup == null)
                            TNHFrameworkLogger.Log("Backup group for loadout entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);

                        BackupGroup = null;
                    }
                }

                return PrimaryGroup != null || BackupGroup != null;
            }

            return false;
        }

        public override string ToString()
        {
            string output = "Loadout Entry";

            if (PrimaryGroup != null)
            {
                output += "\nPrimary Group";
                output += PrimaryGroup.ToString(0);
            }

            if (BackupGroup != null)
            {
                output += "\nBackup Group";
                output += BackupGroup.ToString(0);
            }

            return output;
        }
    }

    public class Level()
    {
        public int NumOverrideTokensForHold;
        public int MinSupplyPoints;
        public int MaxSupplyPoints;
        public int MinConstructors;
        public int MaxConstructors;
        public int MinPanels;
        public int MaxPanels;
        public int MinBoxesSpawned;
        public int MaxBoxesSpawned;
        public int MinTokensPerSupply;
        public int MaxTokensPerSupply;
        public float BoxTokenChance;
        public float BoxHealthChance;
        public List<PanelType> PossiblePanelTypes = [];
        public TakeChallenge TakeChallenge = new();
        public List<Phase> HoldPhases = [];
        public TakeChallenge SupplyChallenge = new();
        public List<Patrol> Patrols = [];

        [JsonIgnore]
        private TNH_Progression.Level level;

        public Level(V1.Level oldLevel) : this()
        {
            if (oldLevel == null)
                return;

            NumOverrideTokensForHold = oldLevel.NumOverrideTokensForHold;
            MinSupplyPoints = oldLevel.MinSupplyPoints;
            MaxSupplyPoints = oldLevel.MaxSupplyPoints;
            MinConstructors = oldLevel.MinConstructors;
            MaxConstructors = oldLevel.MaxConstructors;
            MinPanels = oldLevel.MinPanels;
            MaxPanels = oldLevel.MaxPanels;
            MinBoxesSpawned = oldLevel.MinBoxesSpawned;
            MaxBoxesSpawned = oldLevel.MaxBoxesSpawned;
            MinTokensPerSupply = oldLevel.MinTokensPerSupply;
            MaxTokensPerSupply = oldLevel.MaxTokensPerSupply;
            BoxTokenChance = oldLevel.BoxTokenChance;
            BoxHealthChance = oldLevel.BoxHealthChance;
            PossiblePanelTypes = oldLevel.PossiblePanelTypes ?? [];
            TakeChallenge = new(oldLevel.TakeChallenge);
            HoldPhases = [];

            foreach (V1.Phase oldPhase in oldLevel.HoldPhases)
            {
                HoldPhases.Add(new(oldPhase));
            }

            SupplyChallenge = new(oldLevel.SupplyChallenge);
            Patrols = [];

            foreach (V1.Patrol oldPatrol in oldLevel.Patrols)
            {
                Patrols.Add(new(oldPatrol));
            }
        }

        public Level(TNH_Progression.Level level) : this()
        {
            NumOverrideTokensForHold = level.NumOverrideTokensForHold;
            TakeChallenge = new TakeChallenge(level.TakeChallenge);
            SupplyChallenge = new TakeChallenge(level.TakeChallenge);
            HoldPhases = [.. level.HoldChallenge.Phases.Select(o => new Phase(o))];
            Patrols = [.. level.PatrolChallenge.Patrols.Select(o => new Patrol(o))];
            PossiblePanelTypes =
            [
                PanelType.AmmoReloader,
                PanelType.MagDuplicator,
                PanelType.Recycler,
            ];
            MinConstructors = 1;
            MaxConstructors = 1;
            MinPanels = 1;
            MaxPanels = 1;
            MinSupplyPoints = 2;
            MaxSupplyPoints = 3;
            MinBoxesSpawned = 2;
            MaxBoxesSpawned = 4;
            MinTokensPerSupply = 1;
            MaxTokensPerSupply = 1;
            BoxTokenChance = 0;
            BoxHealthChance = 0.5f;

            this.level = level;
        }

        public TNH_Progression.Level GetLevel()
        {
            if (level == null)
            {
                level = new()
                {
                    NumOverrideTokensForHold = NumOverrideTokensForHold,
                    TakeChallenge = TakeChallenge.GetTakeChallenge(),

                    HoldChallenge = (TNH_HoldChallenge)ScriptableObject.CreateInstance(typeof(TNH_HoldChallenge))
                };

                level.HoldChallenge.Phases = [.. HoldPhases.Select(o => o.GetPhase())];
                level.SupplyChallenge = SupplyChallenge.GetTakeChallenge();
                level.PatrolChallenge = (TNH_PatrolChallenge)ScriptableObject.CreateInstance(typeof(TNH_PatrolChallenge));
                level.PatrolChallenge.Patrols = [.. Patrols.Select(o => o.GetPatrol())];
                level.TrapsChallenge = (TNH_TrapsChallenge)ScriptableObject.CreateInstance(typeof(TNH_TrapsChallenge));
            }

            return level;
        }

        public Patrol GetPatrol(TNH_PatrolChallenge.Patrol patrol)
        {
            if (Patrols.Select(o => o.GetPatrol()).Contains(patrol))
            {
                return Patrols.Find(o => o.GetPatrol().Equals(patrol));
            }

            return null;
        }

        public void DelayedInit(bool isCustom, int levelIndex)
        {
            // If this is a level for a default character, we should try to replicate the vanilla layout
            if (!isCustom)
            {
                MaxSupplyPoints = Mathf.Clamp(levelIndex + 1, 1, 3);
                MinSupplyPoints = Mathf.Clamp(levelIndex + 1, 1, 3);
            }

            TakeChallenge.DelayedInit();

            foreach (Phase phase in HoldPhases)
            {
                phase.DelayedInit(isCustom);
            }

            SupplyChallenge.DelayedInit();

            foreach (Patrol patrol in Patrols)
            {
                patrol.DelayedInit(isCustom);
            }

            Patrols.RemoveAll(o => o.LeaderType == null);
        }

        public bool LevelUsesSosig(string id)
        {
            if (TakeChallenge.EnemyType == id)
            {
                return true;
            }
            else if (SupplyChallenge.EnemyType == id)
            {
                return true;
            }

            foreach (Patrol patrol in Patrols)
            {
                if (patrol.LeaderType == id)
                {
                    return true;
                }

                foreach (string sosigID in patrol.EnemyType)
                {
                    if (sosigID == id)
                    {
                        return true;
                    }
                }
            }

            foreach (Phase phase in HoldPhases)
            {
                if (phase.LeaderType == id)
                {
                    return true;
                }

                foreach (string sosigID in phase.EnemyType)
                {
                    if (sosigID == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }


    public class TakeChallenge()
    {
        public TNH_TurretType TurretType;
        public string EnemyType;

        public int NumTurrets;
        public int NumGuards;
        public int IFFUsed;

        [JsonIgnore]
        private TNH_TakeChallenge takeChallenge;

        public TakeChallenge(V1.TakeChallenge oldTake) : this()
        {
            TurretType = oldTake.TurretType;
            EnemyType = oldTake.EnemyType;

            NumTurrets = oldTake.NumTurrets;
            NumGuards = oldTake.NumGuards;
            IFFUsed = oldTake.IFFUsed;
        }

        public TakeChallenge(TNH_TakeChallenge takeChallenge) : this()
        {
            TurretType = takeChallenge.TurretType;
            EnemyType = takeChallenge.GID.ToString();
            NumGuards = takeChallenge.NumGuards;
            NumTurrets = takeChallenge.NumTurrets;
            IFFUsed = takeChallenge.IFFUsed;

            this.takeChallenge = takeChallenge;
        }

        public TNH_TakeChallenge GetTakeChallenge()
        {
            if (takeChallenge == null)
            {
                takeChallenge = (TNH_TakeChallenge)ScriptableObject.CreateInstance(typeof(TNH_TakeChallenge));
                takeChallenge.TurretType = TurretType;

                // Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType))
                    takeChallenge.GID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType];
                else
                    takeChallenge.GID = TNHFrameworkUtils.ParseEnemyType(EnemyType);

                takeChallenge.NumTurrets = NumTurrets;
                takeChallenge.NumGuards = NumGuards;
                takeChallenge.IFFUsed = IFFUsed;
            }

            return takeChallenge;
        }

        public void DelayedInit()
        {
            if (!LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType))
                EnemyType = "M_Swat_Guard";
        }
    }

    public class Phase()
    {
        public List<TNH_EncryptionType> Encryptions = [];
        public int MinTargets;
        public int MaxTargets;
        public int MinTargetsLimited;
        public int MaxTargetsLimited;
        public List<string> EnemyType = [];
        public string LeaderType;
        public int MinEnemies;
        public int MaxEnemies;
        public int MaxEnemiesAlive;
        public int MaxDirections;
        public float SpawnCadence;
        public float ScanTime;
        public float WarmupTime;
        public int IFFUsed;
        public float GrenadeChance;
        public string GrenadeType;
        public bool SwarmPlayer;
        public bool DespawnBetweenWaves = true;
        public bool UsesVFX = true;

        [JsonIgnore]
        private TNH_HoldChallenge.Phase phase;

        public Phase(V1.Phase oldPhase) : this()
        {
            Encryptions = oldPhase.Encryptions ?? [];
            MinTargets = oldPhase.MinTargets;
            MaxTargets = oldPhase.MaxTargets;
            MinTargetsLimited = oldPhase.MinTargetsLimited;
            MaxTargetsLimited = oldPhase.MaxTargetsLimited;
            EnemyType = oldPhase.EnemyType ?? [];
            LeaderType = oldPhase.LeaderType;
            MinEnemies = oldPhase.MinEnemies;
            MaxEnemies = oldPhase.MaxEnemies;
            MaxEnemiesAlive = oldPhase.MaxEnemiesAlive;
            MaxDirections = oldPhase.MaxDirections;
            SpawnCadence = oldPhase.SpawnCadence;
            ScanTime = oldPhase.ScanTime;
            WarmupTime = oldPhase.WarmupTime;
            IFFUsed = oldPhase.IFFUsed;
            GrenadeChance = oldPhase.GrenadeChance;
            GrenadeType = oldPhase.GrenadeType;
            SwarmPlayer =  oldPhase.SwarmPlayer;
            DespawnBetweenWaves = true;
            UsesVFX = true;
        }

        public Phase(TNH_HoldChallenge.Phase phase) : this()
        {
            Encryptions = [phase.Encryption];
            MinTargets = phase.MinTargets;
            MaxTargets = phase.MaxTargets;
            MinTargetsLimited = 1;
            MaxTargetsLimited = 1;
            EnemyType = [phase.EType.ToString()];
            LeaderType = phase.LType.ToString();
            MinEnemies = phase.MinEnemies;
            MaxEnemies = phase.MaxEnemies;
            MaxEnemiesAlive = phase.MaxEnemiesAlive;
            MaxDirections = phase.MaxDirections;
            SpawnCadence = phase.SpawnCadence;
            ScanTime = phase.ScanTime;
            WarmupTime = phase.WarmUp;
            IFFUsed = phase.IFFUsed;
            GrenadeChance = 0;
            GrenadeType = "Sosiggrenade_Flash";
            SwarmPlayer = false;
            DespawnBetweenWaves = true;
            UsesVFX = true;

            this.phase = phase;
        }

        public TNH_HoldChallenge.Phase GetPhase()
        {
            if (phase == null)
            {
                phase = new TNH_HoldChallenge.Phase
                {
                    Encryption = Encryptions[0],
                    MinTargets = MinTargets,
                    MaxTargets = MaxTargets,
                    MinEnemies = MinEnemies,
                    MaxEnemies = MaxEnemies,
                    MaxEnemiesAlive = MaxEnemiesAlive,
                    MaxDirections = MaxDirections,
                    SpawnCadence = SpawnCadence,
                    ScanTime = ScanTime,
                    WarmUp = Mathf.Max(0f, WarmupTime),
                    IFFUsed = IFFUsed
                };

                // Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType[0]))
                    phase.EType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType[0]];
                else
                    phase.EType = TNHFrameworkUtils.ParseEnemyType(EnemyType[0]);

                if (LoadedTemplateManager.SosigIDDict.ContainsKey(LeaderType))
                    phase.LType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[LeaderType];
                else
                    phase.LType = TNHFrameworkUtils.ParseEnemyType(LeaderType);
            }

            return phase;
        }

        public void DelayedInit(bool isCustom)
        {
            if (isCustom)
            {
                EnemyType.RemoveAll(o => !LoadedTemplateManager.SosigIDDict.ContainsKey(o));

                if (!EnemyType.Any())
                    EnemyType.Add("M_Swat_Guard");
            }
            else
            {
                if (Encryptions[0] == TNH_EncryptionType.Static)
                {
                    MinTargetsLimited = 3;
                    MaxTargetsLimited = 3;
                }
            }
        }
    }

    public class Patrol()
    {
        public List<string> EnemyType = [];
        public string LeaderType;
        public int PatrolSize;
        public int MaxPatrols;
        public int MaxPatrolsLimited;
        public float PatrolCadence;
        public float PatrolCadenceLimited;
        public int IFFUsed;
        public bool SwarmPlayer;
        public Sosig.SosigMoveSpeed AssualtSpeed = Sosig.SosigMoveSpeed.Walking;
        public bool IsBoss;
        public float DropChance;
        public bool DropsHealth;

        [JsonIgnore]
        private TNH_PatrolChallenge.Patrol patrol;

        public Patrol(V1.Patrol oldPatrol) : this()
        {
            EnemyType = oldPatrol.EnemyType ?? [];
            LeaderType = oldPatrol.LeaderType;
            PatrolSize = oldPatrol.PatrolSize;
            MaxPatrols = oldPatrol.MaxPatrols;
            MaxPatrolsLimited = oldPatrol.MaxPatrolsLimited;
            PatrolCadence = oldPatrol.PatrolCadence;
            PatrolCadenceLimited = oldPatrol.PatrolCadenceLimited;
            IFFUsed = oldPatrol.IFFUsed;
            SwarmPlayer = oldPatrol.SwarmPlayer;
            AssualtSpeed = oldPatrol.AssualtSpeed;
            IsBoss = oldPatrol.IsBoss;
            DropChance = oldPatrol.DropChance;
            DropsHealth = oldPatrol.DropsHealth;
        }

        public Patrol(TNH_PatrolChallenge.Patrol patrol) : this()
        {
            EnemyType = [patrol.EType.ToString()];
            LeaderType = patrol.LType.ToString();
            PatrolSize = patrol.PatrolSize;
            MaxPatrols = patrol.MaxPatrols;
            MaxPatrolsLimited = patrol.MaxPatrols_LimitedAmmo;
            PatrolCadence = patrol.TimeTilRegen;
            PatrolCadenceLimited = patrol.TimeTilRegen_LimitedAmmo;
            IFFUsed = patrol.IFFUsed;
            SwarmPlayer = false;
            AssualtSpeed = Sosig.SosigMoveSpeed.Walking;
            DropChance = 0.65f;
            DropsHealth = true;
            IsBoss = false;

            this.patrol = patrol;
        }

        public TNH_PatrolChallenge.Patrol GetPatrol()
        {
            if (patrol == null)
            {
                patrol = new TNH_PatrolChallenge.Patrol();

                // Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType[0]))
                    patrol.EType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType[0]];
                else
                    patrol.EType = TNHFrameworkUtils.ParseEnemyType(EnemyType[0]);

                if (LoadedTemplateManager.SosigIDDict.ContainsKey(LeaderType))
                    patrol.LType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[LeaderType];
                else
                    patrol.LType = TNHFrameworkUtils.ParseEnemyType(LeaderType);

                patrol.PatrolSize = PatrolSize;
                patrol.MaxPatrols = MaxPatrols;
                patrol.MaxPatrols_LimitedAmmo = MaxPatrolsLimited;
                patrol.TimeTilRegen = PatrolCadence;
                patrol.TimeTilRegen_LimitedAmmo = PatrolCadenceLimited;
                patrol.IFFUsed = IFFUsed;
            }

            return patrol;
        }

        public void DelayedInit(bool isCustom)
        {
            if (isCustom)
            {
                EnemyType.RemoveAll(o => !LoadedTemplateManager.SosigIDDict.ContainsKey(o));

                if (!EnemyType.Any())
                    EnemyType.Add("M_Swat_Guard");

                if (!LoadedTemplateManager.SosigIDDict.ContainsKey(LeaderType))
                    LeaderType = EnemyType[0];
            }
        }
    }
}
