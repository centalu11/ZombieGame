using UnityEngine;
using System.Collections;
using ZombieGame.NPC.Enemy.Zombie.Structs;

namespace ZombieGame.NPC.Enemy.Zombie
{
    public class ChasingState : MonoBehaviour
    {
        // Behavior Group
        [Header("Chase Behavior")]
        [Tooltip("How long to continue chasing after losing sight/audio detection before giving up (enhances gameplay by preventing immediate chase exit)")]
        public float chasePersistenceTime = 20f;

        [Tooltip("Maximum distance the zombie will chase before giving up")]
        public float maxChaseDistance = 50f;

        [Tooltip("How often to update chase target position")]
        [Range(0.1f, 1f)]
        public float targetUpdateInterval = 0.2f;



        // Chase Animation Group


        // Visual Feedback Group
        [Header("Visual Feedback")]
        [Tooltip("Whether to show debug information")]
        public bool showDebugInfo = true;

        // Events for integration with other systems
        public System.Action<Vector3> OnChaseStart;
        public System.Action OnChaseEnd;
        public System.Action OnChaseTimeout;
        public System.Action<Transform> OnChasingTargetSet; // New event for when chasing target is set

        // Internal state
        private bool isInChaseState = false;
        private float lastTargetUpdateTime = 0f;

        // Chase persistence timer (similar to VisionDetector timers)
        private bool isChasePersistenceTimerActive = false;
        private float chasePersistenceAccumulatedTime = 0f;
        private Vector3 chaseTarget;
        private Vector3 lastKnownPlayerPosition;
        private Transform playerTransform;
        private Transform currentChaseTarget; // Store the actual target Transform

        // Animation data (set by ZombieAINew)
        private AnimationStateData chaseWalkAnimation;
        private AnimationStateData chaseRunAnimation;
        private bool useWalkForChasing;

        // Components
        private Animator animator;
        private UnityEngine.AI.NavMeshAgent navAgent;
        private Transform zombieTransform;

        // Coroutines
        private Coroutine chaseCoroutine;

        private void Awake()
        {
            // Get required components
            animator = GetComponent<Animator>();
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            zombieTransform = transform;

            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        /// <summary>
        /// Initialize animation data from ZombieAINew
        /// </summary>
        public void InitializeAnimation(AnimationStateData walkAnimation, AnimationStateData runAnimation, bool useWalk)
        {
            chaseWalkAnimation = walkAnimation;
            chaseRunAnimation = runAnimation;
            useWalkForChasing = useWalk;

            // Initialize animation clips
            chaseWalkAnimation.SetAnimationClip();
            chaseRunAnimation.SetAnimationClip();

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] ChasingState animation data initialized");
            }
        }

