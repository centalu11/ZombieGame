using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ZombieGame.Core
{
    [System.Serializable]
    public class TargetCombination
    {
        [Tooltip("Name for this combination (e.g., 'Head Shot', 'Upper Body')")]
        public string combinationName = "New Combination";
        
        [Tooltip("Which detector types can use this combination (Zombies, Humans, etc.)")]
        public LayerMask detectorLayerMask = -1; // Default to "Everything"
        
        [Tooltip("Target objects that must ALL be hit for this combination to succeed")]
        public GameObject[] targetObjects = new GameObject[0];
    }

    public class TargetScript : MonoBehaviour
    {
        [Header("Detection Combinations")]
        [Tooltip("Different combinations of targets that count as successful detection")]
        public List<TargetCombination> combinations = new List<TargetCombination>();

        // Pre-grouped combinations by layer for fast runtime access
        private Dictionary<int, List<TargetCombination>> combinationsByLayer = new Dictionary<int, List<TargetCombination>>();

        /// <summary>
        /// Gets all combinations
        /// </summary>
        public List<TargetCombination> GetCombinations()
        {
            return combinations;
        }

        /// <summary>
        /// Gets combinations filtered by detector layer mask (fast - uses pre-grouped data)
        /// </summary>
        /// <param name="detectorLayer">The layer of the detector (zombie, human, etc.)</param>
        /// <returns>Combinations that match the detector layer</returns>
        public List<TargetCombination> GetCombinationsForDetector(int detectorLayer)
        {
            if (combinationsByLayer.TryGetValue(detectorLayer, out List<TargetCombination> layerCombinations))
            {
                return layerCombinations;
            }
            
            // Return empty list if no combinations for this layer
            return new List<TargetCombination>();
        }

        /// <summary>
        /// Gets combinations filtered by detector GameObject (fast - uses pre-grouped data)
        /// </summary>
        /// <param name="detectorGameObject">The detector GameObject</param>
        /// <returns>Combinations that match the detector's layer</returns>
        public List<TargetCombination> GetCombinationsForDetector(GameObject detectorGameObject)
        {
            return GetCombinationsForDetector(detectorGameObject.layer);
        }

        /// <summary>
        /// Checks if any combination is satisfied by the given hit objects
        /// </summary>
        /// <param name="hitObjects">List of GameObjects that were hit</param>
        /// <returns>True if any combination is fully satisfied</returns>
        public bool IsAnyDetectionSatisfied(List<GameObject> hitObjects)
        {
            foreach (var combination in combinations)
            {
                if (IsCombinationSatisfied(combination, hitObjects))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a specific combination is satisfied by the hit objects
        /// </summary>
        /// <param name="combination">The combination to check</param>
        /// <param name="hitObjects">List of GameObjects that were hit</param>
        /// <returns>True if all required targets in the combination were hit</returns>
        public bool IsCombinationSatisfied(TargetCombination combination, List<GameObject> hitObjects)
        {
            if (combination.targetObjects.Length == 0) return false;

            // Check if ALL target objects in this combination were hit
            return combination.targetObjects.All(target => hitObjects.Contains(target));
        }

        /// <summary>
        /// Gets which combinations are satisfied by the hit objects
        /// </summary>
        /// <param name="hitObjects">List of GameObjects that were hit</param>
        /// <returns>List of satisfied combination names</returns>
        public List<string> GetSatisfiedCombinations(List<GameObject> hitObjects)
        {
            return combinations
                .Where(combo => IsCombinationSatisfied(combo, hitObjects))
                .Select(combo => combo.combinationName)
                .ToList();
        }

        private void Start()
        {
            // Pre-group combinations by layer masks for fast runtime access
            BuildCombinationGroups();

            if (combinations.Count == 0)
            {
                Debug.LogWarning($"[TargetScript] {gameObject.name}: No combinations configured!");
            }
        }

        /// <summary>
        /// Pre-groups combinations by detector layers for fast runtime access
        /// </summary>
        private void BuildCombinationGroups()
        {
            combinationsByLayer.Clear();

            foreach (var combination in combinations)
            {
                // For each layer in the LayerMask, add this combination to that layer's group
                for (int layer = 0; layer < 32; layer++)
                {
                    if ((combination.detectorLayerMask & (1 << layer)) != 0)
                    {
                        if (!combinationsByLayer.ContainsKey(layer))
                        {
                            combinationsByLayer[layer] = new List<TargetCombination>();
                        }
                        combinationsByLayer[layer].Add(combination);
                    }
                }
            }
        }
    }
}
