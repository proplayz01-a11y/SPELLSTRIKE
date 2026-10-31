using System.Collections.Generic;

/// <summary>Current source-letter inventory and selected tile indices, independent of Unity UI.</summary>
public sealed class MinigameTileSelection
{
    public char[] SourceLetters { get; set; } = new char[0];
    public List<int> SelectedIndices { get; } = new List<int>();

    public void Configure(string letters)
    {
        SourceLetters = string.IsNullOrWhiteSpace(letters) ? new char[0] : letters.ToUpperInvariant().ToCharArray();
        SelectedIndices.Clear();
    }

    public void Clear() => SelectedIndices.Clear();
}
