using UnityEngine;
using System.Collections;

namespace ZombieGame.NPC.Enemy.Zombie
{
    [System.Serializable]
    public class DetectionStateData
    {
        [Header("Detection Behavior")]
        [Tooltip("How long to stay in detection state before giving up")]
        public float detectionTimeout = 5f;
        
        [Tooltip("How long to investigate the last seen position")]
        public float investigationTime = 3f;
        
        [Tooltip("Movement speed while investigating")]
        public float investigationSpeed = 1f;
        
        [Tooltip("How close to get to the investigation point")]
        public float investigationDistance = 1f;
        
        [Header("Visual Feedback")]
        [Tooltip("Whether to show debug information")]
        public bool showDebugInfo = true;
        
        [Header("Animation")]
        [Tooltip("Animation parameter name for detection state")]
        public string detectionAnimParam = "IsDetecting";
        
        [Tooltip("Animation parameter name for investigation")]
        public string investigationAnimParam = "IsInvestigating";
    }

    public class DetectingState : MonoBehaviour
    {
        [Header("Detecting State Settings")]
        public DetectionStateData stateData = new DetectionStateData();
        
        // Events for integration with other systems
        public System.Action<Vector3> OnInvestigationStart;
        public System.Action OnInvestigationComplete;
        public System.Action OnDetectionTimeout;
        public System.Action<Vector3> OnPlayerDetected;
        
        // Internal state
        private bool isInvestigating = false;
        private float detectionTimer = 0f;
        private float investigationTimer = 0f;
        private Vector3 investigationTarget;
        private Vector3 previousPosition;
        
        // Components
        private Animator animator;
        private UnityEngine.AI.NavMeshAgent navAgent;
        private Transform zombieTransform;
        private ZombieAINew zombieAI;
        
        // Coroutines
        private Coroutine investigationCoroutine;
        
        private void Awake()
        {
            // Get required components
            animator = GetComponent<Animator>();
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            zombieTransform = transform;
            zombieAI = GetComponent<ZombieAINew>();
        }
        
        /// <summary>
        /// Enter detecting state when player is detected at far range
        /// </summary>
        public void EnterDetectingState(Vector3 lastSeenPosition)
        {
            if (IsDetecting()) return;
            
            detectionTimer = 0f;
            previousPosition = zombieTransform.position;
            investigationTarget = lastSeenPosition;
            
            // Start detecting behavior
            StartDetectingBehavior();
            
            if (stateData.showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Entered Detecting State - Last seen at: {lastSeenPosition}");
            }
        }
        
        /// <summary>
        /// Exit detecting state
        /// </summary>
        public void ExitDetectingState()
        {
            if (!IsDetecting()) return;
            
            isInvestigating = false;
            detectionTimer = 0f;
            investigationTimer = 0f;
            
            // Stop any ongoing coroutines
            if (investigationCoroutine != null)
            {
                StopCoroutine(investigationCoroutine);
                investigationCoroutine = null;
            }
            
            // Reset animation
            SetDetectionAnimation(false);
            SetInvestigationAnimation(false);
            
            // Stop movement
            if (navAgent != null)
            {
                navAgent.isStopped = true;
            }
            
            if (stateData.showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Exited Detecting State");
            }
        }
        
        /// <summary>
        /// Start the detecting behavior (facing direction, then investigating)
        /// </summary>
        private void StartDetectingBehavior()
        {
            // Face the direction of the last seen position
            FaceDirection(investigationTarget);
            
            // Start investigation after a short delay
            investigationCoroutine = StartCoroutine(InvestigationSequence());
        }
        
        /// <summary>
        /// Face towards a specific direction
        /// </summary>
        private void FaceDirection(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - zombieTransform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                zombieTransform.rotation = targetRotation;
            }
        }
        
        /// <summary>
        /// Investigation sequence: move to last seen position, then wait
        /// </summary>
        private IEnumerator InvestigationSequence()
        {
            // Wait a moment before starting investigation
            yield return new WaitForSeconds(0.5f);
            
            // Start investigation
            StartInvestigation();
            
            // Move to investigation target
            yield return StartCoroutine(MoveToPosition(investigationTarget));
            
            // Wait at investigation point
            yield return StartCoroutine(WaitAtInvestigationPoint());
            
            // Complete investigation
            CompleteInvestigation();
        }
        
