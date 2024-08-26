using ClangenReborn.Defs;
using ClangenReborn.Graphics;
using ClangenReborn.Scenes;
using FontStashSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.Content.Pipeline.Builder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using YAXLib;

namespace ClangenReborn;


public class ContentPack
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Description;
    public readonly string? Thumbnail;
    public readonly ReadOnlyCollection<string> Authors;
    public readonly ReadOnlyCollection<Version> SupportedVersions;
    public required DirectoryInfo RootFolder { get; init; }


    internal ContentPack(ContentPackDef Payload)
    {
        if (Payload.Id is null)
            throw new Exception("CONTENT PACK ID IS NULL");

        this.Id = Payload.Id.ToLower();
        this.Name = Payload.Name ?? "???";
        this.Description = Payload.Description ?? "";
        this.Thumbnail = Payload.Thumbnail;
        this.Authors = new(Payload.Authors ?? []);
        this.SupportedVersions = new(Array.ConvertAll(Payload.SupportedVersions ?? [], X => new Version(X)));
    }
}


public static partial class Content
{
    private static readonly Dictionary<Type, YAXSerializer> __SerializerCache = [];
    private static readonly Dictionary<string, Texture2D> __TextureCache = [];
    private static readonly Dictionary<string, Effect> __EffectCache = [];
    private static readonly Dictionary<string, Font> __FontCache = [];

    // TODO Implement methods to add registers at runtime and order specific registers i.e loading a files specifically before a directory and then vice versa
    private static readonly List<(string File, Action<string>[])> FileRegisters =
    [
        ( "/Locale/Languages.xml", [Text.LoadLanguages] ),
        ( "/Defs/Cats/Pronouns.xml", [LoadPronouns] )
    ];

    private static readonly List<(string File, Action<List<string>>[], string? SearchPattern)> DirectoryRegisters =
    [
        ( "/Defs/Themes",     [LoadTheme], "*.xml" ),
        ( "/Defs/Cats/Tints", [LoadTints], null ),
        ( "/Defs/Cats/Poses", [LoadPoses], null ),
        ( "/Defs/Cats/Pelts", [LoadPelts], null ),
    ];

    /// <summary>
    /// The directory of this games execution.
    /// </summary>
    public static readonly DirectoryInfo BaseDirectory;

    /// <summary>
    /// Readonly collection of all paths used by the base game.
    /// </summary>
    public static readonly ReadOnlyCollection<FileInfo> BasePaths;


    public static readonly DirectoryInfo GameDataPath;


    public static readonly DirectoryInfo SaveDataPath;


    private static GraphicsDeviceManager GraphicsDeviceManager;


    private static SpriteBatchEx Batch;


    private static PipelineManager PipelineManager;


    private static Color ClearColor = Color.CornflowerBlue;


    //public static string? AssetHash { get; private set; }


    public static ConfigDef Config { get; private set; }


    public static ReadOnlyCollection<ContentPack>? ActivePacks { get; private set; }


    private static readonly Dictionary<string, ThemeDef> ThemeCache = [];

    public static ThemeDef CurrentTheme { get; private set; }

    public static double Delta { get; private set; }

    static Content() // TODO Allow save data to be customised 
    {
        BaseDirectory = new(".");
        BasePaths = new(BaseDirectory.EnumerateFiles("*.*", SearchOption.AllDirectories).ToArray());
        GameDataPath = new($"{BaseDirectory.FullName}\\GameData");
        SaveDataPath = new($"{BaseDirectory.FullName}\\SaveData");
    }

