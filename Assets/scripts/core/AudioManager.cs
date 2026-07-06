using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default Scene Audio")]
    [SerializeField] private bool playMusicOnStart = false;
    [SerializeField] private AudioClip startMusicClip;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private AudioClip currentMusicClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSources();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var cfg = root.GetComponentInChildren<SceneAudioConfig>(true);
            if (cfg != null && cfg.playOnStart)
            {
                ApplySceneMusic(cfg.musicClip, cfg.loop, cfg.musicVolume);
                return;
            }
        }
    }

    private void Start()
    {
        if (playMusicOnStart && startMusicClip != null)
            PlayMusic(startMusicClip, true);
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            var go = new GameObject("SFXSource");
            go.transform.SetParent(transform);
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    public bool PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 0.0f)
    {
        if (clip == null) return false;
        if (musicSource == null) EnsureAudioSources();
        if (musicSource.clip == clip && musicSource.isPlaying) return true;

        currentMusicClip = clip;

        if (fadeTime > 0f && musicSource.isPlaying)
        {
            StartCoroutine(FadeOutIn(clip, loop, fadeTime));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }

        return true;
    }

    public bool ApplySceneMusic(AudioClip clip, bool loop = true, float volume = 1f)
    {
        if (clip == null)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                SetMusicVolume(volume);
                return true;
            }

            return false;
        }

        if (musicSource == null) EnsureAudioSources();
        if (musicSource.clip == clip && musicSource.isPlaying) return true;

        SetMusicVolume(volume);
        currentMusicClip = clip;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.volume = musicVolume;
        musicSource.Play();
        return true;
    }

    private IEnumerator FadeOutIn(AudioClip newClip, bool loop, float time)
    {
        float start = musicSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.loop = loop;
        musicSource.Play();

        t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t / time);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public bool PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return false;
        if (sfxSource == null) EnsureAudioSources();
        sfxSource.PlayOneShot(clip, volume * sfxVolume);
        return true;
    }

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    public bool IsMusicPlaying() => musicSource != null && musicSource.isPlaying;
}