        /// <summary>
        /// Start investigation behavior
        /// </summary>
        private void StartInvestigation()
        {
            isInvestigating = true;
            investigationTimer = 0f;
            
            SetInvestigationAnimation(true);
            OnInvestigationStart?.Invoke(investigationTarget);
            
            if (stateData.showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Started investigating: {investigationTarget}");
            }
        }
        
        /// <summary>
        /// Move to a specific position
        /// </summary>
        private IEnumerator MoveToPosition(Vector3 targetPosition)
        {
            if (navAgent == null) yield break;
            
            navAgent.isStopped = false;
            navAgent.speed = stateData.investigationSpeed;
            navAgent.SetDestination(targetPosition);
            
            while (navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathPartial || 
                   navAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
            {
                // Try to find a valid path or give up
                yield return new WaitForSeconds(0.1f);
            }
            
            // Wait until we reach the destination
            while (navAgent.remainingDistance > stateData.investigationDistance)
            {
                yield return null;
            }
            
            navAgent.isStopped = true;
        }
        
        /// <summary>
        /// Wait at the investigation point
        /// </summary>
        private IEnumerator WaitAtInvestigationPoint()
        {
            float waitTime = 0f;
            
            while (waitTime < stateData.investigationTime)
            {
                waitTime += Time.deltaTime;
                investigationTimer = waitTime;
                yield return null;
            }
        }
        
        /// <summary>
        /// Complete investigation and decide next action
        /// </summary>
        private void CompleteInvestigation()
        {
            isInvestigating = false;
            SetInvestigationAnimation(false);
            
            OnInvestigationComplete?.Invoke();
            
            if (stateData.showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] Investigation complete");
            }
            
            // Exit detecting state - let the main AI controller decide what to do next
            ExitDetectingState();
        }
        
        /// <summary>
        /// Update detection timer and check for timeout
        /// </summary>
        private void Update()
        {
            if (!IsDetecting()) return;
            
            detectionTimer += Time.deltaTime;
            
            // Check for detection timeout
            if (detectionTimer >= stateData.detectionTimeout)
            {
                OnDetectionTimeout?.Invoke();
                ExitDetectingState();
                
                if (stateData.showDebugInfo)
                {
                    Debug.Log($"[{gameObject.name}] Detection timeout");
                }
            }
        }
        
        /// <summary>
        /// Set detection animation
        /// </summary>
        private void SetDetectionAnimation(bool isDetecting)
        {
            if (animator != null && !string.IsNullOrEmpty(stateData.detectionAnimParam))
            {
                animator.SetBool(stateData.detectionAnimParam, isDetecting);
            }
        }
        
        /// <summary>
        /// Set investigation animation
        /// </summary>
        private void SetInvestigationAnimation(bool isInvestigating)
        {
            if (animator != null && !string.IsNullOrEmpty(stateData.investigationAnimParam))
            {
                animator.SetBool(stateData.investigationAnimParam, isInvestigating);
            }
        }
        
        /// <summary>
        /// Get current detecting state
        /// </summary>
        public bool IsDetecting()
        {
            return zombieAI != null && zombieAI.IsDetecting();
        }
        
        /// <summary>
        /// Get current investigation state
        /// </summary>
        public bool IsInvestigating()
        {
            return isInvestigating;
        }
        
        /// <summary>
        /// Get detection timer progress (0-1)
        /// </summary>
        public float GetDetectionTimerProgress()
        {
            return detectionTimer / stateData.detectionTimeout;
        }
        
        /// <summary>
        /// Get investigation timer progress (0-1)
        /// </summary>
        public float GetInvestigationTimerProgress()
        {
            return investigationTimer / stateData.investigationTime;
        }
        
        /// <summary>
        /// Get the current investigation target
        /// </summary>
        public Vector3 GetInvestigationTarget()
        {
            return investigationTarget;
        }
        
        /// <summary>
        /// Force exit detecting state (for external control)
        /// </summary>
        public void ForceExitDetecting()
        {
            ExitDetectingState();
        }
        
        /// <summary>
        /// Draw debug information
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!stateData.showDebugInfo || !IsDetecting()) return;
            
            // Draw investigation target
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(investigationTarget, 0.5f);
            
            // Draw line to investigation target
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, investigationTarget);
            
            // Draw detection timer progress
            if (IsDetecting())
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, GetDetectionTimerProgress());
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
            }
        }
    }
} 