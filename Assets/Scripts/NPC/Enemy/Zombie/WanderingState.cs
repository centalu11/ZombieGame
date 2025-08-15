using UnityEngine;
using System.Collections;
using ZombieGame.NPC.Enemy.Zombie.Structs;

namespace ZombieGame.NPC.Enemy.Zombie
{
    /// <summary>
    /// Handles wandering behavior for zombies
    /// Currently implements static wandering, with navmesh wandering planned for future
    /// </summary>
    public class WanderingState : MonoBehaviour
    {
        [Header("Static Wandering")]
        [Tooltip("Static wandering component")]
        public StaticWandering staticWandering = new StaticWandering();
        
        [Header("Visual Feedback")]
        [Tooltip("Whether to show debug information")]
        public bool showDebugInfo = true;
        
        // Components
        private Animator animator;
        private Transform zombieTransform;
        private ZombieAINew zombieAI;
        
        // Wandering variables
        private Vector3 wanderTarget;
        private Vector3 desiredStopPosition;
        private bool isInWanderingState = false; // Local state tracking
        
        // Animation data (set by ZombieAINew)
        private AnimationStateData wanderWalkAnimation;
        private AnimationStateData idleAnimation;
        
        // Coroutine reference
        private Coroutine wanderingCoroutine;
        
        // State tracking
        private bool isInIdleState = false;
        
        private void Awake()
        {
            // Get required components
            animator = GetComponent<Animator>();
            zombieTransform = transform;
            zombieAI = GetComponent<ZombieAINew>();
            
            // Initialize static wandering
            staticWandering.Initialize(transform);
        }
        
        private void Update()
        {
            // Handle wandering state animation switching
            if (IsWandering() && staticWandering != null)
            {
                // Check if waiting between steps and switch animation accordingly
                if (staticWandering.IsWaitingBetweenSteps())
                {
                                    // Play idle animation when waiting between steps
                if (animator != null && !idleAnimation.IsNull())
                    {
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(idleAnimation.GetStateName()))
                        {
                            animator.Play(idleAnimation.GetStateName());
                            animator.speed = 1f; // Reset animation speed to normal for idle
                        }
                    }
                }
                else
                {
                    // Play wandering animation when moving between steps
                    if (animator != null && !wanderWalkAnimation.IsNull())
                    {
                        if (!animator.GetCurrentAnimatorStateInfo(0).IsName(wanderWalkAnimation.GetStateName()))
                        {
                            animator.Play(wanderWalkAnimation.GetStateName());
                            animator.speed = wanderWalkAnimation.GetAnimationSpeed();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Initialize animation data from ZombieAINew
        /// </summary>
        public void InitializeAnimation(AnimationStateData walkAnimation, AnimationStateData idleAnim)
        {
            wanderWalkAnimation = walkAnimation;
            idleAnimation = idleAnim;
            
            // Initialize animation clips
            wanderWalkAnimation.SetAnimationClip();
            idleAnimation.SetAnimationClip();
        }
        
        /// <summary>
        /// Enter wandering state
        /// </summary>
        public void EnterWanderingState()
        {
            // Check if already in wandering state
            if (IsWandering())
            {
                return;
            }
            
            // Start wandering behavior
            staticWandering.StartWanderingBehavior();
            
            // Update local state FIRST, then start animation
            isInWanderingState = true; // Update local state BEFORE calling SetWanderingAnimation
            
            // Start animation
            SetWanderingAnimation();
            
            // Start wandering update
            wanderingCoroutine = StartCoroutine(staticWandering.WanderingRoutine(GetMovementSpeed()));
        }
        
        /// <summary>
        /// Exit wandering state
        /// </summary>
        public void ExitWanderingState()
        {
            // Check if not in wandering state
            if (!IsWandering())
            {
                return;
            }
            
            // Stop wandering behavior
            staticWandering.StopWanderingBehavior();
            
            // Stop coroutine
            if (wanderingCoroutine != null)
            {
                StopCoroutine(wanderingCoroutine);
                wanderingCoroutine = null;
            }
            
            // Stop animation
            SetWanderingAnimation();
            isInWanderingState = false; // Update local state
        }
        
        /// <summary>
        /// Get movement speed from animation or default
        /// </summary>
        private float GetMovementSpeed()
        {
            return !wanderWalkAnimation.IsNull() ? 
                wanderWalkAnimation.GetMovementSpeed() : 1f; // Default to 1f if no animation data
        }
        
        /// <summary>
        /// Set wandering animation
        /// </summary>
        private void SetWanderingAnimation()
        {
            if (animator == null || wanderWalkAnimation.IsNull()) return;
            
            if (IsWandering())
            {
                animator.Play(wanderWalkAnimation.GetStateName());
                animator.speed = wanderWalkAnimation.GetAnimationSpeed();
            }
        }

        /// <summary>
        /// Get current wandering state
        /// </summary>
        public bool IsWandering()
        {
            return isInWanderingState;
        }

        /// <summary>
        /// Get current idle state
        /// </summary>
        public bool IsIdle()
        {
            return isInIdleState;
        }

        /// <summary>
        /// Force exit wandering state
        /// </summary>
        public void ForceExitWandering()
        {
            ExitWanderingState();
        }

        /// <summary>
        /// Mark that wandering was interrupted by chase
        /// </summary>
        public void MarkChaseInterruption()
        {
            if (staticWandering != null)
            {
                staticWandering.MarkChaseInterruption();
            }
        }

        /// <summary>
        /// Enter idle state
        /// </summary>
        public void EnterIdleState()
        {
            // Check if already in idle state
            if (isInIdleState)
            {
                return;
            }
            
            // Stop any wandering behavior
            if (isInWanderingState)
            {
                ExitWanderingState();
            }
            
            // Set idle state
            isInIdleState = true;
            
            // Set idle animation
            SetIdleAnimation();
        }

        /// <summary>
        /// Exit idle state
        /// </summary>
        public void ExitIdleState()
        {
            // Check if not in idle state
            if (!isInIdleState)
            {
                return;
            }
            
            // Clear idle state
            isInIdleState = false;
            
            // Stop idle animation
            SetIdleAnimation();
        }

        /// <summary>
        /// Set idle animation
        /// </summary>
        private void SetIdleAnimation()
        {
            if (animator == null || idleAnimation.IsNull()) return;
            
            if (isInIdleState)
            {
                animator.Play(idleAnimation.GetStateName());
                animator.speed = idleAnimation.GetAnimationSpeed();
            }
        }



        /// <summary>
        /// Draw debug gizmos for wandering visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;
            
            // Draw wandering path if available
            if (staticWandering != null)
            {
                staticWandering.DrawGizmos(transform, IsWandering());
            }
        }
    }
} 