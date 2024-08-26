using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using YAXLib.Attributes;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ClangenReborn;

public static class GameInfo
{
    public const byte MAJOR = 0;
    public const byte MINOR = 0;
    public const ushort PATCH = 1;

    /// <summary>
    /// The build of this version of the game
    /// </summary>
    public const uint BUILD = (MAJOR << 24) | (MINOR << 16) | PATCH;

    /// <summary>
    /// A nice little label that represents the current Major/Minor update!
    /// </summary>
    public const string VERSION_LABEL = "Primordial Soup";

    public static readonly string VERSION_MAJOR_MINOR_PATCH = $"{MAJOR}.{MINOR}.{PATCH}";

    public static CultureInfo CultureInfo { get; private set; }

    public static TextInfo TextInfo { get; private set; }

    static GameInfo()
    {
        CultureInfo = CultureInfo.InstalledUICulture;
        TextInfo = CultureInfo.TextInfo;
    }
}


public readonly struct Version
{
    [YAXDontSerialize]
    public readonly byte Major => (byte)(this.Build >> 24);

    [YAXDontSerialize]
    public readonly byte Minor => (byte)(this.Build >> 16);

    [YAXDontSerialize]
    public readonly ushort Revision => (ushort)(this.Build & 0xFFFF);

    /// <summary>
    /// An unsigned integer summarising the version; its useful in save data and error logging
    /// </summary>
    [YAXAttributeForClass]
    public uint Build { get; init; } = GameInfo.BUILD;

    public Version(byte Major, byte Minor, ushort Revision) => this.Build = (uint)((Major << 24) | (Minor << 16) | Revision);

    /// <summary>
    /// Initialise a Snapshot from a string representation i.e "0.1.12", "65548"
    /// </summary>
    public Version(string Version)
    {
        if (uint.TryParse(Version, out uint PotentialBuild))
        {
            this.Build = PotentialBuild;
            return;
        }

        string[] Sections = Version.Split('.');

        if (Sections.Length != 3)
            throw new ArgumentException("Version was malformed, must be in format of MAJOR.MINOR.REVISION, or a string representation of a build number", nameof(Version));

        this.Build = (uint.Parse(Sections[0]) << 24) | (uint.Parse(Sections[1]) << 16) | uint.Parse(Sections[2]);
    }

    /// <summary>
    /// Initialise a Snapshot from an unsigned build number i.e 65548 (0.1.12)
    /// </summary>
    public Version(uint Build) => this.Build = Build;

    public override string ToString() => $"{this.Major}.{this.Minor}.{this.Revision}";
    public override int GetHashCode() => (int)this.Build;

    public static Version Now() => new(GameInfo.BUILD);

    /// <summary>
    /// Check that this Snapshot information is valid.
    /// </summary>
    public bool IsValid() => this.Major switch
    {
        0 => this.Minor switch
        {
            0 => this.Revision switch { <= 0 => true, _ => false, },
            _ => false,
        },
        _ => false,
    };
}

/// <summary>
/// Helper class containing many specific but useful extensions.
/// </summary>
public static class UtilityExtensions
{
    /// <summary>
    /// Generate a hash from a given collection of files using their relative path to a given directory.
    /// </summary>
    public static async Task<byte[]?> ComputeHash(this HashAlgorithm Algorithm, DirectoryInfo RootDirectory, ReadOnlyCollection<FileInfo> Files)
    {
        using (CryptoStream CryptoStream = new(Stream.Null, Algorithm, CryptoStreamMode.Write))
        {
            void __HashFile(FileInfo File)
            {
                using FileStream FileStream = new(File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                FileStream.CopyTo(CryptoStream);
            }

            //var Task = Parallel.ForEach(Files, __HashFile);
            //while (!Task.IsCompleted) { }
            //CryptoStream.FlushFinalBlock();
            //return Algorithm.Hash;

            var HashBlock = new ActionBlock<FileInfo>(__HashFile);

            foreach (var FilePath in Files)
                await HashBlock.SendAsync(FilePath).ConfigureAwait(false);
            
            HashBlock.Complete();
            HashBlock.Completion.Wait();
            CryptoStream.FlushFinalBlock();
        }
        
        return Algorithm.Hash;
    }

    public static void Reset(this Stream Stream)
    {
        Stream.Seek(0, SeekOrigin.Begin);
    }

    /// <summary>
    /// Converts a stream to an array of bytes.
    /// </summary>
    public static byte[] ToByteArray(this Stream Stream)
    {
        if (Stream.CanSeek && Stream.Length == Stream.Position)
            Stream.Seek(0, SeekOrigin.Begin);

        using MemoryStream MemoryStream = new();
        Stream.CopyTo(MemoryStream);
        return MemoryStream.ToArray();
    }

    public static Span<T> AsSpan<T>(this ReadOnlySpan<T> ReadOnlySpan)
    {
        Span<T> NewSpan = GC.AllocateUninitializedArray<T>(ReadOnlySpan.Length);
        ReadOnlySpan.CopyTo(NewSpan);
        return NewSpan;
    }

    public static T[] ToArray<T>(this ReadOnlySpan<T> ReadOnlySpan)
    {
        Span<T> NewArray = GC.AllocateUninitializedArray<T>(ReadOnlySpan.Length);
        ReadOnlySpan.CopyTo(NewArray);
        return NewArray.ToArray();
    }

    /// <summary>
    /// Merge two dictionaries, with the new overwriting the old.
    /// </summary>
    public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> Source, IDictionary<TKey, TValue> Other)
    {
        foreach (TKey Key in Other.Keys)
            Source[Key] = Other[Key];
    }

