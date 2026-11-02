using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Returns from stage selection to the canonical main menu.</summary>
[RequireComponent(typeof(Button))]
public sealed class StageSelectMainMenuButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }
}
