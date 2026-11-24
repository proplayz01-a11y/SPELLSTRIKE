using System;
using System.Collections.Generic;

/// <summary>
/// Offline multiset planner for normal combat. Existing selectable letters and
/// pending pickups are immutable inputs. The plan spends only the new-letter
/// budget and guarantees its advertised words in the projected pool.
/// </summary>
public sealed class IntelligentLetterPoolPlanner
{
    public sealed class Plan
    {
        public string MainWord;
        public string RemainderWord;
        public List<char> NewLetters;
        public int ReusedLetters;
        public int PreferredLength;
        public int ObjectiveMask;
    }

    private sealed class Entry
    {
        public string Word;
        public byte[] Indices;
        public byte[] Counts;
    }

    private struct Ranked
    {
        public Entry Entry;
        public float Score;
        public int Missing;
        public int Mask;
    }

    private const string Difficult = "QXZJKVWY";
    private const int SamplePerLength = 96;
    private const int SamplePerDifficultLetter = 120;
    private const int ShortlistPerPriority = 5;
    private readonly List<Entry>[] byLength = new List<Entry>[17];
    private readonly List<Entry>[] byLetter = new List<Entry>[26];
    private readonly Dictionary<string, Entry> byWord = new(StringComparer.Ordinal);

    public int WordCount => byWord.Count;

    public IntelligentLetterPoolPlanner(IEnumerable<string> dictionary)
    {
        for (int i = 0; i < byLength.Length; i++) byLength[i] = new List<Entry>();
        for (int i = 0; i < byLetter.Length; i++) byLetter[i] = new List<Entry>();
        if (dictionary == null) return;
        foreach (string source in dictionary)
        {
            if (string.IsNullOrWhiteSpace(source)) continue;
            string word = source.Trim().ToUpperInvariant();
            if (word.Length < 3 || word.Length > 16 || byWord.ContainsKey(word)) continue;
            var counts = new byte[26];
            int vowels = 0;
            bool valid = true;
            foreach (char c in word)
            {
                int index = c - 'A';
                if (index < 0 || index >= 26) { valid = false; break; }
                counts[index]++;
                if ("AEIOU".IndexOf(c) >= 0) vowels++;
            }
            if (!valid || vowels < (word.Length >= 5 ? 2 : 1) ||
                vowels * 4 < word.Length) continue;
            var indices = new List<byte>();
            var quantities = new List<byte>();
            for (byte i = 0; i < 26; i++)
            {
                if (counts[i] == 0) continue;
                indices.Add(i);
                quantities.Add(counts[i]);
            }
            var entry = new Entry { Word = word, Indices = indices.ToArray(), Counts = quantities.ToArray() };
            byWord.Add(word, entry);
            byLength[word.Length].Add(entry);
            foreach (byte i in entry.Indices)
                if (Difficult.IndexOf((char)('A' + i)) >= 0) byLetter[i].Add(entry);
        }
    }

