using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized audio manager for playing sound effects and music throughout the game
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private int maxConcurrentSFX = 10; // Limit concurrent sound effects

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

    [Header("Sound Effects")]
    [SerializeField] [Range(0f, 1f)] private float sfxVolume = 0.7f;
    
    [Header("Player Sounds")]
    [SerializeField] private AudioClip playerAttackSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] private AudioClip playerDeathSound;
    [SerializeField] private AudioClip playerPickupSound;
    
    [Header("Enemy Sounds")]
    [SerializeField] private AudioClip enemyShootSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip enemyDeathSound;
    [SerializeField] private AudioClip enemyChargeSound;
    
    [Header("Projectile Sounds")]
    [SerializeField] private AudioClip projectileShootSound;
    [SerializeField] private AudioClip projectileHitSound;
    [SerializeField] private AudioClip projectileWallHitSound;
    
    [Header("Environment Sounds")]
    [SerializeField] private AudioClip tileBreakSound;
    [SerializeField] private AudioClip teleporterSound;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip coinPickupSound;

    // Track active sound effects to limit concurrent playback
    private Queue<AudioSource> activeSFXSources = new Queue<AudioSource>();
    private List<AudioSource> pooledSFXSources = new List<AudioSource>();

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    /// <summary>
    /// Initialize audio sources if not assigned
    /// </summary>
    private void InitializeAudioSources()
    {
        // Create music source if not assigned
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = musicVolume;
        }

        // Create SFX source if not assigned
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFXSource");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        // Pre-create pooled SFX sources
        for (int i = 0; i < maxConcurrentSFX; i++)
        {
            GameObject pooledObj = new GameObject($"PooledSFX_{i}");
            pooledObj.transform.SetParent(transform);
            AudioSource pooledSource = pooledObj.AddComponent<AudioSource>();
            pooledSource.playOnAwake = false;
            pooledSource.volume = sfxVolume;
            pooledSFXSources.Add(pooledSource);
        }
    }

    /// <summary>
    /// Play background music
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;

        if (musicSource.isPlaying && musicSource.clip == clip)
        {
            return; // Already playing this music
        }

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Play a sound effect (uses pooling to limit concurrent sounds)
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.clip = clip;
            source.volume = sfxVolume * volumeMultiplier;
            source.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            
            // Track this source
            activeSFXSources.Enqueue(source);
        }
    }

    /// <summary>
    /// Play a sound effect at a specific position in 3D space
    /// </summary>
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAvailableSFXSource();
        if (source != null)
        {
            source.transform.position = position;
            source.spatialBlend = 1f; // 3D sound
            source.clip = clip;
            source.volume = sfxVolume * volumeMultiplier;
            source.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            
            activeSFXSources.Enqueue(source);
        }
    }

    /// <summary>
    /// Get an available audio source from the pool
    /// </summary>
    private AudioSource GetAvailableSFXSource()
    {
        // Clean up finished sources
        while (activeSFXSources.Count > 0 && !activeSFXSources.Peek().isPlaying)
        {
            AudioSource finished = activeSFXSources.Dequeue();
            finished.spatialBlend = 0f; // Reset to 2D
        }

        // Find an available source
        foreach (AudioSource source in pooledSFXSources)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // All sources busy, use the oldest one (will interrupt it)
        if (activeSFXSources.Count > 0)
        {
            return activeSFXSources.Dequeue();
        }

        // Fallback to main SFX source
        return sfxSource;
    }

    // Convenience methods for common sounds
    public void PlayPlayerAttack() => PlaySFX(playerAttackSound);
    public void PlayPlayerHit() => PlaySFX(playerHitSound);
    public void PlayPlayerDeath() => PlaySFX(playerDeathSound);
    public void PlayPlayerPickup() => PlaySFX(playerPickupSound);
    
    public void PlayEnemyShoot() => PlaySFX(enemyShootSound);
    public void PlayEnemyHit() => PlaySFX(enemyHitSound);
    public void PlayEnemyDeath() => PlaySFX(enemyDeathSound);
    public void PlayEnemyCharge() => PlaySFX(enemyChargeSound);
    
    public void PlayProjectileShoot() => PlaySFX(projectileShootSound);
    public void PlayProjectileHit() => PlaySFX(projectileHitSound);
    public void PlayProjectileWallHit() => PlaySFX(projectileWallHitSound);
    
    public void PlayTileBreak() => PlaySFX(tileBreakSound);
    public void PlayTeleporter() => PlaySFX(teleporterSound);
    public void PlayDoorOpen() => PlaySFX(doorOpenSound);
    public void PlayCoinPickup() => PlaySFX(coinPickupSound);

    /// <summary>
    /// Set music volume (0-1)
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    /// <summary>
    /// Set SFX volume (0-1)
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
        foreach (AudioSource source in pooledSFXSources)
        {
            source.volume = sfxVolume;
        }
    }

    /// <summary>
    /// Get current music volume
    /// </summary>
    public float GetMusicVolume() => musicVolume;

    /// <summary>
    /// Get current SFX volume
    /// </summary>
    public float GetSFXVolume() => sfxVolume;
}