        /// <summary>
        /// Enter chasing state when player is detected at close range
        /// </summary>
        public void EnterChase(Transform target)
        {
            if (isInChaseState) return;

            // Reset everything to initial state first
            Reset();

            // Set new chase state
            isInChaseState = true;
            currentChaseTarget = target;
            chaseTarget = target.position;
            lastKnownPlayerPosition = target.position;

            // Start chasing behavior
            StartChasingBehavior();

            // Emit event that a chasing target has been set
            OnChasingTargetSet?.Invoke(target);
            OnChaseStart?.Invoke(target.position);

            // Register this zombie as chasing the player
            if (target != null)
            {
                var playerState = target.GetComponent<ZombieGame.Player.PlayerState>();
                if (playerState != null)
                {
                    playerState.RegisterChasingZombie(gameObject);
                }
            }

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Entered Chasing State - Target: {target.name} at {target.position}");
            }
        }

        /// <summary>
        /// Exit chasing state
        /// </summary>
        public void ExitChase()
        {
            if (!isInChaseState) return;

            // Reset animation
            SetChaseAnimation(false);

            // Stop movement
            if (HasNavMeshAgent())
            {
                navAgent.isStopped = true;
            }

            // Emit event that chasing target is cleared (set to null)
            OnChasingTargetSet?.Invoke(null);

            // Unregister this zombie from the player's chasing zombies list
            if (currentChaseTarget != null)
            {
                var playerState = currentChaseTarget.GetComponent<ZombieGame.Player.PlayerState>();
                if (playerState != null)
                {
                    playerState.UnregisterChasingZombie(gameObject);
                }
            }

            OnChaseEnd?.Invoke();

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Exited Chasing State");
            }

            // Reset everything to initial state
            Reset();
        }

        /// <summary>
        /// Start the chasing behavior
        /// </summary>
        private void StartChasingBehavior()
        {
            // Set chase animation
            SetChaseAnimation(true);
            
            // Start target update coroutine
            chaseCoroutine = StartCoroutine(TargetUpdateRoutine());
        }

        /// <summary>
        /// Coroutine to update target position at intervals
        /// </summary>
        private IEnumerator TargetUpdateRoutine()
        {
            while (isInChaseState)
            {
                UpdateChaseTarget();
                yield return new WaitForSeconds(targetUpdateInterval);
            }
        }

        /// <summary>
        /// Update the chase target position
        /// </summary>
        private void UpdateChaseTarget()
        {
            // Use the current chase target instead of trying to find player
            if (currentChaseTarget == null)
            {
                ExitChase();
                return;
            }

            Vector3 newTarget = currentChaseTarget.position;
            float distance = Vector3.Distance(zombieTransform.position, newTarget);

            // Check if player is too far away
            if (distance > maxChaseDistance)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[{gameObject.name}] UpdateChaseTarget: Target too far ({distance:F2} > {maxChaseDistance}) - exiting chase");
                }
                ExitChase();
                return;
            }

            // Update target position (movement happens separately in Update())
            chaseTarget = newTarget;
            lastKnownPlayerPosition = newTarget;
        }

        /// <summary>
        /// Check if NavMeshAgent is available and ready to use
        /// </summary>
        private bool HasNavMeshAgent()
        {
            return navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;
        }

        /// <summary>
        /// NavMesh-based chase logic
        /// </summary>
        private void NavMeshChase()
        {
            // Get movement speed
            float moveSpeed = useWalkForChasing ?
                chaseWalkAnimation.GetMovementSpeed() :
                chaseRunAnimation.GetMovementSpeed();

            // Use NavMeshAgent for pathfinding
            navAgent.isStopped = false;
            navAgent.speed = moveSpeed;
            navAgent.SetDestination(chaseTarget);

            // Set chase animation
            SetChaseAnimation(true);
        }

        /// <summary>
        /// Default chase logic when NavMeshAgent is not available
        /// </summary>
        private void DefaultChase()
        {
            // Get movement speed
            float moveSpeed = useWalkForChasing ?
                chaseWalkAnimation.GetMovementSpeed() :
                chaseRunAnimation.GetMovementSpeed();

            // Move towards target using simple transform movement
            Vector3 direction = (currentChaseTarget.position - zombieTransform.position).normalized;
            direction.y = 0; // Keep movement horizontal

            // Move towards target
            zombieTransform.position += direction * moveSpeed * Time.deltaTime;

            // Look at target
            if (direction != Vector3.zero)
            {
                zombieTransform.rotation = Quaternion.LookRotation(direction);
            }

            // Set chase animation
            SetChaseAnimation(true);
        }

        /// <summary>
        /// Update movement and persistence timer every frame
        /// </summary>
        private void Update()
        {
            if (!isInChaseState) return;

            // Perform movement every frame
            PerformChaseMovement();

            // Update chase persistence timer (only when active)
            ChasePersistenceTimer();
        }

        /// <summary>
        /// Perform the actual chase movement every frame
        /// </summary>
        private void PerformChaseMovement()
        {
            if (!isInChaseState || currentChaseTarget == null) return;

            // Choose chase method based on NavMeshAgent availability
            if (HasNavMeshAgent())
            {
                NavMeshChase();
            }
            else
            {
                DefaultChase();
            }
        }

        /// <summary>
        /// Set chase animation
        /// </summary>
        private void SetChaseAnimation(bool isChasing)
        {
            if (animator != null && isChasing)
            {
                string chasingStateName = useWalkForChasing ?
                    chaseWalkAnimation.GetStateName() :
                    chaseRunAnimation.GetStateName();

                animator.Play(chasingStateName);
                animator.speed = useWalkForChasing ?
                    chaseWalkAnimation.GetAnimationSpeed() :
                    chaseRunAnimation.GetAnimationSpeed();
            }
        }

        /// <summary>
        /// Get current chase state
        /// </summary>
        public bool IsInChaseState()
        {
            return isInChaseState;
        }

        /// <summary>
        /// Called when target is lost (no longer visible/audible)
        /// Starts the persistence timer
        /// </summary>
        public void OnTargetLost()
        {
            if (!isInChaseState) return;

            isChasePersistenceTimerActive = true;
            chasePersistenceAccumulatedTime = 0f;

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Target lost - starting chase persistence timer ({chasePersistenceTime}s)");
            }
        }

        /// <summary>
        /// Called when target reappears (becomes visible/audible again)
        /// Resets the persistence timer
        /// </summary>
        public void OnTargetReappear()
        {
            if (!isInChaseState) return;

            ResetChasePersistenceTimer();

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Target reappeared - resetting chase persistence timer");
            }
        }

        /// <summary>
        /// Get chase persistence timer progress (0-1)
        /// </summary>
        public float GetChasePersistenceProgress()
        {
            if (!isChasePersistenceTimerActive) return 0f;
            return chasePersistenceAccumulatedTime / chasePersistenceTime;
        }

        /// <summary>
        /// Update chase persistence timer
        /// </summary>
        private void ChasePersistenceTimer()
        {
            if (!isChasePersistenceTimerActive) return;

            chasePersistenceAccumulatedTime += Time.deltaTime;

            // Check if persistence timer has expired
            if (chasePersistenceAccumulatedTime >= chasePersistenceTime)
            {
                // Timer expired - target is completely lost
                if (showDebugInfo)
                {
                    Debug.Log($"[{gameObject.name}] Chase persistence timer expired after {chasePersistenceTime}s - target completely lost");
                }

                ExitChase();
                return;
            }
        }

        /// <summary>
        /// Reset chase persistence timer to 0 and deactivate it
        /// </summary>
        private void ResetChasePersistenceTimer()
        {
            isChasePersistenceTimerActive = false;
            chasePersistenceAccumulatedTime = 0f;
        }

                /// <summary>
        /// Reset all values to initial state
        /// </summary>
        private void Reset()
        {
            // Reset chase state
            isInChaseState = false;
            
            // Reset timer
            ResetChasePersistenceTimer();
            
            // Reset chase data
            chaseTarget = Vector3.zero;
            lastKnownPlayerPosition = Vector3.zero;
            currentChaseTarget = null;
            
            // Reset update timing
            lastTargetUpdateTime = 0f;
            
            // Stop coroutine
            if (chaseCoroutine != null)
            {
                StopCoroutine(chaseCoroutine);
                chaseCoroutine = null;
            }
        }

        /// <summary>
        /// Get the current chase target
        /// </summary>
        public Vector3 GetChaseTarget()
        {
            return chaseTarget;
        }

        /// <summary>
        /// Get the last known player position
        /// </summary>
        public Vector3 GetLastKnownPlayerPosition()
        {
            return lastKnownPlayerPosition;
        }

        /// <summary>
        /// Force exit chasing state (for external control)
        /// </summary>
        public void ForceExitChasing()
        {
            ExitChase();
        }

        /// <summary>
        /// Draw debug information
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo || !isInChaseState) return;

            // Draw chase target
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(chaseTarget, 0.5f);

            // Draw line to chase target
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, chaseTarget);

            // Draw chase persistence timer progress
            if (isInChaseState && isChasePersistenceTimerActive)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, GetChasePersistenceProgress());
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
            }
        }
    }
}