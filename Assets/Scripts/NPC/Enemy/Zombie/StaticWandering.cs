using UnityEngine;

namespace ZombieGame.NPC.Enemy.Zombie
{
    /// <summary>
    /// Handles static wandering functionality for zombies
    /// </summary>
    [System.Serializable]
    public class StaticWandering
    {
        [Header("Static Wandering Configuration")]
        [Tooltip("Enhanced wandering path - array of directions and distances")]
        public WanderStep[] wanderPath = new WanderStep[]
        {
            new WanderStep(WanderDirection.Forward, 5f),
            new WanderStep(WanderDirection.Right, 3f),
            new WanderStep(WanderDirection.Forward, 3f)
        };
        
        [Tooltip("Idle delay between wander steps (seconds)")]
        public float stepDelay = 1f;
        
        // Private variables for wandering state
        private Transform _zombieTransform;
        
        // Enhanced Wandering Variables
        private int _currentWaypointIndex = 0;
        private Vector3 _originalStartPosition;
        private float _stepDelayTimer = 0f;
        private bool _isWaitingBetweenSteps = false;
        private Vector3[] _waypointPath; // Pre-calculated exact waypoints to follow (includes return to start)
        private bool _wasWanderingBeforeChase = false;
        
        /// <summary>
        /// Initialize the static wandering system
        /// </summary>
        public void Initialize(Transform zombieTransform)
        {
            _zombieTransform = zombieTransform;
        }
        
        /// <summary>
        /// Start static wandering from current position
        /// </summary>
        public void StartWandering()
        {
            if (_zombieTransform == null) return;
            
            // Stop any existing wandering first
            StopWandering();
            
            // Initialize wandering
            InitializeWandering();
        }
        
        /// <summary>
        /// Stop static wandering and reset all state
        /// </summary>
        public void StopWandering()
        {
            // Reset all wandering variables
            _currentWaypointIndex = 0;
            _isWaitingBetweenSteps = false;
            _stepDelayTimer = 0f;
            _wasWanderingBeforeChase = false;
            _waypointPath = null;
        }
        
        /// <summary>
        /// Update wandering logic - call this in Update()
        /// Returns true if wandering is active and moving
        /// </summary>
        public bool UpdateWandering(float speed, bool disableMovement = false)
        {
            if (_zombieTransform == null || wanderPath == null || wanderPath.Length == 0) 
                return false;
            
            // Initialize if needed
            if (_waypointPath == null)
            {
                InitializeWandering();
            }
            
            // Handle idle delay between waypoints
            if (_isWaitingBetweenSteps)
            {
                _stepDelayTimer -= Time.deltaTime;
                if (_stepDelayTimer <= 0f)
                {
                    _isWaitingBetweenSteps = false;
                }
                return false; // Not moving during delay
            }
            
            // Check if we have valid waypoints
            if (_waypointPath == null || _waypointPath.Length == 0)
                return false;
            
            // Get current target waypoint
            Vector3 targetWaypoint = _waypointPath[_currentWaypointIndex];
            
            // Calculate direction to target waypoint
            Vector3 directionToTarget = (targetWaypoint - _zombieTransform.position).normalized;
            directionToTarget.y = 0; // Keep horizontal
            
            // Calculate how far we can move this frame
            float moveDistance = speed * Time.deltaTime;
            float distanceToTarget = Vector3.Distance(_zombieTransform.position, targetWaypoint);
            
            // Check if we can reach the waypoint this frame
            if (moveDistance >= distanceToTarget)
            {
                // Move directly to the exact waypoint position (only if movement is enabled)
                if (!disableMovement)
                {
                    _zombieTransform.position = targetWaypoint;
                }
                
                // Move to next waypoint
                _currentWaypointIndex++;
                
                // Check if completed all waypoints
                if (_currentWaypointIndex >= _waypointPath.Length)
                {
                    _currentWaypointIndex = 0; // Restart the cycle
                }
                
                // Start delay before next waypoint
                _stepDelayTimer = stepDelay;
                _isWaitingBetweenSteps = true;
                return false; // Just completed waypoint
            }
            else
            {
                // Look at target while moving (only if movement is enabled)
                if (!disableMovement && directionToTarget != Vector3.zero)
                {
                    _zombieTransform.rotation = Quaternion.LookRotation(directionToTarget);
                }
                
                // Move towards target waypoint (only if movement is enabled)
                if (!disableMovement)
                {
                    _zombieTransform.position += directionToTarget * moveDistance;
                }
                return true; // Currently moving
            }
        }
        
        /// <summary>
        /// Mark that wandering was interrupted by chase
        /// </summary>
        public void MarkChaseInterruption()
        {
            _wasWanderingBeforeChase = true;
        }
        
        /// <summary>
        /// Handle restoration after chase ends
        /// Returns true if should restore wandering, false if should go to idle
        /// </summary>
        public bool HandleChaseEnd()
        {
            if (_wasWanderingBeforeChase)
            {
                // Restore wandering and return to original position
                _wasWanderingBeforeChase = false;
                _currentWaypointIndex = 0; // Start from beginning
                return true; // Should continue wandering
            }
            
            return false; // Should go to idle
        }
        
        /// <summary>
        /// Get current wandering target position
        /// </summary>
        public Vector3 GetCurrentTarget()
        {
            if (_waypointPath == null || _currentWaypointIndex >= _waypointPath.Length)
                return Vector3.zero;
            return _waypointPath[_currentWaypointIndex];
        }
        
        /// <summary>
        /// Get current desired stop position (for gizmo display)
        /// </summary>
        public Vector3 GetCurrentDesiredStopPosition()
        {
            return GetCurrentTarget(); // Same as current target in new system
        }
        
