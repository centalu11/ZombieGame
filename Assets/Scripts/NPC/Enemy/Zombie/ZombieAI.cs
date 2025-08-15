using UnityEngine;
using ZombieGame.Core;
using ZombieGame.NPC.Enemy.Zombie.Structs;
using ZombieGame.Player;

namespace ZombieGame.NPC.Enemy.Zombie
{
    /// <summary>
    /// Direction options for wandering
    /// </summary>
    public enum WanderDirection
    {
        Forward,
        Right,
        Left,
        Backward,
        ForwardWithAngle,
        BackwardWithAngle
    }
    
    /// <summary>
    /// Single step in a wandering path
    /// </summary>
    [System.Serializable]
    public struct WanderStep
    {
        [Tooltip("Direction to move")]
        public WanderDirection direction;
        
        [Tooltip("Distance to move in this direction")]
        public float distance;
        
        [Tooltip("Angle for angled movements (default 45°, range: -89° to 89°)")]
        [Range(-89f, 89f)]
        public float angle;
        
        /// <summary>
        /// Constructor for basic directions (no angle)
        /// </summary>
        public WanderStep(WanderDirection dir, float dist)
        {
            direction = dir;
            distance = dist;
            angle = 45f; // Default angle
        }
        
        /// <summary>
        /// Constructor for angled directions
        /// </summary>
        public WanderStep(WanderDirection dir, float dist, float ang)
        {
            direction = dir;
            distance = dist;
            angle = Mathf.Clamp(ang, -89f, 89f); // Ensure angle is within valid range
        }
    }
    
    /// <summary>
    /// Simple AI controller for zombies with parameter-based animation control
    /// </summary>
    public class ZombieAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [Tooltip("Detection range for the player")]
        public float detectionRange = 10f;
        
        [Tooltip("Attack range - calculated from arm span (auto-calculated on start)")]
        public float attackRange = 1.5f;
        
        [Tooltip("Minimum attack distance - zombie must be this close to actually attack")]
        public float minAttackDistance = 1f;
        
        [Tooltip("Use automatic arm span calculation for attack range")]
        public bool useArmSpanForAttackRange = true;
        
        [Tooltip("Buffer distance added to arm reach (accounts for body size and combat space)")]
        public float armReachBuffer = 0.5f;
        
        // Movement speeds are now handled per-clip in AnimationStateData
        
        [Header("Static Wandering Settings")]
        public StaticWandering staticWandering = new StaticWandering();
        
        [Header("Animation States")]
        [Tooltip("Idle animation state and clip")]
        public AnimationStateData idleAnimation = new AnimationStateData("Z_Idle", new AnimationClip[0]);
        
        [Tooltip("Wandering walk animation state and clip")]
        public AnimationStateData wanderWalkAnimation = new AnimationStateData("Z_Wander_Walk", new AnimationClip[0]);
        
        [Tooltip("Chasing walk animation state and clip")]
        public AnimationStateData chaseWalkAnimation = new AnimationStateData("Z_Chase_Walk", new AnimationClip[0]);
        
        [Tooltip("Chasing run animation state and clip")]
        public AnimationStateData chaseRunAnimation = new AnimationStateData("Z_Chase_Run", new AnimationClip[0]);
        
        [Tooltip("Attack animation state and clip")]
        public AnimationStateData attackAnimation = new AnimationStateData("Z_Attack", new AnimationClip[0]);
        
        [Tooltip("Death back fall animation state and clip")]
        public AnimationStateData deathBackAnimation = new AnimationStateData("Z_Death_Back", new AnimationClip[0]);
        
        [Tooltip("Death front fall animation state and clip")]
        public AnimationStateData deathFrontAnimation = new AnimationStateData("Z_Death_Front", new AnimationClip[0]);
        
        [Header("Chase Settings")]
        [Tooltip("Use walking animation when chasing (unchecked = running)")]
        public bool useWalkForChasing = false;
        
        [Tooltip("Use front fall death animation (unchecked = back fall)")]
        public bool useFrontFallDeath = true;
        
        [Header("Animation Speed Settings")]
        
        // Animation speeds are now handled per-clip in AnimationStateData
        
        [Header("Head Tracking Settings")]
        [Tooltip("Enable head tracking to look at player when detected")]
        public bool enableHeadTracking = true;
        
