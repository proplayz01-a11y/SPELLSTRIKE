using UnityEngine;
using UnityEngine.SceneManagement;

public class AdventureButtonHandler : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[MainMenu] Cannot load an empty scene name.");
            return;
        }

        Debug.Log($"[MainMenu] Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
