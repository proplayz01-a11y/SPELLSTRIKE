using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuSpellbookEntryView : MonoBehaviour
{
    [SerializeField] private Image accent;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private TMP_Text metadataText;
    [SerializeField] private TMP_Text definitionText;

    public void ConfigureReferences(
        Image accentImage,
        TMP_Text wordLabel,
        TMP_Text metadataLabel,
        TMP_Text definitionLabel)
    {
        accent = accentImage;
        wordText = wordLabel;
        metadataText = metadataLabel;
        definitionText = definitionLabel;
    }

    public void SetEntry(SpellbookEntry entry)
    {
        if (entry == null)
            return;

        string rarity = string.IsNullOrWhiteSpace(entry.rarity)
            ? "UNCLASSIFIED"
            : entry.rarity.Trim().ToUpperInvariant();
        int timesUsed = Mathf.Max(1, entry.timesUsed);

        if (wordText != null)
            wordText.text = entry.word;

        if (metadataText != null)
        {
            metadataText.text =
                $"{rarity}  •  {entry.length} LETTERS  •  USED {timesUsed}x";
            metadataText.color = GetRarityColor(rarity);
        }

        if (definitionText != null)
        {
            definitionText.text = string.IsNullOrWhiteSpace(entry.definition)
                ? "Definition unavailable in the gameplay dictionary."
                : entry.definition;
        }

        if (accent != null)
            accent.color = GetRarityColor(rarity);
    }

    private static Color GetRarityColor(string rarity)
    {
        switch (rarity)
        {
            case "MYTHIC":
                return new Color(0.86f, 0.48f, 1f, 1f);
            case "RARE":
                return new Color(0.30f, 0.72f, 1f, 1f);
            case "UNCOMMON":
                return new Color(0.38f, 0.86f, 0.56f, 1f);
            case "COMMON":
                return new Color(0.82f, 0.84f, 0.88f, 1f);
            default:
                return new Color(0.95f, 0.70f, 0.22f, 1f);
        }
    }
}
