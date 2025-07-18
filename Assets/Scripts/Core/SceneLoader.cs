using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZombieGame.Core
{
    public class SceneLoader : MonoBehaviour
    {
    #if UNITY_EDITOR
        [SerializeField] private SceneAsset playerScene;
        [SerializeField] private string playerSceneName = "PlayerScene";
        
        private void OnValidate()
        {
            if (playerScene != null)
            {
                playerSceneName = playerScene.name;
            }
        }
    #else
        [SerializeField] private string playerSceneName = "PlayerScene";
    #endif
        
    #if UNITY_EDITOR
        private void Reset()
        {
            // Set default values when component is added or reset
            if (playerScene == null)
            {
                playerScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Shared/PlayerScene.unity");
            }
        }
    #endif
        
        private void Start()
        {
            LoadPlayerScene();
        }

        private void LoadPlayerScene()
        {
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
}