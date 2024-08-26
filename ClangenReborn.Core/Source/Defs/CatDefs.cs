using System.Collections.Generic;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace ClangenReborn.Defs;

public sealed class PronounDefs : ThrowawayDef
{
    public sealed class PronounDef : ThrowawayDef
    {
        [YAXAttributeForClass]
        public required string Id { get; init; }

        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public bool IsPlural { get; init; } = false;

        public required string Subjective { get; init; }
        public required string Objective { get; init; }
        public required string Possessive { get; init; }
        public required string PossessiveAdj { get; init; }
        public required string Reflexive { get; init; }
    }

    [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PronounDef")]
    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public List<PronounDef> Defs { get; init; } = [];
}