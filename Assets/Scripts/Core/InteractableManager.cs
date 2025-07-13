using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ZombieGame.Core
{
    [ExecuteInEditMode]
    public class InteractableManager : MonoBehaviour
    {
        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        private void OnHierarchyChanged()
        {
            if (!Application.isPlaying)
            {
                SetupInteractables();
            }
        }

        private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            SetupInteractables();
        }

        private void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            SetupInteractables();
        }

        // private void OnValidate()
        // {
        //     SetupInteractables();
        // }

        private void SetupInteractables()
        {
            // Find all GameObjects in the scene (including inactive ones)
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            
            // Filter to only scene objects (not prefabs in project)
            var sceneObjects = System.Array.FindAll(allObjects, obj => 
                obj.scene.IsValid() && obj.hideFlags == HideFlags.None);
            
            // Handle all objects with Interactable tag
            foreach (var obj in sceneObjects)
            {
                if (obj.CompareTag("Interactable"))
                {
                    var interactable = obj.GetComponent<Interactable>();
                    if (interactable == null)
                    {
                        interactable = obj.AddComponent<Interactable>();
                        Debug.Log($"Added Interactable component to tagged object: {obj.name}");
                        
                        // Mark the scene as dirty so the change gets saved
                        EditorUtility.SetDirty(obj);
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.MarkSceneDirty(obj.scene);
                        }
                    }
                }
            }

            // Find all root objects in scene for vehicle handling
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            
            // Handle vehicles
            foreach (var root in rootObjects)
            {   
                // Check if this is a vehicle parent (has Vehicle layer but parent is not a Vehicle)
                if (root.layer == LayerMask.NameToLayer("Vehicles") && 
                    (root.transform.parent == null || root.transform.parent.gameObject.layer != LayerMask.NameToLayer("Vehicles")))
                {
                    var interactable = root.GetComponent<Interactable>();
                    if (interactable == null)
                    {
                        interactable = root.AddComponent<Interactable>();
                        interactable.Type = Interactable.InteractableType.Vehicle;
                        Debug.Log($"Added Vehicle Interactable to root object: {root.name}");
                        
                        // Mark the scene as dirty
                        EditorUtility.SetDirty(root);
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.MarkSceneDirty(root.scene);
                        }
                    }
                    else if (interactable.Type != Interactable.InteractableType.Vehicle)
                    {
                        interactable.Type = Interactable.InteractableType.Vehicle;
                        Debug.Log($"Updated {root.name}'s Interactable type to Vehicle");
                        
                        // Mark the scene as dirty
                        EditorUtility.SetDirty(root);
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.MarkSceneDirty(root.scene);
                        }
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(InteractableManager))]
    public class InteractableManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This component automatically manages the Interactable component:\n\n" +
                                  "- If tag is 'Interactable': Adds the Interactable component\n" +
                                  "- For Vehicle layer: Adds Vehicle type Interactable to parent objects\n" +
                                  "(Only runs in editor, not during gameplay)", 
                                  MessageType.Info);
        }
    }
}
#endif 