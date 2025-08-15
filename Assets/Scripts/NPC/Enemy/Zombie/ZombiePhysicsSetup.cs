using UnityEngine;

namespace ZombieGame.NPC.Enemy.Zombie
{
    /// <summary>
    /// Physics setup for zombies using Rigidbody system
    /// </summary>
    public class ZombiePhysicsSetup : MonoBehaviour
    {
        [Header("Physics Settings")]
        [Tooltip("Mass of the zombie")]
        public float mass = 60f;
        
        [Tooltip("Drag applied to the zombie")]
        public float drag = 1f;
        
        [Tooltip("Angular drag applied to the zombie")]
        public float angularDrag = 5f;
        
        [Header("Collider Settings")]
        [Tooltip("Height of the capsule collider")]
        public float colliderHeight = 1.8f;
        
        [Tooltip("Radius of the capsule collider")]
        public float colliderRadius = 0.5f;
        
        [Tooltip("Center offset of the collider")]
        public Vector3 colliderCenter = new Vector3(0, 0.9f, 0);
        
        [Header("Dynamic Collider Settings")]
        [Tooltip("Enable dynamic collider that follows zombie's pose")]
        public bool enableDynamicCollider = true;
        
        [Tooltip("Auto-detect bones using common naming patterns")]
        public bool autoDetectBones = true;
        
        [Tooltip("Name of the head bone to follow (if auto-detect fails)")]
        public string headBoneName = "Base HumanHead";
        
        [Tooltip("Name of the left foot bone to follow (if auto-detect fails)")]
        public string leftFootBoneName = "Base HumanLLegFoot";
        
        [Tooltip("Name of the right foot bone to follow (if auto-detect fails)")]
        public string rightFootBoneName = "Base HumanRFoot";
        
        [Tooltip("Update frequency for dynamic collider (lower = more frequent)")]
        [Range(1, 10)]
        public int updateFrequency = 2;
        
        [Header("Constraints")]
        [Tooltip("Freeze rotation on X and Z axes to prevent tipping over")]
        public bool freezeRotation = true;
        
        [Header("Auto Setup")]
        [Tooltip("Automatically setup physics components on Start")]
        public bool autoSetupOnStart = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Components
        private Rigidbody _rigidbody;
        private CapsuleCollider _capsuleCollider;
        private Animator _animator;
        private Transform _headBone;
        private Transform _leftFootBone;
        private Transform _rightFootBone;
        
