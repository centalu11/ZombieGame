using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ZombieGame.Environment;

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
        [Header("Chase Background Music")]
        [Tooltip("Music clip to play during chase")]
        public AudioClip chaseMusicClip;
        
        [Tooltip("Volume of chase music (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float chaseVolume = 0.55f;

        [Tooltip("Enable fade effects for chase music")]
        public bool chaseFade = true;
        
        [Tooltip("Fade in time for chase music (seconds)")]
        [Range(0f, 10f)]
        public float chaseFadeIn = 3f;
        
        [Tooltip("Fade out time for chase music (seconds)")]
        [Range(0f, 10f)]
        public float chaseFadeOut = 5f;

        [Tooltip("Whether chase music should loop")]
        public bool chaseLoop = true;
        
        
        [Header("Chase Music Range")]
        [Tooltip("Start and end times for chase music (0.0 = start of clip, 1.0 = end of clip)")]
        public ChaseMusicRange chaseMusicRange = new ChaseMusicRange();
        
        private AudioSource[] chaseAudioSources = new AudioSource[2];
        private int currentChaseAudioSource = 0; // 0 = intro, 1 = loop
        private double chaseGoalTime = 0; // DSP time for next chase music event
        private bool isChaseMusicActive = false;
        private PlayerState playerState;
        private GameObject environmentAudioObject;
        private EnvironmentAudioController environmentAudioController;
        private Coroutine fadeOutCoroutine;
        private Coroutine fadeInCoroutine;
        private float selectedLoopStartTime = -1f;
        private float currentChaseVolume; // Track current volume independently of AudioSource
        
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
            
            // Try to get EnvironmentAudioController from Environment GameObject
            if (environmentAudioObject != null)
            {
                environmentAudioController = environmentAudioObject.GetComponent<EnvironmentAudioController>();
            }
            
            // Initialize current volume
            currentChaseVolume = chaseVolume;
            
            // Configure chase intro AudioSource (index 0)
            chaseAudioSources[0].clip = null;
            chaseAudioSources[0].volume = chaseVolume;
            chaseAudioSources[0].pitch = 1f; // Normal speed
            chaseAudioSources[0].loop = false;
            chaseAudioSources[0].playOnAwake = false;

            // Configure chase loop AudioSource (index 1) - empty for now
            chaseAudioSources[1].clip = null; // Empty audio source
            chaseAudioSources[1].volume = chaseVolume;
            chaseAudioSources[1].pitch = 1f; // Normal speed
            chaseAudioSources[1].loop = false;
            chaseAudioSources[1].playOnAwake = false;
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
            
            // Add chase AudioSources (second and third AudioSources on the same GameObject)
            chaseAudioSources[0] = environmentAudioObject.AddComponent<AudioSource>();
            chaseAudioSources[1] = environmentAudioObject.AddComponent<AudioSource>();
        }
        
        /// <summary>
        /// Switch to chase background music with DSP scheduling
        /// </summary>
        public void PlayChaseMusic()
        {
            // For reference just in case the input get changed, the loop points are 12.55 and 37.85

            if (chaseAudioSources[0] == null || chaseMusicClip == null) return;
            

            
            float clipLength = chaseMusicClip.length;
            float startTime = chaseMusicRange.GetStartTimeInSeconds(clipLength);
            float loopStartTime = chaseMusicRange.GetRandomLoopStartTimeInSeconds(clipLength); // Use random loop point
            float endTime = chaseMusicRange.GetEndTimeInSeconds(clipLength);
            
            // If chaseGoalTime is 0 or null, play the intro clip
            if (chaseGoalTime == 0)
            {
                // Stop ambience music if EnvironmentAudioController is available
                if (environmentAudioController != null)
                {
                    environmentAudioController.StopMusic();
                }
                
                chaseAudioSources[currentChaseAudioSource].clip = chaseMusicClip;
                chaseAudioSources[currentChaseAudioSource].time = startTime;
                
                // Apply fade in if enabled
                if (chaseFade)
                {
                    chaseAudioSources[currentChaseAudioSource].volume = 0f; // Start at 0 volume
                    currentChaseVolume = 0f; // Update tracked volume
                    // Start fade in coroutine
                    if (fadeInCoroutine != null)
                    {
                        StopCoroutine(fadeInCoroutine);
                    }
                    fadeInCoroutine = StartCoroutine(FadeInChaseMusic());
                }
                else
                {
                    chaseAudioSources[currentChaseAudioSource].volume = currentChaseVolume; // Use current volume
                }
                
                chaseAudioSources[currentChaseAudioSource].PlayScheduled(AudioSettings.dspTime);
                
                // Set next goal time for intro duration
                chaseGoalTime = AudioSettings.dspTime + (loopStartTime - startTime);    
                chaseAudioSources[currentChaseAudioSource].SetScheduledEndTime(chaseGoalTime);

                selectedLoopStartTime = loopStartTime;
            }
            else
            {
                // Check if we have a selected loop start time, if so use it, otherwise use the random loop start time
                // This for the first clip after the into so it will start at the selected loop
                loopStartTime = selectedLoopStartTime > -1f ? selectedLoopStartTime : loopStartTime;

                // There's a goal time, assign the loop clip
                chaseAudioSources[currentChaseAudioSource].clip = chaseMusicClip;
                chaseAudioSources[currentChaseAudioSource].time = loopStartTime;
                chaseAudioSources[currentChaseAudioSource].volume = currentChaseVolume; // Use current volume
                chaseAudioSources[currentChaseAudioSource].PlayScheduled(AudioSettings.dspTime);
                
                // Set next goal time for loop duration
                chaseGoalTime = AudioSettings.dspTime + (endTime - loopStartTime) - 0.1f;
                chaseAudioSources[currentChaseAudioSource].SetScheduledEndTime(chaseGoalTime + 0.1f);


                // After the first loop clip, remove this
                selectedLoopStartTime = -1f;
            }
            
            // Toggle the current AudioSource index
            currentChaseAudioSource = 1 - currentChaseAudioSource;

            isChaseMusicActive = true;
        }
        
        /// <summary>
        /// Revert to ambience background music
        /// </summary>
        public void RevertToOriginalMusic()
        {
            // Don't start a new fade out if one is already running
            if (fadeOutCoroutine != null) return;
            
            // Stop fade in if it's running
            if (fadeInCoroutine != null)
            {
                StopCoroutine(fadeInCoroutine);
                fadeInCoroutine = null;
            }
            
            if (isChaseMusicActive)
            {
                if (chaseFade)
                {
                    // Start fade out coroutine for chase music
                    fadeOutCoroutine = StartCoroutine(FadeOutChaseMusic());
                    // Don't set isChaseMusicActive = false here - let the coroutine do it at the end
                }
                else
                {
                    // Stop chase music immediately without fade
                    chaseAudioSources[0].Stop();
                    chaseAudioSources[1].Stop();
                    chaseAudioSources[0].volume = chaseVolume;
                    chaseAudioSources[1].volume = chaseVolume;
                    chaseGoalTime = 0;
                    selectedLoopStartTime = -1f;
                    
                    // Start ambience music if EnvironmentAudioController is available
                    if (environmentAudioController != null)
                    {
                        environmentAudioController.PlayMusic();
                    }
                    
                    isChaseMusicActive = false;
                }
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
            UpdateForChaseMusic();
        }
        
        private void UpdateForChaseMusic()
        {
            // Check player detection state and update music accordingly
            if (playerState != null)
            {
                // Handle fade in when player enters chase state
                if (playerState.IsBeingChased() && fadeOutCoroutine != null)
                {
                    // Stop fade out and start fade in
                    StopCoroutine(fadeOutCoroutine);
                    fadeOutCoroutine = null;
                    
                    if (chaseFade)
                    {
                        // Start fade in from current volume to full volume
                        if (fadeInCoroutine != null)
                        {
                            StopCoroutine(fadeInCoroutine);
                        }
                        fadeInCoroutine = StartCoroutine(FadeInChaseMusic());
                    }
                }
                
                // Handle music scheduling
                if ((playerState.IsBeingChased() || isChaseMusicActive) && AudioSettings.dspTime > chaseGoalTime)
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
        /// Coroutine to fade out chase music
        /// </summary>
        private IEnumerator FadeOutChaseMusic()
        {
            if (chaseAudioSources[currentChaseAudioSource] == null) yield break;
            
            float startVolume = currentChaseVolume; // Start from current volume
            float fadeTime = chaseFadeOut;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
                currentChaseVolume = newVolume; // Update tracked volume
                chaseAudioSources[0].volume = newVolume;
                chaseAudioSources[1].volume = newVolume;

                yield return null;
            }
            
            // Stop the chase music after fade is complete
            chaseAudioSources[0].Stop();
            chaseAudioSources[1].Stop();
            chaseAudioSources[0].volume = chaseVolume; // Reset volume for next use
            chaseAudioSources[1].volume = chaseVolume;
            currentChaseVolume = chaseVolume; // Reset tracked volume

            isChaseMusicActive = false;
            
            // Reset chase goal time
            chaseGoalTime = 0;
            
            // Now start the ambience music after chase is fully faded out
            if (environmentAudioController != null)
            {
                environmentAudioController.PlayMusic();
            }
            
            fadeOutCoroutine = null;
        }
        
        /// <summary>
        /// Coroutine to fade in chase music
        /// </summary>
        private IEnumerator FadeInChaseMusic()
        {
            if (chaseAudioSources[currentChaseAudioSource] == null) yield break;
            
            float startVolume = currentChaseVolume; // Start from tracked current volume
            float targetVolume = chaseVolume;
            float fadeTime = chaseFadeIn;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float newVolume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / fadeTime);
                currentChaseVolume = newVolume; // Update tracked volume
                chaseAudioSources[0].volume = newVolume;
                chaseAudioSources[1].volume = newVolume;

                yield return null;
            }
            
            // Ensure we reach the target volume exactly
            chaseAudioSources[0].volume = targetVolume;
            chaseAudioSources[1].volume = targetVolume;
            currentChaseVolume = targetVolume; // Update tracked volume
            
            fadeInCoroutine = null;
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