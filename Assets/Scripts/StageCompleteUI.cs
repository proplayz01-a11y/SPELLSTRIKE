using UnityEngine;
using UnityEngine.SceneManagement;

public class StageCompleteUI : MonoBehaviour
{
    [Header("Panel Reference")]
    public GameObject stageCompletePanel;
    public GameObject dimOverlay;

    private void Start()
    {
        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(false);

        if (dimOverlay != null)
            dimOverlay.SetActive(false);
    }

    public void ShowStageComplete()
    {
        if (dimOverlay != null)
            dimOverlay.SetActive(true);

        if (stageCompletePanel != null)
            stageCompletePanel.SetActive(true);

        Time.timeScale = 0f;
        Debug.Log("[StageCompleteUI] Stage complete panel shown. Game paused.");
    }

    public void OnContinuePressed()
    {
        Time.timeScale = 1f;
        Debug.Log("[StageCompleteUI] Continuing to Stage Select.");
        SceneManager.LoadScene("StageSelectScene");
    }
}