    internal static void PrepareContext(GraphicsDeviceManager GraphicsDeviceManager)
    {
        StreamReader Reader;
        StreamWriter Writer;

        // Preprocess certain types
        Assembly[] ExecutionAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        // ^FIX - Doesnt allow for external assemblys, need to finalise mod format first and fully look at any possible security risks
        Assembly Assembly;
        Type[] Types;
        Type Type;

        Log.Debug("Creating game context . . .");

        for (int I = 0; I < ExecutionAssemblies.Length; I++)
        {
            Types = (Assembly = ExecutionAssemblies[I]).GetTypes();
            Log.Trace($"Scanning Assembly: {Assembly.GetName().Name} . . .");

            for (int K = 0; K < Types.Length; K++)
            {
                Type = Types[K];

                if (Type.IsInterface || Type.IsAbstract) // No conceivable scenario where these are needed here.
                    continue;

                if (Type.GetInterfaces().Any(X => X.IsGenericType && X.GetGenericTypeDefinition() == typeof(ThrowawayDef<>)))
                {
                    __SerializerCache[Type] = new(Type);
                }
                else if (typeof(Def).IsAssignableFrom(Type))
                {
                    __SerializerCache[Type] = new(Type);
                }
            }
        }

        // GlobalConfig Loading
        string Path = $"{SaveDataPath.FullName}\\Config\\Global.xml";
        ConfigDef? Config = null;


        if (File.Exists(Path))
        {
            using (Reader = new(Path))
                Config = DeserializeDef<ConfigDef>(Reader);
        }

        if (Config is null) 
        {
            Log.Warning("Config unloadable, creating new");
            Config = new();
        }

        using (Writer = new(Path))
            SerializeDef(Writer, Config);

        Content.Config = Config;

        // Content Metadata Loading
        DirectoryInfo[] ModDirectories = GameDataPath.EnumerateDirectories().ToArray();

        List<ContentPack> Packs = [];
        ContentPackDef? Def;
        for (int I = 0; I < ModDirectories.Length; I++)
        {
            Path = $"{ModDirectories[I].FullName}\\Package.xml";

            if (!File.Exists(Path))
            {
                Log.Warning($"Found Mod at \"{ModDirectories[I].FullName}\" with no Package.xml, mod has not been loaded.");
                continue;
            }

            using (Reader = new(Path))
            {
                if ((Def = DeserializeDef<ContentPackDef>(Reader)) is not null)
                {
                    Packs.Add(new(Def) { RootFolder = ModDirectories[I] });
                }
            }

            Log.Trace($"Found Mod at \"{ModDirectories[I].FullName}\"");
        }

        PipelineManager = new("", "", "")
        {
            Profile = GraphicsDeviceManager.GraphicsDevice.GraphicsProfile,
        };

        ActivePacks = Packs.AsReadOnly();

        Content.GraphicsDeviceManager = GraphicsDeviceManager;
        Batch = new SpriteBatchEx(GraphicsDeviceManager.GraphicsDevice);
    }

    internal static void CreateContext()
    {
        // Start hashing task
        //Task<byte[]?> HashTask;
        //SHA256? Hasher = null;
        //try
        //{
        //    Console.WriteLine("Hashing . . .");
        //    Hasher = SHA256.Create();
        //    HashTask = Task.Run(() => Hasher.ComputeHash(BaseDirectory, BasePaths));
        //}
        //catch
        //{
        //    Hasher?.Dispose();
        //    throw; // TEMPORARY DEBUG
        //}

        if (ActivePacks is null || ActivePacks.Count < 0)
            return;

        // Load Content
        string FilePath;
        
        foreach (ContentPack Pack in ActivePacks)
        {
            LoadingScreen.SetText($"Loading pack \"{Pack.Name}\" . . .");
            Log.Debug($"Integrating pack \"{Pack.Name}\" from \"{Pack.RootFolder}\" . . .");

            foreach ((string File, Action<string>[] Registers) in FileRegisters)
            {
                if (!Path.Exists(FilePath = $"{Pack.RootFolder}/{File}"))
                    continue;

                Array.ForEach(Registers, R => R(FilePath));
            }

            foreach ((string File, Action<List<string>>[]? Registers, string? SearchPattern) in DirectoryRegisters)
            {
                if (!Path.Exists(FilePath = $"{Pack.RootFolder}/{File}"))
                    continue;

                if (SearchPattern is null)
                    Array.ForEach(Registers, R => R(Directory.EnumerateFiles(FilePath).ToList()));
                else
                    Array.ForEach(Registers, R => R(Directory.EnumerateFiles(FilePath, SearchPattern).ToList()));
            }
        }

        Log.Debug("Building TextParseTree . . .");
        Text.BuildParseTree();

        // Cleanup
        //try
        //{
        //LoadingScreen.SetText("Hashing . . .");
        //HashTask.Wait();
        //byte[]? HashResult = HashTask.Result;

        //if (HashResult is null)
        //{
        //    Console.WriteLine("Hashing failed.");
        //}
        //else
        //{
        //    AssetHash = new (Array.ConvertAll(HashResult, K => (char)K));
        //}
        //}
        //finally
        //{
        //    HashTask.Dispose();
        //    Hasher.Dispose();
        //    Console.WriteLine("Hashing completed!");
        //}
    }

    internal static void FinaliseContext()
    {
        Log.Debug("Finalising loadup . . .");
        // Remove un-needed serializers
        foreach (Type Key in __SerializerCache.Keys)
        {
            if (Key.GetInterfaces().Any(X => X.IsGenericType && X.GetGenericTypeDefinition() == typeof(ThrowawayDef<>)))
                __SerializerCache.Remove(Key);
        }

        Text.SetLanguage(Config.Language);
    }

    internal static TDef? DeserializeDef<TDef>(TextReader Reader) where TDef : notnull
    {
        if (!__SerializerCache.TryGetValue(typeof(TDef), out var Serializer))
            __SerializerCache[typeof(TDef)] = Serializer = new(typeof(TDef));

        return (TDef?)Serializer.Deserialize(Reader);
    }

