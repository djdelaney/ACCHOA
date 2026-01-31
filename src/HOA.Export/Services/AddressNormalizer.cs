using System.Text.RegularExpressions;

namespace HOA.Export.Services;

public static partial class AddressNormalizer
{
    private static readonly Dictionary<string, string> StreetAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ln"] = "Lane",
        ["st"] = "Street",
        ["dr"] = "Drive",
        ["ct"] = "Court",
        ["cir"] = "Circle",
        ["blvd"] = "Boulevard",
        ["ave"] = "Avenue",
        ["rd"] = "Road",
        ["pl"] = "Place",
        ["ter"] = "Terrace",
        ["terr"] = "Terrace",
        ["pkwy"] = "Parkway",
        ["hwy"] = "Highway",
        ["trl"] = "Trail",
        ["way"] = "Way",
        ["sq"] = "Square",
        ["xing"] = "Crossing",
        ["pt"] = "Point",
        ["cv"] = "Cove",
        ["aly"] = "Alley",
        ["crk"] = "Creek",
        ["est"] = "Estate",
        ["expy"] = "Expressway",
        ["fwy"] = "Freeway",
        ["grv"] = "Grove",
        ["holw"] = "Hollow",
        ["jct"] = "Junction",
        ["lk"] = "Lake",
        ["ldg"] = "Lodge",
        ["mdw"] = "Meadow",
        ["mdws"] = "Meadows",
        ["mews"] = "Mews",
        ["pass"] = "Pass",
        ["path"] = "Path",
        ["run"] = "Run",
        ["walk"] = "Walk",

        // Directional abbreviations
        ["n"] = "North",
        ["s"] = "South",
        ["e"] = "East",
        ["w"] = "West",
        ["ne"] = "Northeast",
        ["nw"] = "Northwest",
        ["se"] = "Southeast",
        ["sw"] = "Southwest",
    };

    // The expanded street type names (excludes directional words like North/South).
    // After abbreviation expansion, any words trailing the last street type are
    // assumed to be a city name and are stripped.
    private static readonly HashSet<string> StreetTypes = new(StringComparer.Ordinal)
    {
        "Lane", "Street", "Drive", "Court", "Circle", "Boulevard", "Avenue",
        "Road", "Place", "Terrace", "Parkway", "Highway", "Trail", "Way",
        "Square", "Crossing", "Point", "Cove", "Alley", "Creek", "Estate",
        "Expressway", "Freeway", "Grove", "Hollow", "Junction", "Lake",
        "Lodge", "Meadow", "Meadows", "Mews", "Pass", "Path", "Run", "Walk",
    };

    /// <summary>
    /// Normalizes an address for consistent grouping. Converts to title case,
    /// expands abbreviations, and strips city/state/zip suffixes.
    /// </summary>
    public static string Normalize(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        // Strip everything after the first comma (city, state, zip).
        // Track whether a comma was present — that signals the address had a
        // structured "<street> <city>, <state> <zip>" format, which we use
        // later when no street type is recognized (e.g. due to a typo).
        var hadComma = address.Contains(',');
        var commaIndex = address.IndexOf(',');
        if (commaIndex >= 0)
            address = address[..commaIndex];

        // Strip trailing zip codes (5 or 5+4 digit patterns)
        address = TrailingZipCode().Replace(address, "").Trim();

        // Normalize whitespace
        address = MultipleSpaces().Replace(address.Trim(), " ");

        // Split into words, expand abbreviations, and title-case.
        // This must happen before state/town stripping so that street
        // abbreviations like "Ct" (Court) aren't mistaken for states ("CT").
        var words = address.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i].Trim('.');

            // Don't title-case the house number
            if (i == 0 && char.IsDigit(word[0]))
            {
                words[i] = word;
                continue;
            }

            // Expand abbreviations
            if (StreetAbbreviations.TryGetValue(word, out var expanded))
            {
                words[i] = expanded;
            }
            else
            {
                words[i] = ToTitleCase(word);
            }
        }

        // Truncate after the last recognized street type so that any trailing
        // city name, state, or other suffix is removed. For example
        // "11 Evans Court Downingtown" → "11 Evans Court".
        var lastStreetTypeIndex = -1;
        for (var i = 0; i < words.Length; i++)
        {
            if (StreetTypes.Contains(words[i]))
                lastStreetTypeIndex = i;
        }

        if (lastStreetTypeIndex >= 0 && lastStreetTypeIndex < words.Length - 1)
        {
            words = words[..(lastStreetTypeIndex + 1)];
        }
        else if (lastStreetTypeIndex < 0 && hadComma && words.Length > 2)
        {
            // No recognized street type (possibly a typo like "Circe" for
            // "Circle"), but the original address had a comma so we know
            // it included a city name. Strip the last word as the city.
            words = words[..^1];
        }

        return string.Join(" ", words);
    }

    private static string ToTitleCase(string word)
    {
        if (string.IsNullOrEmpty(word))
            return word;

        return char.ToUpper(word[0]) + word[1..].ToLower();
    }

    [GeneratedRegex(@"\s\d{5}(-\d{4})?\s*$")]
    private static partial Regex TrailingZipCode();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpaces();
}
