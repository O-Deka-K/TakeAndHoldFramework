using TNHFramework.Utilities;
using Valve.Newtonsoft.Json;

namespace TNHFramework.ObjectTemplates
{
    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum ObjectCategory
    {
        Uncategorized,
        Firearm,
        Magazine,
        Clip,
        Cartridge,
        Attachment,
        SpeedLoader,
        Thrown,
        MeleeWeapon = 10,
        Explosive = 20,
        Powerup = 25,
        Target = 30,
        Tool = 40,
        Toy,
        Firework,
        Ornament,
        Loot = 50,
        VFX
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagEra
    {
        None,
        Colonial,
        WildWest,
        TurnOfTheCentury,
        WW1,
        WW2,
        PostWar,
        Modern,
        Futuristic,
        Medieval
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagSet
    {
        Real,
        GroundedFictional,
        SciFiFictional,
        Meme,
        MF,
        Holiday,
        TNH,
        NonCombat,
        Sulfur
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmSize
    {
        None,
        Pocket,
        Pistol,
        Compact,
        Carbine,
        FullSize,
        Bulky,
        Oversize
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmAction
    {
        None,
        BreakAction,
        BoltAction,
        Revolver,
        PumpAction,
        LeverAction,
        Automatic,
        RollingBlock,
        OpenBreach,
        Preloaded,
        SingleActionRevolver
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmFiringMode
    {
        None,
        SemiAuto,
        Burst,
        FullAuto,
        SingleFire
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmFeedOption
    {
        None,
        BreachLoad,
        InternalMag,
        BoxMag,
        StripperClip,
        EnblocClip
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmRoundPower
    {
        None,
        Tiny,
        Pistol,
        Shotgun,
        Intermediate,
        FullPower,
        AntiMaterial,
        Ordnance,
        Exotic,
        Fire
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmMount
    {
        None,
        Picatinny,
        Russian,
        Muzzle,
        Stock,
        Bespoke,
        MLokRail,
        RMR
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagFirearmCountryOfOrigin
    {
        None,
        Fictional,
        UnitedStatesOfAmerica = 10,
        MuricanRemnants,
        Canada,
        BritishEmpire = 20,
        UnitedKingdom,
        CommonwealthOfAustralia,
        KingdomOfFrance = 30,
        FrenchSecondRepublic,
        SecondFrenchEmpire,
        FrenchThirdRepublic,
        VichyFrance,
        FrenchFourthRepublic,
        FrenchRepublic,
        GermanEmpire = 40,
        WeimarRepublic,
        GermanReich,
        WestGermany,
        GermanDemocraticRepublic,
        FederalRepublicOfGermany,
        TsardomOfRussia = 50,
        RussianEmpire,
        UnionOfSovietSocialistRepublics,
        RussianFederation,
        KingdomOfBelgium = 60,
        KingdomOfItaly = 70,
        ItalianRepublic,
        SwedishEmpire = 90,
        UnitedKingdomsOfSwedenAndNorway,
        KingdomOfSweden,
        KingdomOfNorway = 100,
        KingdomOfFinland = 110,
        RepublicOfFinland,
        Czechoslovakia = 120,
        CzechRepublic,
        Ukraine = 130,
        SwissConfederation = 140,
        FirstSpanishRepublic = 150,
        SecondSpanishRepublic,
        SpanishState,
        KingdomOfSpain,
        AustrianEmpire = 160,
        AustroHungarianEmpire,
        RepublicOfAustria,
        FirstHungarianRepublic = 170,
        HungarianRepublic,
        KingdomOfHungary,
        HungarianPeoplesRepublic,
        RepublicOfCroatia = 190,
        RepublicOfKorea = 200,
        DemocraticRepublicOfVietnam = 210,
        StateOfIsrael = 220,
        FederativeRepublicOfBrazil = 230,
        EmpireOfJapan = 240,
        Japan,
        RepublicOfSouthAfrica = 250,
        GovernmentOfTheRepublicOfPolandInExile = 262,
        RepublicOfPoland,
        PeoplesRepublicOfChina = 270,
        FormerYugoslavicRepublicOfMacedonia = 280,
        Yugoslavia
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagAttachmentFeature
    {
        None,
        IronSight,
        Magnification,
        Reflex,
        Suppression,
        Stock,
        Laser,
        Illumination,
        Grip,
        Decoration,
        RecoilMitigation,
        BarrelExtension,
        Adapter,
        Bayonet,
        ProjectileWeapon,
        Bipod,
        NightVision
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagMeleeStyle
    {
        None,
        Tactical,
        Tool,
        Improvised,
        Medieval,
        Shield,
        PowerTool
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagMeleeHandedness
    {
        None,
        OneHanded,
        TwoHanded
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagPowerupType
    {
        None = -1,
        Health,
        QuadDamage,
        InfiniteAmmo,
        Invincibility,
        GhostMode,
        FarOutMeat,
        MuscleMeat,
        HomeTown,
        SnakeEye,
        Blort,
        Regen,
        Cyclops,
        WheredIGo,
        ChillOut
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagThrownType
    {
        None,
        ManualFuse,
        Pinned,
        Strange
    }

    [JsonConverter(typeof(TolerantEnumConverter))]
    public enum TagThrownDamageType
    {
        None,
        Kinetic,
        Explosive,
        Fire,
        Utility
    }
}
