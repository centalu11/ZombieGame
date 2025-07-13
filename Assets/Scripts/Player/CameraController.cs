using UnityEngine;
using UnityEngine.InputSystem;
using ZombieGame.Input;
using System.Linq;

namespace ZombieGame.Player
{
    public class CameraController : MonoBehaviour
    {
            [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    [Header("Mouse Settings")]
    [Tooltip("Mouse sensitivity for horizontal movement")]
    public float MouseSensitivityX = 1.0f;

    [Tooltip("Mouse sensitivity for vertical movement")]
    public float MouseSensitivityY = 1.0f;

    [Tooltip("Smooth time for camera rotation (0 = no smoothing, higher = more smooth)")]
    public float RotationSmoothTime = 0.1f;

            // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // smoothing
    private float _rotationVelocityX;
    private float _rotationVelocityY;
    private float _currentYaw;
    private float _currentPitch;

    private const float _threshold = 0.01f;

    private PlayerInput _playerInput;
    private ZombieGameInputs _input;

        private bool IsCurrentDeviceMouse
        {
            get
            {
                return _playerInput.currentControlScheme == "KeyboardMouse";
            }
        }

        private void Start()
        {
            _playerInput = GetComponent<PlayerInput>();
            _input = GetComponent<ZombieGameInputs>();
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _cinemachineTargetPitch = CinemachineCameraTarget.transform.rotation.eulerAngles.x;
            
            // Initialize smoothing variables
            _currentYaw = _cinemachineTargetYaw;
            _currentPitch = _cinemachineTargetPitch;
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                // Apply sensitivity to mouse input
                float yawInput = _input.look.x * MouseSensitivityX * deltaTimeMultiplier;
                float pitchInput = _input.look.y * MouseSensitivityY * deltaTimeMultiplier;

                _cinemachineTargetYaw += yawInput;
                _cinemachineTargetPitch += pitchInput;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Apply smoothing if enabled
            if (RotationSmoothTime > 0f)
            {
                _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _cinemachineTargetYaw, ref _rotationVelocityX, RotationSmoothTime);
                _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _cinemachineTargetPitch, ref _rotationVelocityY, RotationSmoothTime);
                
                // Cinemachine will follow this target with smoothing
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_currentPitch + CameraAngleOverride,
                    _currentYaw, 0.0f);
            }
            else
            {
                // No smoothing - direct rotation
                CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                    _cinemachineTargetYaw, 0.0f);
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnValidate()
        {
            if (GetComponent<PlayerLoader>() == null)
            {
                Debug.LogError($"[CameraController] PlayerLoader component is required on {gameObject.name}! Add PlayerLoader to ensure all required components are properly set up.");
            }

            // Set default camera target if not assigned
            if (CinemachineCameraTarget == null)
            {
                // First try to find it as a child of this GameObject
                Transform cameraRoot = transform.Find("PlayerCameraRoot");
                
                // If not found as a child, try to find it by tag in children
                if (cameraRoot == null)
                {
                    cameraRoot = transform.GetComponentsInChildren<Transform>()
                        .FirstOrDefault(t => t.CompareTag("CinemachineTarget"))?.transform;
                }
                
                if (cameraRoot != null)
                {
                    CinemachineCameraTarget = cameraRoot.gameObject;
                }
            }
        }
    }
} 