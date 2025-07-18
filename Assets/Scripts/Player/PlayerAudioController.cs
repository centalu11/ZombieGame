using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace ZombieGame.Player
{
    [System.Serializable]
    public class ChaseMusicRange
    {
        [Range(0f, 1f)]
        public float startTime = 0f;
        
        [Range(0f, 1f)]
        public float endTime = 1f;
        
        [Range(0f, 1f)]
        public List<float> loopStartTimes = new List<float>() { 0.5f };
        
        public float GetStartTimeInSeconds(float clipLength)
        {
            return startTime * clipLength;
        }
        
        public float GetEndTimeInSeconds(float clipLength)
        {
            return endTime * clipLength;
        }
        
        public float GetLoopStartTimeInSeconds(float clipLength, int index = 0)
        {
            if (index >= 0 && index < loopStartTimes.Count)
            {
                return loopStartTimes[index] * clipLength;
            }
            return 0f;
        }
        
        public float GetRandomLoopStartTimeInSeconds(float clipLength)
        {
            if (loopStartTimes.Count == 0)
            {
                return 0f;
            }
            
            // Filter out null loop points (-1f)
            List<float> validLoopTimes = new List<float>();
            foreach (float time in loopStartTimes)
            {
                if (time >= 0f)
                {
                    validLoopTimes.Add(time);
                }
            }
            
            if (validLoopTimes.Count == 0)
            {
                return 0f;
            }
            else if (validLoopTimes.Count == 1)
            {
                return validLoopTimes[0] * clipLength;
            }
            else
            {
                // Use random selection when there are multiple valid loop points
                int randomIndex = Random.Range(0, validLoopTimes.Count);
                return validLoopTimes[randomIndex] * clipLength;
            }
        }
        
        public float GetDuration(float clipLength)
        {
            return GetEndTimeInSeconds(clipLength) - GetStartTimeInSeconds(clipLength);
        }
        
        public void ValidateLoopStartTimes()
        {
            // Remove any loop start times that are outside the valid range (but keep null values)
            for (int i = loopStartTimes.Count - 1; i >= 0; i--)
            {
                if (loopStartTimes[i] >= 0f && (loopStartTimes[i] < startTime || loopStartTimes[i] >= endTime))
                {
                    loopStartTimes.RemoveAt(i);
                }
            }
            
            // Don't sort - keep them in the order they were added for random selection
            
            // Ensure we have at least one valid loop start time
            bool hasValidLoopTime = false;
            foreach (float time in loopStartTimes)
            {
                if (time >= 0f)
                {
                    hasValidLoopTime = true;
                    break;
                }
            }
            
            if (!hasValidLoopTime)
            {
                loopStartTimes.Add(startTime + (endTime - startTime) * 0.5f);
            }
        }
    }

    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Ambience Background Music")]
        [Tooltip("Background music clip to play")]
        public AudioClip ambienceMusicClip;
        
        [Tooltip("Volume of the background music (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float ambienceVolume = 1f;
        
        [Tooltip("Playback speed of the music (1.0 = normal speed)")]
        [Range(0.1f, 3f)]
        public float ambienceSpeed = 1f;
        
        [Tooltip("Whether the music should loop")]
        public bool ambienceLoop = true;
        
        [Header("Chase Background Music")]
        [Tooltip("Music clip to play during chase")]
        public AudioClip chaseMusicClip;
        
        [Tooltip("Volume of chase music (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float chaseVolume = 1f;
        
        [Tooltip("Playback speed of chase music (1.0 = normal speed)")]
        [Range(0.1f, 3f)]
        public float chaseSpeed = 1f;
        
        [Tooltip("Whether chase music should loop")]
        public bool chaseLoop = true;
        
        [Header("Chase Music Range")]
        [Tooltip("Start and end times for chase music (0.0 = start of clip, 1.0 = end of clip)")]
        public ChaseMusicRange chaseMusicRange = new ChaseMusicRange();
        
        private AudioSource ambienceAudioSource;
        private AudioSource[] chaseAudioSources = new AudioSource[2];
        private int currentChaseAudioSource = 0; // 0 = intro, 1 = loop
        private double chaseGoalTime = 0; // DSP time for next chase music event
        private bool isChaseMusicActive = false;
        private PlayerState playerState;
        private GameObject environmentAudioObject;
        private Coroutine fadeOutCoroutine;
        private Coroutine chaseRangeCoroutine;
        private float originalChaseVolume;
        
        private void Start()
        {
            // Find or create Environment GameObject
            SetupEnvironmentAudio();
            
            // Get PlayerState component
            playerState = GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogWarning("[PlayerAudioController] PlayerState component not found. Chase music functionality will be disabled.");
            }
            
            // Configure ambience AudioSource
            ambienceAudioSource.clip = ambienceMusicClip;
            ambienceAudioSource.volume = ambienceVolume;
            ambienceAudioSource.pitch = ambienceSpeed;
            ambienceAudioSource.loop = ambienceLoop;
            ambienceAudioSource.playOnAwake = true;
            
            // Configure chase intro AudioSource (index 0)
            chaseAudioSources[0].clip = null;
            chaseAudioSources[0].volume = chaseVolume;
            chaseAudioSources[0].pitch = chaseSpeed;
            chaseAudioSources[0].loop = false;
            chaseAudioSources[0].playOnAwake = false;

            // Configure chase loop AudioSource (index 1) - empty for now
            chaseAudioSources[1].clip = null; // Empty audio source
            chaseAudioSources[1].volume = chaseVolume;
            chaseAudioSources[1].pitch = chaseSpeed;
            chaseAudioSources[1].loop = false;
            chaseAudioSources[1].playOnAwake = false;
            
            // Store original chase volume for fade effects
            originalChaseVolume = chaseVolume;
            
            // Start playing ambience if we have a clip
            if (ambienceMusicClip != null)
            {
                ambienceAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Find or create Environment GameObject and set up audio sources
        /// </summary>
        private void SetupEnvironmentAudio()
        {
            // Look for existing Environment GameObject
            environmentAudioObject = GameObject.Find("Environment");
            
            if (environmentAudioObject == null)
            {
                // Create Environment GameObject if it doesn't exist
                environmentAudioObject = new GameObject("Environment");
            }
            
            // Add or get ambience AudioSource
            ambienceAudioSource = environmentAudioObject.GetComponent<AudioSource>();
            if (ambienceAudioSource == null)
            {
                ambienceAudioSource = environmentAudioObject.AddComponent<AudioSource>();
            }
            
            // Add chase AudioSources (second and third AudioSources on the same GameObject)
            chaseAudioSources[0] = environmentAudioObject.AddComponent<AudioSource>();
            chaseAudioSources[1] = environmentAudioObject.AddComponent<AudioSource>();
        }
        
        /// <summary>
        /// Switch to chase background music with DSP scheduling
        /// </summary>
        public void PlayChaseMusic()
        {
            if (chaseAudioSources[0] == null || chaseMusicClip == null) return;
            
            float clipLength = chaseMusicClip.length;
            float startTime = chaseMusicRange.GetStartTimeInSeconds(clipLength);
            float loopStartTime = chaseMusicRange.GetRandomLoopStartTimeInSeconds(clipLength); // Use random loop point
            float endTime = chaseMusicRange.GetEndTimeInSeconds(clipLength);
            
            // If chaseGoalTime is 0 or null, play the intro clip
            if (chaseGoalTime == 0)
            {
                // Stop ambience music
                if (ambienceAudioSource != null)
                {
                    ambienceAudioSource.Stop();
                }
                
                chaseAudioSources[currentChaseAudioSource].clip = chaseMusicClip;
                chaseAudioSources[currentChaseAudioSource].time = startTime;
                chaseAudioSources[currentChaseAudioSource].PlayScheduled(AudioSettings.dspTime);
                
                // Set next goal time for intro duration
                chaseGoalTime = AudioSettings.dspTime + (loopStartTime - startTime);    
                chaseAudioSources[currentChaseAudioSource].SetScheduledEndTime(chaseGoalTime);
            }
            else
            {
                // There's a goal time, assign the loop clip
                chaseAudioSources[currentChaseAudioSource].clip = chaseMusicClip;
                chaseAudioSources[currentChaseAudioSource].time = loopStartTime;
                chaseAudioSources[currentChaseAudioSource].PlayScheduled(AudioSettings.dspTime);
                
                // Set next goal time for loop duration
                chaseGoalTime = AudioSettings.dspTime + (endTime - loopStartTime) - 0.1f;
                chaseAudioSources[currentChaseAudioSource].SetScheduledEndTime(chaseGoalTime + 0.1f);
            }
            
            // Toggle the current AudioSource index
            currentChaseAudioSource = 1 - currentChaseAudioSource;

            isChaseMusicActive = true;
        }
        
        /// <summary>
        /// Start playing chase music within the specified range
        /// </summary>
        private void StartChaseMusicRange()
        {
            if (chaseAudioSources[0] == null || chaseMusicClip == null) return;
            
            // Stop any existing range coroutine
            if (chaseRangeCoroutine != null)
            {
                StopCoroutine(chaseRangeCoroutine);
            }
            
            // Start the range playback coroutine
            chaseRangeCoroutine = StartCoroutine(PlayChaseMusicRange());
        }
        
        /// <summary>
        /// Coroutine to play chase music within the specified range
        /// </summary>
        private IEnumerator PlayChaseMusicRange()
        {
            if (chaseAudioSources[0] == null || chaseMusicClip == null) yield break;
            
            float clipLength = chaseMusicClip.length;
            float startTime = chaseMusicRange.GetStartTimeInSeconds(clipLength);
            float endTime = chaseMusicRange.GetEndTimeInSeconds(clipLength);
            float loopStartTime = chaseMusicRange.GetLoopStartTimeInSeconds(clipLength);
            float rangeDuration = chaseMusicRange.GetDuration(clipLength);
            
            // Validate range
            if (startTime >= endTime || rangeDuration <= 0)
            {
                Debug.LogWarning("[PlayerAudioController] Invalid chase music range. Using full clip.");
                startTime = 0f;
                endTime = clipLength;
                rangeDuration = clipLength;
            }
            
            do
            {
                // Set the start time and play
                chaseAudioSources[0].time = startTime;
                chaseAudioSources[0].Play();
                
                // Wait for the duration of the range
                yield return new WaitForSeconds(rangeDuration);
                
                // If not looping, break out of the loop
                if (!chaseLoop)
                {
                    break;
                }
                
                // If looping and we have a loop start time, set it for next iteration
                if (chaseLoop && isChaseMusicActive)
                {
                    startTime = loopStartTime;
                }
                
            } while (chaseLoop && isChaseMusicActive);
        }
        
        /// <summary>
        /// Revert to ambience background music
        /// </summary>
        public void RevertToOriginalMusic()
        {
            if (ambienceAudioSource == null || ambienceMusicClip == null) return;
            
            if (isChaseMusicActive)
            {
                // Stop the range coroutine
                if (chaseRangeCoroutine != null)
                {
                    StopCoroutine(chaseRangeCoroutine);
                    chaseRangeCoroutine = null;
                }
                
                // Start fade out coroutine for chase music (don't start ambience yet)
                if (fadeOutCoroutine != null)
                {
                    StopCoroutine(fadeOutCoroutine);
                }
                fadeOutCoroutine = StartCoroutine(FadeOutChaseMusic());
                
                isChaseMusicActive = false;
            }
        }
        
        /// <summary>
        /// Check if chase music is currently playing
        /// </summary>
        public bool IsChaseMusicActive()
        {
            return isChaseMusicActive;
        }
        
        private void Update()
        {
            // Check player detection state and update music accordingly
            if (playerState != null)
            {
                if (playerState.IsBeingChased() && AudioSettings.dspTime > chaseGoalTime)
                {
                    PlayChaseMusic();
                }
                else if (!playerState.IsBeingChased() && isChaseMusicActive)
                {
                    RevertToOriginalMusic();
                }
            }
        }
        
        /// <summary>
        /// Force update the background music settings (useful for runtime changes)
        /// </summary>
        public void UpdateBackgroundMusic()
        {
            if (ambienceAudioSource != null)
            {
                // Update ambience AudioSource settings
                ambienceAudioSource.volume = ambienceVolume;
                ambienceAudioSource.pitch = ambienceSpeed;
                ambienceAudioSource.loop = ambienceLoop;
            }
            
            if (chaseAudioSources[0] != null)
            {
                // Update chase intro AudioSource settings
                chaseAudioSources[0].volume = chaseVolume;
                chaseAudioSources[0].pitch = chaseSpeed;
                chaseAudioSources[0].loop = false;
            }
            
            if (chaseAudioSources[1] != null)
            {
                // Update chase loop AudioSource settings
                chaseAudioSources[1].volume = chaseVolume;
                chaseAudioSources[1].pitch = chaseSpeed;
                chaseAudioSources[1].loop = false; // We'll handle looping manually later
            }
        }
        
        /// <summary>
        /// Coroutine to fade out chase music over 5 seconds
        /// </summary>
        private IEnumerator FadeOutChaseMusic()
        {
            if (chaseAudioSources[0] == null) yield break;
            
            float startVolume = chaseAudioSources[0].volume;
            float fadeTime = 5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
                chaseAudioSources[0].volume = newVolume;
                chaseAudioSources[1].volume = newVolume;
                yield return null;
            }
            
            // Stop the chase music after fade is complete
            chaseAudioSources[0].Stop();
            chaseAudioSources[1].Stop();
            chaseAudioSources[0].volume = originalChaseVolume; // Reset volume for next use
            chaseAudioSources[1].volume = originalChaseVolume;
            fadeOutCoroutine = null;
            
            // Now start the ambience music after chase is fully faded out
            if (ambienceAudioSource != null && ambienceMusicClip != null)
            {
                ambienceAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Coroutine to fade in chase music to original volume
        /// </summary>
        private IEnumerator FadeInChaseMusic()
        {
            if (chaseAudioSources[0] == null) yield break;
            
            // Set chase as active immediately to prevent Update from calling PlayChaseMusic again
            isChaseMusicActive = true;
            
            float startVolume = chaseAudioSources[0].volume;
            float targetVolume = originalChaseVolume;
            float fadeTime = 3f; // 3 second fade in as requested
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeTime);
                chaseAudioSources[0].volume = newVolume;
                chaseAudioSources[1].volume = newVolume;
                yield return null;
            }
            
            chaseAudioSources[0].volume = targetVolume;
            chaseAudioSources[1].volume = targetVolume;
        }
        
        /// <summary>
        /// Validate chase music range when clip changes
        /// </summary>
        private void OnValidate()
        {
            if (chaseMusicRange != null)
            {
                // Ensure start time is before end time
                if (chaseMusicRange.startTime >= chaseMusicRange.endTime)
                {
                    chaseMusicRange.endTime = Mathf.Min(1f, chaseMusicRange.startTime + 0.1f);
                }
                
                // Validate and sort loop start times
                chaseMusicRange.ValidateLoopStartTimes();
                
                // Ensure values are within valid range
                chaseMusicRange.startTime = Mathf.Clamp01(chaseMusicRange.startTime);
                chaseMusicRange.endTime = Mathf.Clamp01(chaseMusicRange.endTime);
            }
        }
    }
} 