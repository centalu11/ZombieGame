using UnityEngine;

namespace ZombieGame.NPC.Enemy.Zombie.Structs
{
    /// <summary>
    /// Represents a single animation clip with its speed settings
    /// </summary>
    [System.Serializable]
    public struct AnimationClipEntry
    {
        [Tooltip("Animation clip")]
        public AnimationClip clip;
        
        [Tooltip("Animation speed multiplier (1.0 = normal speed)")]
        [Range(0.1f, 3.0f)]
        public float animationSpeed;
        
        [Tooltip("Movement speed for transform-based movement. If animation shouldn't move the character, leave this at 0")]
        [Range(0f, 10f)]
        public float movementSpeed;
        
        public AnimationClipEntry(AnimationClip clip, float animSpeed = 1.0f, float moveSpeed = 0f)
        {
            this.clip = clip;
            this.animationSpeed = animSpeed;
            this.movementSpeed = moveSpeed;
        }
        
        /// <summary>
        /// Create a default AnimationClipEntry with default values
        /// </summary>
        public static AnimationClipEntry CreateDefault()
        {
            return new AnimationClipEntry(null, 1.0f, 0f);
        }
    }

    /// <summary>
    /// Combines animation state name and animation clips in a single input field
    /// </summary>
    [System.Serializable]
    public struct AnimationStateData
    {
        [Tooltip("State name in the animator controller")]
        public string stateName;
        
        [Tooltip("Animation clips with their speed settings")]
        public AnimationClipEntry[] animationClips;
        
        [Tooltip("Use random animation clip from the array")]
        public bool randomized;
        
        [Tooltip("Selected animation clip index (0-based, only used when not randomized)")]
        [Range(0, 10)] // Will be dynamically adjusted based on array length
        public int selectedClipIndex;
        
        // Runtime selected clip (set once when randomized is true)
        private AnimationClip _selectedClip;
        private bool _clipSelected;
        private int _selectedClipIndex;
        
        /// <summary>
        /// Constructor for AnimationStateData
        /// </summary>
        public AnimationStateData(string state, AnimationClip[] clips)
        {
            stateName = state;
            animationClips = new AnimationClipEntry[clips?.Length ?? 0];
            for (int i = 0; i < animationClips.Length; i++)
            {
                animationClips[i] = new AnimationClipEntry(clips[i], 1.0f, 0f);
            }
            randomized = false;
            selectedClipIndex = 0;
            _selectedClip = null;
            _clipSelected = false;
            _selectedClipIndex = -1;
        }
        
        /// <summary>
        /// Get the state name (for Play() calls)
        /// </summary>
        public string GetStateName()
        {
            return stateName;
        }
        
        /// <summary>
        /// Set the animation clip (handles randomization and selection logic)
        /// </summary>
        public void SetAnimationClip()
        {
            if (animationClips == null || animationClips.Length == 0)
                return;
            
            // Count valid clips (non-null)
            int validClipCount = GetValidClipCount();
            if (validClipCount == 0)
                return;

            if (randomized)
            {
                // If we haven't selected a clip yet, select one randomly from valid clips
                if (!_clipSelected)
                {
                    // Create a list of valid clip indices
                    int[] validIndices = new int[validClipCount];
                    int validIndex = 0;
                    for (int i = 0; i < animationClips.Length; i++)
                    {
                        if (animationClips[i].clip != null)
                        {
                            validIndices[validIndex] = i;
                            validIndex++;
                        }
                    }
                    
                    // Select a random valid clip
                    int randomValidIndex = validIndices[Random.Range(0, validClipCount)];
                    _selectedClipIndex = randomValidIndex;
                    _selectedClip = animationClips[randomValidIndex].clip;
                    _clipSelected = true;
                }
            }
            else
            {
                // Use the manually selected clip index (skipping empty slots)
                int validIndex = 0;
                for (int i = 0; i < animationClips.Length; i++)
                {
                    if (animationClips[i].clip != null)
                    {
                        if (validIndex == selectedClipIndex)
                        {
                            _selectedClipIndex = i;
                            _selectedClip = animationClips[i].clip;
                            _clipSelected = true;
                            return;
                        }
                        validIndex++;
                    }
                }
                
                // If we get here, the selected index is out of range, use the first valid clip
                for (int i = 0; i < animationClips.Length; i++)
                {
                    if (animationClips[i].clip != null)
                    {
                        _selectedClipIndex = i;
                        _selectedClip = animationClips[i].clip;
                        _clipSelected = true;
                        return;
                    }
                }
            }
        }
        
        /// <summary>
        /// Get the animation clip to use (returns cached clip or sets it if not set yet)
        /// </summary>
        public AnimationClip GetAnimationClip()
        {
            // If we haven't selected a clip yet, set it
            if (!_clipSelected)
            {
                SetAnimationClip();
            }
            
            // Return the cached clip
            return _selectedClip;
        }
        
        /// <summary>
        /// Get the animation speed for the selected clip
        /// </summary>
        public float GetAnimationSpeed()
        {
            if (animationClips == null || animationClips.Length == 0 || _selectedClipIndex < 0 || _selectedClipIndex >= animationClips.Length)
                return 1.0f;
            
            return animationClips[_selectedClipIndex].animationSpeed;
        }
        
        /// <summary>
        /// Get the movement speed for the selected clip
        /// </summary>
        public float GetMovementSpeed()
        {
            if (animationClips == null || animationClips.Length == 0 || _selectedClipIndex < 0 || _selectedClipIndex >= animationClips.Length)
                return 0f;

            return animationClips[_selectedClipIndex].movementSpeed;
        }
        
        /// <summary>
        /// Get the number of valid (non-null) animation clips
        /// </summary>
        public int GetValidClipCount()
        {
            if (animationClips == null) return 0;
            
            int count = 0;
            for (int i = 0; i < animationClips.Length; i++)
            {
                if (animationClips[i].clip != null)
                {
                    count++;
                }
            }
            return count;
        }
        
        /// <summary>
        /// Get the maximum valid index for the selectedClipIndex field
        /// </summary>
        public int GetMaxClipIndex()
        {
            int validCount = GetValidClipCount();
            return validCount > 0 ? validCount - 1 : 0;
        }
        
        /// <summary>
        /// Check if this data is valid
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(stateName) && GetValidClipCount() > 0;
        }
        
        /// <summary>
        /// Reset the selected clip (useful for testing different random selections)
        /// </summary>
        public void ResetSelection()
        {
            _clipSelected = false;
            _selectedClip = null;
            _selectedClipIndex = -1;
        }
        
        /// <summary>
        /// Get the number of available animation clips
        /// </summary>
        public int GetClipCount()
        {
            return animationClips != null ? animationClips.Length : 0;
        }
    }
} 