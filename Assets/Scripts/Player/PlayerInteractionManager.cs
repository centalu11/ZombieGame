using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Text;
using ZombieGame.Core;
using ZombieGame.Input;
using ZombieGame.Player.Vehicle;

namespace ZombieGame.Player
{
    public class PlayerInteractionManager : MonoBehaviour
    {
        [Tooltip("Maximum radius around the player to search for interactables")]
        [SerializeField] private float maxInteractionRadius = 15f;
        
        [Header("Look Detection Settings")]
        [Tooltip("Radius of the sphere cast for look detection")]
        [SerializeField] private float lookSphereRadius = 0.25f;
        [Tooltip("Maximum distance to check for look detection")]
        [SerializeField] private float maxLookDistance = 15f;
        [SerializeField] private LayerMask lookLayerMask = -1; // All layers by default

        [Header("Debug Visualization")]
        [SerializeField] private bool showDebugVisuals = true;
        [SerializeField] private Color areaColor = new Color(0f, 0.5f, 1f, 0.1f);
        [SerializeField] private Color wireframeColor = new Color(0f, 0.5f, 1f, 0.5f);
        [SerializeField] private Color lookDirectionColor = Color.red;

        private PlayerBasicController _basicController;
        private ZombieGameInputs _input;
        private Interactable _currentNearestInteractable;
        private Camera _playerMainCamera;
        private IPlayerVehicleController _playerVehicleController;

        private RaycastHit[] _sphereCastHits = new RaycastHit[10]; // Pre-allocate array for sphere cast results

        private void Start()
        {
            _input = GetComponent<ZombieGameInputs>();
            _basicController = GetComponent<PlayerBasicController>();
            _playerVehicleController = ServiceLocator.Get<IPlayerVehicleController>();
            _playerMainCamera = _basicController.GetPlayerCamera().GetComponent<Camera>();

            _input.OnInteractEvent += HandleInteraction;
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnInteractEvent -= HandleInteraction;
            }
        }

        private void HandleInteraction()
        {
            if (_basicController.IsOnFoot()) {
                HandleOnFootInteraction();
            } else if (_basicController.IsInVehicle()) {
                HandleExitVehicleInteraction();
            }
        }

        private void HandleOnFootInteraction()
        {
            // Find all colliders within the player's max interaction radius
            Collider[] foundColliders = Physics.OverlapSphere(transform.position, maxInteractionRadius);
            int numColliders = foundColliders.Length;

            _currentNearestInteractable = null;
            float nearestDistance = float.MaxValue;

            // First try to find what we're looking at
            Vector3 rayOrigin = _playerMainCamera.transform.position;
            Vector3 rayDirection = _playerMainCamera.transform.forward;
            
            int numHits = Physics.SphereCastNonAlloc(
                rayOrigin,
                lookSphereRadius,
                rayDirection,
                _sphereCastHits,
                maxLookDistance,
                lookLayerMask
            );

            // First priority: Check if we're looking directly at an interactable
            bool foundLookTarget = false;
            for (int i = 0; i < numHits; i++)
            {
                var hit = _sphereCastHits[i];
                
                // First check if it has the Interactable tag
                if (!hit.collider.CompareTag("Interactable")) continue;
                
                var interactable = hit.collider.GetComponent<Interactable>();
                
                if (interactable != null)
                {
                    float distance = Vector3.Distance(transform.position, interactable.transform.position);

                    if (distance <= interactable.InteractionThreshold)
                    {
                        _currentNearestInteractable = interactable;
                        foundLookTarget = true;
                        break; // Take the first one we're looking at that's in range
                    }
                }
            }

            // If we're not looking at any valid interactable, fall back to nearest
            if (!foundLookTarget)
            {
                // Check each collider in range
                for (int i = 0; i < numColliders; i++)
                {
                    // First check if it has the Interactable tag
                    if (!foundColliders[i].CompareTag("Interactable")) continue;
                    
                    var interactable = foundColliders[i].GetComponent<Interactable>();
                    if (interactable == null) continue;

                    float distance = Vector3.Distance(transform.position, interactable.transform.position);
                    
                    // Check if it's within the interactable's threshold
                    if (distance <= interactable.InteractionThreshold && distance < nearestDistance)
                    {
                        _currentNearestInteractable = interactable;
                        nearestDistance = distance;
                    }
                }
            }

            if (_currentNearestInteractable == null) return;

            // Get the interactable object
            var interactableObject = _currentNearestInteractable.gameObject;
            
            // Check the interactable type
            switch (_currentNearestInteractable.Type)
            {
                case Interactable.InteractableType.Vehicle:
                    HandleEnterVehicleInteraction(interactableObject);
                    break;
                case Interactable.InteractableType.Door:
                    HandleDoorInteraction(interactableObject);
                    break;
                case Interactable.InteractableType.Item:
                    HandleItemInteraction(interactableObject);
                    break;
            }
        }

        private void HandleDoorInteraction(GameObject door)
        {
            Debug.Log($"Opening/Closing door: {door.name}");
            // Add door specific interaction logic here
        }

        private void HandleItemInteraction(GameObject item)
        {
            Debug.Log($"Picking up item: {item.name}");
            // Add item pickup logic here
        }

        private void HandleEnterVehicleInteraction(GameObject vehicle)
        {
            _playerVehicleController.EnterVehicle(vehicle);
        }

        private void HandleExitVehicleInteraction()
        {
            _playerVehicleController.ExitVehicle();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugVisuals) return;

            // Draw filled sphere for the area
            Gizmos.color = areaColor;
            Gizmos.DrawSphere(transform.position, maxInteractionRadius);

            // Draw wireframe for better visibility
            Gizmos.color = wireframeColor;
            Gizmos.DrawWireSphere(transform.position, maxInteractionRadius);

            // Draw look direction and sphere cast
            if (_playerMainCamera != null)
            {
                Vector3 lookStart = _playerMainCamera.transform.position;
                Vector3 lookEnd = lookStart + _playerMainCamera.transform.forward * maxLookDistance;
                
                // Draw the look direction
                Gizmos.color = lookDirectionColor;
                Gizmos.DrawLine(lookStart, lookEnd);
                
                // Draw spheres along the look direction to visualize the sphere cast
                Gizmos.DrawWireSphere(lookStart + _playerMainCamera.transform.forward * lookSphereRadius, lookSphereRadius);
                Gizmos.DrawWireSphere(lookEnd, lookSphereRadius);
            }

            // If we have a current nearest interactable, draw a line to it
            if (_currentNearestInteractable != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _currentNearestInteractable.transform.position);
            }
        }
    }
} 