    public Plan Build(IReadOnlyList<char> retained, int budget, string consumedWord,
        int requiredLength = 0, IReadOnlyCollection<string> requiredWords = null,
        int seed = 0, int preferredLength = 0)
    {
        if (budget <= 0 || budget > 16 || WordCount == 0) return null;
        int[] available = new int[26];
        int existing = 0;
        if (retained != null)
            foreach (char c in retained)
            {
                int i = char.ToUpperInvariant(c) - 'A';
                if (i >= 0 && i < 26) { available[i]++; existing++; }
            }
        if (existing + budget > 16) return null;
        int capacity = existing + budget;
        var rng = new Random(seed);
        // Keep Ultimates uncommon. The existing length/damage tiers are untouched.
        int roll = rng.Next(100);
        int preferred = preferredLength > 0 ? preferredLength :
            roll < 30 ? rng.Next(7, 10) : roll < 95 ? rng.Next(10, 14) : rng.Next(14, 17);
        preferred = Math.Min(capacity, Math.Max(3, preferred));
        int maxMain = Math.Min(capacity, Math.Max(13, Math.Max(preferred, requiredLength)));
        var priorities = new HashSet<string>(StringComparer.Ordinal);
        if (requiredWords != null)
            foreach (string word in requiredWords)
                if (!string.IsNullOrWhiteSpace(word)) priorities.Add(word.Trim().ToUpperInvariant());

        // A bounded, length-stratified sample plus difficult-letter candidates.
        // This avoids repeatedly scanning the entire dictionary on every attack.
        var candidates = new List<Entry>(1800);
        var seen = new HashSet<Entry>();
        for (int len = 3; len <= maxMain; len++)
            Sample(byLength[len], SamplePerLength, rng, candidates, seen);
        foreach (char c in Difficult)
        {
            int i = c - 'A';
            if (available[i] > 0)
                Sample(byLetter[i], SamplePerDifficultLetter, rng, candidates, seen, maxMain);
        }
        // Explicitly evaluate short words combining multiple difficult
        // leftovers (QUIZ is an example), rather than hoping random sampling hits.
        for (int len = 3; len <= Math.Min(5, capacity); len++)
            foreach (Entry e in byLength[len])
            {
                int rareReused = 0;
                foreach (byte i in e.Indices)
                    if (available[i] > 0 && Difficult.IndexOf((char)('A' + i)) >= 0) rareReused++;
                if (rareReused >= 2 && seen.Add(e)) candidates.Add(e);
            }
        foreach (string word in priorities)
            if (byWord.TryGetValue(word, out Entry e) && e.Word.Length <= capacity && seen.Add(e))
                candidates.Add(e);

        var shortlists = new List<Ranked>[4];
        for (int i = 0; i < shortlists.Length; i++) shortlists[i] = new List<Ranked>(ShortlistPerPriority);
        foreach (Entry entry in candidates)
        {
            if (entry.Word.Length > maxMain) continue;
            int missing = Measure(entry, available, out int reused, out int rareReused, out int rareNew);
            if (missing > budget) continue;
            int mask = Priority(entry, requiredLength, priorities);
            float score = MainScore(entry, preferred, reused, rareReused, rareNew, consumedWord)
                + (float)rng.NextDouble() * 2f;
            AddRanked(shortlists[mask], new Ranked { Entry = entry, Score = score, Missing = missing, Mask = mask });
        }
        // Rare edge case: if the sample missed all feasible entries, search
        // the cached index once. Never advertise an incomplete word.
        bool hasCandidate = false;
        foreach (var list in shortlists) hasCandidate |= list.Count != 0;
        if (!hasCandidate)
        {
            for (int len = 3; len <= maxMain; len++)
                foreach (Entry entry in byLength[len])
                {
                    int missing = Measure(entry, available, out int reused, out int rareReused, out int rareNew);
                    if (missing > budget) continue;
                    int mask = Priority(entry, requiredLength, priorities);
                    AddRanked(shortlists[mask], new Ranked { Entry = entry, Missing = missing, Mask = mask,
                        Score = MainScore(entry, preferred, reused, rareReused, rareNew, consumedWord) });
                }
        }

        Entry bestMain = null, bestSecond = null;
        float bestScore = float.NegativeInfinity;
        int bestMask = 0;
        int[] remaining = new int[26];
        foreach (var list in shortlists)
            foreach (Ranked primary in list)
            {
                Array.Copy(available, remaining, 26);
                RemoveAvailable(primary.Entry, remaining);
                int left = budget - primary.Missing;
                float singleScore = primary.Score + ObjectiveScore(primary.Mask)
                    + primary.Entry.Word.Length * .5f;
                if (singleScore > bestScore)
                {
                    bestScore = singleScore; bestMain = primary.Entry; bestSecond = null; bestMask = primary.Mask;
                }
                foreach (Entry second in candidates)
                {
                    if (primary.Entry.Word.Length + second.Word.Length > capacity) continue;
                    if (second.Word.Length > primary.Entry.Word.Length &&
                        Priority(second, requiredLength, priorities) == 0) continue;
                    int missing = Measure(second, remaining, out int reused, out int rareReused, out int rareNew);
                    if (missing > left) continue;
                    int mask = primary.Mask | Priority(second, requiredLength, priorities);
                    int unplanned = capacity - primary.Entry.Word.Length - second.Word.Length;
                    float score = primary.Score + ObjectiveScore(mask)
                        + primary.Entry.Word.Length * .5f + 5f + reused * 3f
                        + rareReused * 5f - rareNew * 2.5f - unplanned * 2f
                        + (second.Word == primary.Entry.Word ? -8f : 0f)
                        + (second.Word == consumedWord ? -5f : 0f);
                    if (score <= bestScore) continue;
                    bestScore = score; bestMain = primary.Entry; bestSecond = second; bestMask = mask;
                }
            }
        if (bestMain == null) return null;
        Array.Copy(available, remaining, 26);
        var newLetters = new List<char>(budget);
        int reusedTotal = AppendMissing(bestMain, remaining, newLetters);
        if (bestSecond != null) reusedTotal += AppendMissing(bestSecond, remaining, newLetters);
        if (newLetters.Count > budget) return null; // defensive invariant

        int vowelCount = available[0] + available[4] + available[8] + available[14] + available[20];
        foreach (char c in newLetters) if ("AEIOU".IndexOf(c) >= 0) vowelCount++;
        while (newLetters.Count < budget)
        {
            string source = vowelCount * 3 < existing + newLetters.Count + 1
                ? "EEEEAAAIIIOOU" : "RRRTTTNNNSSLLDDGGCCMMPPBBHHFF";
            char c = source[rng.Next(source.Length)];
            newLetters.Add(c);
            if ("AEIOU".IndexOf(c) >= 0) vowelCount++;
        }
        for (int i = newLetters.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (newLetters[i], newLetters[j]) = (newLetters[j], newLetters[i]);
        }
        return new Plan { MainWord = bestMain.Word, RemainderWord = bestSecond?.Word ?? "",
            NewLetters = newLetters, ReusedLetters = reusedTotal, PreferredLength = preferred, ObjectiveMask = bestMask };
    }

