using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZombieGame.Core;

namespace ZombieGame.NPC.Enemy.Zombie
{
    public class VisionDetector : MonoBehaviour
    {
        [Header("Vision Ranges")]
        [Tooltip("Close detection range - instant chase")]
        public float nearVisionRange = 15f;

        [Tooltip("Extended detection range - requires detection time")]
        public float farVisionRange = 30f;

        [Header("Vision Cone")]
        [Tooltip("Field of view in degrees for near detection")]
        [Range(0f, 360f)]
        public float visionAngle = 90f;

        [Tooltip("Field of view in degrees for far detection (narrower)")]
        [Range(0f, 360f)]
        public float farVisionAngle = 45f;

        [Tooltip("Vertical field of view in degrees (up and down from eye level)")]
        [Range(0f, 90f)]
        public float verticalVisionAngle = 60f;

        [Header("Detection Timing")]
        [Tooltip("Time in seconds required before enemy acts on far-vision detection (detecting state)")]
        public float detectionTimeThreshold = 5f;

        [Tooltip("Time in seconds after far detection before auto-chase triggers")]
        public float chaseTimeThreshold = 3f;

        [Tooltip("Time in seconds before losing target when not visible")]
        public float targetLostTimer = 11f;

        [Header("Optimization")]
        [Tooltip("How often to run vision checks (seconds)")]
        [Range(0.1f, 1f)]
        public float updateInterval = 0.2f;
        
        [Tooltip("Use frame-based updates instead of coroutine for instant response (unchecked = optimized coroutine updates)")]
        public bool useFrameUpdates = false;

        [Header("Vision Setup")]
        [Tooltip("Parent NPC GameObject where other scripts are attached (drag the main enemy NPC here)")]
        public GameObject parentObject;

        [Header("Layer Masks")]
        [Tooltip("Layers that can block vision raycasts (Buildings, Obstacles, Vehicles)")]
        public LayerMask blockingLayers;

        [Tooltip("Layers for targets that can be detected (Players, NPCs, HumanEnemies)")]
        public LayerMask targetLayers;

        // Combined layer mask for all raycast operations
        private LayerMask combinedLayerMask;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool showVisionCone = true;
        [SerializeField] private bool showDetectionRanges = true;

        // Events for integration with state scripts
        public System.Action<Transform> OnPlayerDetectedNear; // Instant chase - now passes target Transform
        public System.Action<Transform, int> OnPlayerDetectedFar; // Start detection timer with level (1=detecting, 2=chasing) - now passes target Transform
        public System.Action<Vector3> OnPlayerLost; // Player no longer visible
        public System.Action<Transform> SwitchVisionTarget; // Emitted when target switch timer completes

        // Internal state
        private List<Transform> allPlayers = new List<Transform>();
        private Vector3 lastSeenPosition;
        private Coroutine visionCheckCoroutine;

        // Target management
        private Transform target; // Target from ChasingScript (placeholder for now)

        // Reference to ChasingState for event subscription
        private ChasingState chasingState;

        // Store successful combinations for targets that passed vision cone check
        private Dictionary<Transform, List<TargetCombination>> targetSuccessfulCombinations = new Dictionary<Transform, List<TargetCombination>>();

        // Far distance detection timing
        private Transform farDetectionTarget; // Target being tracked for far detection
        private float farDetectionAccumulatedTime = 0f; // Accumulated detection time (can go up/down)
        private bool isFarDetectionActive = false; // Is far detection timer running
        private bool isTargetCurrentlyVisible = false; // Is target visible this frame for far detection

        // Chase timer (starts after far detection completes)
        private bool isChaseTimerActive = false; // Is chase timer running
        private float chaseAccumulatedTime = 0f; // Accumulated chase time (can go up/down)

        // Target switching timer
        private bool isTargetSwitchTimerActive = false; // Is target switch timer running
        private float targetSwitchAccumulatedTime = 0f; // Accumulated target switch time
        private const float TARGET_SWITCH_TIME_THRESHOLD = 2f; // Time before switching targets

        // Target tracking timing
        private bool isTargetVisible = false; // Is current target visible
        private float targetLostStartTime = 0f; // When target became not visible
        private float lastTargetRaycastTime = 0f; // Last time we raycasted to target
        private const float TARGET_RAYCAST_INTERVAL = 2f; // Raycast interval when target is visible

        // Performance optimization
        private float lastCheckTime = 0f;
        private float sqrNearRange;
        private float sqrFarRange;
        private float cosVisionAngle;
        private float cosFarVisionAngle;

        private void Start()
        {
            Init();

            // Start vision checking
            StartVisionChecking();
        }

        private void Init()
        {
            // Combine blocking and target layers for raycasts
            combinedLayerMask = blockingLayers | targetLayers;

            // Validate required inputs first
            ValidateInputs();

            // Get all players once at startup
            RefreshPlayersFromRegistry();

            // Pre-calculate performance values
            CalculatePerformanceValues();

            // Hook up to ChasingState events
            HookUpToChasingState();
        }

