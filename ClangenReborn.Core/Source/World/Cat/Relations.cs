using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClangenReborn;

[Flags]
public enum RelationType : byte
{
    None,
    Friend,
    Mate
}

/// <summary>
/// Enum representing a Cats' blood relation to another.
/// </summary>
public enum KinRelationType : byte
{
    /// <summary>No blood relation at all.</summary>
    None,
    /// <summary>A sibling.</summary>
    Sibling,
    /// <summary>A parent.</summary>
    Parent,
    /// <summary>A child.</summary>
    Child,
    /// <summary>An aunt or uncle.</summary>
    ParentSibling,
    /// <summary>A niece or nephew.</summary>
    SiblingChild,
}


public struct Relation(CatRef From, CatRef To, KinRelationType KinType, bool IsPresentKin, RelationType Type, sbyte GenerationLevel, byte Loyalty, byte Familiarity, sbyte Approval)
    : IEquatable<Relation>
{
    /// <summary>
    /// The <see cref="Cat"/> that holds this relationship to <see cref="To"/>.<br/><br/>
    /// If this relation is a parent to child, this cat would be a parent of <see cref="To"/>.
    /// </summary>
    public readonly CatRef From = From;

    /// <summary>
    /// The <see cref="Cat"/> this relationship is refering to.<br/><br/>
    /// If this relation is a parent to child, this cat would be a child of <see cref="From"/>.
    /// </summary>
    public readonly CatRef To = To;

    /// <summary>
    /// The type of blood relation these two cats share.<br/><br/>It speaks nothing of personal relations.
    /// </summary>
    public readonly KinRelationType KinType = KinType;

    /// <summary>
    /// The type of relationship this <see cref="Relation"/> represents.
    /// </summary>
    public RelationType Type = Type;

    /// <summary>
    /// Whether or not there is a blood relation and they are present in the life of this cat.
    /// </summary>
    public bool IsPresentKin = IsPresentKin;

    /// <summary>
    /// Relative generations away this <see cref="Cat"/> is to another <see cref="Cat"/> if applicable. <br/>
    /// Negative numbers represent older generations, positive numbers represent younger generations and 0 means an equal generation.<br/><br/>
    /// This means nothing if <see cref="KinType"/> is <see cref="KinRelationType.None"/>.
    /// </summary>
    public sbyte GenerationLevel = GenerationLevel;

    /// <summary>
    /// How loyal a <see cref="Cat"/> is to another <see cref="Cat"/>.
    /// </summary>
    public byte Loyalty = Loyalty;

    /// <summary>
    /// How much a <see cref="Cat"/> knows about another <see cref="Cat"/>.
    /// </summary>
    public byte Familiarity = Familiarity;

    /// <summary>
    /// How much a <see cref="Cat"/> likes or dislikes another <see cref="Cat"/>. <br/>
    /// -128 is reserved for irreversable hate.
    /// </summary>
    public sbyte Approval = Approval;

    public override readonly int GetHashCode() => (this.To.Id << 16) | (this.From.Id);
    public override readonly bool Equals(object? Obj) => Obj is Relation Relation && Equals(Relation);
    public readonly bool Equals(Relation Other) => this.From == Other.From && this.To == Other.To;
    public static bool operator ==(Relation This, Relation Other) => This.Equals(Other);
    public static bool operator !=(Relation This, Relation Other) => !(This == Other);
}


public class Relations : IEnumerable<Relation>
{
    private readonly List<Relation> InternalRelations = [];

    public IEnumerator<Relation> GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.InternalRelations.GetEnumerator();

    public Relation? Get(CatRef CatRef)
    {
        for (int I = 0; I < this.InternalRelations.Count; I++)
        {
            if (this.InternalRelations[I].To == CatRef)
            {
                return this.InternalRelations[I];
            }
        }

        return null;
    }
}


public partial class Cat 
{
    public CatRef FirstParent = CatRef.None;
    public CatRef SecondParent = CatRef.None;

    public Relations Relations = [];
}