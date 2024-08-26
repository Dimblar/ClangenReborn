using System.Collections.Generic;
using YAXLib.Attributes;
using YAXLib.Enums;

namespace ClangenReborn.Defs;


public class TintConfigDef : ThrowawayDef
{
    public class GroupDef
    {
        [YAXAttributeForClass]
        public required string Id { get; set; }

        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
        public required List<string> AllowedColors { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
        public required List<string> PossibleTints { get; set; }
    }

    public class TintDef
    {
        [YAXDictionary(EachPairName = "li", KeyName = "Id", ValueName = "Value", SerializeKeyAs = YAXNodeTypes.Attribute, SerializeValueAs = YAXNodeTypes.Attribute)]
        public required Dictionary<string, string> Colors { get; set; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Group")]
        public required List<GroupDef> Groups { get; set; }
    }

    public TintDef? Tint { get; set; }
    public TintDef? WhiteTint { get; set; }
}


public class PeltPoseDefs : Def
{
    public class PoseItem
    {
        [YAXAttributeForClass]
        public required string Id { get; init; }

        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string? Fallback { get; init; }
    }

    public class PoseDef
    {
        [YAXAttributeForClass]
        public required string Id { get; set; }

        [YAXAttributeForClass]
        public required int Rows { get; set; } = 0;

        [YAXAttributeForClass]
        public required int Columns { get; set; } = 0;

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "li")]
        public required List<PoseItem> Poses { get; set; }
    }

    [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PoseDef")]
    public required List<PoseDef> Poses { get; init; }
}


public class PeltDefs : ThrowawayDef
{
    public class RegionItem
    {
        [YAXAttributeForClass]
        [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
        public string? Color { get; init; }
    }


    public class Regions
    {
        [YAXAttributeForClass]
        public required int Width { get; init; }

        [YAXAttributeForClass]
        public required int Height { get; init; }

        [YAXAttributeForClass]
        public required string PoseSet { get; init; }

        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "li")]
        public required List<RegionItem> RegionItems { get; init; }
    }

    public class PeltDef
    {
        [YAXAttributeForClass]
        public required string Id { get; init; }

        public required string Texture { get; init; }

        public required Regions Regions { get; init; }
    }

    [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "PeltDef")]
    public required List<PeltDef> Defs { get; init; }
}