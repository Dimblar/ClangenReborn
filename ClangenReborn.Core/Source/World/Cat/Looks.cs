using ClangenReborn.Defs;
using ClangenReborn.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static ClangenReborn.Cat;
using static ClangenReborn.Content;
using static ClangenReborn.World;

namespace ClangenReborn;



public static partial class Content
{
    internal static void LoadTints(List<string> Files) // TODO refactor if possible, it looks quite the mess right now
    {
        Dictionary<string, Color> Colors = [];
        Dictionary<string, Color> ColorsWhite = [];
        Dictionary<string, List<string>> ColorGroups = [];
        Dictionary<string, List<string>> ColorGroupsWhite = [];
        Dictionary<string, List<string>> PossibleTints = [];
        Dictionary<string, List<string>> PossibleTintsWhite = [];

        foreach (string FilePath in Files)
        {
            TintConfigDef? TintDef;
            using (StreamReader StreamReader = new(FilePath, System.Text.Encoding.Default, true))
                TintDef = DeserializeDef<TintConfigDef>(StreamReader);

            if (TintDef is null)
                continue;

            if (TintDef.Tint is not null)
            {
                foreach (var (K, V) in TintDef.Tint.Colors ?? [])
                    Colors[K] = Utility.ColorFromString(V);

                foreach (var Group in TintDef.Tint.Groups ?? [])
                {
                    foreach (var Color in Group.AllowedColors ?? [])
                    {
                        if (!ColorGroups.TryGetValue(Color, out var ExistingGroups))
                            ColorGroups[Color] = ExistingGroups = [];
                        ExistingGroups.Add(Group.Id);
                    }

                    if (PossibleTints.TryGetValue(Group.Id, out var Existing))
                        Existing.AddRange(Group.PossibleTints);
                    PossibleTints[Group.Id] = Existing ?? Group.PossibleTints;
                }
            }

            if (TintDef.WhiteTint is not null)
            {
                foreach (var (K, V) in TintDef.WhiteTint.Colors ?? [])
                    ColorsWhite[K] = Utility.ColorFromString(V);

                foreach (var Group in TintDef.WhiteTint.Groups ?? [])
                {
                    foreach (var Color in Group.AllowedColors ?? [])
                    {
                        if (!ColorGroupsWhite.TryGetValue(Color, out var ExistingGroups))
                            ColorGroupsWhite[Color] = ExistingGroups = [];
                        ExistingGroups.Add(Group.Id);
                    }

                    if (PossibleTintsWhite.TryGetValue(Group.Id, out var Existing))
                        Existing.AddRange(Group.PossibleTints);
                    PossibleTintsWhite[Group.Id] = Existing ?? Group.PossibleTints;
                }
            }
        }

        Looks.Tints.Colors = Colors;
        Looks.Tints.ColourGroups = ColorGroups.ToDictionary(I => I.Key, O => (IReadOnlyList<string>)O.Value);
        Looks.Tints.PossibleTints = PossibleTints.ToDictionary(I => I.Key, O => (IReadOnlyList<string>)O.Value);
        Looks.WhiteTints.Colors = ColorsWhite;
        Looks.WhiteTints.ColourGroups = ColorGroupsWhite.ToDictionary(I => I.Key, O => (IReadOnlyList<string>)O.Value);
        Looks.WhiteTints.PossibleTints = PossibleTintsWhite.ToDictionary(I => I.Key, O => (IReadOnlyList<string>)O.Value);
    }

    internal static void LoadPoses(List<string> Files)
    {
        Dictionary<string, PoseSet> PoseSets = [];

        foreach (string FilePath in Files)
        {
            PeltPoseDefs? PoseDef;
            using (StreamReader StreamReader = new(FilePath, System.Text.Encoding.Default, true))
                PoseDef = DeserializeDef<PeltPoseDefs>(StreamReader);

            if (PoseDef is null) 
                continue;

            foreach (var Pose in PoseDef.Poses)
                PoseSets[Pose.Id] = new (Pose);
        }

        Looks.Poses = PoseSets;
    }

