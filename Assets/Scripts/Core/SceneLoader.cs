using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneAsset playerScene;
    private string playerSceneName => playerScene != null ? playerScene.name : "";

    [SerializeField] private SceneAsset networkScene;
    private string networkSceneName => networkScene != null ? networkScene.name : "";
    
#if UNITY_EDITOR
    private void Reset()
    {
        // Set default values when component is added or reset
        if (playerScene == null)
        {
            playerScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Shared/PlayerScene.unity");
        }
        
        if (networkScene == null)
        {
            networkScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Shared/NetworkScene.unity");
        }
    }
#endif
    
    private void Start()
    {
        LoadNetworkScene();
        LoadPlayerScene();
    }

    private void LoadNetworkScene()
    {
        if (networkScene == null)
        {
            Debug.LogError("Network Scene not assigned in SceneLoader!");
            return;
        }

        // Check if player scene is already loaded
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == networkSceneName)
            {
                return; // Player scene is already loaded
            }
        }

        // Load player scene additively
        SceneManager.LoadSceneAsync(networkSceneName, LoadSceneMode.Additive);
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