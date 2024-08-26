using ClangenReborn.Defs;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using static ClangenReborn.Text;
using static ClangenReborn.World;

namespace ClangenReborn;

public static partial class Content
{
    internal static readonly Dictionary<string, PronounDefs.PronounDef> PronounDefs = [];

    internal static void LoadPronouns(string File)
    {
        using StreamReader Reader = new(File);
        PronounDefs? PossiblePronounDef = DeserializeDef<PronounDefs>(Reader);

        if (PossiblePronounDef is null)
        {
            Log.Warning($"Couldn't load pronouns at {File}");
            return;
        }

        foreach (var Def in  PossiblePronounDef.Defs)
            PronounDefs[Def.Id] = Def;
    }
}

public readonly struct Pronouns
{
    private readonly string Id = "";

    public bool IsValid => Content.PronounDefs.ContainsKey(this.Id);

    private PronounDefs.PronounDef? Def => IsValid ? Content.PronounDefs[this.Id] : null;

    public bool IsPlural => this.Def?.IsPlural ?? false;

    public string Subjective => this.Def?.Subjective ?? "SUBJECTIVE";
    public string Objective => this.Def?.Objective ?? "OBJECTIVE";
    public string Possessive => this.Def?.Possessive ?? "POSESSIVE";
    public string PossessiveAdj => this.Def?.PossessiveAdj ?? "POSSESSIVEADJ";
    public string Reflexive => this.Def?.Reflexive ?? "REFLEXIVE";

    public Pronouns(string Id)
    {
        this.Id = Id;
    }
    
}

/// <summary>
/// Helper interface to allow for different naming conventions.
/// </summary>
public abstract class CatName 
    // TODO Revisit -> is this neccessary? Maybe be a better way to do this
{
    /// <summary>
    /// Method to check if this <see cref="CatName"/> is valid.
    /// </summary>
    public abstract bool IsValidName();

    /// <summary>
    /// Method to return a string representation of this <see cref="CatName"/>.
    /// </summary>
    public abstract string GetName();

    public override string ToString() => GetName();
}

/// <summary>
/// A reference to a <see cref="Cat"/> object
/// </summary>
public readonly struct CatRef : IEquatable<CatRef?>, IEquatable<CatRef>
{
    /// <summary>
    /// A <see cref="CatRef"/> leading nowhere.
    /// </summary>
    public static readonly CatRef None = new(0);

    /// <summary>
    /// The ID of the <see cref="Cat"/> object.
    /// </summary>
    public readonly ushort Id;

    /// <summary>
    /// Whether or not this Id is valid.
    /// </summary>
    public readonly bool HasValue;

    /// <summary>
    /// Get the <see cref="Cat"/> object this <see cref="CatRef"/> is referencing.
    /// </summary>
    public Cat Value => ThisWorld!.GetCat(this.Id)!;

    public CatRef(ushort Id)
    {
        this.Id = Id;
        this.HasValue = 0 < Id && ThisWorld is not null && Id <= ThisWorld.LastCatId;
    }

    public override int GetHashCode() => this.Id;
    public override bool Equals(object? Obj) => Obj is CatRef CatRef && Equals(CatRef);
    public bool Equals(CatRef Other) => Other.Id == this.Id;
    public bool Equals(CatRef? Other) => Other.HasValue && Other.Value.Id == this.Id;
    public static bool operator ==(CatRef This, CatRef Other) => This.Equals(Other);
    public static bool operator !=(CatRef This, CatRef Other) => !(This == Other);

    public static implicit operator Cat?(CatRef Value) => ThisWorld!.Cats.TryGetValue(Value.Id, out Cat? Existing) ? Existing : null;
    public static implicit operator CatRef(Cat Value) => new(Value.Id);
}



public partial class Cat : IEquatable<Cat>, IEquatable<CatRef>
{
    private static readonly object __L_AddCat = new();

    /// <summary>
    /// An <see langword="enum"/> representing the stage a <see cref="Cat"/> has aged to.
    /// </summary>
    public enum AgeStage
    {
        Newborn, Kitten, Adolescent, YoungAdult, Adult, SeniorAdult, Senior
    }

    /// <summary>
    /// Unique number to represent this <see cref="Cat"/>. 0 reserved for cats not fully added to world yet
    /// </summary>
    public ushort Id { get; private set; } = 0;

