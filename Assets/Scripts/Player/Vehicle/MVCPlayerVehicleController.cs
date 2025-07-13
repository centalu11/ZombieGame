using UnityEngine;
using ZombieGame.Vehicle;
using ZombieGame.Core;

namespace ZombieGame.Player.Vehicle
{
    public class MVCPlayerVehicleController : MonoBehaviour, IPlayerVehicleController
    {
        // Player Settings - Start
        [Header("Player Settings")]
        [Tooltip("The mesh of the player to be hidden when entering a vehicle")]
        [SerializeField] private GameObject playerMesh;

        [Tooltip("The player follow camera while on foot")]
        [SerializeField] private GameObject playerFollowCamera;
        // Player Settings - End

        // Vehicle Settings - Start
        [Header("Vehicle Settings")]
        [Tooltip("The vehicle follower for the vehicle controller")]
        [SerializeField] private GameObject vehicleFollower;
        
        [Tooltip("The main camera for vehicle mode")]
        [SerializeField] private GameObject vehicleCamera;
        // Vehicle Settings - End

        private GameObject _currentVehicle;
        private PlayerBasicController _basicController;
        private Collider _playerCollider;
        private GameObject _playerCamera;
        
        // Vehicle momentum fields
        private Vector3 _vehicleVelocity;
        private bool _isCoasting = false;
        private Coroutine _coastingCoroutine;

        private void OnValidate()
        {
            if (playerMesh == null)
            {
                Transform meshTransform = transform.Find("Model/Mesh");
                if (meshTransform != null)
                {
                    playerMesh = meshTransform.gameObject;
                }
            }

            if (playerFollowCamera == null)
            {
                // Search for PlayerFollowCamera as a sibling (PlayerArmature and PlayerFollowCamera are both children of Player)
                if (transform.parent != null)
                {
                    Transform playerFollowCameraTransform = transform.parent.Find("PlayerFollowCamera");
                    if (playerFollowCameraTransform != null)
                    {
                        playerFollowCamera = playerFollowCameraTransform.gameObject;
                    }
                }
            }

            if (vehicleFollower == null)
            {
                vehicleFollower = GameObject.Find("VehicleFollower");
            }

            if (vehicleCamera == null)
            {
                vehicleCamera = GameObject.Find("VehicleCamera");
            }
        }

        private void Awake()
        {
            if (playerMesh == null)
            {
                Debug.LogError("Player mesh not found, please drag your player mesh game object to the Player Mesh field");
            }

            if (playerFollowCamera == null)
            {
                Debug.LogError("Player follow camera not found, please drag your player follow camera game object to the Player Follow Camera field");
            }

            if (vehicleFollower == null)
            {
                Debug.LogError("Vehicle follower not found, please drag your vehicle follower game object to the Vehicle Follower field");
            }

            if (vehicleCamera == null)
            {
                Debug.LogError("Vehicle camera not found, please drag your vehicle camera game object to the Vehicle Camera field");
            }

            vehicleFollower.SetActive(false);
            
            // Register this implementation in the service locator
            ServiceLocator.Register<IPlayerVehicleController>(this);
        }

        private void Start()
        {
            _basicController = GetComponent<PlayerBasicController>();
            _playerCollider = GetComponent<Collider>();
            _playerCamera = _basicController.GetPlayerCamera();
        }

        private void OnDestroy()
        {
            // Stop any active coasting simulation
            if (_coastingCoroutine != null)
            {
                StopCoroutine(_coastingCoroutine);
                _coastingCoroutine = null;
                _isCoasting = false;
            }
            
            // Unregister when destroyed
            ServiceLocator.Unregister<IPlayerVehicleController>();
        }

        public void EnterVehicle(GameObject vehicle)
        {
            _basicController.ChangePlayerMode(PlayerMode.Neutral);

            // Store reference to current vehicle
            _currentVehicle = vehicle.transform.parent.gameObject;

            // Disable/Hide the player model
            playerMesh.SetActive(false);
            _playerCollider.enabled = false;

            // Disable player's camera
            _playerCamera.SetActive(false);
            playerFollowCamera.SetActive(false);

            // Reset Player's animation
            _basicController.ResetPlayerAnimator();

            // Enable MVC's vehicle along with its scripts
            var vehicleController = _currentVehicle.GetComponent<VehicleController>();
            if (vehicleController != null)
            {
                // Enable the vehicle
                if (!vehicleController.IsVehicleEnabled())
                {
                    vehicleController.EnableVehicle();
                }

                // Set up camera to follow vehicle
                vehicleFollower.SetActive(false); // Set it to false first to reset camera position
                vehicleFollower.SetActive(true);
            }

            _basicController.ChangePlayerMode(PlayerMode.InVehicle);
        }

