using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneAsset playerScene;
    private string playerSceneName => playerScene != null ? playerScene.name : "";
    
    private void Start()
    {
        LoadPlayerScene();
    }

    private void LoadPlayerScene()
    {
        if (playerScene == null)
        {
            Debug.LogError("Player Scene not assigned in SceneLoader!");
            return;
        }

        // Check if player scene is already loaded
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == playerSceneName)
            {
                return; // Player scene is already loaded
            }
        }

        // Load player scene additively
        SceneManager.LoadSceneAsync(playerSceneName, LoadSceneMode.Additive);
    }
} 