    /// <summary>
    /// The number used as a starting seed for this cats' generation.
    /// </summary>
    public readonly uint Seed;


    public readonly IFaction Faction; // TODO Assess what to do with factions.

    /// <summary>
    /// Biological sex -> <see langword="true"/> for Male and <see langword="false"/> for Female.
    /// </summary>
    public readonly bool Sex;

    public readonly Looks Looks;

    /// <summary>
    /// The moon a <see cref="Cat"/> was born.
    /// </summary>
    public readonly uint BirthMoon;

    /// <summary>
    /// The moon a <see cref="Cat"/> died.
    /// </summary>
    public uint? DeathMoon;

    public Pronouns Pronouns;

    /// <summary>
    /// This cats name.
    /// </summary> 
    public CatName Name;


    //public TranslationKey Thought;

    /// <summary>
    /// Get <see cref="AgeStage"/> Enum based on this cats age, based on biological age.
    /// </summary>
    public AgeStage Age => this.MoonsBiological switch
    {
        0 => AgeStage.Newborn,
        < 6 => AgeStage.Kitten,
        < 12 => AgeStage.Adolescent,
        < 48 => AgeStage.YoungAdult,
        < 96 => AgeStage.Adult,
        < 120 => AgeStage.SeniorAdult,
        _ => AgeStage.Senior
    };

    /// <summary>
    /// The biological age of this Cat. If this cat dies, this will remain as whatever it was when they died.
    /// </summary>
    public uint MoonsBiological;

    /// <summary>
    /// The chronological age of this Cat. This will continue to tick provided this <see cref="Cat"/> exists.
    /// </summary>
    public uint MoonsChronological;

    /// <summary>
    /// Remove a <see cref="Cat"/> object from the loaded worlds' cache of kitties<br/>
    /// Returns <see langword="true"/> if its no longer present, <see langword="false"/> is otherwise.
    /// </summary>
    public bool Decouple()
    {
        if (ThisWorld is not null && ThisWorld.Cats.Remove(this.Id))
        {
            this.Id = 0;
        }

        return !IsCoupled();
    }

    /// <summary>
    /// Adds a <see cref="Cat"/> object to the loaded worlds' cache of kitties<br/>
    /// Returns <see langword="true"/> if its present, <see langword="false"/> is otherwise.
    /// </summary>
    public bool Couple()
    {
        if (ThisWorld is not null)
        {            
            lock (__L_AddCat)
                ThisWorld.Cats[this.Id = ThisWorld.NextCatId()] = this;
        }

        return IsCoupled();
    }

    /// <summary>
    /// Checks if this <see cref="Cat"/> object is coupled to the kitty cache.
    /// </summary>
    public bool IsCoupled() => this.Id == 0 || (ThisWorld is not null && ThisWorld.Cats.ContainsKey(this.Id));

    #region Creation Methods
    /// <summary>
    /// Create a Cat.
    /// </summary>
    public static Cat Create(RandomGenerator? Random = null)
    {
        Random ??= new XorRandom();
        return new();
    }

    /// <summary>
    /// Create a Cat from existing parents.
    /// </summary>
    public static Cat Create(Cat FirstParent, Cat SecondParent, RandomGenerator? Random = null)
    {
        Random ??= new XorRandom();
        throw new NotImplementedException();
    }
    #endregion

    public override string ToString() => $"CAT {this.Id} {this.Seed}";
    public override int GetHashCode() => this.Id;
    public override bool Equals(object? Obj) => Obj is not null && Obj is Cat objCat && Equals(objCat);
    public bool Equals(Cat? Cat) => Cat is not null && (ReferenceEquals(this, Cat) || Cat.Id == this.Id);
    public bool Equals(CatRef Other) => Other.Id == this.Id;
    public static bool operator ==(Cat Cat, CatRef Ref) => Cat.Id == Ref.Id;
    public static bool operator !=(Cat Cat, CatRef Ref) => Cat.Id != Ref.Id;


    public bool IsBaby() => this.Age is AgeStage.Newborn or AgeStage.Kitten;
    public bool IsTeen() => this.Age is AgeStage.Adolescent;
    public bool IsAdult() => !(IsBaby() || IsTeen());
}