        public void ExitVehicle()
        {   
            if (_currentVehicle == null)
            {
                Debug.LogError("No vehicle to exit");
                return;
            }
            
            _basicController.ChangePlayerMode(PlayerMode.Neutral);

            // Get the final position from the MVC vehicle child (which actually moved)
            Vector3 finalVehiclePosition = Vector3.zero;
            Quaternion finalVehicleRotation = Quaternion.identity;
            
            var vehicleController = _currentVehicle.GetComponent<VehicleController>();
            if (vehicleController != null)
            {
                // Get the MVC vehicle that actually moved during driving
                GameObject mvcVehicle = vehicleController.GetMVCVehicle();
                if (mvcVehicle != null)
                {
                        finalVehiclePosition = mvcVehicle.transform.position;
                        finalVehicleRotation = mvcVehicle.transform.rotation;
                        
                        // Capture velocity before disabling MVC vehicle
                        Rigidbody mvcRigidbody = mvcVehicle.GetComponent<Rigidbody>();
                        if (mvcRigidbody != null)
                        {
                            _vehicleVelocity = mvcRigidbody.linearVelocity;
                        }
                        
                        // Move parent to MVC vehicle's final position
                        _currentVehicle.transform.position = finalVehiclePosition;
                        _currentVehicle.transform.rotation = finalVehicleRotation;
                        
                        // Reset MVC vehicle's local transform
                        mvcVehicle.transform.localPosition = Vector3.zero;
                        mvcVehicle.transform.localRotation = Quaternion.identity;
                }

                // Disable the MVC vehicle
                if (vehicleController.IsVehicleEnabled())
                {
                    vehicleController.DisableVehicle();
                }
            }
            
            // Apply momentum to interactable vehicle
            // OnVehicleExit();

            vehicleFollower.SetActive(false);

            // Position player 2 units to the left of the vehicle (using final position)
            // Use the final position for player exit calculation
            Vector3 vehicleLeft = finalVehicleRotation * Vector3.left; // Left direction relative to vehicle's final rotation
            Vector3 exitPosition = finalVehiclePosition + (vehicleLeft * 2f);

            transform.position = exitPosition;
            

            // Enable/Show the player model
            playerMesh.SetActive(true);
            _playerCollider.enabled = true;

            // Enable player's camera
            _playerCamera.SetActive(false);
            _playerCamera.SetActive(true);
            playerFollowCamera.SetActive(false); // Set it to false first to reset camera position
            playerFollowCamera.SetActive(true);

            // Clear current vehicle reference
            _currentVehicle = null;
            
            _basicController.ChangePlayerMode(PlayerMode.OnFoot);
        }

        public bool IsInVehicle()
        {
            return _currentVehicle != null && _basicController.IsInVehicle();
        }

        public GameObject GetCurrentVehicle()
        {
            return _currentVehicle;
        }

        private void OnVehicleExit()
        {
            if (_currentVehicle == null || _vehicleVelocity.magnitude < 0.1f)
            {
                return; // No significant momentum to apply
            }
            
            var vehicleController = _currentVehicle.GetComponent<VehicleController>();
            if (vehicleController == null) return;
            
            GameObject interactableVehicle = vehicleController.GetInteractableVehicle();
            if (interactableVehicle == null) return;
            
            // Stop any existing coasting
            if (_coastingCoroutine != null)
            {
                StopCoroutine(_coastingCoroutine);
            }
            
            // Start momentum simulation
            _isCoasting = true;
            _coastingCoroutine = StartCoroutine(SimulateVehicleMomentum(interactableVehicle));
        }
        