    internal static void SerializeDef<TDef>(TextWriter Writer, TDef Def)
    {
        if (!__SerializerCache.TryGetValue(typeof(TDef), out var Serializer))
            __SerializerCache[typeof(TDef)] = Serializer = new(typeof(TDef));

        Serializer.Serialize(Def, Writer);
    }

    private static void LoadTheme(List<string> Files)
    {
        foreach (string File in Files)
            Array.ForEach(ThemeDef.From(File), T => ThemeCache[T.Id] = T);
    }

    internal static void Update(GameTime GameTime)
    {
        Delta = GameTime.ElapsedGameTime.TotalSeconds;
    }

    internal static void Draw()
    {
        GraphicsDeviceManager.GraphicsDevice.Clear(ClearColor);
        CurrentScene?.Draw(Batch);
    }

    public static void DrawTexture(string Id, Rectangle Destination, Color Color)
        => Batch.Draw(GetTexture(Id), Destination, Color);

    public static void DrawTexture(string Id, Rectangle Destination, Rectangle? Source, Color Color)
        => Batch.Draw(GetTexture(Id), Destination, Source, Color);

    public static void DrawFont(string Id, string Text, Rectangle Bounds, Color? Color = null, int? Size = null) // FIX
    {
        var Face = GetFont(Id).Face;
        Vector2 ActualSize = Face.MeasureString(Text);
        Vector2 Scale = Vector2.One;

        if (Size is not null) 
        { 
            if (Size != Face.FontSize)
            {
                Scale.X = Scale.Y = (float)Size / Face.FontSize;
            }
        }

        Face.DrawText(
            Batch, Text, Bounds.Center.ToVector2(), Color ?? Microsoft.Xna.Framework.Color.White, 0, ActualSize / 2, Scale, 0, 0, 0, TextStyle.None, FontSystemEffect.None, 0
        );
    }    

    public static void DrawFont(string Id, string Text, Vector2 Centre, Color? Color = null)
    {
        var Face = GetFont(Id).Face;
        Vector2 ActualSize = Face.MeasureString(Text);

        Face.DrawText(
            Batch, Text, new Vector2((int)(Centre.X - (ActualSize.X / 2)), (int)(Centre.Y - (ActualSize.Y / 2))), Color ?? Microsoft.Xna.Framework.Color.White
        );
    }

    public static void Fill(Color Fill)
    {
        GraphicsDeviceManager.GraphicsDevice.Clear(Fill);
    }

    public static Texture2D GetTexture(string Id)
    {
        Id = ResolvePath($"Resources\\{Id}");
        if (__TextureCache.TryGetValue(Id, out Texture2D? Texture))
            return Texture;

        using FileStream FileStream = new(Id, FileMode.Open, FileAccess.Read, FileShare.Read);
        return __TextureCache[Id] = Texture2D.FromStream(GraphicsDeviceManager.GraphicsDevice, FileStream);
    }

    public static Effect GetShader(string Id)
    {
        Id = ResolvePath($"Resources\\Shaders\\{Id}.mgfxo");
        if (__EffectCache.TryGetValue(Id, out Effect? Effect))
            return Effect.Clone();

        return (__EffectCache[Id] = new Effect(GraphicsDeviceManager.GraphicsDevice, File.ReadAllBytes(Id))).Clone();
    }

    public static Font GetFont(string Id, uint FontSize = 30)
    {
        Id = ResolvePath($"Resources\\Fonts\\{Id}.ttf");
        if (__FontCache.TryGetValue(Id, out Font? Font))
            return Font;
        

        return __FontCache[Id] = new Font(Id, FontSize);
    }

    public static bool SetTheme(string Id)
    {
        if (ThemeCache.TryGetValue(Id, out ThemeDef? Value))
        {
            CurrentTheme = Value;
            return true;
        }

        return false;
    }

    public static string ResolvePath(string Path)
    {
        string? NewPath = null;

        if (Path.StartsWith('.'))
            NewPath = $"{GameDataPath}\\{Path}";

        for (int Index = 0; Index < Path.Length; Index++)
        {
            switch (Path[Index])
            {
                case ':':
                    GameInfo.TextInfo.ToLower(Path[..Index]);

                    if (ActivePacks is null)
                        break;
                    
                    foreach (ContentPack Pack in ActivePacks)
                    {
                        if (Pack.RootFolder is not null)
                        {
                            NewPath = Path.Remove(0, Index).Insert(0, Pack.RootFolder.FullName);
                            break;
                        }
                    }
                    
                    continue;

                default:
                    continue;
            }
        }

        if (NewPath is null && ActivePacks is not null)
        {
            foreach (ContentPack Pack in ActivePacks)
                if (Pack.RootFolder is not null && File.Exists(NewPath = $"{Pack.RootFolder.FullName}\\{Path}"))
                    break;
        }

        return NewPath ?? Path;
    }
}