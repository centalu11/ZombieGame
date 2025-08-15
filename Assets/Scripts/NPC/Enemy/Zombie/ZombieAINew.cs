using UnityEngine;
using System;
using ZombieGame.NPC.Enemy.Zombie.Structs;

namespace ZombieGame.NPC.Enemy.Zombie
{
    /// <summary>
    /// Centralized AI controller for zombies
    /// Manages state transitions based on detection events
    /// </summary>
    public class ZombieAINew : MonoBehaviour
    {
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
        
        [Header("Initial State")]
        [Tooltip("Start in wandering state instead of idle")]
        public bool isWanderingInitially = false;
        
        [Header("Visual Feedback")]
        [Tooltip("Whether to show debug information")]
        public bool showDebugInfo = true;
        
        [Header("Vision Settings")]
        [Tooltip("Optional main eyes GameObject to get VisionDetector from")]
        public GameObject mainEyes;
        
        // AI Components (auto-found)
        private VisionDetector visionDetector;
        private DetectingState detectingState;
        private ChasingState chasingState;
        private WanderingState wanderingState;
        
        // Current state
        private ZombieState currentState = ZombieState.Idle;
        
        // State tracking for chase restoration
        private ZombieState stateBeforeChase = ZombieState.Idle;
        
        // Components
        private Animator animator;
        private UnityEngine.AI.NavMeshAgent navAgent;
        private Transform playerTransform;
        
        // Events
        public System.Action<ZombieState> OnStateChanged;
        
        public enum ZombieState
        {
            Idle,
            Wandering,
            Detecting,
            Chasing
        }
        
        private void Awake()
        {
            // Get required components
            animator = GetComponent<Animator>();
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            
            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // Get VisionDetector from mainEyes if assigned, otherwise from self
            if (mainEyes != null && visionDetector == null)
            {
                visionDetector = mainEyes.GetComponent<VisionDetector>();
            }

            if (detectingState == null)
                detectingState = GetComponent<DetectingState>();
            
            if (chasingState == null)
                chasingState = GetComponent<ChasingState>();
            
            if (wanderingState == null)
                wanderingState = GetComponent<WanderingState>();
        }
        
        private void Start()
        {
            // Initialize animation data
            InitializeAnimation();
            
            // Subscribe to vision detection events
            if (visionDetector != null)
            {
                visionDetector.OnPlayerDetectedNear += OnPlayerDetectedNear;
                visionDetector.OnPlayerDetectedFar += OnPlayerDetectedFar;
                visionDetector.OnPlayerLost += OnPlayerLostFromVision;
            }
            
            // Subscribe to state events
            if (detectingState != null)
            {
                detectingState.OnInvestigationComplete += OnDetectionComplete;
                detectingState.OnDetectionTimeout += OnDetectionTimeout;
            }
            
            if (chasingState != null)
            {
                chasingState.OnChaseEnd += OnChaseEnd;
                chasingState.OnChaseTimeout += OnChaseTimeout;
            }
            
            // Set initial state
            if (isWanderingInitially)
            {
                // Start in wandering state
                if (wanderingState != null)
                {
                    wanderingState.EnterWanderingState();
                    currentState = ZombieState.Wandering;
                    OnStateChanged?.Invoke(currentState);
                }
            }
            else
            {
                // Start in idle state
                if (wanderingState != null)
                {
                    wanderingState.EnterIdleState();
                    currentState = ZombieState.Idle;
                    OnStateChanged?.Invoke(currentState);
                }
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (visionDetector != null)
            {
                visionDetector.OnPlayerDetectedNear -= OnPlayerDetectedNear;
                visionDetector.OnPlayerDetectedFar -= OnPlayerDetectedFar;
                visionDetector.OnPlayerLost -= OnPlayerLostFromVision;
            }
            
            if (detectingState != null)
            {
                detectingState.OnInvestigationComplete -= OnDetectionComplete;
                detectingState.OnDetectionTimeout -= OnDetectionTimeout;
            }
            
            if (chasingState != null)
            {
                chasingState.OnChaseEnd -= OnChaseEnd;
                chasingState.OnChaseTimeout -= OnChaseTimeout;
            }
        }
        
        /// <summary>
        /// Initialize animation data and pass to state components
        /// </summary>
        private void InitializeAnimation()
        {
            // Check if animator exists
            if (animator == null)
            {
                Debug.LogError("[ZombieAINew] No Animator component found!");
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
            
            // Apply dynamic animation overrides (if DynamicAnimatorController exists)
            // Note: This requires the DynamicAnimatorController class to be available
            DynamicAnimatorController.ApplyOverrides(
                animator,
                animator.runtimeAnimatorController,
                idleAnimation,
                wanderWalkAnimation,
                chaseWalkAnimation,
                chaseRunAnimation,
                attackAnimation,
                deathBackAnimation,
                deathFrontAnimation
            );
            
            // Pass chase animation data to ChasingState if available
            if (chasingState != null)
            {
                chasingState.InitializeAnimation(chaseWalkAnimation, chaseRunAnimation, useWalkForChasing);
            }
            
            // Pass wander animation data to WanderingState if available
            if (wanderingState != null)
            {
                wanderingState.InitializeAnimation(wanderWalkAnimation, idleAnimation);
            }
            

            
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Animation data initialized");
            }
        }

        /// <summary>
        /// Called when player is detected at far range
        /// </summary>
        private void OnPlayerDetectedFar(Transform target, int level)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Player detected at FAR range - Level {level}");
            }
            
