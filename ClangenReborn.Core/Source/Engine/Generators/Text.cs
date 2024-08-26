using ClangenReborn.Defs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using VYaml.Parser;
namespace ClangenReborn;


public static partial class Text 
{
    /// <summary>
    /// The error format used for unsuccessful parsing, the 0 being the error code/subject and 1 being the error description
    /// </summary>
    private const string QUERY_ERROR_FORMAT = "[ERR.{0}; {1}]";


    private static readonly Dictionary<Type, MethodInfo> __ParseTree_Single = [];

    /// <summary>
    /// Thread-safe dictionary, mapping translation keys to single text values of the current language.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> TranslationTable = new();

    /// <summary>
    /// Thread-safe dictionary, mapping translation keys to multiple text values of the current language.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string[]> TranslationArrayTable = new();

    /// <summary>
    /// Thread-safe dictionary, holding purely the metadata of each language.
    /// </summary>
    private static readonly ConcurrentDictionary<string, LocaleDef> LanguageTable = new();

    /// <summary>
    /// The key of the current-selected language.
    /// </summary>
    private static LocaleDef? CurrentLanguage;

    /// <summary>
    /// Load a .YML Translation file into memory.
    /// </summary>
    private static Task LoadFile(string Path)
    {
        using (FileStream Stream = File.OpenRead(Path))
        {
            if (Stream.Length < 1)
                return Task.CompletedTask;

            YamlParser Parser = YamlParser.FromBytes(File.ReadAllBytes(Path));
            Stack<string> KeyStack = [];

            // Skip StreamStart, DocumentStart and MappingStart event.
            Parser.Read(); Parser.Read(); Parser.Read();

            while (Parser.Read()) 
                // TODO Look into potential optimisations, if not atleast just to remove 3rd-party dependancy of VYaml
                // + Nested switch case looks quite ugly
            {
                switch (Parser.CurrentEventType)
                {
                    case ParseEventType.Scalar:
                        string Scalar = Parser.ReadScalarAsString()!;

                        switch (Parser.CurrentEventType)
                        {
                            case ParseEventType.MappingStart:
                                KeyStack.Push(Scalar);
                                break;

                            case ParseEventType.Scalar:
                                if (KeyStack.Count > 0)
                                    TranslationTable[string.Join('.', KeyStack.Reverse()) + "." + Scalar] = Parser.GetScalarAsString()!;
                                else
                                    TranslationTable[Scalar] = Parser.GetScalarAsString()!;

                                break;

                            case ParseEventType.SequenceStart:
                                List<string> TranslationArray = [];

                                Parser.Read();

                                while (Parser.CurrentEventType is not ParseEventType.SequenceEnd)
                                    // TODO Allow for nested sequences, or validate to ensure they arent allowed
                                {
                                    TranslationArray.Add(Parser.ReadScalarAsString()!);
                                }

                                if (TranslationArray.Count > 1)
                                {
                                    if (KeyStack.Count > 0)
                                        TranslationArrayTable[string.Join('.', KeyStack.Reverse()) + "." + Scalar] = [.. TranslationArray];
                                    else
                                        TranslationArrayTable[Scalar] = [.. TranslationArray];
                                }
                                else
                                {
                                    if (KeyStack.Count > 0)
                                        TranslationTable[string.Join('.', KeyStack.Reverse()) + "." + Scalar] = TranslationArray[0];
                                    else
                                        TranslationTable[Scalar] = TranslationArray[0];
                                }

                                break;

                            default:
                                break;
                        }

                        break;

                    case ParseEventType.MappingEnd:
                        if (KeyStack.Count > 0)
                            KeyStack.Pop();

                        break;

                    default:
                        break;
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Set the current game language to a given key. Returns 0 on success, negative number on failure.
    /// </summary>
    public static int SetLanguage(string Key)
    {
        // TODO Remove this part, wheres the consistancy?
        if (!LanguageTable.TryGetValue(Key, out LocaleDef? Metadata))
            return -1; // Metadata doesnt exist
        else if (Metadata.Path is null)
            return -2; // Metadata path wasnt given
        if (CurrentLanguage == Metadata)
            return -3; // Metadata already current in use

        CurrentLanguage = Metadata;

        foreach (string Path in Metadata.Paths)
        {
            string[] Paths = Directory.GetFiles($"{Metadata.ParentFolder}\\{Path}", "*.yml", SearchOption.AllDirectories);
            Array.ConvertAll(Paths, LoadFile).RunTasks().GetAwaiter().GetResult();
        }

        return 0;
    }

    /// <summary>
    /// Load a languages file. If two languages have the same Id, the newest language will overwrite the older.
    /// </summary>
    /// <param name="Path">The path of the file</param>
    internal static void LoadLanguages(string Path) // TODO Implement element-specific overwriting
    {
        LocaleDef[] LocaleDefs;

        using (StreamReader Reader = new(Path))
            LocaleDefs = LocaleDef.From(Reader) ?? [];

        foreach (LocaleDef LocaleDef in LocaleDefs)
        {
            LocaleDef.ParentFolder = Directory.GetParent(Path)!.FullName;
            LocaleDef.Paths = [LocaleDef.Path];
            LanguageTable[LocaleDef.Id] = LocaleDef;
        }
    }


    public static void BuildParseTree()
    {
        foreach (MethodInfo? Method in typeof(Text).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
        {
            if (Method is null || Method.Name != "ParseObject")
                continue;

            ParameterInfo[] Arguments = Method.GetParameters();

            if (Arguments[0].ParameterType == typeof(string) && Arguments.Length == 2)
                __ParseTree_Single[Arguments[1].ParameterType] = Method;
        }
    }


    public static string? GetTranslation(string Key, int? Index = null)
    {
        if (TranslationTable.TryGetValue(Key, out string? Translation))
        {
            return Translation;
        }
        else if (TranslationArrayTable.TryGetValue(Key, out string[]? TranslationArray))
        {
            return Index is not null ? TranslationArray[Index.Value] : XorRandom.Shared.GetItem(TranslationArray);
        }
        else
        {
            return null;
        }
    }


    public static string? EvaluateConstant(string Constant) => Constant switch
    {
        "GameVersion" => GameInfo.VERSION_MAJOR_MINOR_PATCH,

        "Now" => DateTime.Now.ToString(),
        "Now_Date" => DateTime.Now.ToShortDateString(),
        "Now_Time" => DateTime.Now.ToShortTimeString(),
        "UtcNow" => DateTime.UtcNow.ToString(),
        "UtcNow_Date" => DateTime.UtcNow.ToShortDateString(),
        "UtcNow_Time" => DateTime.UtcNow.ToShortTimeString(),
        _ => null,
    };


    private static void EvaluateModifier(ref string Value, string? Modifier)
    {
        switch (Modifier)
        {
            case null:
            case "":
                return;

            case "U":
                Value = GameInfo.TextInfo.ToUpper(Value);
                break;

            case "L":
                Value = GameInfo.TextInfo.ToLower(Value);
                break;

            case "T":
                Value = GameInfo.TextInfo.ToTitleCase(Value);
                break;

            default:
                break;
        }

        Value = Value.Trim();
    }


    private static string? InternalParse<T>(string Query, T Arg, out bool ErrorOccured)
    {
        object? ParseResult = Arg;
        Type ParseType = typeof(T);
        int Index = Query.LastIndexOf('!');
        string? Modifiers = null;

        if (-1 < Index)
        {
            (Query, Modifiers) = (Query[..Index].Trim(), Query[(Index + 1)..].Trim());
        }

        do
        {
            Index = Query.IndexOfAny([':', '.'], 1);
            string Segment = -1 < Index ? Query[1..Index] : Query[1..];

            if (Query.StartsWith('.'))
            {
                object? PotentialParseResult;

                if (!__ParseTree_Single.TryGetValue(ParseType, out var Parser))
                {
                    // TODO Add support for interface / generic parsing as a fallback

                    if (ParseType.IsArray)
                    {
                        PotentialParseResult = ParseObject(Segment, (Array)ParseResult!);
                    }
                    else
                    {
                        ErrorOccured = true;
                        return $"ParserNotFound; for type \"{ParseType.FullName}\"";
                    }
                }
                else
                {
                    PotentialParseResult = Parser.Invoke(null, [Segment, ParseResult]);
                }

                if (PotentialParseResult is null)
                {
                    ErrorOccured = true;
                    return $"FunctionNullReturn; \"{Segment}\" on type \"{ParseType.FullName}\"";
                }

                ParseType = (ParseResult = PotentialParseResult).GetType();
            }
            else if (Query.StartsWith(':'))
            {
                MemberInfo? MemberInfo = ParseType.GetField(Segment) as MemberInfo ?? ParseType.GetProperty(Segment);
                if (MemberInfo is null)
                {
                    ErrorOccured = true;
                    return $"MemberNotFound; \"{Segment}\" for type \"{ParseType.FullName}\"";
                }

                ParseResult = MemberInfo.GetValue(ParseResult);

                if (ParseResult is null)
                {
                    ErrorOccured = true;
                    return $"MemberNullReturn; \"{Segment}\" on type \"{ParseType.FullName}\"";
                }

                ParseType = ParseResult.GetType();
            }

            if (-1 < Index)
                Query = Query[Index..];

        } while (-1 < Index);


        if (ParseResult is null || ParseResult.Equals(Arg))
        {
            ErrorOccured = false;
            return ""; //nothin to detect
        }

        ErrorOccured = false;
        string Result = ParseResult.ToString() ?? string.Empty;
        EvaluateModifier(ref Result, Modifiers);

        return Result;
    }


    public static string ParseExpression<T0>(string Value, T0 Arg1, string Arg1Alias, bool ShouldDisplayErrors = true)
    {
        int Start, End, I = 0;
        while (I < Value.Length)
        {
            string? Result;
            bool ErrorHasOccured = false;

            switch (Value[I++])
            {
                case '{': // QUERIES
                    End = Start = I;
                    while (Value[++End] != '}') ;
                    I = End;

                    string Query = Value[Start..End];

                    if (Query[..Arg1Alias.Length] == Arg1Alias)
                    {
                        Result = InternalParse(Query[Arg1Alias.Length..], Arg1, out ErrorHasOccured);
                    }
                    else
                    {
                        ErrorHasOccured = true;
                        Result = $"NotPartOfScope -> \"{Query[..Query.IndexOfAny([':', '.'], 1).Above(Arg1Alias.Length)]}\"";
                    }

                    break;

                case '[': // CONSTANTS
                    End = Start = I;
                    while (Value[++End] != ']') ;
                    I = End;

                    Result = EvaluateConstant(Value[Start..End]);

                    if (Result is null)
                    {
                        ErrorHasOccured = true;
                        Result = $"ConstantNotFound -> \"{Value[Start..End]}\"";
                    }

                    break;

                case '$': // TRANLATIONS
                    if (I >= Value.Length - 1 || Value[I] != '(')
                        continue;

                    End = Start = I;
                    while (Value[++End] != ')') ;
                    I = End;

                    Start++;

                    Result = GetTranslation(Value[Start..End]);

                    if (ErrorHasOccured = Result is null)
                        Result = $"TranslationKeyNotFound -> \"{Value[Start..End]}\"";

                    Start--;

                    break;

                default:
                    continue;
            }

            if (ErrorHasOccured && ShouldDisplayErrors)
                Result = string.Format(GameInfo.CultureInfo, QUERY_ERROR_FORMAT, Value[Start..End], Result ?? "UnknownError");

            if (Result is not null)
            {
                Value = Value.Remove(Start - 1, 2 + End - Start).Insert(Start - 1, Result);
                I -= 2 + End - Start;
                I += Result.Length;
            }
            else
            {
                Value = Value.Remove(Start - 1, 2 + End - Start);
                I -= 2 + End - Start;
            }
        }

        return Value;
    }
    
    /// <summary>
    /// Represents an inclusive, continuous range of characters.
    /// </summary>
    public readonly struct UnicodeRange
    {
        // Character sets provided from https://jrgraphix.net/r/Unicode/
        public static readonly UnicodeRange Latin = new(0x0020, 0x007F);
        public static readonly UnicodeRange Latin1 = new(0x00A0, 0x00FF);
        public static readonly UnicodeRange LatinExpandedA = new(0x0100, 0x017F);
        public static readonly UnicodeRange LatinExpandedB = new(0x0180, 0x024F);

        public readonly ushort Lower;
        public readonly ushort Upper;
        public readonly ushort Range => (ushort)(this.Upper - this.Lower);

        public UnicodeRange(ushort Upper) : this(0, Upper) { }
        public UnicodeRange(ushort Lower, ushort Upper)
        {
            if (Lower > Upper)
                (this.Lower, this.Upper) = (Upper, Lower);
            else
                (this.Lower, this.Upper) = (Lower, Upper);
        }

        public override string ToString()
            => (this.Lower == 0 && this.Lower < this.Upper) ? $"UnicodeRange({this.Upper})" : (
                (this.Lower < this.Upper) ? $"UnicodeRange({this.Lower}, {this.Upper})" : $"UnicodeRange({this.Upper}, {this.Lower})"
            );
    }
}
