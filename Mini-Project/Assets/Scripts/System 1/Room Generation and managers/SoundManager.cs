using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    
    [Header("Background Music")]
    [Tooltip("List of background music tracks - will play in random order")]
    public AudioClip[] backgroundMusicTracks;
    
    [Tooltip("Volume for background music (0-1)")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    
    [Header("End Room Audio")]
    [Tooltip("Special audio that plays when player interacts with end room object")]
    public AudioClip endRoomAudio;
    
    [Tooltip("Volume for end room audio (0-1)")]
    [Range(0f, 1f)]
    public float endRoomVolume = 0.7f;
    
    [Tooltip("How long to fade out background music (seconds)")]
    public float fadeOutDuration = 2f;
    
    [Header("Audio Sources")]
    private AudioSource musicSource;
    private AudioSource endRoomSource;
    
    private List<AudioClip> remainingTracks = new List<AudioClip>();
    private bool isPlayingEndRoomAudio = false;
    private Coroutine fadeCoroutine;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Create audio sources
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = false;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;
        
        endRoomSource = gameObject.AddComponent<AudioSource>();
        endRoomSource.loop = false;
        endRoomSource.volume = endRoomVolume;
        endRoomSource.playOnAwake = false;
    }
    
    void Start()
    {
        StartBackgroundMusic();
    }
    
    void Update()
    {
        // Check if current background music finished, play next one
        if (!isPlayingEndRoomAudio && !musicSource.isPlaying && backgroundMusicTracks != null && backgroundMusicTracks.Length > 0)
        {
            PlayNextBackgroundTrack();
        }
    }
    
    /// <summary>
    /// Starts playing background music from a randomized list
    /// </summary>
    public void StartBackgroundMusic()
    {
        if (backgroundMusicTracks == null || backgroundMusicTracks.Length == 0)
        {
            Debug.LogWarning("No background music tracks assigned!");
            return;
        }
        
        // Initialize the playlist
        RefillPlaylist();
        PlayNextBackgroundTrack();
    }
    
    /// <summary>
    /// Plays the next track in the randomized list
    /// </summary>
    void PlayNextBackgroundTrack()
    {
        if (isPlayingEndRoomAudio) return;
        
        // If playlist empty, refill it
        if (remainingTracks.Count == 0)
        {
            RefillPlaylist();
        }
        
        // Get random track from remaining tracks
        int randomIndex = Random.Range(0, remainingTracks.Count);
        AudioClip nextTrack = remainingTracks[randomIndex];
        remainingTracks.RemoveAt(randomIndex);
        
        musicSource.clip = nextTrack;
        musicSource.volume = musicVolume;
        musicSource.Play();
        
        Debug.Log($"Now playing: {nextTrack.name} ({remainingTracks.Count} tracks remaining in playlist)");
    }
    
    /// <summary>
    /// Refills the playlist with all tracks (shuffled)
    /// </summary>
    void RefillPlaylist()
    {
        remainingTracks.Clear();
        remainingTracks.AddRange(backgroundMusicTracks);
        Debug.Log($"Playlist refilled with {remainingTracks.Count} tracks");
    }
    
    /// <summary>
    /// Plays the end room audio - fades out background music, waits for end audio to finish
    /// Call this when player interacts with the end room object
    /// </summary>
    public void PlayEndRoomAudio(System.Action onComplete = null)
    {
        if (endRoomAudio == null)
        {
            Debug.LogWarning("No end room audio assigned!");
            onComplete?.Invoke();
            return;
        }
        
        StartCoroutine(PlayEndRoomAudioCoroutine(onComplete));
    }
    
    IEnumerator PlayEndRoomAudioCoroutine(System.Action onComplete)
    {
        isPlayingEndRoomAudio = true;
        
        // Start fading out background music (but don't wait for it)
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutMusic());
        
        // Play end room audio IMMEDIATELY (no waiting for fade)
        endRoomSource.clip = endRoomAudio;
        endRoomSource.volume = endRoomVolume;
        endRoomSource.Play();
        
        Debug.Log($"Playing end room audio: {endRoomAudio.name} (duration: {endRoomAudio.length}s) - fading out background music");
        
        // Wait for end room audio to finish
        yield return new WaitForSeconds(endRoomAudio.length);
        
        // Mark as complete
        isPlayingEndRoomAudio = false;
        
        Debug.Log("End room audio finished");
        
        // Callback to trigger level generation
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Fades out the background music
    /// </summary>
    IEnumerator FadeOutMusic()
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        musicSource.volume = 0f;
        musicSource.Stop();
        
        Debug.Log("Background music faded out");
    }
    
    /// <summary>
    /// Resumes background music after end room audio
    /// Called automatically after PlayEndRoomAudio completes
    /// </summary>
    public void ResumeBackgroundMusic()
    {
        isPlayingEndRoomAudio = false;
        musicSource.volume = musicVolume;
        PlayNextBackgroundTrack();
    }
    
    /// <summary>
    /// Stops all audio
    /// </summary>
    public void StopAllAudio()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        musicSource.Stop();
        endRoomSource.Stop();
        isPlayingEndRoomAudio = false;
    }
}