        /// <summary>
        /// Check if currently waiting between steps
        /// </summary>
        public bool IsWaitingBetweenSteps()
        {
            return _isWaitingBetweenSteps;
        }
        
        /// <summary>
        /// Start wandering behavior - called from WanderingState
        /// </summary>
        public void StartWanderingBehavior()
        {
            StartWandering();
        }
        
        /// <summary>
        /// Stop wandering behavior - called from WanderingState
        /// </summary>
        public void StopWanderingBehavior()
        {
            StopWandering();
        }
        
        /// <summary>
        /// Update wandering behavior continuously - called from WanderingState
        /// </summary>
        public void UpdateWanderingBehavior(float movementSpeed)
        {
            UpdateWandering(movementSpeed);
        }
        
        /// <summary>
        /// Main wandering routine - handles all wandering logic
        /// </summary>
        public System.Collections.IEnumerator WanderingRoutine(float movementSpeed)
        {
            while (true)
            {
                UpdateWandering(movementSpeed);
                yield return null;
            }
        }

        /// <summary>
        /// Draw gizmos for wandering path visualization
        /// </summary>
        public void DrawGizmos(Transform zombieTransform, bool isCurrentlyWandering)
        {
            if (zombieTransform == null) return;
            
            // Draw waypoint path
            if (_waypointPath != null && _waypointPath.Length > 0)
            {
                // Draw start position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(zombieTransform.position, 0.3f);
                
                // Draw lines between waypoints
                Vector3 previousPos = zombieTransform.position;
                for (int i = 0; i < _waypointPath.Length; i++)
                {
                    // Color coding for different waypoints
                    if (i == _waypointPath.Length - 1)
                    {
                        Gizmos.color = Color.yellow; // Final waypoint (return to start)
                    }
                    else
                    {
                        Gizmos.color = Color.cyan; // Regular waypoints
                    }
                    
                    // Draw line to waypoint
                    Gizmos.DrawLine(previousPos, _waypointPath[i]);
                    
                    // Draw waypoint sphere
                    Gizmos.DrawWireSphere(_waypointPath[i], 0.2f);
                    
                    previousPos = _waypointPath[i];
                }
                
                // Draw current target if wandering
                if (isCurrentlyWandering && _currentWaypointIndex < _waypointPath.Length)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(_waypointPath[_currentWaypointIndex], 0.25f);
                }
            }
            else if (wanderPath != null && wanderPath.Length > 0)
            {
                // Draw potential path preview when not wandering
                Vector3 currentPos = zombieTransform.position;
                Vector3 startPos = zombieTransform.position;
                Quaternion currentRotation = zombieTransform.rotation;
                
                // Draw the forward path segments
                for (int i = 0; i < wanderPath.Length; i++)
                {
                    Gizmos.color = Color.blue; // Preview path color
                    
                    Vector3 stepDirection = GetStepDirection(wanderPath[i], currentRotation);
                    Vector3 nextPos = currentPos + stepDirection * wanderPath[i].distance;
                    
                    Gizmos.DrawLine(currentPos, nextPos);
                    Gizmos.DrawWireSphere(nextPos, 0.15f);
                    
                    currentPos = nextPos;
                    currentRotation = Quaternion.LookRotation(stepDirection);
                }
                
                // Draw the closing line (direct return to start) - completes the loop
                Gizmos.color = Color.gray;
                Gizmos.DrawLine(currentPos, startPos);
                
                // Mark start position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(startPos, 0.2f);
            }
        }
        
        // Private Methods
        
        /// <summary>
        /// Initialize wandering system
        /// </summary>
        private void InitializeWandering()
        {
            _originalStartPosition = _zombieTransform.position;
            _currentWaypointIndex = 0;
            _isWaitingBetweenSteps = false;
            
            // Calculate all waypoints including return to start
            CalculateWaypointPath();
        }
        
        /// <summary>
        /// Calculate exact waypoint positions for the zombie to follow
        /// </summary>
        private void CalculateWaypointPath()
        {
            if (wanderPath == null || wanderPath.Length == 0) return;
            
            // Create waypoint array: each wander step + return to start
            _waypointPath = new Vector3[wanderPath.Length + 1];
            
            Vector3 currentPos = _originalStartPosition;
            Quaternion currentRotation = _zombieTransform.rotation;
            
            // Calculate each waypoint position
            for (int i = 0; i < wanderPath.Length; i++)
            {
                Vector3 stepDirection = GetStepDirection(wanderPath[i], currentRotation);
                currentPos += stepDirection * wanderPath[i].distance;
                _waypointPath[i] = currentPos;
                
                // Update rotation for next step (relative to current orientation)
                currentRotation = Quaternion.LookRotation(stepDirection);
            }
            
            // Add return to start as final waypoint
            _waypointPath[wanderPath.Length] = _originalStartPosition;
        }
        
        /// <summary>
        /// Get direction vector for a wander step
        /// </summary>
        private Vector3 GetStepDirection(WanderStep step, Quaternion currentRotation)
        {
            Vector3 forward = currentRotation * Vector3.forward;
            Vector3 right = currentRotation * Vector3.right;
            
            switch (step.direction)
            {
                case WanderDirection.Forward:
                    return forward;
                case WanderDirection.Right:
                    return right;
                case WanderDirection.Left:
                    return -right;
                case WanderDirection.Backward:
                    return -forward;
                case WanderDirection.ForwardWithAngle:
                    return Quaternion.AngleAxis(step.angle, Vector3.up) * forward;
                case WanderDirection.BackwardWithAngle:
                    return Quaternion.AngleAxis(step.angle, Vector3.up) * (-forward);
                default:
                    return forward;
            }
        }
        

    }
} 