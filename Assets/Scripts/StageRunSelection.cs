using UnityEngine.SceneManagement;

public static class StageRunSelection
{
    private const string DefaultStageSceneName = "SampleScene";
    private const string DefaultStageTitle = "Stage 1: Enchanted Kingdom";

    public static int SelectedStageIndex { get; private set; } = 0;
    public static string SelectedStageSceneName { get; private set; } = DefaultStageSceneName;
    public static string SelectedStageTitle { get; private set; } = DefaultStageTitle;

    public static void SetSelectedStage(int stageIndex, string sceneName, string title)
    {
        SelectedStageIndex = stageIndex;
        SelectedStageSceneName = string.IsNullOrWhiteSpace(sceneName) ? DefaultStageSceneName : sceneName;
        SelectedStageTitle = string.IsNullOrWhiteSpace(title) ? SelectedStageSceneName : title;
    }

    public static void LoadSelectedStage()
    {
        string sceneName = string.IsNullOrWhiteSpace(SelectedStageSceneName) ? DefaultStageSceneName : SelectedStageSceneName;
        SceneManager.LoadScene(sceneName);
    }
}
