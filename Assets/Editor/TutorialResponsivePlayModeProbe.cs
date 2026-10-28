#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class TutorialResponsivePlayModeProbe
{
    private const string ArmedKey = "SpellStrike_TutorialResponsiveProbe_Armed";
    private const string PreviousStartSceneKey = "SpellStrike_TutorialResponsiveProbe_PreviousStartScene";

    static TutorialResponsivePlayModeProbe()
    {
        if (SessionState.GetBool(ArmedKey, false))
            return;

        SceneAsset previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(PreviousStartSceneKey, previous != null ? AssetDatabase.GetAssetPath(previous) : string.Empty);
        EditorSceneManager.playModeStartScene = null;
        SessionState.SetBool(ArmedKey, true);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Debug.Log("[TutorialResponsiveProbe] Temporarily cleared Play Mode Start Scene so the current dirty TutorialScene can be tested directly.");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ArmedKey, false))
            return;

        string previousPath = SessionState.GetString(PreviousStartSceneKey, string.Empty);
        EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
        SessionState.EraseBool(ArmedKey);
        SessionState.EraseString(PreviousStartSceneKey);
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        Debug.Log("[TutorialResponsiveProbe] Restored the previous Play Mode Start Scene.");
    }
}
#endif
