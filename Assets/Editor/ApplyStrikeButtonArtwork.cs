using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Re-skins the existing combat AttackButton in SampleScene without replacing
/// its Button component, UnityEvent, hierarchy, or responsive RectTransform.
/// It is deliberately an explicit Editor menu action, not an automatic edit.
/// </summary>
internal static class ApplyStrikeButtonArtwork
{
    private const string SpritePath =
        "Assets/UI/StrikeButton/StrikeButton_OrnateBlank.png";
    private const string FontPath =
        "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset";

    [MenuItem("SPELLSTRIKE/HUD/Apply STRIKE Button Artwork", priority = 226)]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[STRIKE] Exit Play Mode before changing the button.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded ||
            scene.path != "Assets/Scenes/SampleScene.unity")
        {
            Debug.LogWarning("[STRIKE] Open production SampleScene in Edit Mode.");
            return;
        }

        var matches = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "AttackButton")
            .ToArray();

        if (matches.Length != 1 ||
            matches[0].GetComponent<Button>() is not Button button ||
            matches[0].GetComponent<Image>() is not Image image)
        {
            Debug.LogError("[STRIKE] Exactly one existing UI AttackButton is required.");
            return;
        }

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length != 1 ||
            (labels[0].text != "Attack" && labels[0].text != "STRIKE"))
        {
            Debug.LogError("[STRIKE] Unexpected button label; no changes made.");
            return;
        }

        bool hasAttackBinding = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            var target = button.onClick.GetPersistentTarget(i);
            if (target != null && target.GetType().Name == "AttackController" &&
                button.onClick.GetPersistentMethodName(i) == "ExecuteAttack")
            {
                hasAttackBinding = true;
                break;
            }
        }
        if (!hasAttackBinding)
        {
            Debug.LogError("[STRIKE] ExecuteAttack binding was not found; stopped safely.");
            return;
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (sprite == null || font == null)
        {
            Debug.LogError("[STRIKE] Art sprite or Cinzel font is not imported.");
            return;
        }

        TMP_Text label = labels[0];
        Undo.RecordObjects(new Object[] { image, label },
            "Apply Ornate STRIKE Button Skin");

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = true;

        // Keep typography LIVE instead of baking it into the exported PNG.
        label.text = "STRIKE";
        label.font = font;
        label.fontSize = 24f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.91f, 0.7f, 1f);
        label.characterSpacing = 3f;
        label.raycastTarget = false;

        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(label);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[STRIKE] Updated existing AttackButton art and live text. " +
                  "ExecuteAttack, Button component, original RectTransform " +
                  "and parent hierarchy were preserved. " +
                  "Review in Game View, then save SampleScene.", button);
        Selection.activeGameObject = button.gameObject;
    }

    [MenuItem("SPELLSTRIKE/HUD/Apply STRIKE Button Artwork", true)]
    private static bool CanApply()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path ==
                   "Assets/Scenes/SampleScene.unity";
    }
}
