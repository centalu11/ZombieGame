using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using ZombieGame.Core;
using ZombieGame.Input;
using ZombieGame.Player.Vehicle;

namespace ZombieGame.Player
{
    // [RequireComponent(typeof(FishNet.Component.Transforming.NetworkTransform))]
    public class PlayerNetworkController : NetworkBehaviour
    {
        [Header("Network Settings")]
        public readonly SyncVar<string> playerName = new SyncVar<string>("Player");
        public readonly SyncVar<Color> playerColor = new SyncVar<Color>(Color.white);

        private PlayerBasicController _playerBasicController;
        private PlayerInteractionManager _playerInteractionManager;
        private CrosshairController _crosshairController;
        private CameraController _cameraController;
        private MVCPlayerVehicleController _vehicleController;
        private ZombieGameInputs _zombieGameInputs;

        public override void OnStartServer()
        {
            base.OnStartServer();

            // Find the PlayerArmature child GameObject
            Transform playerArmature = GetPlayerArmature();
            if (playerArmature != null)
            {
                PlayerRegistry.Instance.AddPlayer(playerArmature.gameObject);
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            // Find the PlayerArmature child GameObject
            Transform playerArmature = GetPlayerArmature();
            if (playerArmature != null)
            {
                PlayerRegistry.Instance.RemovePlayer(playerArmature.gameObject);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            
            // Find the PlayerArmature child GameObject
            Transform playerArmature = GetPlayerArmature();
            if (playerArmature == null)
            {
                return;
            }
            
            // Auto-find and assign all controller components from PlayerArmature
            _playerBasicController = playerArmature.GetComponent<PlayerBasicController>();
            _playerInteractionManager = playerArmature.GetComponent<PlayerInteractionManager>();
            _crosshairController = playerArmature.GetComponent<CrosshairController>();
            _cameraController = playerArmature.GetComponent<CameraController>();
            _vehicleController = playerArmature.GetComponent<MVCPlayerVehicleController>();
            _zombieGameInputs = playerArmature.GetComponent<ZombieGameInputs>();
            if (base.IsOwner)
            {
                // This is MY player - enable all components
                EnableAllComponents();
                _zombieGameInputs.SetCursorState(true);
            }
            else
            {
                // This is someone else's player - disable local control components
                DisableLocalComponents();
            }
        }

        private Transform GetPlayerArmature()
        {
            // Find the PlayerArmature child GameObject
            Transform playerArmature = transform.Find("PlayerArmature");
            if (playerArmature == null)
            {
                Debug.LogError("PlayerArmature child not found! Make sure the child GameObject is named 'PlayerArmature'");
                return null;
            }

            return playerArmature;
        }

        private void EnableAllComponents()
        {
            if (_playerBasicController != null) _playerBasicController.enabled = true;
            if (_playerInteractionManager != null) _playerInteractionManager.enabled = true;
            if (_crosshairController != null) _crosshairController.enabled = true;
            if (_cameraController != null) _cameraController.enabled = true;
            if (_vehicleController != null) _vehicleController.enabled = true;
        }

        private void DisableLocalComponents()
        {
            if (_playerBasicController != null) _playerBasicController.enabled = false;
            if (_playerInteractionManager != null) _playerInteractionManager.enabled = false;
            if (_crosshairController != null) _crosshairController.enabled = false;
            if (_cameraController != null) _cameraController.enabled = false;
            if (_vehicleController != null) _vehicleController.enabled = false;
        }
    }
}