        /// <summary>
        /// Validate required inputs - requires parent NPC object reference
        /// </summary>
        private void ValidateInputs()
        {
            if (parentObject == null)
            {
                Debug.LogError($"[VisionDetector] {gameObject.name}: parentObject is not assigned! Please assign the main enemy NPC GameObject in the inspector.");
            }
        }

        /// <summary>
        /// Pre-calculate squared distances and cosine values for performance optimization
        /// </summary>
        private void CalculatePerformanceValues()
        {
            sqrNearRange = nearVisionRange * nearVisionRange;
            sqrFarRange = farVisionRange * farVisionRange;
            cosVisionAngle = Mathf.Cos(visionAngle * 0.5f * Mathf.Deg2Rad);
            cosFarVisionAngle = Mathf.Cos(farVisionAngle * 0.5f * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Get the eye position for raycast origin at runtime (now using current object's transform since script is attached to eye)
        /// </summary>
        private Vector3 GetEyePosition()
        {
            return transform.position; // Use current object's transform since script is attached to eye
        }

        private void OnEnable()
        {
            StartCoroutine(SubscribeToPlayerRegistry());
        }

        private void OnDisable()
        {
            UnsubscribeFromPlayerRegistry();
        }

        private IEnumerator SubscribeToPlayerRegistry()
        {
            while (ZombieGame.Core.PlayerRegistry.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            var registry = ZombieGame.Core.PlayerRegistry.Instance;
            if (registry != null)
            {
                registry.PlayersChanged += OnPlayerRegistryChanged;
            }

            // Stop the coroutine after successful registration
            StopCoroutine(SubscribeToPlayerRegistry());
        }

        private void UnsubscribeFromPlayerRegistry()
        {
            var registry = ZombieGame.Core.PlayerRegistry.Instance;
            if (registry != null)
            {
                registry.PlayersChanged -= OnPlayerRegistryChanged;
            }
        }

        private void OnPlayerRegistryChanged(IReadOnlyList<GameObject> players)
        {
            RefreshPlayersFromList(players);
        }

        private void OnDestroy()
        {
            StopVisionChecking();

            // Unsubscribe from ChasingState events
            if (chasingState != null)
            {
                chasingState.OnChasingTargetSet -= OnChasingTargetSet;
            }
        }

        /// <summary>
        /// Start the vision checking coroutine
        /// </summary>
        public void StartVisionChecking()
        {
            if (visionCheckCoroutine == null && !useFrameUpdates)
            {
                visionCheckCoroutine = StartCoroutine(VisionCheckRoutine());
            }
        }

        /// <summary>
        /// Stop the vision checking coroutine
        /// </summary>
        public void StopVisionChecking()
        {
            if (visionCheckCoroutine != null)
            {
                StopCoroutine(visionCheckCoroutine);
                visionCheckCoroutine = null;
            }
        }
        
        /// <summary>
        /// Toggle between frame updates and coroutine updates
        /// </summary>
        public void ToggleUpdateMode()
        {
            useFrameUpdates = !useFrameUpdates;
            
            if (useFrameUpdates)
            {
                // Stop coroutine when switching to frame updates
                StopVisionChecking();
                if (showDebugInfo)
                {
                    Debug.Log($"[VisionDetector] Switched to Frame Updates - Instant response");
                }
            }
            else
            {
                // Start coroutine when switching to optimized updates
                StartVisionChecking();
                if (showDebugInfo)
                {
                    Debug.Log($"[VisionDetector] Switched to Coroutine Updates - Optimized ({updateInterval}s interval)");
                }
            }
        }

        /// <summary>
        /// Main vision checking coroutine with sparse updates
        /// </summary>
        private IEnumerator VisionCheckRoutine()
        {
            while (true)
            {
                CheckVision();

                yield return new WaitForSeconds(updateInterval);
            }
        }

        /// <summary>
        /// Main vision checking logic - calls appropriate function based on whether we have a target
        /// </summary>
        private void CheckVision()
        {
            if (target != null)
            {
                CheckTarget();
            }
            else
            {
                CheckForNewTarget();
            }
        }

        /// <summary>
        /// Check for new targets when we don't have a current target
        /// </summary>
        private void CheckForNewTarget()
        {
            if (CheckForPlayerDetection(out Transform detectedTarget, out float distance))
            {
                // Update last seen position
                lastSeenPosition = detectedTarget.position;

                // Handle detection
                TargetDetected(detectedTarget, detectedTarget.position, distance * distance);
            }
            else
            {
                isTargetCurrentlyVisible = false;
            }
        }

        /// <summary>
        /// Check if we should switch to a better target while already chasing
        /// </summary>
        private void CheckForTargetSwitch()
        {
            // Only check for target switching if we have a current target
            if (target == null) return;

            // Get all players within NEAR range only (target switching should only happen at close range)
            List<Transform> targetsInRange = GetWithinRangeTargets(nearVisionOnly: true);
            if (targetsInRange.Count == 0)
            {
                if (isTargetSwitchTimerActive)
                {
                    ResetTargetSwitchTimer();
                }
                return;
            }
            // If there's only 1 target and it's the original target, reset timer and return immediately
            else if (targetsInRange.Count == 1 && targetsInRange[0] == target)
            {
                if (isTargetSwitchTimerActive)
                {
                    ResetTargetSwitchTimer();
                }
                return;
            }

            // Get players within NEAR vision cone only
            List<Transform> targetsInCone = GetWithinVisionTargets(targetsInRange, nearVisionOnly: true);
            if (targetsInCone.Count == 0)
            {
                if (isTargetSwitchTimerActive)
                {
                    ResetTargetSwitchTimer();
                }
                return;
            }
            // If there's only 1 target and it's the original target, reset timer and return immediately
            else if (targetsInCone.Count == 1 && targetsInCone[0] == target)
            {
                if (isTargetSwitchTimerActive)
                {
                    ResetTargetSwitchTimer();
                }
                return;
            }



            // Remove current target from consideration
            targetsInCone.RemoveAll(t => t == target);

            // Use the tested CheckLineOfSightDetection to find a potential target
            if (CheckLineOfSightDetection(targetsInCone, out Transform potentialTarget, out float bestDistance, target))
            {
                // If the returned target is the same as the original target, reset timer and don't start target switch
                if (potentialTarget == target)
                {
                    if (isTargetSwitchTimerActive)
                    {
                        if (showDebugInfo)
                    {
                        Debug.Log($"[VisionDetector] Original target {target.name} is visible - resetting target switch timer");
                        }
                        ResetTargetSwitchTimer();
                    }
                    return;
                }

                // Start target switch timer if not already running
                if (!isTargetSwitchTimerActive)
                {
                    StartTargetSwitchTimer(potentialTarget);
                }
            }
        }

        /// <summary>
        /// Start the target switch timer for a potential new target
        /// </summary>
        private void StartTargetSwitchTimer(Transform potentialTarget)
        {
            isTargetSwitchTimerActive = true;
            targetSwitchAccumulatedTime = 0f;
            if (showDebugInfo)
            {
            Debug.Log($"[VisionDetector] Starting target switch timer for {potentialTarget.name}");
            }
        }

        /// <summary>
        /// Check if current target is still visible using raycast
        /// </summary>
        private void CheckTarget()
        {
            if (target == null) return;

            // Check if we should switch to a better target first
            CheckForTargetSwitch();

            bool shouldRaycast = false;

            // Determine if we should raycast this frame
            if (isTargetVisible)
            {
                // Target was visible last check - raycast every 2 seconds
                if (Time.time - lastTargetRaycastTime >= TARGET_RAYCAST_INTERVAL)
                {
                    shouldRaycast = true;
                }
            }
            else
            {
                // Target is not visible - raycast every frame while timer is active
                shouldRaycast = true;
            }

            if (shouldRaycast)
            {
                bool targetStillVisible = IsTargetVisibleRaycast();
                lastTargetRaycastTime = Time.time;

                if (targetStillVisible && !isTargetVisible)
                {
                    // Target became visible again - reset timer
                    isTargetVisible = true;
                    targetLostStartTime = 0f;
                    if (showDebugInfo)
                    {
                    Debug.Log("[VisionDetector] Target became visible again");
                    }
                }
                else if (!targetStillVisible && isTargetVisible)
                {
                    // Target became not visible - start timer
                    isTargetVisible = false;
                    targetLostStartTime = Time.time;
                    if (showDebugInfo)
                    {
                    Debug.Log("[VisionDetector] Target lost from sight - starting timer");
                    }
                }
                // Timer logic moved to UpdateTargetLostTimer() - runs independently every frame
            }
        }

        /// <summary>
        /// Update far detection timer every frame for precise timing
        /// </summary>
        private void Update()
        {
            // Update far detection timer
            UpdateFarDetectionTimer();

            // Update chase timer
            UpdateChaseTimer();

            // Update target lost timer
            UpdateTargetLostTimer();

            // Update target switch timer
            UpdateTargetSwitchTimer();

            if (useFrameUpdates)
            {
                CheckVision();
            }
        }

        /// <summary>
        /// Refresh cached players list from PlayerRegistry helper.
        /// </summary>
        private void RefreshPlayersFromRegistry()
        {
            var registry = ZombieGame.Core.PlayerRegistry.Instance;
            if (registry == null) return;
            var players = registry.GetAllPlayers();
            RefreshPlayersFromList(players);
        }

        /// <summary>
        /// Refresh cached players list from provided players list.
        /// </summary>
        private void RefreshPlayersFromList(IReadOnlyList<GameObject> players)
        {
            allPlayers.Clear();
            if (players == null || players.Count == 0) return;
            for (int i = 0; i < players.Count; i++)
            {
                var go = players[i];
                if (go != null) allPlayers.Add(go.transform);
            }
        }

        /// <summary>
        /// Phase 1: Check if any target is within detection radius
        /// </summary>
        /// <param name="nearVisionOnly">If true, only check near vision range. If false, check far vision range.</param>
        /// <returns>List of targets within detection range</returns>
        private List<Transform> GetWithinRangeTargets(bool nearVisionOnly = false)
        {
            List<Transform> targetsInRange = new List<Transform>();
            if (allPlayers.Count == 0) return targetsInRange;

            float rangeToCheck = nearVisionOnly ? sqrNearRange : sqrFarRange;

            foreach (Transform targetEntity in allPlayers)
            {
                if (targetEntity == null) continue;

                float sqrDistance = (targetEntity.position - transform.position).sqrMagnitude;

                // Check if target is within detection radius
                if (sqrDistance <= rangeToCheck)
                {
                    targetsInRange.Add(targetEntity);
                }
            }

            return targetsInRange;
        }

        /// <summary>
        /// Phase 2: Check vision cone for targets in range (using appropriate cone based on distance)
        /// </summary>
        /// <param name="targetsInRange">Targets that passed Phase 1</param>
        /// <param name="nearVisionOnly">If true, only use near vision cone logic. If false, use both near and far vision cones.</param>
        /// <returns>List of targets within vision cone</returns>
        private List<Transform> GetWithinVisionTargets(List<Transform> targetsInRange, bool nearVisionOnly = false)
        {
            // Clear previous successful combinations
            targetSuccessfulCombinations.Clear();

            List<Transform> targetsInCone = new List<Transform>();

            if (targetsInRange.Count == 0) return targetsInCone;

            foreach (Transform targetEntity in targetsInRange)
            {
                // Check if target has TargetScript component first
                TargetScript targetScript = targetEntity.GetComponent<TargetScript>();
                if (targetScript == null) continue; // Skip if no TargetScript

                // Get combinations for this detector
                var applicableCombinations = targetScript.GetCombinationsForDetector(parentObject);
                bool hasSuccessfulCombination = false;

                // Check which combinations have ALL body parts within the vision cone
                List<TargetCombination> successfulCombinations = new List<TargetCombination>();

                foreach (var combination in applicableCombinations)
                {
                    bool combinationSuccessful = true;

                    // ALL body parts in this combination must be within the vision cone
                    foreach (var targetObject in combination.targetObjects)
                    {
                        if (targetObject == null) continue;

                        // Check if this body part is within vision cone
                        Vector3 directionToBodyPart = (targetObject.transform.position - transform.position).normalized;
                        // Calculate vertical angle relative to the eye's current forward direction
                        float totalAngle = Vector3.Angle(transform.forward, directionToBodyPart);

                        // Calculate horizontal angle for this body part
                        Vector3 forwardFlat = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                        Vector3 directionFlat = new Vector3(directionToBodyPart.x, 0, directionToBodyPart.z).normalized;
                        float horizontalAngle = Vector3.Angle(forwardFlat, directionFlat);
                        
                        // Calculate vertical angle using Pythagorean theorem
                        float verticalAngle = Mathf.Sqrt(totalAngle * totalAngle - horizontalAngle * horizontalAngle);

                        float sqrDistance = (targetObject.transform.position - transform.position).sqrMagnitude;

                        // Use appropriate vision cone based on distance
                        bool inHorizontalCone = false;
                        bool inVerticalCone = verticalAngle <= verticalVisionAngle * 0.5f;

                        if (nearVisionOnly)
                        {
                            // Near vision only: always use near vision cone
                            inHorizontalCone = horizontalAngle <= visionAngle * 0.5f;
                        }
                        else
                        {
                            // Both vision types: use appropriate cone based on distance
                            if (sqrDistance <= sqrNearRange)
                            {
                                // Near range: use wider cone (normal vision angle)
                                inHorizontalCone = horizontalAngle <= visionAngle * 0.5f;
                            }
                            else
                            {
                        // Far range: use narrower cone (far vision angle)
                                inHorizontalCone = horizontalAngle <= farVisionAngle * 0.5f;
                            }
                        }

                        // This body part must be within BOTH horizontal and vertical cones
                        if (!inHorizontalCone || !inVerticalCone)
                        {
                            // This body part is outside the cone, so this combination fails
                            combinationSuccessful = false;
                            break; // Break out of inner loop (body parts), but continue checking other combinations
                        }
                    }

                    // If this combination is successful, add it to the list
                    if (combinationSuccessful)
                    {
                        successfulCombinations.Add(combination);
                        hasSuccessfulCombination = true;
                    }
                }

                {
                    targetsInCone.Add(targetEntity);
                    // Store the successful combinations for this target
                    targetSuccessfulCombinations[targetEntity] = successfulCombinations;

                    // Log successful combinations
                    if (showDebugInfo)
                    {
                        Debug.Log($"[VisionDetector] Target {targetEntity.name} has {successfulCombinations.Count} successful combinations");
                    }
                }
            }

            return targetsInCone;
        }

        /// <summary>
        /// Phase 3: Raycast to all targets in cone and find nearest with clear line of sight
        /// </summary>
        /// <param name="targetsInCone">Targets that passed Phase 2</param>
        /// <param name="detectedTarget">Output parameter for the detected target</param>
        /// <param name="distance">Output parameter for the distance</param>
        /// <param name="priorityTarget">Optional priority target to check first</param>
        /// <returns>True if a target is successfully detected</returns>
        private bool CheckLineOfSightDetection(List<Transform> targetsInCone, out Transform detectedTarget, out float distance, Transform priorityTarget = null)
        {
            detectedTarget = null;
            distance = float.MaxValue;

            if (targetsInCone.Count == 0) return false;

            // If priority target is specified and in the cone, check it first using combination logic
            if (priorityTarget != null && targetsInCone.Contains(priorityTarget))
            {
                TargetScript priorityTargetScript = priorityTarget.GetComponent<TargetScript>();
                if (priorityTargetScript != null)
                {
                    Vector3 priorityEyePosition = GetEyePosition();
                    List<GameObject> hitObjects = new List<GameObject>();

                    bool priorityTargetDetected = false;
                    // Cast raycasts to pre-filtered successful combinations for priority target
                    if (targetSuccessfulCombinations.ContainsKey(priorityTarget))
                    {
                        var successfulCombinations = targetSuccessfulCombinations[priorityTarget];
                        
                        foreach (var combination in successfulCombinations)
                        {
                            if (priorityTargetDetected) break; // Early exit if already detected
                            
                            foreach (var targetObject in combination.targetObjects)
                            {
                                if (targetObject == null) continue;

                                // Calculate direction to this specific target object (actual position)
                                Vector3 directionToTarget = (targetObject.transform.position - priorityEyePosition).normalized;
                                float targetDistance = Vector3.Distance(priorityEyePosition, targetObject.transform.position);

                                if (Physics.Raycast(priorityEyePosition, directionToTarget, out RaycastHit hit, targetDistance, combinedLayerMask))
                                {
                                    // Check if we hit the parent (which has the collider)
                                    if (hit.collider.transform == priorityTarget)
                                    {
                                        // We hit the parent collider while aiming at this specific target object
                                        hitObjects.Add(targetObject);
                                        
                                        // Check if this combination is satisfied - if so, we can stop checking
                                        if (priorityTargetScript.IsAnyDetectionSatisfied(hitObjects))
                                        {
                                            priorityTargetDetected = true;
                                            break; // Exit inner loop
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Check if any combination is satisfied for priority target
                    if (priorityTargetDetected)
                    {
                        detectedTarget = priorityTarget;
                        distance = Vector3.Distance(priorityEyePosition, priorityTarget.position);
                        return true;
                    }
                }
            }

            // List to store targets with clear line of sight
            List<Transform> visibleTargets = new List<Transform>();
            List<float> visibleDistances = new List<float>();

            // Raycast to ALL targets in cone
            Vector3 eyePosition = GetEyePosition();

            foreach (Transform targetEntity in targetsInCone)
            {
                TargetScript targetScript = targetEntity.GetComponent<TargetScript>();
                if (targetScript == null) continue; // Skip if no TargetScript (should not happen due to filtering)

                Vector3 raycastStart = eyePosition;
                List<GameObject> hitObjects = new List<GameObject>();

                // Cast raycasts to pre-filtered successful combinations
                bool targetDetected = false;
                if (targetSuccessfulCombinations.ContainsKey(targetEntity))
                {
                    var successfulCombinations = targetSuccessfulCombinations[targetEntity];
                    
                    foreach (var combination in successfulCombinations)
                    {
                        if (targetDetected) break; // Early exit if already detected
                        
                        foreach (var targetObject in combination.targetObjects)
                        {
                            if (targetObject == null) continue;

                            // Calculate direction to this specific target object (actual position)
                            Vector3 directionToTarget = (targetObject.transform.position - raycastStart).normalized;
                            float targetDistance = Vector3.Distance(raycastStart, targetObject.transform.position);

                            // Draw debug line to ACTUAL target object position (not eye height)
                            if (showDebugInfo)
                            {
                                Debug.DrawLine(raycastStart, targetObject.transform.position, Color.cyan, 0.1f);
                            }

                            if (Physics.Raycast(raycastStart, directionToTarget, out RaycastHit hit, targetDistance, combinedLayerMask))
                            {
                                // Check if we hit the parent (which has the collider)
                                if (hit.collider.transform == targetEntity)
                                {
                                    // We hit the parent collider while aiming at this specific target object
                                    hitObjects.Add(targetObject);
                                    
                                    // Check if this combination is satisfied - if so, we can stop checking
                                    if (targetScript.IsAnyDetectionSatisfied(hitObjects))
                                    {
                                        targetDetected = true;
                                        break; // Exit inner loop
                                    }
                                }
                            }
                        }
                    }
                }

                // Check if any combination is satisfied
                if (targetDetected)
                {
                    // At least one combination is satisfied - target is detected
                    visibleTargets.Add(targetEntity);
                    visibleDistances.Add(Vector3.Distance(eyePosition, targetEntity.position));

                    var satisfiedCombos = targetScript.GetSatisfiedCombinations(hitObjects);
                    // Debug.Log($"Target {targetEntity.name} detected! Satisfied combinations: {string.Join(", ", satisfiedCombos)}");
                }
            }

            // Find the nearest target among those with clear line of sight
            if (visibleTargets.Count > 0)
            {
                float nearestDistance = float.MaxValue;
                int nearestIndex = -1;

                for (int i = 0; i < visibleDistances.Count; i++)
                {
                    if (visibleDistances[i] < nearestDistance)
                    {
                        nearestDistance = visibleDistances[i];
                        nearestIndex = i;
                    }
                }

                if (nearestIndex >= 0)
                {
                    detectedTarget = visibleTargets[nearestIndex];
                    distance = nearestDistance;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if any target is detected using three-phase detection
        /// </summary>
        /// <param name="detectedTarget">Output parameter for the detected target</param>
        /// <param name="distance">Output parameter for the distance</param>
        /// <returns>True if any target is detected</returns>
        private bool CheckForPlayerDetection(out Transform detectedTarget, out float distance)
        {
            detectedTarget = null;
            distance = float.MaxValue;

            // Phase 1: Get targets within detection range
            List<Transform> withinRangeTargets = GetWithinRangeTargets();
            if (withinRangeTargets.Count == 0) return false;

            // Phase 2: Get targets within vision cone
            List<Transform> withinVisionTargets = GetWithinVisionTargets(withinRangeTargets);
            if (withinVisionTargets.Count == 0) return false;

            // Phase 3: Check line of sight and perform raycast
            return CheckLineOfSightDetection(withinVisionTargets, out detectedTarget, out distance);
        }

        /// <summary>
        /// Check if current target is still visible using combination-based detection
        /// </summary>
        private bool IsTargetVisibleRaycast()
        {
            if (target == null) return false;

            float distanceToTarget = Vector3.Distance(GetEyePosition(), target.position);

            // Don't check targets beyond far vision range
            if (distanceToTarget > farVisionRange)
            {
                return false;
            }

            // Use CheckLineOfSightDetection with current target as priority
            List<Transform> singleTargetList = new List<Transform> { target };
            return CheckLineOfSightDetection(singleTargetList, out Transform detectedTarget, out float distance, target);
        }



        /// <summary>
        /// Check if there's a clear line of sight to the target
        /// </summary>


        /// <summary>
        /// Handle target detection with timing logic
        /// </summary>
        private void TargetDetected(Transform detectedTarget, Vector3 position, float sqrDistance)
        {
            lastSeenPosition = position;

            if (sqrDistance <= sqrNearRange)
            {
                // Near range - instant chase (reset far detection timer)
                ResetFarDetectionTimer();
                if (showDebugInfo)
                {
                    Debug.Log($"[VisionDetector] Target detected NEAR at squared distance: {sqrDistance:F2}");
                }
                OnPlayerDetectedNear?.Invoke(detectedTarget);
            }
            else
            {
                // Far range - use timer logic
                HandleFarDetection(detectedTarget, position);
            }
        }

        /// <summary>
        /// Handle far distance detection with reversible timer
        /// </summary>
        private void HandleFarDetection(Transform detectedTarget, Vector3 position)
        {
            // Check if target changed or starting new detection
            if (farDetectionTarget != detectedTarget)
            {
                farDetectionTarget = detectedTarget;
                farDetectionAccumulatedTime = 0f;
                isFarDetectionActive = true;
            }

            // Just tell the timer the target is visible - the timer in Update() handles the rest
            isTargetCurrentlyVisible = true;
        }

        /// <summary>
        /// Update the target lost timer - runs independently every frame
        /// </summary>
        private void UpdateTargetLostTimer()
        {
            // Only run if we have a target and target lost timer is active
            if (target == null || targetLostStartTime == 0f) return;

            // Check if target lost timer has expired
            if (Time.time - targetLostStartTime >= targetLostTimer)
            {
                // Timer expired - target is lost
                if (showDebugInfo)
                {
                Debug.Log($"[VisionDetector] Target lost timer expired after {targetLostTimer}s");
                }
                TargetLost();
                return;
            }
        }


        /// <summary>
        /// Update the reversible far detection timer - runs independently every frame
        /// </summary>
        private void UpdateFarDetectionTimer()
        {
            // Only run if timer is active and we have a target
            if (!isFarDetectionActive || farDetectionTarget == null) return;

            // Timer runs independently - just check the visibility flag
            if (isTargetCurrentlyVisible)
            {
                // Target visible - count UP
                farDetectionAccumulatedTime += Time.deltaTime;

                // Check if timer hit limit (trigger event)
                if (farDetectionAccumulatedTime >= detectionTimeThreshold)
                {
                    // Timer completed - trigger far detection event (level 1 = detecting)
                if (showDebugInfo)
                {
                    Debug.Log($"[VisionDetector] Target detected FAR after {farDetectionAccumulatedTime:F2}s");
                }
                OnPlayerDetectedFar?.Invoke(farDetectionTarget, 1); // Level 1 = detecting state

                    // Start chase timer if target is still visible
                    if (isTargetCurrentlyVisible)
                    {
                        isChaseTimerActive = true;
                        chaseAccumulatedTime = 0f;
                    if (showDebugInfo)
                    {
                        Debug.Log("[VisionDetector] Starting chase timer");
                    }
                    }

                    ResetFarDetectionTimer(); // Reset after triggering
                    return;
                }
            }
            else
            {
                // Target not visible - count DOWN
                farDetectionAccumulatedTime -= Time.deltaTime;

                // Clamp to 0 (don't go negative)
                if (farDetectionAccumulatedTime <= 0f)
                {
                    farDetectionAccumulatedTime = 0f;
                    if (showDebugInfo)
                    {
                    Debug.Log("[VisionDetector] Far detection timer reached 0 - stopping timer");
                    }
                    ResetFarDetectionTimer();
                    return;
                }
            }
        }

        /// <summary>
        /// Update the chase timer - runs independently every frame
        /// </summary>
        private void UpdateChaseTimer()
        {
            // Only run if chase timer is active and we have a target
            if (!isChaseTimerActive || farDetectionTarget == null) return;

            // Timer runs independently - just check the visibility flag
            if (isTargetCurrentlyVisible)
            {
                // Target visible - count UP
                chaseAccumulatedTime += Time.deltaTime;

                // Check if timer hit limit (trigger chase event)
                if (chaseAccumulatedTime >= chaseTimeThreshold)
                {
                    // Timer completed - trigger chase event
                    if (showDebugInfo)
                    {
                        Debug.Log($"[VisionDetector] Target detected for CHASE after {chaseAccumulatedTime:F2}s");
                    }
                    OnPlayerDetectedFar?.Invoke(farDetectionTarget, 2); // Level 2 = chasing
                    ResetChaseTimer(); // Reset after triggering
                    return;
                }
            }
            else
            {
                // Target not visible - count DOWN
                chaseAccumulatedTime -= Time.deltaTime;

                // Clamp to 0 (don't go negative)
                if (chaseAccumulatedTime <= 0f)
                {
                    chaseAccumulatedTime = 0f;
                    if (showDebugInfo)
                    {
                    Debug.Log("[VisionDetector] Chase timer reached 0 - stopping timer");
                    }
                    ResetChaseTimer();
                    return;
                }
            }
        }

        /// <summary>
        /// Reset far detection timer
        /// </summary>
        private void ResetFarDetectionTimer()
        {
            isFarDetectionActive = false;
            farDetectionTarget = null;
            farDetectionAccumulatedTime = 0f;
            isTargetCurrentlyVisible = false;

            // Also reset chase timer when far detection resets
            ResetChaseTimer();
        }

        /// <summary>
        /// Reset chase timer
        /// </summary>
        private void ResetChaseTimer()
        {
            isChaseTimerActive = false;
            chaseAccumulatedTime = 0f;
        }

        /// <summary>
        /// Update target switch timer - runs independently every frame
        /// </summary>
        private void UpdateTargetSwitchTimer()
        {
            // Only run if timer is active
            if (!isTargetSwitchTimerActive) return;

            // Count UP the timer
            targetSwitchAccumulatedTime += Time.deltaTime;

            // Check if timer hit limit (trigger target switch)
            if (targetSwitchAccumulatedTime >= TARGET_SWITCH_TIME_THRESHOLD)
            {
                // Timer completed - emit switch event
                if (showDebugInfo)
                {
                Debug.Log($"[VisionDetector] Target switch timer completed after {targetSwitchAccumulatedTime:F2}s");
                }
                SwitchVisionTarget?.Invoke(target);

                // Reset timer
                ResetTargetSwitchTimer();
            }
        }

        /// <summary>
        /// Reset target switch timer
        /// </summary>
        private void ResetTargetSwitchTimer()
        {
            isTargetSwitchTimerActive = false;
            targetSwitchAccumulatedTime = 0f;
        }

        /// <summary>
        /// Handle target being lost
        /// </summary>
        private void TargetLost()
        {
            ResetFarDetectionTimer(); // Reset timer when target is lost
            if (showDebugInfo)
            {
            Debug.Log($"[VisionDetector] Target LOST at last seen position: {lastSeenPosition}");
            }
            OnPlayerLost?.Invoke(lastSeenPosition);
        }

        /// <summary>
        /// Get the current target
        /// </summary>
        public Transform GetTarget()
        {
            return target;
        }

        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearTarget()
        {
            target = null;
        }

        /// <summary>
        /// Get the last seen position
        /// </summary>
        public Vector3 GetLastSeenPosition()
        {
            return lastSeenPosition;
        }

        /// <summary>
        /// Reset vision detection state
        /// </summary>
        public void ResetVision()
        {
            // Reset detection state
            ResetFarDetectionTimer();
            isTargetVisible = true;
            targetLostStartTime = 0f;
        }

        /// <summary>
        /// Hook up to ChasingState events
        /// </summary>
        private void HookUpToChasingState()
        {
            if (parentObject != null)
            {
                chasingState = parentObject.GetComponent<ChasingState>();
                if (chasingState != null)
                {
                    chasingState.OnChasingTargetSet += OnChasingTargetSet;
                }
            }
        }

        /// <summary>
        /// Event handler for ChasingState target changes
        /// </summary>
        private void OnChasingTargetSet(Transform newTarget)
        {
            // OnTargetSet(newTarget);
        }

        /// <summary>
        /// Called when target is set by ChasingState script via event
        /// </summary>
        public void OnTargetSet(Transform newTarget)
        {
            // Set the target from ChasingScript
            target = newTarget;

            // Reset target visibility state for new target
            isTargetVisible = true;
            lastTargetRaycastTime = Time.time;

            if (showDebugInfo)
            {
            Debug.Log($"[VisionDetector] Target set: {newTarget?.name}");
            }
        }

        #region Debug Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Draw detection ranges
            if (showDetectionRanges)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, nearVisionRange);

                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(transform.position, farVisionRange);
            }

            // Draw vision cones
            if (showVisionCone)
            {
                DrawNearVisionCone();
                DrawFarVisionCone();
            }

            // Draw last seen position if we have one
            if (lastSeenPosition != Vector3.zero)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);

                // Draw line to last seen position
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, lastSeenPosition);
            }
        }

        private void DrawNearVisionCone()
        {
            int segments = 20;
            float angleStep = visionAngle / segments;

            Vector3 forward = transform.forward;
            Vector3 startPoint = transform.position;

            // Calculate Y offset for vertical angles (outside the loop for performance)
            float yOffset = Mathf.Sin(verticalVisionAngle * 0.5f * Mathf.Deg2Rad);

            // Draw near vision cone from 0 to nearVisionRange (15 units)
            for (int i = 0; i <= segments; i++)
            {
                float angle = -visionAngle * 0.5f + angleStep * i;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 endPoint = startPoint + direction * nearVisionRange;

                // Create vertical directions by adding/subtracting Y offset from current direction Y
                Vector3 upDir = new Vector3(direction.x, direction.y + yOffset, direction.z).normalized;
                Vector3 downDir = new Vector3(direction.x, direction.y - yOffset, direction.z).normalized;

                Vector3 upPoint = startPoint + upDir * nearVisionRange;
                Vector3 downPoint = startPoint + downDir * nearVisionRange;

                if (i > 0)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(startPoint, endPoint);
                    Gizmos.DrawLine(startPoint, upPoint);
                    Gizmos.DrawLine(startPoint, downPoint);
                }
            }
        }

        private void DrawFarVisionCone()
        {
            Gizmos.color = Color.magenta;

            int segments = 20;
            float angleStep = farVisionAngle / segments;

            Vector3 forward = transform.forward;
            Vector3 startPoint = transform.position;

            // Calculate Y offset for vertical angles (outside the loop for performance)
            float yOffset = Mathf.Sin(verticalVisionAngle * 0.5f * Mathf.Deg2Rad);

            // Draw far vision cone from nearVisionRange (15) to farVisionRange (30 units)
            // This shows the actual detection boundary where far vision takes over
            for (int i = 0; i <= segments; i++)
            {
                float angle = -farVisionAngle * 0.5f + angleStep * i;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                // Draw the inner boundary (where far vision starts)
                Vector3 innerPoint = startPoint + direction * nearVisionRange;
                // Draw the outer boundary (where far vision ends)
                Vector3 outerPoint = startPoint + direction * farVisionRange;

                // Create vertical directions by adding/subtracting Y offset from current direction Y
                Vector3 upDir = new Vector3(direction.x, direction.y + yOffset, direction.z).normalized;
                Vector3 downDir = new Vector3(direction.x, direction.y - yOffset, direction.z).normalized;

                Vector3 upInnerPoint = startPoint + upDir * nearVisionRange;
                Vector3 upOuterPoint = startPoint + upDir * farVisionRange;
                Vector3 downInnerPoint = startPoint + downDir * nearVisionRange;
                Vector3 downOuterPoint = startPoint + downDir * farVisionRange;

                if (i > 0)
                {
                    // Draw line from inner to outer boundary to show far vision range
                    Gizmos.DrawLine(innerPoint, outerPoint);
                    Gizmos.DrawLine(upInnerPoint, upOuterPoint);
                    Gizmos.DrawLine(downInnerPoint, downOuterPoint);
                }
            }

            // Also draw the transition line at nearVisionRange to show where vision cone changes
            Gizmos.color = Color.yellow;
            for (int i = 0; i <= segments; i++)
            {
                float angle = -farVisionAngle * 0.5f + (farVisionAngle / segments) * i;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 transitionPoint = startPoint + direction * nearVisionRange;

                if (i > 0)
                {
                    Gizmos.DrawWireSphere(transitionPoint, 0.2f);
                }

                // Create vertical directions by adding/subtracting Y offset from current direction Y
                Vector3 upDir = new Vector3(direction.x, direction.y + yOffset, direction.z).normalized;
                Vector3 downDir = new Vector3(direction.x, direction.y - yOffset, direction.z).normalized;

                Vector3 upTransitionPoint = startPoint + upDir * nearVisionRange;
                Vector3 downTransitionPoint = startPoint + downDir * nearVisionRange;
                Gizmos.DrawWireSphere(upTransitionPoint, 0.2f);
                Gizmos.DrawWireSphere(downTransitionPoint, 0.2f);
            }
        }

        #endregion
    }
}