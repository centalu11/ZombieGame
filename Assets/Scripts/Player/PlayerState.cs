using UnityEngine;

namespace ZombieGame.Player
{
    public class PlayerState : MonoBehaviour
    {
        [Header("Stats")]
        [Tooltip("Player's health points")]
        public int hp = 100;
        
        [Header("Zombie Interactions")]
        [Tooltip("Current detection state by zombies")]
        public DetectionState detectionState = DetectionState.Undetected;
        
        public enum DetectionState
        {
            Undetected,
            Detected,
            BeingChased
        }
        
        // Track chasing zombies
        private int chasingZombieCount = 0;
        
        /// <summary>
        /// Set the detection state
        /// </summary>
        public void SetDetectionState(DetectionState newState)
        {
            detectionState = newState;
        }
        
        /// <summary>
        /// Register that a zombie has started chasing
        /// </summary>
        public void RegisterChasingZombie()
        {
            chasingZombieCount++;
            detectionState = DetectionState.BeingChased;
        }
        
        /// <summary>
        /// Unregister that a zombie has stopped chasing
        /// </summary>
        public void UnregisterChasingZombie()
        {
            chasingZombieCount--;
            if (chasingZombieCount <= 0)
            {
                chasingZombieCount = 0;
                detectionState = DetectionState.Undetected;
            }
        }
        
        /// <summary>
        /// Check if player is currently being chased
        /// </summary>
        public bool IsBeingChased()
        {
            return detectionState == DetectionState.BeingChased;
        }
        
        /// <summary>
        /// Check if player is detected by any zombie
        /// </summary>
        public bool IsDetected()
        {
            return detectionState == DetectionState.Detected || detectionState == DetectionState.BeingChased;
        }
    }
} 