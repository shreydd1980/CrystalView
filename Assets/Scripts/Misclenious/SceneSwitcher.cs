using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public void SwitchScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    // Keep the original method for backward compatibility
    public void SwitchScene()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
    
    public string sceneToLoad; // Optional field for Inspector usage
}