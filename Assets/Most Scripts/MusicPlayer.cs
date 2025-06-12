using System.Collections;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    [Header("Music Tracks")]
    public AudioClip[] lobbyTracks;
    public AudioClip[] ambientSounds;
    
    [Header("Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;
    [Range(0f, 1f)]
    public float ambientVolume = 0.3f;
    public float fadeTime = 2f;
    public bool shufflePlaylist = true;
    
    private AudioSource musicSource;
    private AudioSource ambientSource;
    private int currentTrackIndex = 0;
    private Coroutine fadeCoroutine;
    
    void Awake()
    {
        SetupAudioSources();
    }
    
    void SetupAudioSources()
    {
        // Music source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = false;
        musicSource.volume = musicVolume;
        musicSource.priority = 64;
        
        // Ambient source
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;
        ambientSource.volume = ambientVolume;
        ambientSource.priority = 128;
    }
    
    void Start()
    {
        PlayLobbyMusic();
        PlayAmbientSounds();
    }
    
    public void PlayLobbyMusic()
    {
        if (lobbyTracks != null && lobbyTracks.Length > 0)
        {
            StartCoroutine(MusicPlaylist());
        }
    }
    
    public void PlayAmbientSounds()
    {
        if (ambientSounds != null && ambientSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, ambientSounds.Length);
            ambientSource.clip = ambientSounds[randomIndex];
            ambientSource.Play();
        }
    }
    
    IEnumerator MusicPlaylist()
    {
        while (gameObject != null)
        {
            // Select next track
            if (shufflePlaylist)
            {
                currentTrackIndex = Random.Range(0, lobbyTracks.Length);
            }
            else
            {
                currentTrackIndex = (currentTrackIndex + 1) % lobbyTracks.Length;
            }
            
            var track = lobbyTracks[currentTrackIndex];
            if (track != null)
            {
                // Fade in new track
                yield return StartCoroutine(FadeInTrack(track));
                
                // Wait for track to finish
                yield return new WaitForSeconds(track.length - fadeTime);
                
                // Fade out current track
                yield return StartCoroutine(FadeOutTrack());
                
                // Brief pause between tracks
                yield return new WaitForSeconds(1f);
            }
            else
            {
                yield return new WaitForSeconds(5f); // Wait if no track available
            }
        }
    }
    
    IEnumerator FadeInTrack(AudioClip track)
    {
        musicSource.clip = track;
        musicSource.volume = 0f;
        musicSource.Play();
        
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, timer / fadeTime);
            yield return null;
        }
        
        musicSource.volume = musicVolume;
    }
    
    IEnumerator FadeOutTrack()
    {
        float startVolume = musicSource.volume;
        
        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
            yield return null;
        }
        
        musicSource.volume = 0f;
        musicSource.Stop();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }
    
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null)
        {
            ambientSource.volume = ambientVolume;
        }
    }
    
    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.UnPause();
        }
    }
    
    public void StopMusic()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        
        fadeCoroutine = StartCoroutine(FadeOutTrack());
    }
    
    void OnDestroy()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
    }
}