        private System.Collections.IEnumerator SimulateVehicleMomentum(GameObject interactableVehicle)
        {
            // Physics constants
            const float dragCoefficient = 0.98f;       // Air resistance
            const float frictionCoefficient = 0.95f;   // Ground friction
            const float minVelocity = 0.1f;            // Minimum velocity to continue simulation
            const float collisionCheckDistance = 0.5f; // Distance to check for collisions ahead
            
            Vector3 currentVelocity = _vehicleVelocity;
            Vector3 lastPosition = interactableVehicle.transform.position;
            
            // Get vehicle's forward direction based on its rotation
            Vector3 vehicleForward = interactableVehicle.transform.forward;
            
            // Calculate initial momentum direction (considering drift/angle)
            Vector3 momentumDirection = currentVelocity.normalized;
            
            // If velocity direction differs significantly from forward direction, it's drifting
            float driftAngle = Vector3.Angle(vehicleForward, momentumDirection);
            
            Debug.Log($"🚗 Vehicle Momentum: Speed={currentVelocity.magnitude:F2} m/s, Drift Angle={driftAngle:F1}°");
            
            Collider vehicleCollider = interactableVehicle.GetComponent<Collider>();
            
            while (_isCoasting && currentVelocity.magnitude > minVelocity)
            {
                // Check for collisions ahead
                bool hitObstacle = false;
                if (vehicleCollider != null)
                {
                    Vector3 checkDirection = currentVelocity.normalized;
                    float checkDistance = collisionCheckDistance + (currentVelocity.magnitude * Time.fixedDeltaTime);
                    
                    RaycastHit hit;
                    if (Physics.BoxCast(
                        vehicleCollider.bounds.center,
                        vehicleCollider.bounds.extents * 0.8f,
                        checkDirection,
                        out hit,
                        interactableVehicle.transform.rotation,
                        checkDistance,
                        ~(1 << interactableVehicle.layer))) // Ignore vehicle's own layer
                    {
                        hitObstacle = true;
                        Debug.Log($"🚧 Vehicle collision detected with: {hit.collider.name}");
                    }
                }
                
                if (hitObstacle)
                {
                    // Stop momentum simulation on collision
                    break;
                }
                
                // Apply physics-based movement
                Vector3 deltaPosition = currentVelocity * Time.fixedDeltaTime;
                interactableVehicle.transform.position += deltaPosition;
                
                // Apply drag and friction to reduce velocity
                currentVelocity *= dragCoefficient * frictionCoefficient;
                
                // Additional velocity reduction based on ground contact
                // Simulate tire friction and rolling resistance
                float groundFriction = 0.99f; // Additional ground resistance
                currentVelocity *= groundFriction;
                
                // Gradually align momentum direction with vehicle forward (simulates steering correction)
                if (driftAngle > 5f) // Only if significant drift
                {
                    float alignmentRate = 0.02f; // How quickly drift corrects
                    momentumDirection = Vector3.Slerp(momentumDirection, vehicleForward, alignmentRate);
                    
                    // Update velocity direction while preserving magnitude
                    float speed = currentVelocity.magnitude;
                    currentVelocity = momentumDirection * speed;
                }
                
                // Rotate vehicle slightly based on momentum direction (simulates drift physics)
                if (currentVelocity.magnitude > 1f)
                {
                    float rotationInfluence = Mathf.Clamp01(driftAngle / 45f) * 0.5f; // Scale rotation by drift
                    Vector3 targetDirection = Vector3.Slerp(vehicleForward, momentumDirection, rotationInfluence);
                    
                    if (targetDirection != Vector3.zero)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                        interactableVehicle.transform.rotation = Quaternion.Slerp(
                            interactableVehicle.transform.rotation, 
                            targetRotation, 
                            Time.fixedDeltaTime * 2f
                        );
                    }
                }
                
                // Debug info
                if (Time.fixedTime % 0.5f < Time.fixedDeltaTime) // Log every 0.5 seconds
                {
                    float distanceTraveled = Vector3.Distance(lastPosition, interactableVehicle.transform.position);
                    Debug.Log($"🏁 Coasting: Speed={currentVelocity.magnitude:F2} m/s, Distance={distanceTraveled:F2}m");
                    lastPosition = interactableVehicle.transform.position;
                }
                
                yield return new WaitForFixedUpdate();
            }
            
            // Momentum simulation complete
            _isCoasting = false;
            _coastingCoroutine = null;
            
            float finalDistance = Vector3.Distance(_currentVehicle.transform.position, interactableVehicle.transform.position);
            Debug.Log($"✅ Vehicle coasting complete. Total distance traveled: {finalDistance:F2}m");
        }
    }
} 