    /// <summary>
    /// Gets the value associated with the specific key and casts it to <typeparamref name="TResult"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the object that implements <see cref="IDictionary{TKey, TValue}"/> contains an element with specified key; otherwise, <see langword="false"/></returns>
    public static bool TryGetValueCast<TKey, TValue, TResult>(this IDictionary<TKey, TValue> Source, TKey Key, out TResult? Value) 
        where TResult : notnull, TValue
    {
        bool Result;
        Value = (Result = Source.TryGetValue(Key, out TValue? __T)) ? (TResult?)__T : default;
        return Result;
    }

    /// <summary>
    /// Gets the value associated with the specific key and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the object that implements <see cref="IReadOnlyDictionary{TKey, TValue}"/> contains an element with specified key; otherwise, <see langword="false"/></returns>
    public static bool TryGetValueCast<K, V, T>(this IReadOnlyDictionary<K, V> Source, K Key, out T? Value) 
        where T : notnull, V
    {
        bool Result;
        Value = (Result = Source.TryGetValue(Key, out V? __T)) ? (T?)__T : default;
        return Result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Between<T>(this T Value, T Minimum, T Maximum) where T : notnull, IComparable<T> => Above(Below(Value, Maximum), Minimum);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Below<T>(this T Value, T Maximum) where T : notnull, IComparable<T> => Value.CompareTo(Maximum) > 0 ? Maximum : Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Above<T>(this T Value, T Minimum) where T : notnull, IComparable<T> => Value.CompareTo(Minimum) < 0 ? Minimum : Value;


    public static object? GetValue<T>(this MemberInfo Member, T Instance) => Member.MemberType switch
    {
        MemberTypes.Property => ((PropertyInfo)Member).GetValue(Instance, null),
        MemberTypes.Field => ((FieldInfo)Member).GetValue(Instance),
        _ => throw new Exception("Property must be of type FieldInfo or PropertyInfo")
    };


    /// <summary>
    /// Run multiple tasks at the same time, with exceptions thrown as needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task RunTasks(this IEnumerable<Task> Tasks)
    {
        Task AllTasks = Task.WhenAll(Tasks);

        try
        {
            await AllTasks; return;
        }
        catch (Exception Exc)
        {
            throw AllTasks.Exception ?? Exc;
        }
    }

    /// <summary>
    /// Run multiple tasks at the same time, with exceptions thrown as needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<T[]> RunTasks<T>(this IEnumerable<Task<T>> Tasks)
    {
        Task<T[]> AllTasks = Task.WhenAll(Tasks);

        try
        {
            return await AllTasks;
        }
        catch (Exception Exc)
        {
            throw AllTasks.Exception ?? Exc;
        }
    }
}

public static class Log
    // TODO Revisit; this is only a temporary solution, I want a performant logger that writes both to file and an in-game console
    // I also want asynchronous or threaded operations! Big must!
{
    private static void Write(string Message)
    {
        Console.WriteLine(Message);
    }

    public static void Trace(string Message)
    {
        Write($"[TRACE::{DateTime.Now}] {Message}");
    }

    public static void Debug(string Message) 
    {
        Write($"[DEBUG::{DateTime.Now}] {Message}");
    }

    public static void Information(string Message)
    {
        Write($"[INFO::{DateTime.Now}] {Message}");
    }

    public static void Warning(string Message)
    {
        Write($"[WARNING::{DateTime.Now}] {Message}");
    }

    public static void Error(string Message)
    {
        Write($"[ERROR::{DateTime.Now}] {Message}");
    }

    public static void Fatal(string Message)
    {
        Write($"[FATAL::{DateTime.Now}] {Message}");
    }
}


/// <summary>
/// Helper class containing many bodys for niche cases or miscellaneous purpose.
/// </summary>
public static partial class Utility
{
    public class FramesPerSecondCounter
    {
        public const int FPS_SAMPLES = 5;
        private float Sample = 0;
        private int Tally = 0;
        public float Average { get; private set; } = 0;
        public float Current { get; private set; } = 0;

        public void Update(GameTime GameTime)
        {
            this.Sample += this.Current = 1.0f / (float)GameTime.ElapsedGameTime.TotalSeconds;

            if (++this.Tally >= FPS_SAMPLES)
            {
                this.Average = this.Sample / FPS_SAMPLES;
                this.Sample = 0;
                this.Tally = 0;
            }
        }
    }


    public static Color ColorFromString(string Input) // TODO FIX -> allow for RGBA, HSV, HEX and CMYK input
    {
        if (uint.TryParse(Input, NumberStyles.HexNumber, null, out uint ColorValue))
            return new(ColorValue);

        byte[] Fragments = Array.ConvertAll(Input[1..^1].Split(','), byte.Parse);
        return new(Fragments[0], Fragments[1], Fragments[2]);
    }


    private static readonly Dictionary<Type, Func<int>> __CachedSizeOfDelegates = [];

    /// <summary>
    /// Return and cache the size of an object in bytes at runtime.
    /// </summary>
    public static int SizeOf<T>()
    {
        if (__CachedSizeOfDelegates.TryGetValue(typeof(T), out Func<int>? Func))
            return Func();

        DynamicMethod DynMeth = new("_", typeof(int), Type.EmptyTypes, typeof(Utility));
        ILGenerator IL = DynMeth.GetILGenerator();
        IL.Emit(OpCodes.Sizeof, typeof(T));
        IL.Emit(OpCodes.Ret);

        return (__CachedSizeOfDelegates[typeof(T)] = (Func<int>)DynMeth.CreateDelegate(typeof(Func<int>)))();
    }
}