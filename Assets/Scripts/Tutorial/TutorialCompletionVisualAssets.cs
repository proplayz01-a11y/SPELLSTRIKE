using TMPro;
using UnityEngine;

/// <summary>
/// Scene-authored references for the approved tutorial reward artwork.
/// Keeps the runtime-created completion modal lightweight and build-safe:
/// no Resources duplicates, editor-only asset lookups or extra cameras.
/// </summary>
public sealed class TutorialCompletionVisualAssets : MonoBehaviour
{
    [Header("Parchment (trimmed v02 sprites)")]
    [SerializeField] private Sprite panelShadow;
    [SerializeField] private Sprite parchmentFill;
    [SerializeField] private Sprite panelFrame;
    [SerializeField] private Sprite headerDivider;

    [Header("Spellbook and independent depth")]
    [SerializeField] private Sprite groundShadow;
    [SerializeField] private Sprite castShadow;
    [SerializeField] private Sprite halo;
    [SerializeField] private Sprite orbitBack;
    [SerializeField] private Sprite spellbook;
    [SerializeField] private Sprite runeGlow;
    [SerializeField] private Sprite orbitFront;
    [SerializeField] private Sprite sparkles;

    [Header("Reusable buttons and their shadows")]
    [SerializeField] private Sprite continueShadow;
    [SerializeField] private Sprite mainMenuShadow;
    [SerializeField] private Sprite continueWordMaster;
    [SerializeField] private Sprite continueWordMasterHover;
    [SerializeField] private Sprite mainMenuOriginal;

    [Header("Existing project font assets")]
    [SerializeField] private TMP_FontAsset cinzelFont;
    [SerializeField] private TMP_FontAsset alegreyaItalicFont;

    public Sprite PanelShadow => panelShadow;
    public Sprite ParchmentFill => parchmentFill;
    public Sprite PanelFrame => panelFrame;
    public Sprite HeaderDivider => headerDivider;
    public Sprite GroundShadow => groundShadow;
    public Sprite CastShadow => castShadow;
    public Sprite Halo => halo;
    public Sprite OrbitBack => orbitBack;
    public Sprite Spellbook => spellbook;
    public Sprite RuneGlow => runeGlow;
    public Sprite OrbitFront => orbitFront;
    public Sprite Sparkles => sparkles;
    public Sprite ContinueShadow => continueShadow;
    public Sprite MainMenuShadow => mainMenuShadow;
    public Sprite ContinueWordMaster => continueWordMaster;
    public Sprite ContinueWordMasterHover => continueWordMasterHover;
    public Sprite MainMenuOriginal => mainMenuOriginal;
    public TMP_FontAsset CinzelFont => cinzelFont;
    public TMP_FontAsset AlegreyaItalicFont => alegreyaItalicFont;
}