    internal static void LoadPelts(List<string> Files)
    {
        if (Looks.Poses is null)
            throw new Exception();

        Dictionary<string, PeltType> PeltTypes = [];

        foreach (string FilePath in Files)
        {
            PeltDefs? PeltDefs;
            using (StreamReader StreamReader = new(FilePath, System.Text.Encoding.Default, true))
                PeltDefs = DeserializeDef<PeltDefs>(StreamReader);

            if (PeltDefs is null)
                continue;

            foreach (var Def in PeltDefs.Defs)
            {
                if (!File.Exists(ResolvePath($"Resources\\{Def.Texture}")))
                {
                    Log.Warning($"PeltDef.Texture of \"{Def.Id}\" in \"{FilePath}\" not found \"{Def.Texture}\"");
                    continue;
                }

                if (!Looks.Poses.TryGetValue(Def.Regions.PoseSet, out PoseSet? Poses))
                    throw new Exception();

                Rectangle TexSize = GetTexture(Def.Texture).Bounds;
                int Width = TexSize.Width / (Poses.Columns * Def.Regions.Width);
                int Height = TexSize.Height / (Poses.Rows * Def.Regions.Height);

                int PoseWidth = Width * Poses.Columns, PoseHeight = Height * Poses.Rows;

                Dictionary<string, Rectangle> PoseColorMap = [];
                for (int I = 0; I < Def.Regions.RegionItems.Count; I++)
                {
                    var Region = Def.Regions.RegionItems[I];
                    if (Region.Color is null)
                        continue;

                    PoseColorMap[Region.Color] = new (PoseWidth * (I % Def.Regions.Width), PoseHeight * (I / Def.Regions.Width), Width * Poses.Columns, Height * Poses.Rows);
                    
                }

                PeltTypes[Def.Id] = new PeltType()
                {
                    TexturePath = Def.Texture,
                    Regions = PoseColorMap.AsReadOnly(),
                    PoseSet = Poses
                };
            }
        }

        Looks.PeltTypes = PeltTypes;
    }
}

public class PoseSet
{
    private readonly string[] PoseIds;
    private readonly string?[] PoseDefaults;
    public readonly string Id;
    public readonly int Rows;
    public readonly int Columns;

    internal PoseSet(PeltPoseDefs.PoseDef Def)
    {
        this.Id = Def.Id;
        this.Rows = Def.Rows;
        this.Columns = Def.Columns;
        this.PoseIds = new string[Def.Poses.Count];
        this.PoseDefaults = new string?[Def.Poses.Count];

        int Index = 0;
        foreach (var Pose in Def.Poses)
            (this.PoseIds[Index], this.PoseDefaults[Index++]) = (Pose.Id, Pose.Fallback);
    }

    public string? Get(int Index)
    {
        return this.PoseIds[Index];
    }

    public string? GetDefault(string Id)
    {
        int Index = IndexOf(Id);
        return -1 < Index ? this.PoseDefaults[Index] : null;
    }

    public void ResolveSize(ref Rectangle Source, int Index)
    {
        Source.X += Index % this.Columns * (Source.Width  /= this.Columns);
        Source.Y += Index / this.Columns * (Source.Height /= this.Rows   );
    }

    public int IndexOf(string Id) => Array.IndexOf(this.PoseIds, Id);
}

public class PeltType
{
    public required string TexturePath;
    public required PoseSet PoseSet;
    public required IReadOnlyDictionary<string, Rectangle> Regions;
}

public static class LooksGroups // TODO abstract string values to another value type + abstract into file format
{
    public static readonly IReadOnlyList<string> EyeColours = [
        "Yellow", "Amber", "Hazel", "Palegreen", "Green", "Blue", "Darkblue", "Grey", "Cyan", "Emerald",
        "Paleblue", "Paleyellow", "Gold", "Heatherblue", "Copper", "Sage", "Cobalt", "Sunlitice", "Greenyellow", "Bronze",
        "Silver"
    ];

    public static readonly IReadOnlyList<string> YellowEyes = [
        "Yellow", "Amber", "Paleyellow", "Gold", "Copper", "Greenyellow", "Bronze", "Silver"
    ];

    public static readonly IReadOnlyList<string> BlueEyes = [
        "Blue", "Darkblue", "Cyan", "Paleblue", "Heatherblue", "Cobalt", "Sunlitice", "Grey"
    ];

    public static readonly IReadOnlyList<string> GreenEyes = [
        "Palegreen", "Green", "Emerald", "Sage", "Hazel"
    ];

    public static readonly IReadOnlyList<string> Vitiligo = [
        "Vitiligo", "Vitiligotwo", "Moon", "Phantom", "Karpati", "Powder", "Bleached", "Smokey"
    ];

