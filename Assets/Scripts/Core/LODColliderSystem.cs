using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public class LODColliderSystem : MonoBehaviour
    {
        [Header("LOD Settings")]
        [SerializeField] private float collisionRadius = 50f;
        [SerializeField] private float updateTriggerRadius = 30f;
        [SerializeField] private LayerMask buildingLayerMask = -1;
        [SerializeField] private string buildingTag = "Building";
        
        [Header("Performance Settings")]
        [SerializeField] private float updateCheckInterval = 0.5f;
        [SerializeField] private int maxCollidersPerFrame = 20;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color collisionRadiusColor = Color.green;
        [SerializeField] private Color triggerRadiusColor = Color.yellow;
        
        // Private variables
        private Transform playerTransform;
        private Vector3 lastUpdatePosition;
        private HashSet<Collider> activeColliders = new HashSet<Collider>();
        private Dictionary<GameObject, List<Collider>> buildingColliders = new Dictionary<GameObject, List<Collider>>();
        private Coroutine updateCoroutine;
        private bool isInitialized = false;
        
        // Events
        public System.Action<int> OnCollidersEnabled;
        public System.Action<int> OnCollidersDisabled;
        
        #region Unity Methods
        
        private void Start()
        {
            InitializeSystem();
        }
        
        private void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !isInitialized) return;
            
            Vector3 playerPos = playerTransform != null ? playerTransform.position : transform.position;
            
            // Draw collision radius
            Gizmos.color = collisionRadiusColor;
            Gizmos.DrawWireSphere(playerPos, collisionRadius);
            
            // Draw trigger radius
            Gizmos.color = triggerRadiusColor;
            Gizmos.DrawWireSphere(lastUpdatePosition, updateTriggerRadius);
            
            // Draw line from last update position to current position
            if (playerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(lastUpdatePosition, playerPos);
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSystem()
        {
            // Find player transform
            FindPlayerTransform();
            
            if (playerTransform == null)
            {
                Debug.LogError("LODColliderSystem: Player transform not found!");
                return;
            }
            
            // Cache all building colliders
            CacheBuildingColliders();
            
            // Set initial update position
            lastUpdatePosition = playerTransform.position;
            
            // Perform initial collider update
            StartCoroutine(UpdateCollidersCoroutine(playerTransform.position, true));
            
            // Start the monitoring coroutine
            updateCoroutine = StartCoroutine(MonitorPlayerMovement());
            
            isInitialized = true;
            
            Debug.Log($"LODColliderSystem initialized with {buildingColliders.Count} buildings");
        }
        
        private void FindPlayerTransform()
        {
            // Try to find player by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                return;
            }
            
            // Try to find by name
            player = GameObject.Find("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                return;
            }
            
            // If still not found, use Camera.main as fallback
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
                Debug.LogWarning("LODColliderSystem: Player not found, using main camera as reference");
            }
        }
        
        private void CacheBuildingColliders()
        {
            buildingColliders.Clear();
            
            // Find all game objects with building tag
            GameObject[] buildings = GameObject.FindGameObjectsWithTag(buildingTag);
            
            foreach (GameObject building in buildings)
            {
                List<Collider> colliders = new List<Collider>();
                
                // Get all colliders in the building (including children)
                Collider[] buildingCollidersArray = building.GetComponentsInChildren<Collider>();
                
                foreach (Collider col in buildingCollidersArray)
                {
                    // Skip trigger colliders
                    if (!col.isTrigger)
                    {
                        colliders.Add(col);
                        // Initially disable all colliders
                        col.enabled = false;
                    }
                }
                
                if (colliders.Count > 0)
                {
                    buildingColliders[building] = colliders;
                }
            }
            
            Debug.Log($"Cached colliders from {buildingColliders.Count} buildings");
        }
        
        #endregion
        
        #region Update System
        
        private IEnumerator MonitorPlayerMovement()
        {
            while (playerTransform != null)
            {
                yield return new WaitForSeconds(updateCheckInterval);
                
                float distanceFromLastUpdate = Vector3.Distance(playerTransform.position, lastUpdatePosition);
                
                // Check if player moved outside the trigger radius
                if (distanceFromLastUpdate >= updateTriggerRadius)
                {
                    Debug.Log($"Player moved {distanceFromLastUpdate:F1}m, updating colliders...");
                    
                    // Start collider update
                    yield return StartCoroutine(UpdateCollidersCoroutine(playerTransform.position, false));
                    
                    // Update last position
                    lastUpdatePosition = playerTransform.position;
                }
            }
        }
        
        private IEnumerator UpdateCollidersCoroutine(Vector3 centerPosition, bool isInitial)
        {
            HashSet<Collider> newActiveColliders = new HashSet<Collider>();
            List<Collider> toEnable = new List<Collider>();
            List<Collider> toDisable = new List<Collider>();
            
            // Find colliders within collision radius
            foreach (var kvp in buildingColliders)
            {
                GameObject building = kvp.Key;
                List<Collider> colliders = kvp.Value;
                
                if (building == null) continue;
                
                float distance = Vector3.Distance(building.transform.position, centerPosition);
                
                if (distance <= collisionRadius)
                {
                    // Building is within range
                    foreach (Collider col in colliders)
                    {
                        if (col != null)
                        {
                            newActiveColliders.Add(col);
                            
                            if (!col.enabled)
                            {
                                toEnable.Add(col);
                            }
                        }
                    }
                }
            }
            
            // Find colliders to disable (previously active but no longer in range)
            foreach (Collider col in activeColliders)
            {
                if (col != null && !newActiveColliders.Contains(col))
                {
                    toDisable.Add(col);
                }
            }
            
            // Enable colliders in batches
            int enabledCount = 0;
            for (int i = 0; i < toEnable.Count; i++)
            {
                if (toEnable[i] != null)
                {
                    toEnable[i].enabled = true;
                    enabledCount++;
                    
                    // Spread work across frames
                    if (i % maxCollidersPerFrame == 0 && i > 0)
                    {
                        yield return null;
                    }
                }
            }
            
            // Disable colliders in batches
            int disabledCount = 0;
            for (int i = 0; i < toDisable.Count; i++)
            {
                if (toDisable[i] != null)
                {
                    toDisable[i].enabled = false;
                    disabledCount++;
                    
                    // Spread work across frames
                    if (i % maxCollidersPerFrame == 0 && i > 0)
                    {
                        yield return null;
                    }
                }
            }
            
            // Update active colliders set
            activeColliders = newActiveColliders;
            
            // Invoke events
            if (enabledCount > 0)
            {
                OnCollidersEnabled?.Invoke(enabledCount);
            }
            
            if (disabledCount > 0)
            {
                OnCollidersDisabled?.Invoke(disabledCount);
            }
            
            string logMessage = isInitial ? 
                $"Initial collider setup: {enabledCount} colliders enabled" :
                $"Collider update: +{enabledCount} enabled, -{disabledCount} disabled, {activeColliders.Count} total active";
            
            Debug.Log(logMessage);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Manually trigger a collider update
        /// </summary>
        public void ForceUpdate()
        {
            if (playerTransform != null && isInitialized)
            {
                StartCoroutine(UpdateCollidersCoroutine(playerTransform.position, false));
                lastUpdatePosition = playerTransform.position;
            }
        }
        
        /// <summary>
        /// Get the number of currently active colliders
        /// </summary>
        public int GetActiveColliderCount()
        {
            return activeColliders.Count;
        }
        
        /// <summary>
        /// Get the total number of managed colliders
        /// </summary>
        public int GetTotalColliderCount()
        {
            int total = 0;
            foreach (var kvp in buildingColliders)
            {
                total += kvp.Value.Count;
            }
            return total;
        }
        
        /// <summary>
        /// Change the collision radius at runtime
        /// </summary>
        public void SetCollisionRadius(float newRadius)
        {
            collisionRadius = newRadius;
            ForceUpdate();
        }
        
        /// <summary>
        /// Change the update trigger radius at runtime
        /// </summary>
        public void SetUpdateTriggerRadius(float newRadius)
        {
            updateTriggerRadius = newRadius;
        }
        
        /// <summary>
        /// Refresh the building cache (use when buildings are added/removed)
        /// </summary>
        public void RefreshBuildingCache()
        {
            CacheBuildingColliders();
            ForceUpdate();
        }
        
        #endregion
        
        #region Debug Methods
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogStats()
        {
            Debug.Log($"LOD Collider System Stats:\n" +
                     $"- Total Buildings: {buildingColliders.Count}\n" +
                     $"- Total Colliders: {GetTotalColliderCount()}\n" +
                     $"- Active Colliders: {GetActiveColliderCount()}\n" +
                     $"- Collision Radius: {collisionRadius}m\n" +
                     $"- Update Trigger Radius: {updateTriggerRadius}m\n" +
                     $"- Last Update Position: {lastUpdatePosition}");
        }
        
        #endregion
    }
} 