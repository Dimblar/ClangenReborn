using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using YAXLib;
using YAXLib.Attributes;

namespace ClangenReborn;

public enum GamemodeType : byte
{
    Classic, Expanded, Cruel
}

public enum Season : byte
{
    Summer, Autumn, Winter, Spring
}


public class WorldConfig
{
    public static readonly WorldConfig Default = new();

    public class CatGenerationDef
    {
        public float BaseHeterochromiaChance { get; set; } = 1.0f / 125;

        public double FullwhiteChance { get; set; } = 1.0 / 20;

        public double RandomPointChance { get; set; } = 1.0 / (1 << 5);

        public double MaleTortieChance { get; set; } = 1.0 / (1 << 13);

        public double FemaleTortieChance { get; set; } = 1.0 / (1 << 4);

        public double WildcardTortieChance { get; set; } = 1.0 / (1 << 9);

        public double VitiligoChance { get; set; } = 1.0 / 8;
    }

    public CatGenerationDef CatGeneration { get; set; } = new();
}


public class World
{
    private static readonly YAXSerializer<World> __Serializer = new();


    public static World? ThisWorld { get; internal set; }


    public ulong WorldSeed { get; init; }



    public uint Moon { get; private set; }


    public GamemodeType GameMode { get; init; }


    public Season Season { get; private set; }


    public Version LastPLayedVersion { get; init; } = Version.Now();


    public WorldConfig WorldConfig { get; set; } = WorldConfig.Default;


    public int CatCount => this.Cats.Count;


    public Dictionary<ushort, Cat> Cats { get; set; } = [];

    /// <summary>
    /// The last used Id.
    /// </summary>
    internal ushort LastCatId = 0;


    internal ushort NextCatId() => ++this.LastCatId;

    public Cat? GetCat(ushort Id) => this.Cats.TryGetValue(Id, out Cat? Existing) ? Existing : null;


    [YAXDontSerialize]
    public string File { get; private set; }

    public World() { }
    public World(ulong WorldSeed, GamemodeType GameMode, Season Season, string File)
    {
        this.WorldSeed = WorldSeed;
        this.GameMode = GameMode;
        this.Season = Season;
        this.File = File;
    }


    public void Save()
    {
        __Serializer.SerializeToFile(this, $"{Content.SaveDataPath}//{this.File}");
    }


    public static World? Load(string File)
    {
        World? World = __Serializer.DeserializeFromFile(File);
        if (World is not null)
            World.File = File;

        return World;
    }

    public static void Set(World World)
    {
        World.ThisWorld = World;
    }
}