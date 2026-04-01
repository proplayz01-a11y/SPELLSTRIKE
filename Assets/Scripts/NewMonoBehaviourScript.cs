using UnityEngine;
using UnityEngine.SceneManagement;

public class AdventureButtonHandlers : MonoBehaviour
{
  
    public void LoadScene(string sceneName)
    {
        // Load the comic intro scene
        SceneManager.LoadScene(sceneName);
    }

 
    
}
