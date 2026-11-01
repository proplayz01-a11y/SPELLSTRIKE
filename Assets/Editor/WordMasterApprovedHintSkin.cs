using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the approved black-body hint art and Cinzel typography to the
/// *currently open* WordMasterScene without rebuilding its UI or changing
/// the user's existing RectTransforms.
/// </summary>
public static class WordMasterApprovedHintSkin
{
    private const string WordMasterScene = "Assets/Scenes/WordMasterScene.unity";
    private const string AssetRoot = "Assets/Minigames/WordMasterIntegration/Resources/WordMasterRoyal/";
    private const string CinzelPath = "Assets/Fonts/SPELLSTRIKE/Cinzel-Variable SDF.asset";

    [MenuItem("SPELLSTRIKE/Word Master/Apply Approved Hint Panel and Cinzel")]
    public static void ApplyToOpenScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[WordMaster Hint] Exit Play Mode before applying scene changes.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != WordMasterScene)
        {
            Debug.LogError("[WordMaster Hint] Open WordMasterScene before applying the skin.");
            return;
        }

        Sprite main = AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot + "HintDarkOrnateMain.png");
        Sprite popup = AssetDatabase.LoadAssetAtPath<Sprite>(AssetRoot + "HintDarkOrnatePopup.png");
        TMP_FontAsset cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CinzelPath);
        if (main == null || popup == null || cinzel == null)
        {
            Debug.LogError("[WordMaster Hint] Missing new panel sprite(s) or the existing Cinzel TMP asset. No changes applied.");
            return;
        }

        WordMasterRoyalUI royal = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            royal = root.GetComponentInChildren<WordMasterRoyalUI>(true);
            if (royal != null) break;
        }
        if (royal == null)
        {
            Debug.LogError("[WordMaster Hint] WordMasterRoyalUI is missing. No changes applied.");
            return;
        }

        Transform layout = royal.transform.Find("RoyalLayout");
        Transform hint = layout?.Find("HintPanelGroup");
        Image art = hint?.Find("HintParchment")?.GetComponent<Image>();
        if (layout == null || hint == null || art == null)
        {
            Debug.LogError("[WordMaster Hint] Existing serialized hint layout is incomplete. No changes applied.");
            return;
        }

        // Backup the on-disk scene before any live changes. The user's unsaved
        // Editor state stays in memory and is preserved when saving below.
        string backupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads", "WORD MASTER", "WordMaster_HintPanel_Backup_20261101_1719");
        Directory.CreateDirectory(backupDirectory);
        string backupPath = Path.Combine(backupDirectory,
            "WordMasterScene_before_editor_apply_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        File.Copy(Path.Combine(projectRoot, scene.path), backupPath);

        Undo.RecordObject(royal, "Assign Word Master Cinzel");
        SerializedObject serialized = new SerializedObject(royal);
        SerializedProperty fontField = serialized.FindProperty("cinzelFont");
        if (fontField == null)
        {
            Debug.LogError("[WordMaster Hint] WordMasterRoyalUI.cinzelFont field not found. No changes applied.");
            return;
        }
        fontField.objectReferenceValue = cinzel;
        serialized.ApplyModifiedProperties();

        Undo.RecordObject(art, "Replace Word Master Hint Artwork");
        art.sprite = main;
        art.type = Image.Type.Simple;
        art.preserveAspect = false;
        art.color = Color.white;
        art.raycastTarget = false;

        foreach (string obsolete in new[] { "HintOutsideGlow", "HintDivider" })
        {
            Image overlay = hint.Find(obsolete)?.GetComponent<Image>();
            if (overlay == null) continue;
            Undo.RecordObject(overlay, "Remove Obsolete Hint Overlay");
            overlay.enabled = false;
        }

        Transform closeX = hint.Find("HintCloseX");
        if (closeX != null)
        {
            Undo.RecordObject(closeX.gameObject, "Hide Obsolete Hint Close X");
            closeX.gameObject.SetActive(false);
        }

        int textCount = 0;
        foreach (TextMeshProUGUI label in layout.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            Undo.RecordObject(label, "Set Word Master Cinzel Font");
            label.font = cinzel;
            if (label.name == "HintTitle" && label.transform.parent == hint)
                label.color = new Color(1f, .82f, .43f, 1f);
            else if (label.name == "HintMessage" && label.transform.parent == hint)
                label.color = new Color(1f, .95f, .84f, 1f);
            EditorUtility.SetDirty(label);
            textCount++;
        }

        EditorUtility.SetDirty(royal);
        EditorUtility.SetDirty(art);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("[WordMaster Hint] Could not save WordMasterScene. Check the Console.");
            return;
        }

        Debug.Log("[WordMaster Hint] Applied black-body ornate panel, removed obsolete " +
            "overlay graphics, and assigned Cinzel to " + textCount +
            " serialized TMP labels. Runtime Turns/Prize/Invalid hints use " +
            "HintDarkOrnatePopup. Scene saved. Backup: " + backupPath);
    }
}
