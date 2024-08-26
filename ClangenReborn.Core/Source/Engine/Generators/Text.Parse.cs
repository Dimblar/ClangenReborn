using System;
using System.Runtime.CompilerServices;

namespace ClangenReborn;

public static partial class Text
{
    #region Parsers

    internal static object? ParseObject(string Query, Cat Cat)
    {
        switch (Query)
        {
            case "GetName":
                return Cat.Name;

            case "GetGenderNoun":
                // TODO Update once pronouns and genders are implemented
                return Cat.Sex ? "tom-cat" : "she-cat";

            case "TestArray": // TODO Remove
                return new int[3] { 0, 1, 2 };

            default:
                return null;
        }
    }

    internal static object? ParseObject(string Query, Array Array)
    {
        switch (Query)
        {
            case "Any":
                return XorRandom.Shared.GetItem(Array);

            default:
                return null;
        }
    }

    #endregion
}