    public static readonly IReadOnlyList<string> TortieBases = [
        "Single", "Tabby", "Bengal", "Marbled", "Ticked", "Smoke", "Rosette", "Speckled", "Mackerel", "Classic", "Sokoke", "Agouti", "Singlestripe", "Masked"
    ];

    public static readonly IReadOnlyList<string> TortiePatterns = [
        "One", "Two", "Three", "Four", "Redtail", "Delilah", "Minimalone", "Minimaltwo", "Minimalthree", "Minimalfour", "Half", "Oreo", "Swoop", "Mottled", "Sidemask", "Eyedot", "Bandana", "Pacman", "Streamstrike",
        "Oriole", "Chimera", "Daub", "Ember", "Blanket", "Robin", "Brindle", "Paige", "Rosetail", "Safi", "Smudged", "Dapplenight", "Streak", "Mask", "Chest", "Armtail", "Smoke", "Grumpyface", "Brie", "Beloved",
        "Body", "Shiloh", "Freckled", "Heartbeat"
    ];

    public static readonly IReadOnlyList<string> PeltColours = [
        "White", "Palegrey", "Silver", "Grey", "Darkgrey", "Ghost", "Black", "Cream", "Paleginger", "Golden", 
        "Ginger", "Darkginger", "Sienna", "Lightbrown", "Lilac", "Brown", "Goldbrown", "Darkbrown", "Chocolate"
    ];

    public static readonly IReadOnlyList<string> Tabbies = ["Tabby", "Ticked", "Mackerel", "Classic", "Sokoke", "Agouti"];
    public static readonly IReadOnlyList<string> Spotted = ["Speckled", "Rosette"];
    public static readonly IReadOnlyList<string> Plain = ["Single", "Singlestripe", "TwoColour", "Smoke"];
    public static readonly IReadOnlyList<string> Exotic = ["Bengal", "Marbled", "Masked"];
    public static readonly IReadOnlyList<string> Torties = ["Tortie", "Calico"];
    public static readonly IReadOnlyList<IReadOnlyList<string>> PeltCategories = [Tabbies, Spotted, Plain, Exotic, Torties];

    public static readonly IReadOnlyList<string> GingerColours = ["Cream", "Paleginger", "Golden", "Ginger", "Darkginger", "Sienna"];
    public static readonly IReadOnlyList<string> BlackColours = ["Grey", "Darkgrey", "Ghost", "Black"];
    public static readonly IReadOnlyList<string> WhiteColours = ["White", "Palegrey", "Silver"];
    public static readonly IReadOnlyList<string> BrownColours = ["Lightbrown", "Lilac", "Brown", "Goldbrown", "Darkbrown", "Chocolate"];
    public static readonly IReadOnlyList<IReadOnlyList<string>> ColourCategories = [GingerColours, BlackColours, WhiteColours, BrownColours];

    public static readonly IReadOnlyList<string> PointMarkings = [
        "Colourpoint", "Ragdoll", "Sepiapoint", "Minkpoint", "Sealpoint"
    ];

    public static readonly IReadOnlyList<string> LittleWhite = [
        "Little", "LightTuxedo", "Buzzardfang", "Tip", "Blaze", "Bib", "Vee", "Paws", "Belly", "Tailtip", "Toes", "Brokenblaze", "Liltwo", "Scourge", "Toestail", "Ravenpaw",
        "Honey", "Luna", "Extra", "Mustache", "Reverseheart", "Sparkle", "Rightear", "Leftear", "Estrella", "ReverseEye", "Backspot", "Eyebags", "Locket", "Blazemask", "Tears"
    ];

    public static readonly IReadOnlyList<string> MiddleWhite = [
        "Tuxedo", "Fancy", "Unders", "Damien", "Skunk", "Mitaine", "Squeaks", "Star", "Wings", "Diva", "Savannah", "Fadespots", "Beard", "Dapplepaw", "Topcover", "Woodpecker",
        "Miss", "Bowtie", "Vest", "Fadebelly", "Digit", "Fctwo", "Fcone", "Mia", "Rosina", "Princess", "Dougie"
    ];

