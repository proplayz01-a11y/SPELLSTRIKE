using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the baked parchment artwork for the stage chosen in Stage Select.
/// Only the StagePage background sprite and existing live title are updated;
/// the potion UI, inventory, equipment and navigation remain untouched.
/// StageRunSelection uses zero-based indexes: Stages 1/2/3 = 0/1/2.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class WizardsVaultStagePagePresenter : MonoBehaviour
{
    [SerializeField] private Sprite enchantedKingdomPage;
    [SerializeField] private Sprite sunkenSeasPage;
    [SerializeField] private Sprite mythicRuinsPage;
    [SerializeField] private Image stagePageImage;
    [SerializeField] private TMP_Text stageTitleText;

    private void Awake() => ResolveReferences();

    private void Start() => ApplySelectedStage();

    // Used by the isolated editor assembly and by non-destructive QA tests.
    public void Configure(Sprite stageOne, Sprite stageTwo)
    {
        Configure(stageOne, stageTwo, mythicRuinsPage);
    }

    public void Configure(Sprite stageOne, Sprite stageTwo, Sprite stageThree)
    {
        enchantedKingdomPage = stageOne;
        sunkenSeasPage = stageTwo;
        mythicRuinsPage = stageThree;
        ResolveReferences();

        // Avoid showing Stage 2 art while authoring the default Stage 1 page.
        if (!Application.isPlaying && stagePageImage != null && enchantedKingdomPage != null)
            stagePageImage.sprite = enchantedKingdomPage;
    }

    public void ApplySelectedStage()
    {
        ResolveReferences();

        Sprite selected = null;
        switch (StageRunSelection.SelectedStageIndex)
        {
            case 0: selected = enchantedKingdomPage; break;
            case 1: selected = sunkenSeasPage; break;
            case 2: selected = mythicRuinsPage; break;
        }

        if (stagePageImage != null && selected != null)
        {
            stagePageImage.sprite = selected;
            stagePageImage.color = Color.white;
            stagePageImage.type = Image.Type.Simple;
        }

        // ItemSelectionSceneController already updates this title in RefreshUI.
        // Synchronizing it here also makes the isolated preview useful for QA
        // without requiring a Stage Select scene transition.
        if (stageTitleText != null)
            stageTitleText.text = StageRunSelection.SelectedStageTitle;
    }

    private void ResolveReferences()
    {
        if (stagePageImage == null)
            stagePageImage = GetComponent<Image>();
        if (stageTitleText == null)
            stageTitleText = transform.Find("StageTitle")?.GetComponent<TMP_Text>();
    }
}
