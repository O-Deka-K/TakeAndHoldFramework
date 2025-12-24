using FistVR;
using System.Collections.Generic;
using System.Linq;
using TNHFramework.Utilities;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace TNHFramework.ObjectTemplates
{
    public class SosigTemplate()
    {
        public string DisplayName;
        public SosigEnemyCategory SosigEnemyCategory;
        public string SosigEnemyID;
        public List<string> SosigPrefabs = [];
        public List<SosigConfig> Configs = [];
        public List<SosigConfig> ConfigsEasy = [];
        public List<OutfitConfig> OutfitConfigs = [];
        public List<string> WeaponOptions = [];
        public List<string> WeaponOptionsSecondary = [];
        public List<string> WeaponOptionsTertiary = [];
        public float SecondaryChance;
        public float TertiaryChance;
        public float DroppedLootChance;
        public V1.EquipmentGroup DroppedObjectPool = new();

        [JsonIgnore]
        private SosigEnemyTemplate template;

        public SosigTemplate(SosigEnemyTemplate template) : this()
        {
            DisplayName = template.DisplayName;
            SosigEnemyCategory = template.SosigEnemyCategory;
            SosigEnemyID = template.SosigEnemyID.ToString();
            SecondaryChance = template.SecondaryChance;
            TertiaryChance = template.TertiaryChance;

            SosigPrefabs = [.. template.SosigPrefabs.Select(o => o.ItemID)];
            WeaponOptions = [.. template.WeaponOptions.Select(o => o.ItemID)];
            WeaponOptionsSecondary = [.. template.WeaponOptions_Secondary.Select(o => o.ItemID)];
            WeaponOptionsTertiary = [.. template.WeaponOptions_Tertiary.Select(o => o.ItemID)];

            Configs = [.. template.ConfigTemplates.Select(o => new SosigConfig(o))];
            ConfigsEasy = [.. template.ConfigTemplates_Easy.Select(o => new SosigConfig(o))];
            OutfitConfigs = [.. template.OutfitConfig.Select(o => new OutfitConfig(o))];

            DroppedLootChance = 0;
            DroppedObjectPool = new();

            this.template = template;
        }

        public SosigEnemyTemplate GetSosigEnemyTemplate()
        {
            if (template == null)
            {
                TNHFrameworkLogger.Log("Getting sosig template", TNHFrameworkLogger.LogType.Character);

                template = (SosigEnemyTemplate)ScriptableObject.CreateInstance(typeof(SosigEnemyTemplate));

                template.DisplayName = DisplayName;
                template.SosigEnemyCategory = SosigEnemyCategory;
                template.SecondaryChance = SecondaryChance;
                template.TertiaryChance = TertiaryChance;

                TNHFrameworkLogger.Log("Getting sosig config", TNHFrameworkLogger.LogType.Character);

                Configs.RemoveAll(o => o == null);
                template.ConfigTemplates = [.. Configs.Select(o => o.GetConfigTemplate())];

                ConfigsEasy.RemoveAll(o => o == null);
                template.ConfigTemplates_Easy = [.. ConfigsEasy.Select(o => o.GetConfigTemplate())];

                OutfitConfigs.RemoveAll(o => o == null);
                template.OutfitConfig = [.. OutfitConfigs.Select(o => o.GetOutfitConfig())];
            }

            return template;
        }

        public void DelayedInit()
        {
            if (template != null)
            {
                TNHFrameworkLogger.Log("Delayed init of sosig: " + DisplayName, TNHFrameworkLogger.LogType.Character);

                TNHFrameworkUtils.RemoveUnloadedObjectIDs(this);

                template.SosigPrefabs = [.. SosigPrefabs.Select(o => IM.OD[o])];
                template.WeaponOptions = [.. WeaponOptions.Select(o => IM.OD[o])];
                template.WeaponOptions_Secondary = [.. WeaponOptionsSecondary.Select(o => IM.OD[o])];
                template.WeaponOptions_Tertiary = [.. WeaponOptionsTertiary.Select(o => IM.OD[o])];

                foreach (OutfitConfig outfit in OutfitConfigs)
                {
                    outfit.DelayedInit();
                }

                DroppedObjectPool?.DelayedInit();
                
				// Add the new sosig template to the global dictionaries
                ManagerSingleton<IM>.Instance.odicSosigObjsByID.Add(template.SosigEnemyID, template);
                ManagerSingleton<IM>.Instance.odicSosigIDsByCategory[template.SosigEnemyCategory].Add(template.SosigEnemyID);
                ManagerSingleton<IM>.Instance.odicSosigObjsByCategory[template.SosigEnemyCategory].Add(template);
            }
        }
    }

    public class SosigConfig()
    {
        public float ViewDistance = 250f;
        public Vector3Serializable StateSightRangeMults = new(new Vector3(0.1f, 0.35f, 1f));
        public float HearingDistance = 300f;
        public Vector3Serializable StateHearingRangeMults = new(new Vector3(0.6f, 1f, 1f));
        public float MaxFOV = 105f;
        public Vector3Serializable StateFOVMults = new(new Vector3(0.5f, 0.6f, 1f));
        public bool HasABrain = true;
        public bool HasNightVision;
        public bool RegistersPassiveThreats;
        public bool DoesAggroOnFriendlyFire;
        public bool IgnoresNeedForWeapons;
        public float SearchExtentsModifier = 1f;
        public bool DoesDropWeaponsOnBallistic = true;
        public bool CanPickupRanged = true;
        public bool CanPickupMelee = true;
        public bool CanPickupOther = true;
        public float MaxThreatingIFFReactionRange_Visual = 50f;
        public float MaxThreatingIFFReactionRange_Sonic = 50f;
        public float AggroSensitivityMultiplier = 1f;
        public float EntityRecognitionSpeedMultiplier = 1f;
        public float CombatTargetIdentificationSpeedMultiplier = 1f;
        public int TargetCapacity = 5;
        public float TargetTrackingTime = 2f;
        public float NoFreshTargetTime = 1.5f;
        public float AssaultPointOverridesSkirmishPointWhenFurtherThan = 200f;
        public float TimeInSkirmishToAlert = 1f;
        public float RunSpeed = 3.5f;
        public float WalkSpeed = 1.4f;
        public float SneakSpeed = 0.6f;
        public float CrawlSpeed = 0.3f;
        public float TurnSpeed = 2f;
        public float MaxJointLimit = 6f;
        public float MovementRotMagnitude = 10f;
        public bool AppliesDamageResistToIntegrityLoss;
        public float TotalMustard = 100f;
        public float BleedDamageMult = 0.5f;
        public float BleedRateMultiplier = 1f;
        public float BleedVFXIntensity = 0.2f;
        public float DamMult_Projectile = 1f;
        public float DamMult_Explosive = 1f;
        public float DamMult_Melee = 1f;
        public float DamMult_Piercing = 1f;
        public float DamMult_Blunt = 1f;
        public float DamMult_Cutting = 1f;
        public float DamMult_Thermal = 1f;
        public float DamMult_Chilling = 1f;
        public float DamMult_EMP = 1f;
        public List<float> LinkDamageMultipliers = [];
        public List<float> LinkStaggerMultipliers = [];
        public List<Vector2Serializable> StartingLinkIntegrity = [];
        public List<float> StartingChanceBrokenJoint = [];
        public float ShudderThreshold = 2f;
        public float ConfusionThreshold = 0.3f;
        public float ConfusionMultiplier = 6f;
        public float ConfusionTimeMax = 4f;
        public float StunThreshold = 1.4f;
        public float StunMultiplier = 2f;
        public float StunTimeMax = 4f;
        public bool CanBeKnockedOut = true;
        public float MaxUnconsciousTime = 90f;
        public bool CanBeGrabbed = true;
        public bool CanBeSevered = true;
        public bool CanBeStabbed = true;
        public bool CanBeSurpressed = true;
        public float SuppressionMult = 1f;
        public bool DoesJointBreakKill_Head = true;
        public bool DoesJointBreakKill_Upper;
        public bool DoesJointBreakKill_Lower;
        public bool DoesSeverKill_Head = true;
        public bool DoesSeverKill_Upper = true;
        public bool DoesSeverKill_Lower = true;
        public bool DoesExplodeKill_Head = true;
        public bool DoesExplodeKill_Upper = true;
        public bool DoesExplodeKill_Lower = true;
        //public bool OverrideSpeech = false;
        //public SosigSpeechSet OverrideSpeechSet;

        [JsonIgnore]
        private SosigConfigTemplate template;

        public SosigConfig(SosigConfigTemplate template) : this()
        {
            ViewDistance = template.ViewDistance;
            StateSightRangeMults = new(template.StateSightRangeMults);
            HearingDistance = template.HearingDistance;
            StateHearingRangeMults = new(template.StateHearingRangeMults);
            MaxFOV = template.MaxFOV;
            StateFOVMults = new(template.StateFOVMults);
            HasABrain = template.HasABrain;
            HasNightVision = template.HasNightVision;
            RegistersPassiveThreats = template.RegistersPassiveThreats;
            DoesAggroOnFriendlyFire = template.DoesAggroOnFriendlyFire;
            IgnoresNeedForWeapons = template.IgnoresNeedForWeapons;
            SearchExtentsModifier = template.SearchExtentsModifier;
            DoesDropWeaponsOnBallistic = template.DoesDropWeaponsOnBallistic;
            CanPickupRanged = template.CanPickup_Ranged;
            CanPickupMelee = template.CanPickup_Melee;
            CanPickupOther = template.CanPickup_Other;
            MaxThreatingIFFReactionRange_Visual = template.MaxThreatingIFFReactionRange_Visual;
            MaxThreatingIFFReactionRange_Sonic = template.MaxThreatingIFFReactionRange_Sonic;
            AggroSensitivityMultiplier = template.AggroSensitivityMultiplier;
            EntityRecognitionSpeedMultiplier = template.EntityRecognitionSpeedMultiplier;
            CombatTargetIdentificationSpeedMultiplier = template.CombatTargetIdentificationSpeedMultiplier;
            TargetCapacity = template.TargetCapacity;
            TargetTrackingTime = template.TargetTrackingTime;
            NoFreshTargetTime = template.NoFreshTargetTime;
            AssaultPointOverridesSkirmishPointWhenFurtherThan = template.AssaultPointOverridesSkirmishPointWhenFurtherThan;
            TimeInSkirmishToAlert = template.TimeInSkirmishToAlert;
            RunSpeed = template.RunSpeed;
            WalkSpeed = template.WalkSpeed;
            SneakSpeed = template.SneakSpeed;
            CrawlSpeed = template.CrawlSpeed;
            TurnSpeed = template.TurnSpeed;
            MaxJointLimit = template.MaxJointLimit;
            MovementRotMagnitude = template.MovementRotMagnitude;
            AppliesDamageResistToIntegrityLoss = template.AppliesDamageResistToIntegrityLoss;
            TotalMustard = template.TotalMustard;
            BleedDamageMult = template.BleedDamageMult;
            BleedRateMultiplier = template.BleedRateMultiplier;
            BleedVFXIntensity = template.BleedVFXIntensity;
            DamMult_Projectile = template.DamMult_Projectile;
            DamMult_Explosive = template.DamMult_Explosive;
            DamMult_Melee = template.DamMult_Melee;
            DamMult_Piercing = template.DamMult_Piercing;
            DamMult_Blunt = template.DamMult_Blunt;
            DamMult_Cutting = template.DamMult_Cutting;
            DamMult_Thermal = template.DamMult_Thermal;
            DamMult_Chilling = template.DamMult_Chilling;
            DamMult_EMP = template.DamMult_EMP;
            LinkDamageMultipliers = template.LinkDamageMultipliers;
            LinkStaggerMultipliers = template.LinkStaggerMultipliers;
            StartingLinkIntegrity = [.. template.StartingLinkIntegrity.Select(o => new Vector2Serializable(o))];
            StartingChanceBrokenJoint = template.StartingChanceBrokenJoint;
            ShudderThreshold = template.ShudderThreshold;
            ConfusionThreshold = template.ConfusionThreshold;
            ConfusionMultiplier = template.ConfusionMultiplier;
            ConfusionTimeMax = template.ConfusionTimeMax;
            StunThreshold = template.StunThreshold;
            StunMultiplier = template.StunMultiplier;
            StunTimeMax = template.StunTimeMax;
            CanBeKnockedOut = template.CanBeKnockedOut;
            MaxUnconsciousTime = template.MaxUnconsciousTime;
            CanBeGrabbed = template.CanBeGrabbed;
            CanBeSevered = template.CanBeSevered;
            CanBeStabbed = template.CanBeStabbed;
            CanBeSurpressed = template.CanBeSurpressed;
            SuppressionMult = template.SuppressionMult;
            DoesJointBreakKill_Head = template.DoesJointBreakKill_Head;
            DoesJointBreakKill_Upper = template.DoesJointBreakKill_Upper;
            DoesJointBreakKill_Lower = template.DoesJointBreakKill_Lower;
            DoesSeverKill_Head = template.DoesSeverKill_Head;
            DoesSeverKill_Upper = template.DoesSeverKill_Upper;
            DoesSeverKill_Lower = template.DoesSeverKill_Lower;
            DoesExplodeKill_Head = template.DoesExplodeKill_Head;
            DoesExplodeKill_Upper = template.DoesExplodeKill_Upper;
            DoesExplodeKill_Lower = template.DoesExplodeKill_Lower;

            this.template = template;
        }

        public SosigConfigTemplate GetConfigTemplate()
        {
            if (template == null)
            {
                template = (SosigConfigTemplate)ScriptableObject.CreateInstance(typeof(SosigConfigTemplate));

                template.ViewDistance = ViewDistance;
                template.StateSightRangeMults = StateSightRangeMults.GetVector3();
                template.HearingDistance = HearingDistance;
                template.StateHearingRangeMults = StateHearingRangeMults.GetVector3();
                template.MaxFOV = MaxFOV;
                template.StateFOVMults = StateFOVMults.GetVector3();
                template.HasABrain = HasABrain;
                template.HasNightVision = HasNightVision;
                template.RegistersPassiveThreats = RegistersPassiveThreats;
                template.DoesAggroOnFriendlyFire = DoesAggroOnFriendlyFire;
                template.IgnoresNeedForWeapons = IgnoresNeedForWeapons;
                template.SearchExtentsModifier = SearchExtentsModifier;
                template.DoesDropWeaponsOnBallistic = DoesDropWeaponsOnBallistic;
                template.CanPickup_Ranged = CanPickupRanged;
                template.CanPickup_Melee = CanPickupMelee;
                template.CanPickup_Other = CanPickupOther;
                template.MaxThreatingIFFReactionRange_Visual = MaxThreatingIFFReactionRange_Visual;
                template.MaxThreatingIFFReactionRange_Sonic = MaxThreatingIFFReactionRange_Sonic;
                template.AggroSensitivityMultiplier = AggroSensitivityMultiplier;
                template.EntityRecognitionSpeedMultiplier = EntityRecognitionSpeedMultiplier;
                template.CombatTargetIdentificationSpeedMultiplier = CombatTargetIdentificationSpeedMultiplier;
                template.TargetCapacity = TargetCapacity;
                template.TargetTrackingTime = TargetTrackingTime;
                template.NoFreshTargetTime = NoFreshTargetTime;
                template.AssaultPointOverridesSkirmishPointWhenFurtherThan = AssaultPointOverridesSkirmishPointWhenFurtherThan;
                template.TimeInSkirmishToAlert = TimeInSkirmishToAlert;
                template.RunSpeed = RunSpeed;
                template.WalkSpeed = WalkSpeed;
                template.SneakSpeed = SneakSpeed;
                template.CrawlSpeed = CrawlSpeed;
                template.TurnSpeed = TurnSpeed;
                template.MaxJointLimit = MaxJointLimit;
                template.MovementRotMagnitude = MovementRotMagnitude;
                template.AppliesDamageResistToIntegrityLoss = AppliesDamageResistToIntegrityLoss;
                template.TotalMustard =	TotalMustard;
                template.BleedDamageMult = BleedDamageMult;
                template.BleedRateMultiplier = BleedRateMultiplier;
                template.BleedVFXIntensity = BleedVFXIntensity;
                template.DamMult_Projectile = DamMult_Projectile;
                template.DamMult_Explosive = DamMult_Explosive;
                template.DamMult_Melee = DamMult_Melee;
                template.DamMult_Piercing = DamMult_Piercing;
                template.DamMult_Blunt = DamMult_Blunt;
                template.DamMult_Cutting = DamMult_Cutting;
                template.DamMult_Thermal = DamMult_Thermal;
                template.DamMult_Chilling = DamMult_Chilling;
                template.DamMult_EMP = DamMult_EMP;
                template.LinkDamageMultipliers = LinkDamageMultipliers;
                template.LinkStaggerMultipliers = LinkStaggerMultipliers;
                template.StartingLinkIntegrity = [.. StartingLinkIntegrity.Select(o => o.GetVector2())];
                template.StartingChanceBrokenJoint = StartingChanceBrokenJoint;
                template.ShudderThreshold = ShudderThreshold;
                template.ConfusionThreshold = ConfusionThreshold;
                template.ConfusionMultiplier = ConfusionMultiplier;
                template.ConfusionTimeMax = ConfusionTimeMax;
                template.StunThreshold = StunThreshold;
                template.StunMultiplier = StunMultiplier;
                template.StunTimeMax = StunTimeMax;
                template.CanBeKnockedOut = CanBeKnockedOut;
                template.MaxUnconsciousTime = MaxUnconsciousTime;
                template.CanBeGrabbed = CanBeGrabbed;
                template.CanBeSevered = CanBeSevered;
                template.CanBeStabbed = CanBeStabbed;
                template.CanBeSurpressed = CanBeSurpressed;
                template.SuppressionMult = SuppressionMult;
                template.DoesJointBreakKill_Head = DoesJointBreakKill_Head;
                template.DoesJointBreakKill_Upper = DoesJointBreakKill_Upper;
                template.DoesJointBreakKill_Lower = DoesJointBreakKill_Lower;
                template.DoesSeverKill_Head = DoesSeverKill_Head;
                template.DoesSeverKill_Upper = DoesSeverKill_Upper;
                template.DoesSeverKill_Lower = DoesSeverKill_Lower;
                template.DoesExplodeKill_Head = DoesExplodeKill_Head;
                template.DoesExplodeKill_Upper = DoesExplodeKill_Upper;
                template.DoesExplodeKill_Lower = DoesExplodeKill_Lower;
                template.UsesLinkSpawns = false;
                template.LinkSpawns = [];
                template.LinkSpawnChance = [];
                template.OverrideSpeech = false;
            }

            return template;
        }
    }

    public class OutfitConfig()
    {
        public List<string> Headwear = [];
        public float Chance_Headwear;
        public bool ForceWearAllHead;
        public bool HeadUsesTorsoIndex;

        public List<string> Eyewear = [];
        public float Chance_Eyewear;
        public bool ForceWearAllEye;

        public List<string> Facewear = [];
        public float Chance_Facewear;
        public bool ForceWearAllFace;

        public List<string> Torsowear = [];
        public float Chance_Torsowear;
        public bool ForceWearAllTorso;

        public List<string> Pantswear = [];
        public float Chance_Pantswear;
        public bool ForceWearAllPants;
        public bool PantsUsesTorsoIndex;

        public List<string> Pantswear_Lower = [];
        public float Chance_Pantswear_Lower;
        public bool ForceWearAllPantsLower;
        public bool PantsLowerUsesPantsIndex;

        public List<string> Backpacks = [];
        public float Chance_Backpacks;
        public bool ForceWearAllBackpacks;

        public List<string> TorosDecoration = [];
        public float Chance_TorosDecoration;
        public bool ForceWearAllTorosDecorations;

        public List<string> Belt = [];
        public float Chance_Belt;
        public bool ForceWearAllBelts;

        [JsonIgnore]
        private SosigOutfitConfig template;

        public OutfitConfig(SosigOutfitConfig template) : this()
        {
            Headwear = [.. template.Headwear.Select(o => o.ItemID)];
            Chance_Headwear = template.Chance_Headwear;
            HeadUsesTorsoIndex = template.HeadUsesTorsoIndex;

            Eyewear = [.. template.Eyewear.Select(o => o.ItemID)];
            Chance_Eyewear = template.Chance_Eyewear;

            Facewear = [.. template.Facewear.Select(o => o.ItemID)];
            Chance_Facewear = template.Chance_Facewear;

            Torsowear = [.. template.Torsowear.Select(o => o.ItemID)];
            Chance_Torsowear = template.Chance_Torsowear;

            Pantswear = [.. template.Pantswear.Select(o => o.ItemID)];
            Chance_Pantswear = template.Chance_Pantswear;
            PantsUsesTorsoIndex = template.PantsUsesTorsoIndex;

            Pantswear_Lower = [.. template.Pantswear_Lower.Select(o => o.ItemID)];
            Chance_Pantswear_Lower = template.Chance_Pantswear_Lower;
            PantsLowerUsesPantsIndex = template.PantsLowerUsesPantsIndex;

            Backpacks = [.. template.Backpacks.Select(o => o.ItemID)];
            Chance_Backpacks = template.Chance_Backpacks;

            TorosDecoration = [.. template.TorosDecoration.Select(o => o.ItemID)];
            Chance_TorosDecoration = template.Chance_TorosDecoration;

            Belt = [.. template.Belt.Select(o => o.ItemID)];
            Chance_Belt = template.Chance_Belt;

            this.template = template;
        }

        public SosigOutfitConfig GetOutfitConfig()
        {
            if (template == null)
            {
                template = (SosigOutfitConfig)ScriptableObject.CreateInstance(typeof(SosigOutfitConfig));
                
                template.Chance_Headwear = Chance_Headwear;
                template.HeadUsesTorsoIndex = HeadUsesTorsoIndex;
                template.Chance_Eyewear = Chance_Eyewear;
                template.Chance_Facewear = Chance_Facewear;
                template.Chance_Torsowear = Chance_Torsowear;
                template.Chance_Pantswear = Chance_Pantswear;
                template.PantsUsesTorsoIndex = PantsUsesTorsoIndex;
                template.Chance_Pantswear_Lower = Chance_Pantswear_Lower;
                template.PantsLowerUsesPantsIndex = PantsLowerUsesPantsIndex;
                template.Chance_Backpacks = Chance_Backpacks;
                template.Chance_TorosDecoration = Chance_TorosDecoration;
                template.Chance_Belt = Chance_Belt;
            }

            return this.template;
        }

        public void DelayedInit()
        {
            template.Headwear = [.. Headwear.Select(o => IM.OD[o])];
            template.Eyewear = [.. Eyewear.Select(o => IM.OD[o])];
            template.Facewear = [.. Facewear.Select(o => IM.OD[o])];
            template.Torsowear = [.. Torsowear.Select(o => IM.OD[o])];
            template.Pantswear = [.. Pantswear.Select(o => IM.OD[o])];
            template.Pantswear_Lower = [.. Pantswear_Lower.Select(o => IM.OD[o])];
            template.Backpacks = [.. Backpacks.Select(o => IM.OD[o])];
            template.TorosDecoration = [.. TorosDecoration.Select(o => IM.OD[o])];
            template.Belt = [.. Belt.Select(o => IM.OD[o])];
        }
    }
}