        // Dynamic collider tracking
        private int _updateCounter = 0;
        
        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupPhysics();
            }
        }
        
        private void Update()
        {
            if (enableDynamicCollider)
            {
                UpdateDynamicCollider();
            }
        }
        
        /// <summary>
        /// Setup physics components for the zombie
        /// </summary>
        [ContextMenu("Setup Physics")]
        public void SetupPhysics()
        {
            SetupRigidbody();
            SetupCapsuleCollider();
            SetupBoneReferences();
        }
        
        /// <summary>
        /// Setup Rigidbody component
        /// </summary>
        private void SetupRigidbody()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }
            
            // Configure Rigidbody
            _rigidbody.mass = mass;
            _rigidbody.linearDamping = drag;
            _rigidbody.angularDamping = angularDrag;
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            
            // Freeze rotation to prevent tipping over
            if (freezeRotation)
            {
                _rigidbody.freezeRotation = true;
            }
        }
        
        /// <summary>
        /// Setup CapsuleCollider component
        /// </summary>
        private void SetupCapsuleCollider()
        {
            _capsuleCollider = GetComponent<CapsuleCollider>();
            if (_capsuleCollider == null)
            {
                _capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            
            // Configure CapsuleCollider
            _capsuleCollider.height = colliderHeight;
            _capsuleCollider.radius = colliderRadius;
            _capsuleCollider.center = colliderCenter;
            _capsuleCollider.direction = 1; // Y-axis (upright)
        }
        
        /// <summary>
        /// Setup bone references for dynamic collider
        /// </summary>
        private void SetupBoneReferences()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            
            if (_animator != null)
            {
                if (autoDetectBones)
                {
                    // Try auto-detection first
                    _headBone = AutoDetectHeadBone();
                    _leftFootBone = AutoDetectLeftFootBone();
                    _rightFootBone = AutoDetectRightFootBone();
                }
                
                // Fallback to manual names if auto-detection failed
                if (_headBone == null) _headBone = FindBoneByName(headBoneName);
                if (_leftFootBone == null) _leftFootBone = FindBoneByName(leftFootBoneName);
                if (_rightFootBone == null) _rightFootBone = FindBoneByName(rightFootBoneName);
                
                if (_headBone == null || _leftFootBone == null || _rightFootBone == null)
                {
                    Debug.LogWarning($"[ZombiePhysicsSetup] Could not find required bones. Tried auto-detection and manual names. Dynamic collider disabled.");
                    Debug.LogWarning($"Available bones: {string.Join(", ", System.Array.ConvertAll(GetComponentsInChildren<Transform>(), t => t.name))}");
                    enableDynamicCollider = false;
                }
            }
            else
            {
                Debug.LogWarning($"[ZombiePhysicsSetup] No Animator found. Dynamic collider disabled.");
                enableDynamicCollider = false;
            }
        }
        
        /// <summary>
        /// Find a bone by name in the hierarchy
        /// </summary>
        private Transform FindBoneByName(string boneName)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            foreach (Transform child in allChildren)
            {
                if (child.name.Contains(boneName))
                {
                    return child;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Auto-detect head bone using common naming patterns
        /// </summary>
        private Transform AutoDetectHeadBone()
        {
            string[] headPatterns = {
                "Head", "head", "HEAD",
                "HumanHead", "Base HumanHead",
                "Bip01 Head", "Bip001 Head",
                "mixamorig:Head", "mixamorig:head",
                "head.", "Head.", 
                "head_", "Head_"
            };
            
            return FindBoneByPatterns(headPatterns);
        }
        
        /// <summary>
        /// Auto-detect left foot bone using common naming patterns
        /// </summary>
        private Transform AutoDetectLeftFootBone()
        {
            string[] leftFootPatterns = {
                "LeftFoot", "leftFoot", "left_foot", "Left_Foot", "LEFTFOOT",
                "LFoot", "lFoot", "l_foot", "L_Foot",
                "Base HumanLLegFoot", "HumanLLegFoot", "LLegFoot",
                "Bip01 L Foot", "Bip001 L Foot",
                "mixamorig:LeftFoot", "mixamorig:leftFoot",
                "foot.L", "Foot.L", "foot_L", "Foot_L",
                "LeftAnkle", "left_ankle", "L_Ankle"
            };
            
            return FindBoneByPatterns(leftFootPatterns);
        }
        
        /// <summary>
        /// Auto-detect right foot bone using common naming patterns
        /// </summary>
        private Transform AutoDetectRightFootBone()
        {
            string[] rightFootPatterns = {
                "RightFoot", "rightFoot", "right_foot", "Right_Foot", "RIGHTFOOT",
                "RFoot", "rFoot", "r_foot", "R_Foot",
                "Base HumanRFoot", "HumanRFoot", "RFoot",
                "Bip01 R Foot", "Bip001 R Foot",
                "mixamorig:RightFoot", "mixamorig:rightFoot",
                "foot.R", "Foot.R", "foot_R", "Foot_R",
                "RightAnkle", "right_ankle", "R_Ankle"
            };
            
            return FindBoneByPatterns(rightFootPatterns);
        }
        
        /// <summary>
        /// Find bone by trying multiple naming patterns
        /// </summary>
        private Transform FindBoneByPatterns(string[] patterns)
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>();
            
            // First try exact matches
            foreach (string pattern in patterns)
            {
                foreach (Transform child in allChildren)
                {
                    if (child.name.Equals(pattern, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }
            
            // Then try contains matches
            foreach (string pattern in patterns)
            {
                foreach (Transform child in allChildren)
                {
                    if (child.name.ToLower().Contains(pattern.ToLower()))
                    {
                        return child;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update the collider to follow the zombie's pose
        /// </summary>
        private void UpdateDynamicCollider()
        {
            if (_capsuleCollider == null || _headBone == null || _leftFootBone == null || _rightFootBone == null)
                return;
            
            // Update less frequently for performance
            _updateCounter++;
            if (_updateCounter < updateFrequency)
                return;
            _updateCounter = 0;
            
            // Calculate positions in local space
            Vector3 headPos = transform.InverseTransformPoint(_headBone.position);
            Vector3 leftFootPos = transform.InverseTransformPoint(_leftFootBone.position);
            Vector3 rightFootPos = transform.InverseTransformPoint(_rightFootBone.position);
            
            // Find the lowest point between both feet
            Vector3 lowestFootPos = leftFootPos.y < rightFootPos.y ? leftFootPos : rightFootPos;
            
            // Calculate the direction from feet to head (body orientation)
            Vector3 feetToHead = (headPos - lowestFootPos).normalized;
            
            // Calculate the height based on the distance from feet to head (full body height)
            float dynamicHeight = Vector3.Distance(lowestFootPos, headPos) + 0.3f; // Add some padding
            dynamicHeight = Mathf.Max(dynamicHeight, colliderRadius * 2); // Minimum height is 2*radius
            
            // Calculate center point so the collider bottom touches the ground
            // Instead of centering between feet and head, position so bottom aligns with ground
            Vector3 centerPoint;
            if (Vector3.Dot(feetToHead, Vector3.up) < 0.7f) // If zombie is horizontal/lying down
            {
                // For horizontal pose, center between feet and head but adjust for ground contact
                centerPoint = (lowestFootPos + headPos) * 0.5f;
                // Adjust Y to ensure the collider touches the ground
                centerPoint.y = Mathf.Max(centerPoint.y, colliderRadius);
            }
            else
            {
                // For upright pose, center the collider with bottom at ground level
                float groundLevel = 0f; // Assuming zombie's transform is at ground level
                centerPoint = new Vector3(
                    (lowestFootPos.x + headPos.x) * 0.5f,  // X: center between feet and head
                    groundLevel + (dynamicHeight * 0.5f),  // Y: half height above ground
                    (lowestFootPos.z + headPos.z) * 0.5f   // Z: center between feet and head
                );
            }
            
            // Update collider properties
            _capsuleCollider.center = centerPoint;
            _capsuleCollider.height = dynamicHeight;
            
            // Adjust collider orientation based on the zombie's pose
            if (Vector3.Dot(feetToHead, Vector3.up) < 0.7f) // If zombie is more horizontal than vertical
            {
                // Zombie is lying down or falling - adjust collider orientation
                _capsuleCollider.direction = 2; // Z-axis (horizontal)
            }
            else
            {
                // Zombie is upright - use default orientation
                _capsuleCollider.direction = 1; // Y-axis (upright)
            }
        }
        
        /// <summary>
        /// Get the Rigidbody component
        /// </summary>
        public Rigidbody GetRigidbody()
        {
            return _rigidbody;
        }
        
        /// <summary>
        /// Move the zombie using physics
        /// </summary>
        public void MoveWithPhysics(Vector3 direction, float speed)
        {
            if (_rigidbody == null) return;
            
            Vector3 moveForce = direction.normalized * speed;
            _rigidbody.AddForce(moveForce, ForceMode.VelocityChange);
        }
        
        /// <summary>
        /// Stop the zombie movement
        /// </summary>
        public void StopMovement()
        {
            if (_rigidbody == null) return;
            
            _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
        }
        
        /// <summary>
        /// Check if zombie is grounded
        /// </summary>
        public bool IsGrounded()
        {
            if (_capsuleCollider == null) return false;
            
            float rayDistance = _capsuleCollider.height / 2f + 0.1f;
            return Physics.Raycast(transform.position, Vector3.down, rayDistance);
        }
        
        /// <summary>
        /// Setup all zombies in scene
        /// </summary>
        [ContextMenu("Setup All Zombies in Scene")]
        public void SetupAllZombiesInScene()
        {
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int setupCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("zombie"))
                {
                    ZombiePhysicsSetup physicsSetup = obj.GetComponent<ZombiePhysicsSetup>();
                    if (physicsSetup == null)
                    {
                        physicsSetup = obj.AddComponent<ZombiePhysicsSetup>();
                    }
                    physicsSetup.SetupPhysics();
                    setupCount++;
                }
            }
        }
        
        /// <summary>
        /// Draw gizmos for debugging
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw capsule collider
            Gizmos.color = Color.green;
            Vector3 center = transform.position + (_capsuleCollider != null ? _capsuleCollider.center : colliderCenter);
            Vector3 size = new Vector3(
                (_capsuleCollider != null ? _capsuleCollider.radius : colliderRadius) * 2,
                _capsuleCollider != null ? _capsuleCollider.height : colliderHeight,
                (_capsuleCollider != null ? _capsuleCollider.radius : colliderRadius) * 2
            );
            Gizmos.DrawWireCube(center, size);
            
            // Draw ground check ray
            Gizmos.color = Color.red;
            float rayDistance = (_capsuleCollider != null ? _capsuleCollider.height : colliderHeight) / 2f + 0.1f;
            Gizmos.DrawRay(transform.position, Vector3.down * rayDistance);
            
            // Draw bone connections if available
            if (_headBone != null && _leftFootBone != null && _rightFootBone != null)
            {
                Gizmos.color = Color.yellow;
                
                // Draw lines from feet to head
                Gizmos.DrawLine(_leftFootBone.position, _headBone.position);
                Gizmos.DrawLine(_rightFootBone.position, _headBone.position);
                
                // Draw connection between feet
                Gizmos.DrawLine(_leftFootBone.position, _rightFootBone.position);
                
                // Draw bone spheres
                Gizmos.DrawSphere(_headBone.position, 0.05f);
                Gizmos.DrawSphere(_leftFootBone.position, 0.05f);
                Gizmos.DrawSphere(_rightFootBone.position, 0.05f);
            }
        }
    }
} 