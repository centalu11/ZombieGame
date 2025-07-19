using UnityEngine;

namespace ZombieGame.Environment
{
    public class EnvironmentAudioController : MonoBehaviour
    {
        [Header("Ambience Background Music")]
        [Tooltip("Background music clip to play")]
        public AudioClip ambienceMusicClip;
        
        [Tooltip("Volume of the background music (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float ambienceVolume = 0.35f;
        
        [Tooltip("Whether the music should loop")]
        public bool ambienceLoop = true;
        
        private AudioSource ambienceAudioSource;
        
        private void Start()
        {
            // Get or add AudioSource component to this GameObject
            ambienceAudioSource = GetComponent<AudioSource>();
            if (ambienceAudioSource == null)
            {
                ambienceAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Configure ambience AudioSource
            ambienceAudioSource.clip = ambienceMusicClip;
            ambienceAudioSource.volume = ambienceVolume;
            ambienceAudioSource.pitch = 1f; // Normal speed
            ambienceAudioSource.loop = ambienceLoop;
            ambienceAudioSource.playOnAwake = true;
            
            // Start playing ambience if we have a clip
            if (ambienceMusicClip != null)
            {
                ambienceAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Play the current ambience music
        /// </summary>
        public void PlayMusic()
        {
            if (ambienceAudioSource != null && ambienceMusicClip != null)
            {
                ambienceAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Stop the current ambience music
        /// </summary>
        public void StopMusic()
        {
            if (ambienceAudioSource != null)
            {
                ambienceAudioSource.Stop();
            }
        }
        
        /// <summary>
        /// Set a new music clip and optionally play it
        /// </summary>
        /// <param name="musicClip">The new music clip to set</param>
        public void SetMusic(AudioClip musicClip)
        {
            ambienceMusicClip = musicClip;
            if (ambienceAudioSource != null)
            {
                ambienceAudioSource.clip = musicClip;
            }
        }
    }
} 