            // Handle based on detection level
            if (level == 1)
            {
                // Level 1: Enter detecting state (investigation)
                if (currentState == ZombieState.Idle || currentState == ZombieState.Wandering)
                {
                    if (detectingState != null)
                    {
                        if (showDebugInfo)
                        {
                            Debug.Log($"[{gameObject.name}] Entering Detecting State (Level 1)");
                        }
                        EnterDetectingState(target.position);
                    }
                    else
                    {
                        // No detecting state component, immediately chase
                        if (showDebugInfo)
                        {
                            Debug.Log($"[{gameObject.name}] No DetectingState component - Immediately chasing");
                        }
                        EnterChasingState(target);
                    }
                }
                // If already in Detecting or Chasing state, ignore
            }
            else if (level == 2)
            {
                // Level 2: Enter chasing state (auto-chase after detecting)
                if (currentState == ZombieState.Detecting)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"[{gameObject.name}] Auto-chase triggered from Detecting State (Level 2)");
                    }
                    EnterChasingState(target);
                }
                else if (currentState == ZombieState.Idle || currentState == ZombieState.Wandering)
                {
                    // If somehow we get level 2 from idle/wandering, go straight to chase
                    if (showDebugInfo)
                    {
                        Debug.Log($"[{gameObject.name}] Level 2 detection from Idle/Wandering - Entering Chasing State");
                    }
                    EnterChasingState(target);
                }
                // If already in Chasing state, ignore
            }
        }

        /// <summary>
        /// Called when player is detected at near range
        /// </summary>
        private void OnPlayerDetectedNear(Transform target)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Player detected at NEAR range - Entering Chasing State");
            }
            
            EnterChasingState(target);
        }
        
        /// <summary>
        /// Called when player is lost from vision detector (with position parameter)
        /// </summary>
        private void OnPlayerLostFromVision(Vector3 lastSeenPosition)
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Player lost from vision - Last seen at: {lastSeenPosition}");
            }
            
            // Handle based on current state
            switch (currentState)
            {
                case ZombieState.Detecting:
                    if (detectingState != null)
                    {
                        detectingState.ForceExitDetecting();
                    }
                    break;
                    
                case ZombieState.Chasing:
                    if (chasingState != null)
                    {
                        chasingState.OnTargetLost();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Enter detecting state
        /// </summary>
        private void EnterDetectingState(Vector3 playerPosition)
        {
            if (currentState == ZombieState.Detecting) return;
            
            // Exit current state
            ExitCurrentState();
            
            // Enter detecting state
            currentState = ZombieState.Detecting;
            
            if (detectingState != null)
            {
                detectingState.EnterDetectingState(playerPosition);
            }
            
            OnStateChanged?.Invoke(currentState);
            
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Entered Detecting State");
            }
        }
        
        /// <summary>
        /// Enter chasing state
        /// </summary>
        private void EnterChasingState(Transform target)
        {
            if (chasingState == null || currentState == ZombieState.Chasing) return;
            
            // Save current state before entering chase
            stateBeforeChase = currentState;
            
            // Mark chase interruption in wandering state if currently wandering
            if (currentState == ZombieState.Wandering && wanderingState != null)
            {
                wanderingState.MarkChaseInterruption();
            }
            
            // Exit current state
            ExitCurrentState();
            
            // Enter chasing state
            currentState = ZombieState.Chasing;
            
            if (chasingState != null)
            {
                chasingState.EnterChase(target);
            }
            
            OnStateChanged?.Invoke(currentState);
            
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Entered Chasing State");
            }
        }
        
        /// <summary>
        /// Exit current state
        /// </summary>
        private void ExitCurrentState()
        {
            switch (currentState)
            {
                case ZombieState.Idle:
                    if (wanderingState != null)
                    {
                        wanderingState.ExitIdleState();
                    }
                    break;
                    
                case ZombieState.Wandering:
                    if (wanderingState != null)
                    {
                        wanderingState.ExitWanderingState();
                    }
                    break;
                    
                case ZombieState.Detecting:
                    if (detectingState != null)
                    {
                        detectingState.ExitDetectingState();
                    }
                    break;
                    
                case ZombieState.Chasing:
                    if (chasingState != null)
                    {
                        chasingState.ExitChase();
                    }
                    break;
            }
            
            currentState = ZombieState.Idle;
        }
        
        // Event handlers for state components
        private void OnDetectionComplete()
        {
            // This should get the target from detecting state to be transitioned to chasing state
        }
        
        private void OnDetectionTimeout()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Detection Timeout - Returning to Idle");
            }
            
            if (wanderingState != null)
            {
                wanderingState.EnterIdleState();
                currentState = ZombieState.Idle;
                OnStateChanged?.Invoke(currentState);
            }
        }
        
        private void OnChaseEnd()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Chase End - Returning to {stateBeforeChase}");
            }
            
            // Restore the state that was active before chase
            switch (stateBeforeChase)
            {
                case ZombieState.Wandering:
                    if (wanderingState != null)
                    {
                        currentState = ZombieState.Wandering;
                        wanderingState.EnterWanderingState();
                        OnStateChanged?.Invoke(currentState);
                    }
                    else
                    {
                        // Fallback to idle if wandering state not available
                        currentState = ZombieState.Idle;
                        OnStateChanged?.Invoke(currentState);
                    }
                    break;
                    
                case ZombieState.Idle:
                default:
                    if (wanderingState != null)
                    {
                        wanderingState.EnterIdleState();
                        currentState = ZombieState.Idle;
                        OnStateChanged?.Invoke(currentState);
                    }
                    break;
            }
        }
        
        private void OnChaseTimeout()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Chase Timeout - Returning to Idle");
            }
            
            if (wanderingState != null)
            {
                wanderingState.EnterIdleState();
                currentState = ZombieState.Idle;
                OnStateChanged?.Invoke(currentState);
            }
        }

        // Public methods for external control
        public ZombieState GetCurrentState()
        {
            return currentState;
        }
        
        public bool IsChasing()
        {
            return currentState == ZombieState.Chasing;
        }
        
        public bool IsDetecting()
        {
            return currentState == ZombieState.Detecting;
        }
        
        public bool IsWandering()
        {
            return currentState == ZombieState.Wandering;
        }
        
        public bool IsIdle()
        {
            return currentState == ZombieState.Idle;
        }
        
        public void ForceExitCurrentState()
        {
            ExitCurrentState();
        }
        
        /// <summary>
        /// Draw debug information
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;
        }
    }
} 