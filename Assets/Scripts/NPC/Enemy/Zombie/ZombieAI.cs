using UnityEngine;

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
        
        [Tooltip("Running speed when chasing")]
        public float runSpeed = 3.5f;
        
        [Header("Static Wandering Settings")]
        public StaticWandering staticWandering = new StaticWandering();
        
        [Header("Animation Assignments")]
        [Tooltip("Animation to play when zombie is idle")]
        public AnimationClip idleAnimation;
        
        [Tooltip("Animation to play when zombie is wandering")]
        public AnimationClip walkWanderingAnimation;
        
        [Tooltip("Animation to play when zombie is walking while chasing")]
        public AnimationClip walkChasingAnimation;
        
        [Tooltip("Animation to play when zombie is running while chasing")]
        public AnimationClip runChasingAnimation;
        
        [Tooltip("Animation to play when zombie dies - back fall")]
        public AnimationClip deathBackFallAnimation;
        
        [Tooltip("Animation to play when zombie dies - front fall")]
        public AnimationClip deathFrontFallAnimation;
        
        [Tooltip("Animation to play when zombie attacks")]
        public AnimationClip attackAnimation;
        
        [Header("Chase Settings")]
        [Tooltip("Use walking animation when chasing (unchecked = running)")]
        public bool useWalkForChasing = false;
        
        [Tooltip("Use front fall death animation (unchecked = back fall)")]
        public bool useFrontFallDeath = true;
        
        [Header("Animation Speed Settings")]
        [Tooltip("Speed multiplier for wandering walk animation")]
        public float wanderAnimationSpeed = 1.5f;
        
        [Tooltip("Speed multiplier for chasing walk animation")]
        public float chaseWalkAnimationSpeed = 1.5f;
        
        [Tooltip("Speed multiplier for chasing run animation")]
        public float chaseRunAnimationSpeed = 1f;
        
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
            
            // Find player
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                _player = playerObj.transform;
            }
            
            // Calculate arm span for attack range
            if (useArmSpanForAttackRange)
            {
                CalculateArmSpanAttackRange();
            }
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
                
                Debug.Log($"Zombie arm span: {armSpan:F2}m, Raw arm reach: {armReach:F2}m, Attack range (with buffer): {attackRange:F2}m, Min attack distance: {minAttackDistance:F2}m");
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
                
                Debug.Log($"Zombie arm reach estimated from shoulders: {armReach:F2}m, Attack range (with buffer): {attackRange:F2}m, Min attack distance: {minAttackDistance:F2}m");
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
            float distanceToPlayer = _player != null ? Vector3.Distance(transform.position, _player.position) : float.MaxValue;
            
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
            if (_animator != null && idleAnimation != null)
            {
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnimation.name))
                {
                    _animator.Play(idleAnimation.name);
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
            bool isMoving = staticWandering.UpdateWandering();
            bool isWaitingBetweenSteps = staticWandering.IsWaitingBetweenSteps();
            
            // Update legacy variables for compatibility
            _wanderTarget = staticWandering.GetCurrentTarget();
            _desiredStopPosition = staticWandering.GetCurrentDesiredStopPosition();
            
            // Handle animations based on wandering state
            if (_animator != null)
            {
                if (isMoving && walkWanderingAnimation != null)
                {
                    // Play walking animation when moving
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(walkWanderingAnimation.name))
                    {
                        _animator.Play(walkWanderingAnimation.name);
                    }
                    // Set animation speed to match movement
                    _animator.speed = wanderAnimationSpeed;
                }
                else if (isWaitingBetweenSteps && idleAnimation != null)
                {
                    // Play idle animation when waiting between steps
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnimation.name))
                    {
                        _animator.Play(idleAnimation.name);
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
            float moveSpeed = useWalkForChasing ? staticWandering.wanderWalkSpeed : runSpeed;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Look at player
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            
            // Play appropriate chasing animation
            if (_animator != null)
            {
                AnimationClip chasingAnimation = useWalkForChasing ? walkChasingAnimation : runChasingAnimation;
                if (chasingAnimation != null)
                {
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(chasingAnimation.name))
                    {
                        _animator.Play(chasingAnimation.name);
                    }
                    // Set animation speed to match movement
                    _animator.speed = useWalkForChasing ? chaseWalkAnimationSpeed : chaseRunAnimationSpeed;
                }
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
            if (_animator != null && attackAnimation != null)
            {
                if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(attackAnimation.name))
                {
                    _animator.Play(attackAnimation.name);
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
            
            // Play death animation (front fall or back fall)
            if (_animator != null)
            {
                AnimationClip deathAnimation = useFrontFallDeath ? deathFrontFallAnimation : deathBackFallAnimation;
                if (deathAnimation != null)
                {
                    if (!_animator.GetCurrentAnimatorStateInfo(0).IsName(deathAnimation.name))
                    {
                        _animator.Play(deathAnimation.name);
                    }
                    // Reset animation speed to normal for death
                    _animator.speed = 1f;
                }
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
            
            if (idleAnimation == null)
            {
                missingCount++;
            }
            
            if (walkWanderingAnimation == null)
            {
                missingCount++;
            }
            
            if (walkChasingAnimation == null)
            {
                missingCount++;
            }
            
            if (runChasingAnimation == null)
            {
                missingCount++;
            }
            
            if (attackAnimation == null)
            {
                missingCount++;
            }
            
            if (deathBackFallAnimation == null)
            {
                missingCount++;
            }
            
            if (deathFrontFallAnimation == null)
            {
                missingCount++;
            }
            
            if (missingCount == 0)
            {
                // All animations assigned
            }
            else
            {
                // Some animations missing
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
