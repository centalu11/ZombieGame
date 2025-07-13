using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using ZombieGame.Input;
using ZombieGame.Player.Vehicle;
using ZombieGame.Player.Animations;

namespace ZombieGame.Player
{
    /// <summary>
    /// Single entry point for loading all required player components.
    /// Add this component to automatically set up all necessary player scripts.
    /// Supports any IPlayerVehicleController implementation (MVCPlayerVehicleController, etc.)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(ZombieGameInputs))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerBasicController))]
    [RequireComponent(typeof(PlayerInteractionManager))]
    // Vehicle controller will be validated in OnValidate - any IPlayerVehicleController implementation is acceptable
    [RequireComponent(typeof(CrosshairController))]
    [RequireComponent(typeof(CameraController))]
    [RequireComponent(typeof(EyeAnimationHandler))]
    public class PlayerLoader : MonoBehaviour
    {
        private void OnValidate()
        {
            SetupPlayerInput();
            
        }

        private void Awake()
        {
            ValidateVehicleController();
        }
        
        private void ValidateVehicleController()
        {
            // Check if any IPlayerVehicleController implementation is present
            var vehicleController = GetComponent<IPlayerVehicleController>();
            if (vehicleController == null)
            {
                Debug.LogError($"[PlayerLoader] No IPlayerVehicleController implementation found on {gameObject.name}. " +
                    "Please add MVCPlayerVehicleController component.");
            }
            
            // Check for multiple implementations (not recommended)
            var allControllers = GetComponents<IPlayerVehicleController>();
            if (allControllers.Length > 1)
            {
                Debug.LogError($"[PlayerLoader] Multiple IPlayerVehicleController implementations found on {gameObject.name}. " +
                    "Only one should be used at a time.");
            }
        }

        private void SetupPlayerInput()
        {
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                // Direct reference to the asset
                playerInput.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Scripts/Input/ZombieGameInputs.inputactions");
                playerInput.defaultActionMap = "Player";
                playerInput.notificationBehavior = PlayerNotifications.SendMessages;
            }
        }
    }
} 