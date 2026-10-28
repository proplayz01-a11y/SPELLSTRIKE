#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor-only, visible UI for personally testing the vocabulary review.
/// This launcher is attached only in StageSelectBook_VocabularyReviewArt_Isolated.
/// It does not manage progression, write PlayerPrefs, or record review results.
/// </summary>
[DefaultExecutionOrder(8500)]
public sealed class StageVocabularyReviewManualTestLauncher : MonoBehaviour
{
    private const string IsolatedScene =
        "Assets/Scenes/StageSelectBook_VocabularyReviewArt_Isolated.unity";

    [SerializeField] private StageVocabularyReviewController reviewController;
    [SerializeField] private StageVocabularyProfile stage1Profile;
    [SerializeField] private StageVocabularyProfile stage2Profile;
    [SerializeField] private StageVocabularyProfile stage3Profile;
    [SerializeField] private bool automaticallyStartStage1 = true;

    private int selectedStage = 1;
    private string message = "Select a stage to test its real vocabulary questions.";

    private IEnumerator Start()
    {
        if (!IsSafeTestScene())
            yield break;

        // Start after StageSelectManager's normal initialization. The real
        // selection/progression path remains untouched and inactive.
        yield return null;
        if (automaticallyStartStage1)
            StartStage(1);
    }

    private bool IsSafeTestScene()
    {
        return Application.isPlaying &&
            SceneManager.GetActiveScene().path == IsolatedScene &&
            !StageVocabularyReviewPendingState.HasPendingReview;
    }

    private void StartStage(int stage)
    {
        if (!IsSafeTestScene() || reviewController == null)
        {
            message = "Preview blocked: active mandatory review or wrong scene.";
            return;
        }

        StageVocabularyProfile profile = stage switch
        {
            1 => stage1Profile,
            2 => stage2Profile,
            3 => stage3Profile,
            _ => null
        };
        if (profile == null)
        {
            message = "Stage profile reference is missing.";
            return;
        }

        reviewController.gameObject.SetActive(true);
        if (!reviewController.BeginSandboxReview(profile))
        {
            message = "Preview could not start. Check Console and profiles.";
            return;
        }

        selectedStage = stage;
        message = "Sandbox only: answers and stage progress are NOT saved.";
    }

    private void OnGUI()
    {
        if (!Application.isPlaying ||
            SceneManager.GetActiveScene().path != IsolatedScene)
            return;

        // Keep the launcher in the black exterior, away from the navy
        // vocabulary panel, so it cannot obscure answers or Continue.
        GUILayout.BeginArea(new Rect(8f, 16f, 235f, 295f),
            "VOCAB UI TEST", GUI.skin.window);
        GUILayout.Label("Play the actual questions:");

        bool safe = IsSafeTestScene();
        GUI.enabled = safe;
        if (GUILayout.Button("Stage 1 - Kingdom", GUILayout.Height(34)))
            StartStage(1);
        if (GUILayout.Button("Stage 2 - Seas", GUILayout.Height(34)))
            StartStage(2);
        if (GUILayout.Button("Stage 3 - Ruins", GUILayout.Height(34)))
            StartStage(3);
        if (GUILayout.Button("Restart Stage " + selectedStage, GUILayout.Height(30)))
            StartStage(selectedStage);
        GUI.enabled = true;

        if (reviewController != null && reviewController.ReviewCompleted)
            GUILayout.Label("Finished! Choose another stage.");
        else
            GUILayout.Label(message);
        GUILayout.EndArea();
    }
}
#endif