        [Tooltip("Maximum head rotation angle horizontally (zombie-like flexibility)")]
        [Range(0f, 180f)]
        public float maxHeadRotationHorizontal = 120f;
        
        [Tooltip("Maximum head rotation angle vertically (zombie-like flexibility)")]
        [Range(0f, 90f)]
        public float maxHeadRotationVertical = 80f;
        
        [Tooltip("Head rotation speed")]
        public float headRotationSpeed = 3f;
        
        [Header("Arm Pointing Settings")]
        [Tooltip("Enable arm pointing towards player when detected")]
        public bool enableArmPointing = true;
        
        [Tooltip("Maximum arm rotation angle (zombie-like flexibility)")]
        [Range(0f, 180f)]
        public float maxArmRotationAngle = 150f;
        
        [Tooltip("Arm rotation speed")]
        public float armRotationSpeed = 2f;
        
        [Header("Initial State")]
        [Tooltip("Choose the initial state when zombie spawns")]
        public InitialState initialState = InitialState.Idle;
        
        // Initial state options
        public enum InitialState
        {
            Idle,
            Wandering
        }
        
        // State management
        private enum ZombieState
        {
            Idle,
            Wandering,
            Chasing,
            Attacking,
            Dead
        }
        
        private ZombieState _currentState = ZombieState.Idle;
        private Transform _player;
        private Animator _animator;
        private bool _isDead = false;
        
        // Detection state
        private bool _isPlayerDetected = false;
        
        /// <summary>
        /// Public property to check if player is currently detected
        /// </summary>
        public bool IsPlayerDetected => _isPlayerDetected;
        
        // Head and arm tracking variables
        private Transform _headBone;
        private Transform _leftArmBone;
        private Transform _rightArmBone;
        private Quaternion _originalHeadRotation;
        private Quaternion _originalLeftArmRotation;
        private Quaternion _originalRightArmRotation;
        private bool _trackingInitialized = false;
        
        // Wandering Variables (legacy - kept for backward compatibility)
        private Vector3 _wanderTarget;
        private Vector3 _desiredStopPosition;
        
        private void Start()
        {
            InitializeComponents();
            
            // Initialize static wandering system
            staticWandering.Initialize(transform);
            
            // Set initial state based on input
            switch (initialState)
            {
                case InitialState.Idle:
                    SetState(ZombieState.Idle);
                    break;
                case InitialState.Wandering:
                    SetState(ZombieState.Wandering);
                    break;
            }
        }
        
        private void Update()
        {
            if (_isDead) return;
            
            // Main AI always runs
            UpdateAI();
        }
        