    public static readonly IReadOnlyList<string> HighWhite = [
        "Any", "Anytwo", "Broken", "Freckles", "Ringtail", "HalfFace", "Pantstwo", "Goatee", "Prince", "Farofa", "Mister", "Pants", "Reversepants", "Halfwhite", "Appaloosa", "Piebald",
        "Curved", "Glass", "Maskmantle", "Mao", "Painted", "Shibainu", "Owl", "Bub", "Sparrow", "Trixie", "Sammy", "Front", "Blossomstep", "Bullseye", "Finn", "Scar", "Buster",
        "Hawkblaze", "Cake"
    ];

    public static readonly IReadOnlyList<string> MostlyWhite = [
        "Van", "OneEar", "Lightsong", "Tail", "Heart", "Moorish", "Apron", "Capsaddle", "Chestspeck", "Blackstar", "Petal", "HeartTwo", "Pebbleshine", "Boots", "Cow", "Cowtwo", "Lovebug",
        "Shootingstar", "Eyespot", "Pebble", "Tailtwo", "Buddy", "Kropka"
    ];

    public static readonly IReadOnlyList<IReadOnlyList<string>> AllWhites = [
        LittleWhite, MiddleWhite, HighWhite, MostlyWhite, ["Fullwhite"]
    ];

    public static readonly IRandomTable<string> SkinSprites = XorTable<string>.Create(
        "Black", "Pink", "Darkbrown", "Brown", "Lightbrown", "Dark", "Darkgrey", "Grey", "Darksalmon",
        "Salmon", "Peach", "Darkmarbled", "Marbled", "Lightmarbled", "Darkblue", "Blue", "Lightblue", "Red"
    );

    public static readonly IRandomTable<string> Scars = XorTable<string>.Create(
        "One", "Two", "Three", "Tailscar", "Snout", "Cheek", "Side", "Throat", "Tailbase", "Belly",
        "Legbite", "Neckbite", "Face", "Manleg", "Brightheart", "Mantail", "Bridge", "Rightblind", "Leftblind", "Bothblind",
        "Beakcheek", "Beaklower", "Catbite", "Ratbite", "Quillchunk", "Quillscratch"
    );

    public static readonly IRandomTable<string> MissingScars = XorTable<string>.Create(
        "Leftear", "Rightear", "Notail", "Halftail", "Nopaw", "Noleftear", "Norightear", "Noear"
    );

    public static readonly IRandomTable<string> SpecialScars = XorTable<string>.Create(
        "Snake", "Toetrap", "Burnpaws", "Burntail", "Burnbelly", "Burnrump", "Frostface", "FrostTail", "Frostmitt"
    );

    public static readonly IRandomTable<string> PlantAccessories = XorTable<string>.Create(
        "Mapleleaf", "Holly", "Blueberries", "Forgetmenots", "Ryestalk", "Laurel", "Bluebells", "Nettle", "Poppy", "Lavender",
        "Herbs", "Petals", "Dryherbs", "Oakleaves", "Catmint", "Mapleseed", "Juniper"
    );

    public static readonly IRandomTable<string> WildAccessories = XorTable<string>.Create(
        "Redfeathers", "Bluefeathers", "Jayfeathers", "Mothwings", "Cicadawings"
    );

    public static readonly IRandomTable<string> Collars = XorTable<string>.Create(
        "Crimson", "Blue", "Yellow", "Cyan", "Red", "Lime", "Green", "Rainbow", "Black", "Spikes", "White", "Pink", "Purple", "Multi", "Indigo"
    );
}


public enum PeltLength
{
    Short, Medium, Long
}


public static class SpriteType
{
    public const byte Kit0 = 0;
    public const byte Kit1 = 1;
    public const byte Kit2 = 2;
    public const byte Adolescent0 = 3;
    public const byte Adolescent1 = 4;
    public const byte Adolescent2 = 5;
    public const byte YoungShort = 6;
    public const byte AdultShort = 7;
    public const byte SeniorShort = 8;
    public const byte YoungLong = 9;
    public const byte AdultLong = 10;
    public const byte SeniorLong = 11;
    public const byte Senior0 = 12;
    public const byte Senior1 = 13;
    public const byte Senior2 = 14;
    public const byte ParalyzedShort = 15;
    public const byte ParalyzedLong = 16;
    public const byte ParalyzedYoung = 17;
    public const byte SickAdult = 18;
    public const byte SickYoung = 19;
    public const byte Newborn = 20;
}

