using ADepIn;
using FistVR;
using FistVR.Ugc;
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
        public string TableID;

        [JsonIgnore]
        public int CharacterGroup;

        [JsonProperty(PropertyName = "CharacterGroup")]
        private int CharacterGroupAlt
        {
            set { CharacterGroup = value; }
        }

        [JsonConverter(typeof(UgcReferenceConverter))]
        public VaultFileWrapper DisplayCharacter;

        public int StartingTokens;
        public bool ForceAllAgentWeapons;
        public bool ForceDisableOutfitFunctionality;
        public bool UsesPurchasePriceIncrement;
        public bool DisableCleanupSosigDrops;
        public bool HasPrimaryWeapon = true;
        public bool HasSecondaryWeapon = true;
        public bool HasTertiaryWeapon = true;
        public bool HasPrimaryItem = true;
        public bool HasSecondaryItem = true;
        public bool HasTertiaryItem = true;
        public bool HasShield = true;
        public bool SecondaryWeaponCopiesPrimary;

        public bool UsesObjectConstructor = true;
        public bool UsesAmmoReloader = true;
        public bool UsesMagDuplicator = true;
        public bool UsesGunRecycler = true;
        public bool AllowConstructorUnlockFirearm = true;
        public bool AllowConstructorUnlockEquipment = true;
        public bool AllowConstructorUnlockConsumable = true;
        public bool AllowConstructorUnlockStorage = true;
        public bool AllowConstructorUnlockLocomotion = true;

        public List<FireArmRoundClass> RoundClassBlacklist = [];
        public List<TagEra> ValidAmmoEras = [];
        public List<TagSet> ValidAmmoSets = [];
        public List<string> GlobalAmmoBlacklist = [];
        public List<string> GlobalObjectBlacklist = [];
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
            CategoryData.Name = character.CharacterGroup;
            CharacterGroup = 0;
            TableID = character.TableID;
            StartingTokens = character.StartingTokens;
            ForceAllAgentWeapons = character.ForceAllAgentWeapons;
            Description = character.Description;
            UsesPurchasePriceIncrement = character.UsesPurchasePriceIncrement;
            RoundClassBlacklist = [.. character.RoundClassBlacklist];
            HasPrimaryWeapon = character.Has_Weapon_Primary;
            HasSecondaryWeapon = character.Has_Weapon_Secondary;
            HasTertiaryWeapon = character.Has_Weapon_Tertiary;
            HasPrimaryItem = character.Has_Item_Primary;
            HasSecondaryItem = character.Has_Item_Secondary;
            HasTertiaryItem = character.Has_Item_Tertiary;
            HasShield = character.Has_Item_Shield;
            ValidAmmoEras = [.. character.ValidAmmoEras.Select(o => (TagEra)o)];
            ValidAmmoSets = [.. character.ValidAmmoSets.Select(o => (TagSet)o)];
            PrimaryWeapon = new LoadoutEntry(character.Weapon_Primary);
            SecondaryWeapon = new LoadoutEntry(character.Weapon_Secondary);
            TertiaryWeapon = new LoadoutEntry(character.Weapon_Tertiary);
            PrimaryItem = new LoadoutEntry(character.Item_Primary);
            SecondaryItem = new LoadoutEntry(character.Item_Secondary);
            TertiaryItem = new LoadoutEntry(character.Item_Tertiary);
            Shield = new LoadoutEntry(character.Item_Shield);
            SecondaryWeaponCopiesPrimary = character.SecondaryWeaponCopiesPrimary;

            UsesObjectConstructor = character.UsesObjectConstructor;
            UsesAmmoReloader = character.UsesAmmoReloader;
            UsesMagDuplicator = character.UsesMagDuplicator;
            UsesGunRecycler = character.UsesGunRecycler;
            AllowConstructorUnlockFirearm = character.AllowConstructorUnlockFirearm;
            AllowConstructorUnlockEquipment = character.AllowConstructorUnlockEquipment;
            AllowConstructorUnlockConsumable = character.AllowConstructorUnlockConsumable;
            AllowConstructorUnlockStorage = character.AllowConstructorUnlockStorage;
            AllowConstructorUnlockLocomotion = character.AllowConstructorUnlockLocomotion;

            RequireSightTable = new EquipmentGroup(character.RequireSightTable);

            EquipmentPools = [.. character.EquipmentPool.Entries.Select(o => new EquipmentPool(o))];
            Levels = [.. character.Progressions[0].Levels.Select(o => new Level(o))];
            LevelsEndless = [.. character.Progressions_Endless[0].Levels.Select(o => new Level(o))];

            ForceDisableOutfitFunctionality = false;

            this.character = character;
        }

        public TNH_CharacterDef GetCharacter(Sprite thumbnail)
        {
            if (character == null)
            {
                ValidAmmoSets ??= [];
                ValidAmmoEras ??= [];

                character = (TNH_CharacterDef)ScriptableObject.CreateInstance(typeof(TNH_CharacterDef));

                character.UgcModule = UgcManager.H3Module;
                character.UgcId = "h3vr:TNHF_0_" + DisplayName.Replace(" ", string.Empty);
                character.UgcFilePath = null;
                character.UgcIsWritable = false;

                character.DisplayName = DisplayName;

                if (CategoryData.Name == "")
                {
                    CategoryData.Name = CharacterGroup switch
                    {
                        0 => "Daring Defaults",
                        1 => "Wieners Through Time",
                        2 => "Memetastic Meats",
                        3 => "Competitive Casings",
                        _ => "Daring Defaults",
                    };
                }

                character.CharacterGroup = CategoryData.Name;
                character.TableID = TableID;
                character.StartingTokens = StartingTokens;
                character.ForceAllAgentWeapons = ForceAllAgentWeapons;
                character.Description = Description;
                character.UsesPurchasePriceIncrement = UsesPurchasePriceIncrement;
                character.Has_Weapon_Primary = HasPrimaryWeapon;
                character.Has_Weapon_Secondary = HasSecondaryWeapon;
                character.Has_Weapon_Tertiary = HasTertiaryWeapon;
                character.Has_Item_Primary = HasPrimaryItem;
                character.Has_Item_Secondary = HasSecondaryItem;
                character.Has_Item_Tertiary = HasTertiaryItem;
                character.Has_Item_Shield = HasShield;
                character.RoundClassBlacklist = [.. RoundClassBlacklist];
                character.ValidAmmoEras = [.. ValidAmmoEras.Select(o => (FVRObject.OTagEra)o)];
                character.ValidAmmoSets = [.. ValidAmmoSets.Select(o => (FVRObject.OTagSet)o)];
                character.Picture = thumbnail;
                character.Weapon_Primary = PrimaryWeapon.GetLoadoutEntry(character.UgcId, 0, "PrimaryWeapon");
                character.Weapon_Secondary = SecondaryWeapon.GetLoadoutEntry(character.UgcId, 0, "SecondaryWeapon");
                character.Weapon_Tertiary = TertiaryWeapon.GetLoadoutEntry(character.UgcId, 0, "TertiaryWeapon");
                character.Item_Primary = PrimaryItem.GetLoadoutEntry(character.UgcId, 0, "PrimaryItem");
                character.Item_Secondary = SecondaryItem.GetLoadoutEntry(character.UgcId, 0, "SecondaryItem");
                character.Item_Tertiary = TertiaryItem.GetLoadoutEntry(character.UgcId, 0, "TertiaryItem");
                character.Item_Shield = Shield.GetLoadoutEntry(character.UgcId, 0, "Shield");
                character.SecondaryWeaponCopiesPrimary = SecondaryWeaponCopiesPrimary;

                character.UsesObjectConstructor = UsesObjectConstructor;
                character.UsesAmmoReloader = UsesAmmoReloader;
                character.UsesMagDuplicator = UsesMagDuplicator;
                character.UsesGunRecycler = UsesGunRecycler;
                character.AllowConstructorUnlockFirearm = AllowConstructorUnlockFirearm;
                character.AllowConstructorUnlockEquipment = AllowConstructorUnlockEquipment;
                character.AllowConstructorUnlockConsumable = AllowConstructorUnlockConsumable;
                character.AllowConstructorUnlockStorage = AllowConstructorUnlockStorage;
                character.AllowConstructorUnlockLocomotion = AllowConstructorUnlockLocomotion;

                character.RequireSightTable = RequireSightTable.GetObjectTableDef(character.UgcId, 0, "RequireSightTable");
                character.EquipmentPool = (EquipmentPoolDef)ScriptableObject.CreateInstance(typeof(EquipmentPoolDef));
                character.EquipmentPool.Entries = [.. EquipmentPools.Select((o, index) => o.GetPoolEntry(character.UgcId, index, "EquipmentPool"))];

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
            if (HasPrimaryWeapon && !PrimaryWeapon.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting weapon had no pools to spawn from, and will not spawn equipment!");
                HasPrimaryWeapon = false;
                character.Has_Weapon_Primary = false;
            }

            TNHFrameworkLogger.Log("Init of Secondary Weapon", TNHFrameworkLogger.LogType.Character);
            if (HasSecondaryWeapon && !SecondaryWeapon.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting weapon had no pools to spawn from, and will not spawn equipment!");
                HasSecondaryWeapon = false;
                character.Has_Weapon_Secondary = false;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Weapon", TNHFrameworkLogger.LogType.Character);
            if (HasTertiaryWeapon && !TertiaryWeapon.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting weapon had no pools to spawn from, and will not spawn equipment!");
                HasTertiaryWeapon = false;
                character.Has_Weapon_Tertiary = false;
            }

            TNHFrameworkLogger.Log("Init of Primary Item", TNHFrameworkLogger.LogType.Character);
            if (HasPrimaryItem && !PrimaryItem.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Primary starting item had no pools to spawn from, and will not spawn equipment!");
                HasPrimaryItem = false;
                character.Has_Item_Primary = false;
            }

            TNHFrameworkLogger.Log("Init of Secondary Item", TNHFrameworkLogger.LogType.Character);
            if (HasSecondaryItem && !SecondaryItem.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Secondary starting item had no pools to spawn from, and will not spawn equipment!");
                HasSecondaryItem = false;
                character.Has_Item_Secondary = false;
            }

            TNHFrameworkLogger.Log("Init of Tertiary Item", TNHFrameworkLogger.LogType.Character);
            if (HasTertiaryItem && !TertiaryItem.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Tertiary starting item had no pools to spawn from, and will not spawn equipment!");
                HasTertiaryItem = false;
                character.Has_Item_Tertiary = false;
            }

            TNHFrameworkLogger.Log("Init of Shield", TNHFrameworkLogger.LogType.Character);
            if (HasShield && !Shield.DelayedInit(character.UgcId, GlobalObjectBlacklist))
            {
                TNHFrameworkLogger.LogWarning("Shield starting item had no pools to spawn from, and will not spawn equipment!");
                HasShield = false;
                character.Has_Item_Shield = false;
            }

            TNHFrameworkLogger.Log("Init of required sights table", TNHFrameworkLogger.LogType.Character);
            if (RequireSightTable != null && !RequireSightTable.DelayedInit(character.UgcId, GlobalObjectBlacklist, false))
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
                if (!pool.DelayedInit(character.UgcId, GlobalObjectBlacklist))
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

        [JsonIgnore]
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
        public EquipmentPoolDef.PoolEntry.PoolEntryType Type = EquipmentPoolDef.PoolEntry.PoolEntryType.Firearm;
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

        public EquipmentPool(EquipmentPoolDef.PoolEntry pool) : this()
        {
            Type = pool.Type;
            IconName = (pool.TableDef.Icon != null) ? pool.TableDef.Icon.name : pool.TableDef.IconEnum.ToString();
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

        public EquipmentPoolDef.PoolEntry GetPoolEntry(string id, int index, string suffix)
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
                    pool.Rarity = PrimaryGroup.Rarity;
                else
                    pool.Rarity = 1;

                pool.TableDef = PrimaryGroup.GetObjectTableDef(id, index, suffix);
                pool.TableDef.SpawnsInLargeCase = SpawnsInLargeCase;
                pool.TableDef.SpawnsInSmallCase = SpawnsInSmallCase;
            }

            return pool;
        }

        public EquipmentPoolDef.PoolEntry GetPoolEntry()
        {
            if (pool == null)
            {
                TNHFrameworkLogger.Log("Tried to get PoolEntry, but it hasn't been initialized yet! Returning null!", TNHFrameworkLogger.LogType.Character);
                return null;
            }

            return pool;
        }

        public bool DelayedInit(string id, List<string> globalObjectBlacklist)
        {
            pool ??= GetPoolEntry(id, 0, "EquipmentPool");

            if (pool != null)
            {
                if (LoadedTemplateManager.DefaultIconSprites.ContainsKey(IconName))
                {
                    if (pool.TableDef == null)
                        pool.TableDef = (PrimaryGroup as EquipmentGroup).GetObjectTableDef();

                    pool.TableDef.Icon = LoadedTemplateManager.DefaultIconSprites[IconName];
                    pool.TableDef.SpawnsInLargeCase = SpawnsInLargeCase;
                    pool.TableDef.SpawnsInSmallCase = SpawnsInSmallCase;
                }

                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(id, globalObjectBlacklist, true))
                    {
                        TNHFrameworkLogger.Log("Primary group for equipment pool entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(id, globalObjectBlacklist, true))
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
                return PrimaryGroup.GetSpawnedEquipmentGroups();
            else if (BackupGroup != null)
                return BackupGroup.GetSpawnedEquipmentGroups();

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
        public ObjectCategory Category = ObjectCategory.Firearm;
        public float Rarity;
        public int ItemsToSpawn;
        public int MinAmmoCapacity;
        public int MaxAmmoCapacity;
        public bool SpawnsAreTheSame;
        public int NumMagsSpawned;
        public int NumClipsSpawned;
        public int NumRoundsSpawned;
        public bool SpawnMagAndClip;
        public float BespokeAttachmentChance;
        public bool IsCompatibleMagazine;
        public bool AutoPopulateGroup;
        public bool ForceSpawnAllSubPools;
        public List<FVRObject> ObjBlacklist = [];
        public List<string> IDOverride = [];
        public List<string> IDOverrideBackup = [];
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
        public List<FireArmRoundType> RoundTypes = [];
        public List<TagAttachmentFeature> Features = [];
        public List<TagMeleeStyle> MeleeStyles = [];
        public List<TagMeleeHandedness> MeleeHandedness = [];
        public List<TagFirearmMount> MountTypes = [];
        public List<TagPowerupType> PowerupTypes = [];
        public List<TagThrownType> ThrownTypes = [];
        public List<TagThrownDamageType> ThrownDamageTypes = [];
        public List<TagFirearmCountryOfOrigin> Countries = [];
        public FVRTags Tags = new();
        public List<EquipmentGroup> SubGroups = [];

        [JsonIgnore]
        public string RequiredQuest;

        [JsonIgnore]
        public bool UsesVaultFileListOverride;

        [JsonIgnore]
        public bool UseFullPlayerVaultRandom;

        [JsonIgnore]
        public List<VaultFileWrapper> OverrideVaultFileWrappers = [];

        [JsonIgnore]
        public ObjectTableDef.VaultFileUsage OverrideFileUsage = ObjectTableDef.VaultFileUsage.SingleObject;

        [JsonIgnore]
        private ObjectTableDef objectTableDef;

        [JsonIgnore]
        private List<string> objects = [];

        [JsonIgnore]
        private List<VaultFile> vaultFiles = [];

        [JsonIgnore]
        private Dictionary<string, List<FVRObject>> familyDic = [];

        public EquipmentGroup(ObjectTableDef objectTableDef) : this()
        {
            Category = (ObjectCategory)objectTableDef.Category;
            ItemsToSpawn = 1;
            MinAmmoCapacity = objectTableDef.MinAmmoCapacity;
            MaxAmmoCapacity = objectTableDef.MaxAmmoCapacity;
            SpawnsAreTheSame = objectTableDef.SpawnsAreTheSame;
            NumMagsSpawned = 3;
            NumClipsSpawned = 3;
            NumRoundsSpawned = 8;
            BespokeAttachmentChance = 0.5f;
            IsCompatibleMagazine = false;
            AutoPopulateGroup = !objectTableDef.UseIDListOverride;
            ObjBlacklist = (objectTableDef.ObjBlacklist == null) ? null : [.. objectTableDef.ObjBlacklist];
            IDOverride = (objectTableDef.IDOverride == null) ? null : [.. objectTableDef.IDOverride];
            IDOverrideBackup = [];

            MinYear = -1;
            MaxYear = -1;
            Eras = [.. objectTableDef.Eras.Select(o => (TagEra)o)];
            Sets = [.. objectTableDef.Sets.Select(o => (TagSet)o)];
            Sizes = [.. objectTableDef.Sizes.Select(o => (TagFirearmSize)o)];
            Actions = [.. objectTableDef.Actions.Select(o => (TagFirearmAction)o)];
            Modes = [.. objectTableDef.Modes.Select(o => (TagFirearmFiringMode)o)];
            ExcludedModes = [.. objectTableDef.ExcludeModes.Select(o => (TagFirearmFiringMode)o)];
            FeedOptions = [.. objectTableDef.Feedoptions.Select(o => (TagFirearmFeedOption)o)];
            MountsAvailable = [.. objectTableDef.MountsAvailable.Select(o => (TagFirearmMount)o)];
            RoundPowers = [.. objectTableDef.RoundPowers.Select(o => (TagFirearmRoundPower)o)];
            RoundTypes = [.. objectTableDef.RoundTypes];
            Features = [.. objectTableDef.Features.Select(o => (TagAttachmentFeature)o)];
            MeleeHandedness = [.. objectTableDef.MeleeHandedness.Select(o => (TagMeleeHandedness)o)];
            MeleeStyles = [.. objectTableDef.MeleeStyles.Select(o => (TagMeleeStyle)o)];
            MountTypes = [.. objectTableDef.MountTypes.Select(o => (TagFirearmMount)o)];
            PowerupTypes = [.. objectTableDef.PowerupTypes.Select(o => (TagPowerupType)o)];
            ThrownTypes = [.. objectTableDef.ThrownTypes.Select(o => (TagThrownType)o)];
            ThrownDamageTypes = [.. objectTableDef.ThrownDamageTypes.Select(o => (TagThrownDamageType)o)];
            Countries = [.. objectTableDef.Countries.Select(o => (TagFirearmCountryOfOrigin)o)];

            Tags = new()
            {
                MinYear = -1,
                MaxYear = -1,
                Eras = [.. objectTableDef.Eras.Select(o => (TagEra)o)],
                Sets = [.. objectTableDef.Sets.Select(o => (TagSet)o)],
                Sizes = [.. objectTableDef.Sizes.Select(o => (TagFirearmSize)o)],
                Actions = [.. objectTableDef.Actions.Select(o => (TagFirearmAction)o)],
                Modes = [.. objectTableDef.Modes.Select(o => (TagFirearmFiringMode)o)],
                ExcludedModes = [.. objectTableDef.ExcludeModes.Select(o => (TagFirearmFiringMode)o)],
                FeedOptions = [.. objectTableDef.Feedoptions.Select(o => (TagFirearmFeedOption)o)],
                MountsAvailable = [.. objectTableDef.MountsAvailable.Select(o => (TagFirearmMount)o)],
                RoundPowers = [.. objectTableDef.RoundPowers.Select(o => (TagFirearmRoundPower)o)],
                RoundTypes = [.. objectTableDef.RoundTypes],
                Features = [.. objectTableDef.Features.Select(o => (TagAttachmentFeature)o)],
                MeleeHandedness = [.. objectTableDef.MeleeHandedness.Select(o => (TagMeleeHandedness)o)],
                MeleeStyles = [.. objectTableDef.MeleeStyles.Select(o => (TagMeleeStyle)o)],
                MountTypes = [.. objectTableDef.MountTypes.Select(o => (TagFirearmMount)o)],
                PowerupTypes = [.. objectTableDef.PowerupTypes.Select(o => (TagPowerupType)o)],
                ThrownTypes = [.. objectTableDef.ThrownTypes.Select(o => (TagThrownType)o)],
                ThrownDamageTypes = [.. objectTableDef.ThrownDamageTypes.Select(o => (TagThrownDamageType)o)],
                Countries = [.. objectTableDef.Countries.Select(o => (TagFirearmCountryOfOrigin)o)]
            };

            SubGroups = [];

            UsesVaultFileListOverride = objectTableDef.UsesVaultFileListOverride;
            UseFullPlayerVaultRandom = objectTableDef.UseFullPlayerVaultRandom;
            OverrideVaultFileWrappers = [.. objectTableDef.OverrideVaultFileWrappers];
            OverrideFileUsage = objectTableDef.OverrideFileUsage;

            this.objectTableDef = objectTableDef;
        }

        public ObjectTableDef GetObjectTableDef(string id, int index, string suffix)
        {
            if (objectTableDef == null)
            {
                Eras ??= [];
                Sets ??= [];
                Sizes ??= [];
                Actions ??= [];
                Modes ??= [];
                ExcludedModes ??= [];
                FeedOptions ??= [];
                MountsAvailable ??= [];
                RoundPowers ??= [];
                RoundTypes ??= [];
                Features ??= [];
                MeleeHandedness ??= [];
                MeleeStyles ??= [];
                PowerupTypes ??= [];
                ThrownTypes ??= [];
                ThrownDamageTypes ??= []; 

                Tags.Eras ??= [];
                Tags.Sets ??= [];
                Tags.Sizes ??= [];
                Tags.Actions ??= [];
                Tags.Modes ??= [];
                Tags.ExcludedModes ??= [];
                Tags.FeedOptions ??= [];
                Tags.MountsAvailable ??= [];
                Tags.RoundPowers ??= [];
                Tags.RoundTypes ??= [];
                Tags.Features ??= [];
                Tags.MeleeHandedness ??= [];
                Tags.MeleeStyles ??= [];
                Tags.PowerupTypes ??= [];
                Tags.ThrownTypes ??= [];
                Tags.ThrownDamageTypes ??= [];
                Tags.Countries ??= [];

                objectTableDef = (ObjectTableDef)ScriptableObject.CreateInstance(typeof(ObjectTableDef));

                objectTableDef.UgcModule = UgcManager.H3Module;
                objectTableDef.UgcId = $"{id}_{index}_{Category}_{suffix}";
                objectTableDef.UgcFilePath = null;
                objectTableDef.UgcIsWritable = false;

                objectTableDef.name = Category.ToString();
                objectTableDef.Category = (FVRObject.ObjectCategory)Category;
                objectTableDef.MinAmmoCapacity = MinAmmoCapacity;
                objectTableDef.MaxAmmoCapacity = MaxAmmoCapacity;
                objectTableDef.SpawnsAreTheSame = SpawnsAreTheSame;
                objectTableDef.RequiredExactCapacity = -1;
                objectTableDef.IsBlanked = false;
                objectTableDef.SpawnsInSmallCase = false;
                objectTableDef.SpawnsInLargeCase = false;
                objectTableDef.UseIDListOverride = !AutoPopulateGroup;
                objectTableDef.ObjBlacklist = [.. ObjBlacklist];
                objectTableDef.IDOverride = ["M1911"];

                if (HasNewTags())
                    CopyNewTags();
                else
                    CopyOldTags();

                objectTableDef.Eras = [.. Eras.Select(o => (FVRObject.OTagEra)o)];
                objectTableDef.Sets = [.. Sets.Select(o => (FVRObject.OTagSet)o)];
                objectTableDef.Sizes = [.. Sizes.Select(o => (FVRObject.OTagFirearmSize)o)];
                objectTableDef.Actions = [.. Actions.Select(o => (FVRObject.OTagFirearmAction)o)];
                objectTableDef.Modes = [.. Modes.Select(o => (FVRObject.OTagFirearmFiringMode)o)];
                objectTableDef.ExcludeModes = [.. ExcludedModes.Select(o => (FVRObject.OTagFirearmFiringMode)o)];
                objectTableDef.Feedoptions = [.. FeedOptions.Select(o => (FVRObject.OTagFirearmFeedOption)o)];
                objectTableDef.MountsAvailable = [.. MountsAvailable.Select(o => (FVRObject.OTagFirearmMount)o)];
                objectTableDef.RoundPowers = [.. RoundPowers.Select(o => (FVRObject.OTagFirearmRoundPower)o)];
                objectTableDef.RoundTypes = [.. RoundTypes];
                objectTableDef.Features = [.. Features.Select(o => (FVRObject.OTagAttachmentFeature)o)];
                objectTableDef.MeleeHandedness = [.. MeleeHandedness.Select(o => (FVRObject.OTagMeleeHandedness)o)];
                objectTableDef.MeleeStyles = [.. MeleeStyles.Select(o => (FVRObject.OTagMeleeStyle)o)];
                objectTableDef.MountTypes = [.. MountTypes.Select(o => (FVRObject.OTagFirearmMount)o)];
                objectTableDef.PowerupTypes = [.. PowerupTypes.Select(o => (FVRObject.OTagPowerupType)o)];
                objectTableDef.ThrownTypes = [.. ThrownTypes.Select(o => (FVRObject.OTagThrownType)o)];
                objectTableDef.ThrownDamageTypes = [.. ThrownDamageTypes.Select(o => (FVRObject.OTagThrownDamageType)o)];
                objectTableDef.Countries = [.. Countries.Select(o => (FVRObject.OTagFirearmCountryOfOrigin)o)];

                objectTableDef.UsesVaultFileListOverride = UsesVaultFileListOverride;
                objectTableDef.UseFullPlayerVaultRandom = UseFullPlayerVaultRandom;
                objectTableDef.OverrideVaultFileWrappers = [.. OverrideVaultFileWrappers];
                objectTableDef.OverrideFileUsage = OverrideFileUsage;
            }

            return objectTableDef;
        }

        public ObjectTableDef GetObjectTableDef()
        {
            if (objectTableDef == null)
            {
                TNHFrameworkLogger.LogError("Tried to get ObjectTableDef, but it hasn't been initialized yet! Returning null!");
                return null;
            }

            return objectTableDef;
        }

        public List<string> GetObjects()
        {
            return objects;
        }

        public string GetRandomObject()
        {
            if (!objects.Any())
            {
                TNHFrameworkLogger.LogWarning($"GetRandomObject() was called on an empty objects list!");
                return string.Empty;
            }

            string itemID = objects[Random.Range(0, objects.Count)];

            // If this object is part of a family, choose an object from the family
            if (IM.OD.ContainsKey(itemID))
            {
                FVRObject obj = IM.OD[itemID];

                if (!string.IsNullOrEmpty(obj.Family) && familyDic.ContainsKey(obj.Family))
                {
                    List<FVRObject> list = familyDic[obj.Family];
                    itemID = list[Random.Range(0, list.Count)].ItemID;
                }
            }

            return itemID;
        }

        public bool UsesVaultFiles()
        {
            return vaultFiles != null && vaultFiles.Any();
        }

        public VaultFile GetRandomVaultFile()
        {
            if (!UsesVaultFiles())
            {
                TNHFrameworkLogger.LogWarning($"GetRandomVaultFile() was called on an empty vaultFiles list!");
                return null;
            }

            return vaultFiles[Random.Range(0, vaultFiles.Count)];
        }

        private bool HasNewTags()
        {
            return (Tags.Eras.Any()
                || Tags.Sets.Any()
                || Tags.Sizes.Any()
                || Tags.Actions.Any()
                || Tags.Modes.Any()
                || Tags.ExcludedModes.Any()
                || Tags.FeedOptions.Any()
                || Tags.MountsAvailable.Any()
                || Tags.RoundPowers.Any()
                || Tags.RoundTypes.Any()
                || Tags.Features.Any()
                || Tags.MeleeStyles.Any()
                || Tags.MeleeHandedness.Any()
                || Tags.MountTypes.Any()
                || Tags.PowerupTypes.Any()
                || Tags.ThrownTypes.Any()
                || Tags.ThrownDamageTypes.Any()
                || Tags.Countries.Any());
        }

        public bool CopyOldTags()
        {
            if (!HasNewTags())
            {
                Tags.MinYear = MinYear;
                Tags.MaxYear = MaxYear;
                Tags.Eras = [.. Eras];
                Tags.Sets = [.. Sets];
                Tags.Sizes = [.. Sizes];
                Tags.Actions = [.. Actions];
                Tags.Modes = [.. Modes];
                Tags.ExcludedModes = [.. ExcludedModes];
                Tags.FeedOptions = [.. FeedOptions];
                Tags.MountsAvailable = [.. MountsAvailable];
                Tags.RoundPowers = [.. RoundPowers];
                Tags.RoundTypes = [.. RoundTypes];
                Tags.Features = [.. Features];
                Tags.MeleeStyles = [.. MeleeStyles];
                Tags.MeleeHandedness = [.. MeleeHandedness];
                Tags.MountTypes = [.. MountTypes];
                Tags.PowerupTypes = [.. PowerupTypes];
                Tags.ThrownTypes = [.. ThrownTypes];
                Tags.ThrownDamageTypes = [.. ThrownDamageTypes];
                return true;
            }

            return false;
        }

        public bool CopyNewTags()
        {
            if (HasNewTags())
            {
                MinYear = Tags.MinYear;
                MaxYear = Tags.MaxYear;
                Eras = [.. Tags.Eras];
                Sets = [.. Tags.Sets];
                Sizes = [.. Tags.Sizes];
                Actions = [.. Tags.Actions];
                Modes = [.. Tags.Modes];
                ExcludedModes = [.. Tags.ExcludedModes];
                FeedOptions = [.. Tags.FeedOptions];
                MountsAvailable = [.. Tags.MountsAvailable];
                RoundPowers = [.. Tags.RoundPowers];
                RoundTypes = [.. Tags.RoundTypes];
                Features = [.. Tags.Features];
                MeleeStyles = [.. Tags.MeleeStyles];
                MeleeHandedness = [.. Tags.MeleeHandedness];
                MountTypes = [.. Tags.MountTypes];
                PowerupTypes = [.. Tags.PowerupTypes];
                ThrownTypes = [.. Tags.ThrownTypes];
                ThrownDamageTypes = [.. Tags.ThrownDamageTypes];
                return true;
            }

            return false;
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

                float randomSelection = Random.Range(0, combinedRarity);

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
                            return SubGroups[i].GetSpawnedEquipmentGroups();
                    }
                }
            }

            return [];
        }

        /// <summary>
        /// Fills out the object table and removes any unloaded items
        /// </summary>
        /// <returns> Returns true if valid, and false if empty </returns>
        public bool DelayedInit(string id, List<string> globalObjectBlacklist, bool allowInjection)
        {
            // Before we add anything from the IDOverride list, remove anything that isn't loaded
            TNHFrameworkUtils.RemoveUnloadedObjectIDs(this);
            objects.Clear();
            vaultFiles.Clear();

            if (IsCompatibleMagazine)
            {
                // Don't need to do anything yet
            }
            else if (UsesVaultFileListOverride)
            {
                ObjectTable objectTable = new();
                objectTable.Initialize(GetObjectTableDef(id, 0, "DroppedObjectPool"));
                vaultFiles = [.. objectTable.VaultFiles];
            }
            // Every item in IDOverride gets added to the list of spawnable objects
            else if (IDOverride != null && IDOverride.Any())
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
            else if (AutoPopulateGroup)
            {
                Initialize(globalObjectBlacklist);
            }

            // Perform delayed init on all subgroups. If they are empty, we remove them
            if (SubGroups != null && SubGroups.Any())
            {
                for (int i = SubGroups.Count - 1; i >= 0; i--)
                {
                    if (!SubGroups[i].DelayedInit(id, globalObjectBlacklist, allowInjection))
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
            return vaultFiles.Any() || objects.Any() || IsCompatibleMagazine || (SubGroups != null && SubGroups.Any());
        }

        public void Initialize(List<string> globalObjectBlacklist)
        {
            TNHFrameworkLogger.Log($"Autopopulating {Category} equipment group", TNHFrameworkLogger.LogType.Character);
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
                    {
                        continue;
                    }
                }
                else if (MaxAmmoCapacity > -1 && fvrobject.MinCapacityRelated > MaxAmmoCapacity)
                {
                    if (Category != ObjectCategory.MeleeWeapon)  // Fix for Meat Fortress melee weapons
                    {
                        continue;
                    }
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
                else if (Tags.RoundTypes != null && Tags.RoundTypes.Any() && !Tags.RoundTypes.Contains(fvrobject.RoundType))
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
                            continue;
                    }

                    if (Tags.ExcludedModes != null && Tags.ExcludedModes.Any())
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
                            continue;
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
                            continue;
                    }

                    if (Tags.MountsAvailable != null && Tags.MountsAvailable.Any())
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
                            continue;
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
                    else if (Tags.Countries != null && Tags.Countries.Any() && !Tags.Countries.Contains((TagFirearmCountryOfOrigin)fvrobject.TagFirearmCountryOfOrigin))
                    {
                        continue;
                    }
                    else if (ObjBlacklist != null && ObjBlacklist.Contains(fvrobject))
                    {
                        continue;
                    }
                    else if (!string.IsNullOrEmpty(fvrobject.Family))
                    {
                        if (familyDic.ContainsKey(fvrobject.Family))
                        {
                            familyDic[fvrobject.Family].Add(fvrobject);
                            continue;
                        }
                        else
                        {
                            familyDic.Add(fvrobject.Family, []);
                            familyDic[fvrobject.Family].Add(fvrobject);
                        }
                    }

                    objects.Add(fvrobject.ItemID);
                }
            }

            // Adjust probability for families
            foreach (KeyValuePair<string, List<FVRObject>> item in familyDic)
            {
                List<FVRObject> value = item.Value;
                int numToAdd = Mathf.RoundToInt(Mathf.Sqrt(value.Count)) - 1;

                for (int num = 0; num < numToAdd; num++)
                {
                    objects.Add(value[0].ItemID);
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
        public List<FireArmRoundType> RoundTypes = [];
        public List<TagAttachmentFeature> Features = [];
        public List<TagMeleeStyle> MeleeStyles = [];
        public List<TagMeleeHandedness> MeleeHandedness = [];
        public List<TagFirearmMount> MountTypes = [];
        public List<TagPowerupType> PowerupTypes = [];
        public List<TagThrownType> ThrownTypes = [];
        public List<TagThrownDamageType> ThrownDamageTypes = [];
        public List<TagFirearmCountryOfOrigin> Countries = [];
    }

    public class LoadoutEntry()
    {
        public EquipmentGroup PrimaryGroup = new();
        public EquipmentGroup BackupGroup = new();
        public FVRObject AmmoObjectOverride = null;

        [JsonIgnore]
        private TNH_CharacterDef.LoadoutEntry loadout;

        public LoadoutEntry(TNH_CharacterDef.LoadoutEntry loadout) : this()
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry
                {
                    TableDefs = [],
                    ListOverride = [],
                    Num_Mags_SL_Clips = 3,
                    Num_Rounds = 9,
                    AmmoObjectOverride = AmmoObjectOverride
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

                AmmoObjectOverride = loadout.AmmoObjectOverride;
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
                        Rarity = 0,
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

                AmmoObjectOverride = loadout.AmmoObjectOverride;
            }

            this.loadout = loadout;
        }

        public TNH_CharacterDef.LoadoutEntry GetLoadoutEntry(string id, int index, string suffix)
        {
            if (loadout == null)
            {
                loadout = new TNH_CharacterDef.LoadoutEntry
                {
                    TableDefs = [],
                    ListOverride = [],
                    Num_Mags_SL_Clips = 3,
                    Num_Rounds = 9,
                    AmmoObjectOverride = AmmoObjectOverride
                };

                if (PrimaryGroup != null)
                {
                    loadout.TableDefs = [PrimaryGroup.GetObjectTableDef(id, index, suffix)];
                }
            }

            return loadout;
        }

        public bool DelayedInit(string id, List<string> globalObjectBlacklist)
        {
            if (loadout != null)
            {
                if (PrimaryGroup != null)
                {
                    if (!PrimaryGroup.DelayedInit(id, globalObjectBlacklist, false))
                    {
                        TNHFrameworkLogger.Log("Primary group for loadout entry was empty, setting to null!", TNHFrameworkLogger.LogType.Character);
                        PrimaryGroup = null;
                    }
                }

                if (BackupGroup != null)
                {
                    if (!BackupGroup.DelayedInit(id, globalObjectBlacklist, false))
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

#if (false)  // ODK - Not going to implement this for now
    public class SosigLoot()
    {
        public SosigLootGroup LootGroup_Default;
        public SosigLootGroup LootGroup_Boxes;
        public List<SosigLootGroup> SosigLootGroups = [];

        [JsonIgnore]
        TNH_SosigLootTable sosigLootTable;
    }

    public class SosigLootGroup()
    {
        public bool SpawnsForSupply;
        public bool SpawnsForPatrol;
        public bool SpawnsForTake;
        public bool SpawnsForHold;
        public float SpawnChance = 1f;
        public bool DoesLootDropInBoxes = true;
        public float ChanceofSpawnSmartAmmo;
        public float MinForMultiObject = 1f;
        public float MaxForMultiObject = 1f;
        public float MinCapacity = -1f;
        public float MaxCapacity = -1f;

        public List<string> SosigEnemyIDs = [];
        public List<SosigPool> EquipPool;

        [JsonIgnore]
        private TNH_SosigLootTable.SosigLootGroup sosigLootGroup;
    }

    public class SosigPool()
    {
        public int MinLevelAppears = 1;
        public int MaxLevelAppears = 2;
        public float Rarity = 1f;
        public EquipmentGroup Equipment;

        [JsonIgnore]
        TNH_LootPoolDef.PoolEntry poolEntry;
    }
#endif

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

        public Level(TNH_Progression.Level level) : this()
        {
            TakeChallenge = new TakeChallenge(level.TakeChallenge);
            HoldPhases = [.. level.HoldChallenge.Phases.Select(o => new Phase(o))];
            SupplyChallenge = new TakeChallenge(level.TakeChallenge);
            Patrols = [.. level.PatrolChallenge.Patrols.Select(o => new Patrol(o))];
            NumOverrideTokensForHold = level.NumOverrideTokensForHold;

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
            PossiblePanelTypes =
            [
                PanelType.AmmoReloader,
                PanelType.MagDuplicator,
                PanelType.Recycler,
            ];

            this.level = level;
        }

        public TNH_Progression.Level GetLevel()
        {
            if (level == null)
            {
                level = new()
                {
                    TakeChallenge = TakeChallenge.GetTakeChallenge(),
                    HoldChallenge = (TNH_HoldChallenge)ScriptableObject.CreateInstance(typeof(TNH_HoldChallenge)),
                    SupplyChallenge = SupplyChallenge.GetTakeChallenge(),
                    PatrolChallenge = (TNH_PatrolChallenge)ScriptableObject.CreateInstance(typeof(TNH_PatrolChallenge)),
                    NumOverrideTokensForHold = NumOverrideTokensForHold
                };

                level.HoldChallenge.Phases = [.. HoldPhases.Select(o => o.GetPhase())];
                level.PatrolChallenge.Patrols = [.. Patrols.Select(o => o.GetPatrol())];
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
                return true;

            if (SupplyChallenge.EnemyType == id)
                return true;

            foreach (Patrol patrol in Patrols)
            {
                if (patrol.LeaderType == id)
                    return true;

                foreach (string sosigID in patrol.EnemyType)
                {
                    if (sosigID == id)
                        return true;
                }
            }

            foreach (Phase phase in HoldPhases)
            {
                if (phase.LeaderType == id)
                    return true;

                foreach (string sosigID in phase.EnemyType)
                {
                    if (sosigID == id)
                        return true;
                }
            }

            return false;
        }
    }


    public class TakeChallenge()
    {
        public TNH_TurretType TurretType;
        public int NumTurrets;
        public string EnemyType;
        public int NumGuards;
        public int IFFUsed;

        [JsonIgnore]
        public SosigEnemyTemplate OverrideGID;

        [JsonIgnore]
        private TNH_TakeChallenge takeChallenge;

        public TakeChallenge(TNH_TakeChallenge takeChallenge) : this()
        {
            TurretType = takeChallenge.TurretType;
            NumTurrets = takeChallenge.NumTurrets;
            EnemyType = takeChallenge.GID.ToString();
            NumGuards = takeChallenge.NumGuards;
            IFFUsed = takeChallenge.IFFUsed;
            OverrideGID = takeChallenge.OverrideGID;

            this.takeChallenge = takeChallenge;
        }

        public TNH_TakeChallenge GetTakeChallenge()
        {
            if (takeChallenge == null)
            {
                takeChallenge = (TNH_TakeChallenge)ScriptableObject.CreateInstance(typeof(TNH_TakeChallenge));
                takeChallenge.OverrideGID = OverrideGID;
                takeChallenge.TurretType = TurretType;
                takeChallenge.NumTurrets = NumTurrets;
                takeChallenge.GID = SosigEnemyID.None;
                takeChallenge.NumGuards = NumGuards;
                takeChallenge.IFFUsed = IFFUsed;

                // Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType))
                    takeChallenge.GID = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType];
                else
                    takeChallenge.GID = TNHFrameworkUtils.ParseEnemyType(EnemyType);
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
        public float SpawnCadence;
        public int MaxEnemiesAlive;
        public int MaxDirections;
        public float ScanTime;
        public float WarmupTime;
        public bool IsLeaderBoss;
        public int IFFUsed;
        public float GrenadeChance;
        public string GrenadeType;
        public bool SwarmPlayer;

        [JsonIgnore]
        public SosigEnemyTemplate OverrideEType;

        [JsonIgnore]
        public SosigEnemyTemplate OverrideLType;

        [JsonIgnore]
        private TNH_HoldChallenge.Phase phase;

        public Phase(TNH_HoldChallenge.Phase phase) : this()
        {
            Encryptions = [phase.Encryption];
            MinTargets = phase.MinTargets;
            MaxTargets = phase.MaxTargets;
            MinTargetsLimited = phase.MinTargets_Limited;
            MaxTargetsLimited = phase.MaxTargets_Limited;
            EnemyType = [phase.EType.ToString()];
            LeaderType = phase.LType.ToString();
            MinEnemies = phase.MinEnemies;
            MaxEnemies = phase.MaxEnemies;
            SpawnCadence = phase.SpawnCadence;
            MaxEnemiesAlive = phase.MaxEnemiesAlive;
            MaxDirections = phase.MaxDirections;
            ScanTime = phase.ScanTime;
            WarmupTime = phase.WarmUp;
            IsLeaderBoss = phase.IsLeaderBoss;
            IFFUsed = phase.IFFUsed;
            GrenadeChance = 0;
            GrenadeType = "Sosiggrenade_Flash";
            SwarmPlayer = false;

            OverrideEType = phase.OverrideEType;
            OverrideLType = phase.OverrideLType;
            this.phase = phase;
        }

        public TNH_HoldChallenge.Phase GetPhase()
        {
            if (phase == null)
            {
                phase = new TNH_HoldChallenge.Phase
                {
                    OverrideEType = OverrideEType,
                    OverrideLType = OverrideLType,
                    Encryption = Encryptions[0],
                    MinTargets = MinTargets,
                    MaxTargets = MaxTargets,
                    MinTargets_Limited = MinTargetsLimited,
                    MaxTargets_Limited = MaxTargetsLimited,
                    EType = SosigEnemyID.None,
                    LType = SosigEnemyID.None,
                    MinEnemies = MinEnemies,
                    MaxEnemies = MaxEnemies,
                    SpawnCadence = SpawnCadence,
                    MaxEnemiesAlive = MaxEnemiesAlive,
                    MaxDirections = MaxDirections,
                    ScanTime = ScanTime,
                    WarmUp = Mathf.Max(0f, WarmupTime),
                    IsLeaderBoss = IsLeaderBoss,
                    IFFUsed = IFFUsed
                };

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
        public SosigEnemyTemplate OverrideEType;

        [JsonIgnore]
        public SosigEnemyTemplate OverrideLType;

        [JsonIgnore]
        private TNH_PatrolChallenge.Patrol patrol;

        public Patrol(TNH_PatrolChallenge.Patrol patrol) : this()
        {
            OverrideEType = patrol.OverrideEType;
            OverrideLType = patrol.OverrideLType;
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
                patrol = new TNH_PatrolChallenge.Patrol
                {
                    OverrideEType = OverrideEType,
                    OverrideLType = OverrideLType,
                    EType = SosigEnemyID.None,
                    LType = SosigEnemyID.None,
                    PatrolSize = PatrolSize,
                    MaxPatrols = MaxPatrols,
                    MaxPatrols_LimitedAmmo = MaxPatrolsLimited,
                    TimeTilRegen = PatrolCadence,
                    TimeTilRegen_LimitedAmmo = PatrolCadenceLimited,
                    IFFUsed = IFFUsed
                };

                // Try to get the necessary SosigEnemyIDs
                if (LoadedTemplateManager.SosigIDDict.ContainsKey(EnemyType[0]))
                    patrol.EType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[EnemyType[0]];
                else
                    patrol.EType = TNHFrameworkUtils.ParseEnemyType(EnemyType[0]);

                if (LoadedTemplateManager.SosigIDDict.ContainsKey(LeaderType))
                    patrol.LType = (SosigEnemyID)LoadedTemplateManager.SosigIDDict[LeaderType];
                else
                    patrol.LType = TNHFrameworkUtils.ParseEnemyType(LeaderType);
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