    private static void Sample(List<Entry> source, int limit, Random rng, List<Entry> output,
        HashSet<Entry> seen, int maxLength = 16)
    {
        if (source.Count <= limit)
        {
            foreach (Entry e in source) if (e.Word.Length <= maxLength && seen.Add(e)) output.Add(e);
            return;
        }
        // Fixed sample work even for large buckets. Small/rare buckets are exhaustive.
        for (int i = 0; i < limit; i++)
        {
            Entry e = source[rng.Next(source.Count)];
            if (e.Word.Length <= maxLength && seen.Add(e)) output.Add(e);
        }
    }

    private static int Priority(Entry e, int length, HashSet<string> words) =>
        (e.Word.Length == length ? 1 : 0) | (words.Contains(e.Word) ? 2 : 0);
    private static int ObjectiveScore(int mask) => ((mask & 1) != 0 ? 1000 : 0) + ((mask & 2) != 0 ? 1000 : 0);
    private static float MainScore(Entry e, int preferred, int reused, int rareReused, int rareNew, string consumed) =>
        -Math.Abs(e.Word.Length - preferred) * 5f + reused * 3f + rareReused * 5f
        - rareNew * 2.5f - (e.Word == consumed ? 10f : 0f);

    private static void AddRanked(List<Ranked> list, Ranked candidate)
    {
        int index = 0;
        while (index < list.Count && list[index].Score >= candidate.Score) index++;
        if (index >= ShortlistPerPriority) return;
        list.Insert(index, candidate);
        if (list.Count > ShortlistPerPriority) list.RemoveAt(list.Count - 1);
    }

    private static int Measure(Entry e, int[] available, out int reused, out int rareReused, out int rareNew)
    {
        reused = rareReused = rareNew = 0;
        int missing = 0;
        for (int k = 0; k < e.Indices.Length; k++)
        {
            int i = e.Indices[k];
            int use = Math.Min(available[i], e.Counts[k]);
            int need = e.Counts[k] - use;
            reused += use; missing += need;
            if (Difficult.IndexOf((char)('A' + i)) >= 0) { rareReused += use; rareNew += need; }
        }
        return missing;
    }

    private static void RemoveAvailable(Entry e, int[] available)
    {
        for (int k = 0; k < e.Indices.Length; k++)
            available[e.Indices[k]] = Math.Max(0, available[e.Indices[k]] - e.Counts[k]);
    }

    private static int AppendMissing(Entry e, int[] available, List<char> output)
    {
        int reused = 0;
        for (int k = 0; k < e.Indices.Length; k++)
        {
            int i = e.Indices[k];
            int use = Math.Min(available[i], e.Counts[k]);
            available[i] -= use; reused += use;
            for (int n = use; n < e.Counts[k]; n++) output.Add((char)('A' + i));
        }
        return reused;
    }
}
