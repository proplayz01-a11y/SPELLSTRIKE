using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class MiniJsonDictionaryParser
{
    private static readonly Regex EntryRegex = new Regex(
        "\"(?<word>(?:\\\\.|[^\"])*)\"\\s*:\\s*\\{(?<body>.*?)\\}\\s*(?:,|$)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex StringFieldRegex = new Regex(
        "\"(?<key>definition|rarity)\"\\s*:\\s*\"(?<value>(?:\\\\.|[^\"])*)\"",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex LengthFieldRegex = new Regex(
        "\"length\"\\s*:\\s*\"?(?<value>\\d+)\"?",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static Dictionary<string, DictionaryManager.GameplayWordData> ParseGameplayDictionary(string json)
    {
        Dictionary<string, DictionaryManager.GameplayWordData> result = new Dictionary<string, DictionaryManager.GameplayWordData>();

        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[MiniJsonDictionaryParser] Dictionary JSON is empty.");
            return result;
        }

        foreach (Match entryMatch in EntryRegex.Matches(json))
        {
            string word = Unescape(entryMatch.Groups["word"].Value).ToUpperInvariant();
            string body = entryMatch.Groups["body"].Value;

            if (string.IsNullOrWhiteSpace(word)) continue;

            DictionaryManager.GameplayWordData data = ParseWordData(body, word.Length);

            result[word] = data;
        }

        if (result.Count == 0)
        {
            Debug.LogError("[MiniJsonDictionaryParser] No dictionary entries were parsed.");
        }

        return result;
    }

    private static DictionaryManager.GameplayWordData ParseWordData(string body, int fallbackLength)
    {
        DictionaryManager.GameplayWordData data = new DictionaryManager.GameplayWordData
        {
            length = fallbackLength
        };

        foreach (Match fieldMatch in StringFieldRegex.Matches(body))
        {
            string key = fieldMatch.Groups["key"].Value;
            string value = Unescape(fieldMatch.Groups["value"].Value);

            if (key == "definition")
                data.definition = value;
            else if (key == "rarity")
                data.rarity = value;
        }

        Match lengthMatch = LengthFieldRegex.Match(body);
        if (lengthMatch.Success && int.TryParse(lengthMatch.Groups["value"].Value, out int parsedLength))
        {
            data.length = parsedLength;
        }

        return data;
    }

    private static string Unescape(string value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : Regex.Unescape(value);
    }
}