        /// <summary>
        /// Initialize zombie components
        /// </summary>
        private void InitializeComponents()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("[ZombieAI] No Animator component found!");
                return;
            }

            // Pre-select clips to set indices in the original structs
            idleAnimation.SetAnimationClip();
            wanderWalkAnimation.SetAnimationClip();
            chaseWalkAnimation.SetAnimationClip();
            chaseRunAnimation.SetAnimationClip();
            attackAnimation.SetAnimationClip();
            deathBackAnimation.SetAnimationClip();
            deathFrontAnimation.SetAnimationClip();
            
            // Apply dynamic animation overrides
            DynamicAnimatorController.ApplyOverrides(
                _animator,
                _animator.runtimeAnimatorController,
                idleAnimation,
                wanderWalkAnimation,
                chaseWalkAnimation,
                chaseRunAnimation,
                attackAnimation,
                deathBackAnimation,
                deathFrontAnimation
            );
            
            // Initialize head and arm tracking
            InitializeTracking();
            
            // Calculate arm span for attack range
            if (useArmSpanForAttackRange)
            {
                CalculateArmSpanAttackRange();
            }
        }
        
        /// <summary>
        /// Initialize head and arm tracking system
        /// </summary>
        private void InitializeTracking()
        {
            if (!enableHeadTracking && !enableArmPointing) return;
            
            if (_animator == null) return;
            
            // Auto-detect head bone
            if (enableHeadTracking)
            {
                _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
                if (_headBone == null)
                {
                    _headBone = FindChildByName(transform, "Head");
                }
                if (_headBone == null)
                {
                    _headBone = FindChildByName(transform, "head");
                }
                if (_headBone == null)
                {
                    _headBone = FindChildByName(transform, "Neck");
                }
                if (_headBone == null)
                {
                    _headBone = FindChildByName(transform, "neck");
                }
                
                if (_headBone != null)
                {
                    _originalHeadRotation = _headBone.localRotation;
                }
                else
                {
                    Debug.LogWarning($"[ZombieAI] {gameObject.name}: Head bone not found! Head tracking disabled.");
                    enableHeadTracking = false;
                }
            }
            
            // Auto-detect arm bones
            if (enableArmPointing)
            {
                _leftArmBone = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                _rightArmBone = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                
                if (_leftArmBone == null)
                {
                    _leftArmBone = FindChildByName(transform, "LeftArm");
                }
                if (_leftArmBone == null)
                {
                    _leftArmBone = FindChildByName(transform, "leftArm");
                }
                if (_leftArmBone == null)
                {
                    _leftArmBone = FindChildByName(transform, "L_Arm");
                }
                
                if (_rightArmBone == null)
                {
                    _rightArmBone = FindChildByName(transform, "RightArm");
                }
                if (_rightArmBone == null)
                {
                    _rightArmBone = FindChildByName(transform, "rightArm");
                }
                if (_rightArmBone == null)
                {
                    _rightArmBone = FindChildByName(transform, "R_Arm");
                }
                
                if (_leftArmBone != null)
                {
                    _originalLeftArmRotation = _leftArmBone.localRotation;
                }
                if (_rightArmBone != null)
                {
                    _originalRightArmRotation = _rightArmBone.localRotation;
                }
                
                if (_leftArmBone == null && _rightArmBone == null)
                {
                    Debug.LogWarning($"[ZombieAI] {gameObject.name}: Arm bones not found! Arm pointing disabled.");
                    enableArmPointing = false;
                }
            }
            
            _trackingInitialized = true;
        }
        
        /// <summary>
        /// Find a child transform by name (recursive search)
        /// </summary>
        private Transform FindChildByName(Transform parent, string name)
        {
            // Check direct children first
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }
            
            // Recursively search in all children
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindChildByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find the closest player from the PlayerRegistry
        /// </summary>
        private void FindClosestPlayer()
        {
            return;
            if (PlayerRegistry.Instance == null || PlayerRegistry.Instance.Players.Count == 0)
            {
                _player = null;
                return;
            }
            
            Transform closestPlayer = null;
            float closestDistance = float.MaxValue;
            
            foreach (var playerObj in PlayerRegistry.Instance.Players)
            {
                if (playerObj == null) continue;
                
                float distance = Vector3.Distance(transform.position, playerObj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = playerObj.transform;
                }
            }
            
            _player = closestPlayer;
        }
        
        /// <summary>
        /// Calculate arm reach from bone positions and set attack range
        /// </summary>
        private void CalculateArmSpanAttackRange()
        {
            if (_animator == null) return;
            
            // Try to get bone transforms
            Transform leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            Transform leftShoulder = _animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
            Transform rightShoulder = _animator.GetBoneTransform(HumanBodyBones.RightShoulder);
            Transform chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
            
            float armReach = 0f;
            
            if (leftHand != null && rightHand != null)
            {
                // Calculate full arm span (hand to hand) for reference
                float armSpan = Vector3.Distance(leftHand.position, rightHand.position);
                
                // Calculate actual arm reach from center to hand
                Vector3 centerPosition = transform.position;
                if (chest != null)
                {
                    centerPosition = chest.position; // Use chest as more accurate center
                }
                
                float leftArmReach = Vector3.Distance(centerPosition, leftHand.position);
                float rightArmReach = Vector3.Distance(centerPosition, rightHand.position);
                
                // Use the longer arm reach
                armReach = Mathf.Max(leftArmReach, rightArmReach);
                
                // Add buffer for body size and combat space
                attackRange = armReach + armReachBuffer;
                
                // Set minimum attack distance to 80% of attack range
                minAttackDistance = attackRange * 0.8f;
            }
            else if (leftShoulder != null && rightShoulder != null)
            {
                // Fallback: calculate from shoulder to estimated hand position
                Vector3 centerPosition = transform.position;
                if (chest != null)
                {
                    centerPosition = chest.position;
                }
                
                float leftShoulderDistance = Vector3.Distance(centerPosition, leftShoulder.position);
                float rightShoulderDistance = Vector3.Distance(centerPosition, rightShoulder.position);
                
                // Estimate arm reach as shoulder distance + estimated forearm length
                float shoulderReach = Mathf.Max(leftShoulderDistance, rightShoulderDistance);
                armReach = shoulderReach * 2.2f; // Rough estimate: shoulder to hand is about 2.2x shoulder to center
                
                // Add buffer for body size and combat space
                attackRange = armReach + armReachBuffer;
                minAttackDistance = attackRange * 0.8f;
            }
            else
            {
                // Fallback to default values
                Debug.LogWarning("Could not calculate zombie arm reach - using default attack range");
                attackRange = 1.5f;
                minAttackDistance = 1f;
            }
        }
        
        /// <summary>
        /// Main AI update logic
        /// </summary>
        private void UpdateAI()
        {
            // Always find the closest player from registry
            FindClosestPlayer();
            
            float distanceToPlayer = _player != null ? Vector3.Distance(transform.position, _player.position) : float.MaxValue;
            
            // Update detection state
            UpdateDetectionState(distanceToPlayer);
            
            // Update head and arm tracking
            UpdateTracking();
            
            switch (_currentState)
            {
                case ZombieState.Idle:
                    HandleIdleState(distanceToPlayer);
                    break;
                    
                case ZombieState.Wandering:
                    HandleWanderingState(distanceToPlayer);
                    break;
                    
                case ZombieState.Chasing:
                    HandleChasingState(distanceToPlayer);
                    break;
                    
                case ZombieState.Attacking:
                    HandleAttackingState(distanceToPlayer);
                    break;
            }
        }
        
        /// <summary>
        /// Update detection state based on player distance
        /// </summary>
        private void UpdateDetectionState(float distanceToPlayer)
        {
            // Player is detected if within detection range
            _isPlayerDetected = _player != null && distanceToPlayer <= detectionRange;
        }
        
        /// <summary>
        /// Update head and arm tracking to look at player
        /// </summary>
        private void UpdateTracking()
        {
            if (!_trackingInitialized || _player == null) 
            {
                ResetTracking();
                return;
            }
            
            // Update head tracking (only when player is detected)
            if (enableHeadTracking && _headBone != null && _isPlayerDetected)
            {
                UpdateHeadTracking();
            }
            else if (enableHeadTracking && _headBone != null)
            {
                ResetHeadTracking();
            }
            
            // Update arm pointing (only when player is detected)
            if (enableArmPointing && _isPlayerDetected)
            {
                UpdateArmPointing();
            }
            else
            {
                ResetArmPointing();
            }
        }
        
        /// <summary>
        /// Update head tracking to look at player with zombie-like flexibility
        /// </summary>
        private void UpdateHeadTracking()
        {
            if (_headBone == null || _player == null) return;

            // Calculate direction from head to player
            Vector3 directionToPlayer = (_player.position - _headBone.position).normalized;
            
            // Create a rotation that looks at the player
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);

            // Convert to local space relative to the head bone's parent
            if (_headBone.parent != null)
            {
                targetRotation = Quaternion.Inverse(_headBone.parent.rotation) * targetRotation;
            }
            
            // Get euler angles and handle wrapping
            Vector3 eulerAngles = targetRotation.eulerAngles;
            
            // Convert from 0-360 to -180 to 180
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f;
            if (eulerAngles.y > 180f) eulerAngles.y -= 360f;
            if (eulerAngles.z > 180f) eulerAngles.z -= 360f;
            
            // Clamp angles with zombie-like flexibility
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -maxHeadRotationVertical, maxHeadRotationVertical);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -maxHeadRotationHorizontal, maxHeadRotationHorizontal);
            eulerAngles.z = 0; // No roll rotation
            
            targetRotation = Quaternion.Euler(eulerAngles);
            
            // Smoothly rotate head
            _headBone.localRotation = Quaternion.Slerp(_headBone.localRotation, targetRotation, headRotationSpeed * Time.deltaTime);
        }
        
        /// <summary>
        /// Update arm pointing towards player
        /// </summary>
        private void UpdateArmPointing()
        {
            if (_player == null) return;
            
            // Point left arm
            if (_leftArmBone != null)
            {
                UpdateArmBonePointing(_leftArmBone, _originalLeftArmRotation);
            }
            
            // Point right arm
            if (_rightArmBone != null)
            {
                UpdateArmBonePointing(_rightArmBone, _originalRightArmRotation);
            }
        }
        
        /// <summary>
        /// Update individual arm bone pointing
        /// </summary>
        private void UpdateArmBonePointing(Transform armBone, Quaternion originalRotation)
        {
            if (armBone == null || _player == null) return;
            
            // Calculate direction from arm to player
            Vector3 directionToPlayer = (_player.position - armBone.position).normalized;
            
            // Create a rotation that points at the player
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            
            // Convert to local space relative to the arm bone's parent
            if (armBone.parent != null)
            {
                targetRotation = Quaternion.Inverse(armBone.parent.rotation) * targetRotation;
            }
            
            // Get euler angles and handle wrapping
            Vector3 eulerAngles = targetRotation.eulerAngles;
            
            // Convert from 0-360 to -180 to 180
            if (eulerAngles.x > 180f) eulerAngles.x -= 360f;
            if (eulerAngles.y > 180f) eulerAngles.y -= 360f;
            if (eulerAngles.z > 180f) eulerAngles.z -= 360f;
            
            // Clamp angles with zombie-like flexibility
            eulerAngles.x = Mathf.Clamp(eulerAngles.x, -maxArmRotationAngle, maxArmRotationAngle);
            eulerAngles.y = Mathf.Clamp(eulerAngles.y, -maxArmRotationAngle, maxArmRotationAngle);
            eulerAngles.z = Mathf.Clamp(eulerAngles.z, -maxArmRotationAngle, maxArmRotationAngle);
            
            targetRotation = Quaternion.Euler(eulerAngles);
            
            // Smoothly rotate arm
            armBone.localRotation = Quaternion.Slerp(armBone.localRotation, targetRotation, armRotationSpeed * Time.deltaTime);
        }
        
        /// <summary>
        /// Reset all tracking to original positions
        /// </summary>
        private void ResetTracking()
        {
            ResetHeadTracking();
            ResetArmPointing();
        }
        
        /// <summary>
        /// Reset head rotation to original position
        /// </summary>
        private void ResetHeadTracking()
        {
            if (_headBone != null && _trackingInitialized)
            {
                _headBone.localRotation = Quaternion.Slerp(_headBone.localRotation, _originalHeadRotation, headRotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Reset arm pointing to original positions
        /// </summary>
        private void ResetArmPointing()
        {
            if (_leftArmBone != null && _trackingInitialized)
            {
                _leftArmBone.localRotation = Quaternion.Slerp(_leftArmBone.localRotation, _originalLeftArmRotation, armRotationSpeed * Time.deltaTime);
            }
            
            if (_rightArmBone != null && _trackingInitialized)
            {
                _rightArmBone.localRotation = Quaternion.Slerp(_rightArmBone.localRotation, _originalRightArmRotation, armRotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Handle idle state behavior
        /// </summary>
        private void HandleIdleState(float distanceToPlayer)
        {
            // Check if player is in detection range
            if (distanceToPlayer <= detectionRange)
            {
                SetState(ZombieState.Chasing);
                return;
            }
            
            // Play idle animation
            if (_animator != null)
            {
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnimation.GetStateName()))
                {
                    _animator.Play(idleAnimation.GetStateName());
                }
                // Reset animation speed to normal for idle
                _animator.speed = 1f;
            }
        }
        
        /// <summary>
        /// Handle wandering state behavior
        /// </summary>
        private void HandleWanderingState(float distanceToPlayer)
        {
            // Check if player is in detection range
            if (distanceToPlayer <= detectionRange)
            {
                // Remember that we were wandering before chase
                staticWandering.MarkChaseInterruption();
                SetState(ZombieState.Chasing);
                return;
            }

            // HandleNavmeshWandering();
            HandleStaticWandering();
        }
        
        /// <summary>
        /// Handle regular wandering (NavMesh-based - empty for now)
        /// </summary>
        private void HandleNavmeshWandering()
        {
            // TODO: Implement NavMesh-based wandering here
        }
        
        /// <summary>
        /// Handle static wandering movement
        /// </summary>
        private void HandleStaticWandering()
        {
            // Update static wandering - returns true if currently moving
            bool isMoving = staticWandering.UpdateWandering(wanderWalkAnimation.GetMovementSpeed());
            bool isWaitingBetweenSteps = staticWandering.IsWaitingBetweenSteps();
            
            // Update legacy variables for compatibility
            _wanderTarget = staticWandering.GetCurrentTarget();
            _desiredStopPosition = staticWandering.GetCurrentDesiredStopPosition();
            
            // Handle animations based on wandering state
            if (_animator != null)
            {
                if (isMoving && wanderWalkAnimation.IsValid())
                {
                    // Play walking animation when moving
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(wanderWalkAnimation.GetStateName()))
                    {
                        _animator.Play(wanderWalkAnimation.GetStateName());
                    }
                    // Set animation speed to match movement
                    _animator.speed = wanderWalkAnimation.GetAnimationSpeed();
                }
                else if (isWaitingBetweenSteps && idleAnimation.IsValid())
                {
                    // Play idle animation when waiting between steps
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnimation.GetStateName()))
                    {
                        _animator.Play(idleAnimation.GetStateName());
                    }
                    // Reset animation speed to normal for idle
                    _animator.speed = 1f;
                }
            }
        }
        
        /// <summary>
        /// Handle the end of a chase sequence
        /// </summary>
        private void HandleChaseEnd()
        {
            // Unregister this zombie as chasing the player
            if (_player != null)
            {
                var playerState = _player.GetComponent<PlayerState>();
                if (playerState != null)
                {
                    playerState.UnregisterChasingZombie(gameObject);
                }
            }
            
            if (staticWandering.HandleChaseEnd())
            {
                // Restore wandering state
                SetState(ZombieState.Wandering);
            }
            else
            {
                // Just go to idle
                SetState(ZombieState.Idle);
            }
        }
        
        /// <summary>
        /// Handle chasing state behavior
        /// </summary>
        private void HandleChasingState(float distanceToPlayer)
        {
            if (_player == null)
            {
                HandleChaseEnd();
                return;
            }
            
            // Check if close enough to attack (must be within minimum attack distance)
            if (distanceToPlayer <= minAttackDistance)
            {
                SetState(ZombieState.Attacking);
                return;
            }
            
            // Check if player is too far away
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                HandleChaseEnd();
                return;
            }
            
            // Move towards player using simple transform movement
            Vector3 direction = (_player.position - transform.position).normalized;
            direction.y = 0; // Keep movement horizontal
            
            // Move towards player (speed depends on walk/run setting)
            float moveSpeed = useWalkForChasing ? chaseWalkAnimation.GetMovementSpeed() : chaseRunAnimation.GetMovementSpeed();
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Look at player
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
                        // Play appropriate chasing animation
            if (_animator != null)
            {
                string chasingStateName = useWalkForChasing ? chaseWalkAnimation.GetStateName() : chaseRunAnimation.GetStateName();
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(chasingStateName))
                {
                    _animator.Play(chasingStateName);
                }
                // Set animation speed to match movement
                _animator.speed = useWalkForChasing ? chaseWalkAnimation.GetAnimationSpeed() : chaseRunAnimation.GetAnimationSpeed();
            }
        }
        
        /// <summary>
        /// Handle attacking state behavior
        /// </summary>
        private void HandleAttackingState(float distanceToPlayer)
        {
            if (_player == null)
            {
                SetState(ZombieState.Idle);
                return;
            }
            
            // Check if player moved out of attack range
            if (distanceToPlayer > attackRange)
            {
                SetState(ZombieState.Chasing);
                return;
            }
            
            // Look at player
            Vector3 lookDirection = (_player.position - transform.position).normalized;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // Play attack animation
            if (_animator != null)
            {
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(attackAnimation.GetStateName()))
                {
                    _animator.Play(attackAnimation.GetStateName());
                }
                // Reset animation speed to normal for attack
                _animator.speed = 1f;
            }
        }
        
        /// <summary>
        /// Set zombie state and handle transitions
        /// </summary>
        private void SetState(ZombieState newState)
        {
            if (_currentState == newState) return;
            _currentState = newState;
            
            // Handle state-specific setup
            switch (newState)
            {
                case ZombieState.Idle:
                    break;
                    
                case ZombieState.Wandering:
                    // Start static wandering
                    staticWandering.StartWandering();
                    break;
                    
                case ZombieState.Chasing:
                    // Register this zombie as chasing the player
                    if (_player != null)
                    {
                        var playerState = _player.GetComponent<PlayerState>();
                        if (playerState != null)
                        {
                            playerState.RegisterChasingZombie(gameObject);
                        }
                    }
                    break;
                    
                case ZombieState.Attacking:
                    break;
                    
                case ZombieState.Dead:
                    _isDead = true;
                    break;
            }
        }
        
        /// <summary>
        /// Kill the zombie
        /// </summary>
        public void Die()
        {
            SetState(ZombieState.Dead);
            
            // Reset head and arm tracking
            ResetTracking();
            
            // Play death animation (front fall or back fall)
            if (_animator != null)
            {
                string deathStateName = useFrontFallDeath ? deathFrontAnimation.GetStateName() : deathBackAnimation.GetStateName();
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(deathStateName))
                {
                    _animator.Play(deathStateName);
                }
                // Reset animation speed to normal for death
                _animator.speed = 1f;
            }
        }
        
        /// <summary>
        /// Regular wandering function - will use NavMesh later
        /// </summary>
        public void StartWandering()
        {
            // TODO: Implement NavMesh-based wandering
            // This function will be used for proper AI wandering with NavMesh
            SetState(ZombieState.Wandering);
        }
        
        /// <summary>
        /// Static wandering function - enhanced path-based wandering
        /// </summary>
        public void StartStaticWandering()
        {
            if (_isDead) return;
            
            // Start enhanced wandering
            SetState(ZombieState.Wandering);
        }
        
        /// <summary>
        /// Stop static wandering
        /// </summary>
        public void StopStaticWandering()
        {
            staticWandering.StopWandering();
            
            // Reset legacy variables
            _wanderTarget = Vector3.zero;
            _desiredStopPosition = Vector3.zero;
        }
        
        /// <summary>
        /// Force zombie to idle state (for testing)
        /// </summary>
        [ContextMenu("Force Idle")]
        public void ForceIdle()
        {
            SetState(ZombieState.Idle);
        }
        
        /// <summary>
        /// Start regular wandering (for testing)
        /// </summary>
        [ContextMenu("Start Regular Wandering")]
        public void StartWanderingTest()
        {
            StartWandering();
        }
        
        /// <summary>
        /// Start static wandering (for testing)
        /// </summary>
        [ContextMenu("Start Static Wandering")]
        public void StartStaticWanderingTest()
        {
            StartStaticWandering();
        }
        
        /// <summary>
        /// Stop static wandering (for testing)
        /// </summary>
        [ContextMenu("Stop Static Wandering")]
        public void StopStaticWanderingTest()
        {
            StopStaticWandering();
        }
        
        /// <summary>
        /// Test death animation (for testing)
        /// </summary>
        [ContextMenu("Test Death Animation")]
        public void TestDeathAnimation()
        {
            Die();
        }
        
        /// <summary>
        /// Validate animation assignments
        /// </summary>
        [ContextMenu("Validate Animations")]
        public void ValidateAnimations()
        {
            int missingCount = 0;
            
            if (!idleAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!wanderWalkAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!chaseWalkAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!chaseRunAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!attackAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!deathBackAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (!deathFrontAnimation.IsValid())
            {
                missingCount++;
            }
            
            if (missingCount > 0)
            {
                Debug.LogWarning($"[ZombieAI] {gameObject.name}: {missingCount} animation(s) are missing assignments!");
            }
        }
        
        /// <summary>
        /// Recalculate arm span and attack range (for testing)
        /// </summary>
        [ContextMenu("Recalculate Arm Span")]
        public void RecalculateArmSpan()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            CalculateArmSpanAttackRange();
        }

        /// <summary>
        /// Draw debug information
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw attack range (outer ring)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw minimum attack distance (inner ring)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, minAttackDistance);
            
            // Draw head tracking visualization
            if (enableHeadTracking && _headBone != null && _player != null && _isPlayerDetected)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(_headBone.position, _player.position);
            }
            
            // Draw arm pointing visualization
            if (enableArmPointing && _player != null && _isPlayerDetected)
            {
                if (_leftArmBone != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(_leftArmBone.position, _player.position);
                }
                if (_rightArmBone != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(_rightArmBone.position, _player.position);
                }
            }
            
            // Draw static wandering path
            staticWandering.DrawGizmos(transform, _currentState == ZombieState.Wandering);
            
            // Draw line to player if in range
            if (_player != null && Vector3.Distance(transform.position, _player.position) <= detectionRange)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _player.position);
            }
        }
    }
}
