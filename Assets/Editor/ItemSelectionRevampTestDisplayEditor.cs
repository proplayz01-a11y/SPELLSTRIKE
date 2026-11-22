using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Test-prefab layout controls. Does not change the passive inventory or
/// production Wizard's Vault. Sample names are editor preview only.
/// </summary>
[CustomEditor(typeof(ItemSelectionRevampTestDisplay))]
internal sealed class ItemSelectionRevampTestDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox(
            "Edit the TEST prefab in Prefab Mode (double-click it). " +
            "For each EquippedSlot, expand EquippedLayout. Select ItemIcon to " +
            "change RectTransform Width/Height/Pos X/Pos Y; select ItemName " +
            "to change RectTransform and TextMeshPro font size. " +
            "The runtime changes only the displayed item name, icon sprite, " +
            "and empty/equipped visibility — NOT your size or position. " +
            "Edit Mode adjustments are saved; Play Mode adjustments are temporary.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview 3 Equipped Items"))
                Preview(true);
            if (GUILayout.Button("Preview Empty Slots"))
                Preview(false);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }

    private void Preview(bool occupied)
    {
        var display = (ItemSelectionRevampTestDisplay)target;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Item Selection] Edit-mode preview only. Stop Play Mode first.");
            return;
        }

        const string label = "Preview Item Selection Slot Layout";
        // Record GameObjects (active toggles) and content components (sample
        // text/sprite) so the preview can be undone without affecting layout.
        foreach (Transform child in display.GetComponentsInChildren<Transform>(true))
        {
            Undo.RecordObject(child.gameObject, label);
            if (child.TryGetComponent(out TMP_Text text))
                Undo.RecordObject(text, label);
            if (child.TryGetComponent(out Image image))
                Undo.RecordObject(image, label);
        }
        Undo.RecordObject(display, label);
        display.PreviewLayout(occupied);
        if (display.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(display.gameObject.scene);
        SceneView.RepaintAll();
        Repaint();
    }
}