public class Looks
{
    public static class Tints
    {
        public static IReadOnlyDictionary<string, Color>? Colors { get; internal set; }
        public static IReadOnlyDictionary<string, IReadOnlyList<string>>? ColourGroups { get; internal set; }
        public static IReadOnlyDictionary<string, IReadOnlyList<string>>? PossibleTints { get; internal set; }
    }

    public static class WhiteTints
    {
        public static IReadOnlyDictionary<string, Color>? Colors { get; internal set; }
        public static IReadOnlyDictionary<string, IReadOnlyList<string>>? ColourGroups { get; internal set; }
        public static IReadOnlyDictionary<string, IReadOnlyList<string>>? PossibleTints { get; internal set; }
    }

    public static IReadOnlyDictionary<string, PoseSet>? Poses { get; internal set; }

    public static IReadOnlyDictionary<string, PeltType>? PeltTypes { get; internal set; }

    public PeltLength Length;

    public string Base;
    public string Colour;
    public string? Pattern;

    public string EyeColour;
    public string? EyeColour2;

    public string Skin;
    public string? Accessory;
    public List<string>? Scars;

    public string? TortieBase;
    public string? TortieColour;
    public string? TortiePattern;

    public string? Vitiligo;
    public string? WhitePatches;
    public string? Points;

    public string? Tint;
    public string? WhiteTint;

    public byte SpriteNewborn = SpriteType.Newborn;
    public byte SpriteKitten;
    public byte SpriteAdolescent;
    public byte SpriteYoungAdult;
    public byte SpriteYoungSick = SpriteType.SickYoung;
    public byte SpriteYoungParalyzed = SpriteType.ParalyzedLong;
    public byte SpriteAdult;
    public byte SpriteAdultSick = SpriteType.SickAdult;
    public byte SpriteAdultParalyzed;
    public byte SpriteSeniorAdult;
    public byte SpriteSenior;

    public byte Opacity = 255;
    public bool Reversed;

    public bool HasWhite => this.WhitePatches is null && this.Points is null;


    private static readonly double[] RPC_PeltWeights = [35, 20, 30, 15, 0];
    private static readonly double[] RPC_WhiteWeights = [10, 10, 10, 10, 1];
    private static readonly double[] RPC_WhiteWeightsTortie = [2, 1, 0, 0, 0];
    private static readonly double[] RPC_WhiteWeightsCalico = [0, 0, 20, 15, 1];
    private static readonly string[] LC_TortiePatterns = ["Tabby", "Mackerel", "Classic", "Single", "Smoke", "Agouti", "Ticked"];
    private static readonly IReadOnlyCollection<string> LC_TortiePatternPrerequisites = ["Singlestripe", "Smoke", "Single"];

    /// <summary>
    /// Initialise this <see cref="Looks"/> object puesdo-randomly based on a given <see cref="Cat">
    /// </summary>
    public Looks([DisallowNull] Cat Source, RandomGenerator? GivenRandom = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Initialise this <see cref="Looks"/> object puesdo-randomly based on a given <see cref="Cat"> and their parents.
    /// </summary>
    public Looks(Cat Source, Cat FirstParent, Cat SecondParent, RandomGenerator? GivenRandom = null)
    {
        throw new NotImplementedException();
    }

    public void Fix()
    {
        if (this.Scars?.Contains("Notail") ?? false)
            this.Scars.Remove("Halftail");
    }

    public byte GetRegionIndexByAge(AgeStage Age) => Age switch
    {
        AgeStage.Newborn => this.SpriteNewborn,
        AgeStage.Kitten => this.SpriteKitten,
        AgeStage.Adolescent => this.SpriteAdolescent,
        AgeStage.YoungAdult => this.SpriteYoungAdult,
        AgeStage.Adult => this.SpriteAdult,
        AgeStage.SeniorAdult => this.SpriteSeniorAdult,
        AgeStage.Senior => this.SpriteSenior,
        _ => 0
    };
}



public partial class Cat
{
    public void Draw(SpriteBatchEx Batch, Rectangle Destination)
    {
        if (this.Looks.Base == "TwoColour")
            return;

        if (Looks.PeltTypes is null)
            return;

        var PeltData = Looks.PeltTypes[this.Looks.Base];
        var PoseData = PeltData.PoseSet;

        Rectangle Source = PeltData.Regions[this.Looks.Colour];
        PoseData.ResolveSize(ref Source, this.Looks.GetRegionIndexByAge(this.Age));

        DrawTexture(PeltData.TexturePath, Destination, Source, Color.White);
    }
}