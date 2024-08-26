using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using YAXLib;
using YAXLib.Attributes;
using YAXLib.Enums;
using ClangenReborn;
using System.Linq;
using System;
using System.Text;

namespace ClangenReborn.Defs;

/// <summary>
/// Represents a two-way bridge from .xml to a C# class or struct.
/// </summary>
public abstract class Def
{
    public Def() { }
}

/// <summary>
/// Represents a one-way bridge from .xml to a C# class, being thrown away after load-up
/// </summary>
/// <typeparam name="TOutput">The class or struct to produce</typeparam>
public abstract class ThrowawayDef
{
    public ThrowawayDef() { }
}

/// <summary>
/// Represents a one-way bridge from .xml to a C# class or struct.
/// </summary>
/// <typeparam name="TOutput">The class or struct to produce</typeparam>
public abstract class ThrowawayDef<TOutput>
{
    public ThrowawayDef() { }

    public abstract bool Digest(out TOutput Output);
}



internal class ContentPackDef : ThrowawayDef
{
    [YAXAttributeForClass]
    public required string Id { get; set; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? Name { get; set; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? Description { get; set; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? Icon { get; set; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? Thumbnail { get; set; }

    [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string[]? Authors { get; set; }

    [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string[]? SupportedVersions { get; set; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? RootFolder { get; set; }
}



public class LocaleDef : ThrowawayDef
{
    [YAXSerializeAs("MetaData")]
    internal class RootElement
    {
        [YAXCollection(YAXCollectionSerializationTypes.RecursiveWithNoContainingElement, EachElementName = "Locale")]
        public LocaleDef[]? MetaData { get; set; }
    }

    [YAXAttributeForClass]
    public required string Id { get; init; }

    [YAXAttributeForClass]
    public required string Path { get; init; }

    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public string? Name { get; init; } = "???";

    [YAXDictionary(EachPairName = "li", KeyName = "Id", ValueName = "Name", SerializeKeyAs = YAXNodeTypes.Attribute, SerializeValueAs = YAXNodeTypes.Attribute)]
    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public Dictionary<string, string> Aliases { get; init; } = [];

    [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
    [YAXErrorIfMissed(YAXExceptionTypes.Ignore)]
    public List<string> Charsets { get; init; } = [];

    [YAXDontSerialize]
    public required string ParentFolder;

    [YAXDontSerialize]
    public List<string> Paths = [];


    public static LocaleDef[]? From(TextReader Reader) => Content.DeserializeDef<RootElement>(Reader)?.MetaData;
}



public class ConfigDef : Def 
    // TODO -> Look into a better way to do this
    // + Does not seem to be able to be deserialised at the moment
{
    public uint LastPlayedVersion { get; init; } = 0;

    [YAXCollection(YAXCollectionSerializationTypes.Recursive, EachElementName = "li")]
    public List<string> ActiveMods { get; init; } = new(["core"]);

    // Utility Related
    public bool CheckForUpdates { get; set; } = true;
    public bool ShowChangelog { get; set; } = true;
    public ushort AutosaveInterval { get; set; } = 15;
    public string Language { get; set; } = "LEnglish";

    // Theme Related
    public bool IsFullscreen { get; set; } = false;
    public bool IsDarkMode { get; set; } = true;
    public bool CustomCursorAllowed { get; set; } = true;
    public bool ShadersAllowed { get; set; } = false;
    public bool GoreAllowed { get; set; } = false;

    // Misc
    public bool DiscordIntegration { get; set; } = false;
}

public class ThemeDef : ThrowawayDef
{
    private readonly Dictionary<string, string> Values = [];
    private readonly Dictionary<string, object> ParsedValues = [];

    public required string Id { get; init; }

    public string? GetValue(string Key)
    {
        if (this.Values.TryGetValue(Key, out string? Value))
        {
            return Value;
        }
        else if (this.ParsedValues.TryGetValue(Key, out object? Obj))
        {
            return Obj.ToString();
        }

        return null;
    }

    public T? GetValue<T>(string Key)
    {
        if (this.ParsedValues.TryGetValue(Key, out object? Obj))
        {
            if (Obj is T ObjCasted)
                return ObjCasted;
        }

        return default;
    }

    public bool Contains(string Key) => this.Values.ContainsKey(Key);
    public bool TryGetValue(string Key, out string? Value) => this.Values.TryGetValue(Key, out Value);

    private static object? ParseValue(string Value, string Type)
    {
        switch (Type)
        {
            case "Color":
                return Utility.ColorFromString(Value);

            default:
                return null;
        }
    }

    private static void CrawlDoc(ref ThemeDef Def, XmlReader Doc)
    {
        Stack<string> KeyStack = [];

        Doc.Read();

        XmlNodeType LastNodeType = XmlNodeType.None;
        string? AttributeType = null;

        while (Doc.Read())
        {
            switch (Doc.NodeType)
            {
                case XmlNodeType.Element:
                    KeyStack.Push(Doc.Name);

                    if (Doc.HasAttributes)
                    {
                        Doc.MoveToFirstAttribute();

                        if (Doc.Name == "Type")
                            AttributeType = Doc.GetAttribute("Type");
                    }

                    if (AttributeType is not null)
                        AttributeType = null;

                    break;

                case XmlNodeType.EndElement:
                    if (Doc.Name == "ThemeDef")
                        return;
                    

                    KeyStack.Pop();
                    break;

                case XmlNodeType.Text:
                    if (LastNodeType is not XmlNodeType.Element)
                        continue;

                    if (AttributeType is not null)
                    {
                        object? ParsedValue = ParseValue(Doc.Value, AttributeType);
                        if (ParsedValue is not null)
                        {
                            Def.ParsedValues[string.Join('.', KeyStack.Reverse())] = ParsedValue;
                            AttributeType = null;
                            break;
                        }
                        else
                        {
                            Log.Warning($"Couldn't parse \"{Doc.Value}\" as \"{AttributeType}\" in \"{Def.Id}.{string.Join('.', KeyStack.Reverse())}\"");
                            AttributeType = null;
                        }
                    }

                    Def.Values[string.Join('.', KeyStack.Reverse())] = Doc.Value;
                    break;

                default:
                    break;
            }

            LastNodeType = Doc.NodeType;
        }
    }

    public static ThemeDef[] From(string Path)
    {
        using StreamReader Reader = new(Path, Encoding.Unicode);
        XmlReader Doc = XmlReader.Create(Path);
       
        if (!Doc.Read())
            return [];

        Doc.MoveToElement();
        Doc.Read();

        List<ThemeDef> Themes = [];

        while (Doc.Read())
        {
            if (Doc.NodeType != XmlNodeType.Element || Doc.Name != "ThemeDef")
                continue;

            if (Doc.AttributeCount == 0)
            {
                Log.Warning($"Theme in \"{Path}\" had no provided ID");
                continue;
            }

            ThemeDef Def = new()
            {
                Id = Doc.GetAttribute(0)
            };

            Log.Trace($"Loading Theme \"{Def.Id}\" in \"{Path}\" . . .");
            CrawlDoc(ref Def, Doc);
            Themes.Add(Def);
        }

        return [.. Themes];
    }
}