using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource transitionMusicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Default Scene Audio")]
    [SerializeField] private bool playMusicOnStart = false;
    [SerializeField] private AudioClip startMusicClip;

    [Header("Fade Settings")]
    [SerializeField, Min(0f)] private float defaultMusicFadeTime = 0.75f;
    [SerializeField, Min(0f)] private float defaultStopFadeTime = 0.5f;

    private float musicVolume = 1f;
    private float sfxVolume = 1f;
    private AudioClip currentMusicClip;
    private Coroutine musicFadeRoutine;
    private AudioSource activeMusicSource;
    private AudioSource standbyMusicSource;

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
                ApplySceneMusic(cfg.musicClip, cfg.loop, cfg.musicVolume, cfg.fadeTime);
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

        if (transitionMusicSource == null)
        {
            var go = new GameObject("TransitionMusicSource");
            go.transform.SetParent(transform);
            transitionMusicSource = go.AddComponent<AudioSource>();
            transitionMusicSource.playOnAwake = false;
        }

        if (activeMusicSource == null)
            activeMusicSource = musicSource;
        if (standbyMusicSource == null || standbyMusicSource == activeMusicSource)
            standbyMusicSource = activeMusicSource == musicSource ? transitionMusicSource : musicSource;

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
        EnsureAudioSources();

        if (currentMusicClip == clip && musicFadeRoutine != null)
            return true;

        if (activeMusicSource.clip == clip && activeMusicSource.isPlaying)
        {
            activeMusicSource.loop = loop;
            activeMusicSource.volume = musicVolume;
            return true;
        }

        currentMusicClip = clip;
        float resolvedFadeTime = fadeTime > 0f ? fadeTime : (activeMusicSource.isPlaying ? defaultMusicFadeTime : 0f);

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (resolvedFadeTime > 0f && activeMusicSource.isPlaying)
        {
            musicFadeRoutine = StartCoroutine(CrossFadeMusic(clip, loop, resolvedFadeTime));
        }
        else
        {
            PlayMusicImmediately(activeMusicSource, clip, loop);
            if (standbyMusicSource != null) standbyMusicSource.Stop();
        }

        return true;
    }

    public bool ApplySceneMusic(AudioClip clip, bool loop = true, float volume = 1f, float fadeTime = -1f)
    {
        if (clip == null)
        {
            if (activeMusicSource != null && activeMusicSource.isPlaying)
            {
                SetMusicVolume(volume);
                return true;
            }

            return false;
        }

        if (musicSource == null) EnsureAudioSources();
        musicVolume = Mathf.Clamp01(volume);
        return PlayMusic(clip, loop, fadeTime >= 0f ? fadeTime : defaultMusicFadeTime);
    }

    private void PlayMusicImmediately(AudioSource source, AudioClip clip, bool loop)
    {
        source.clip = clip;
        source.loop = loop;
        source.volume = musicVolume;
        source.Play();
    }

    private IEnumerator CrossFadeMusic(AudioClip newClip, bool loop, float time)
    {
        AudioSource from = activeMusicSource;
        AudioSource to = standbyMusicSource;
        if (to == null || to == from)
        {
            PlayMusicImmediately(from, newClip, loop);
            musicFadeRoutine = null;
            yield break;
        }

        to.Stop();
        to.clip = newClip;
        to.loop = loop;
        to.volume = 0f;
        to.Play();

        float fromStartVolume = from.volume;
        float targetVolume = musicVolume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(t / time);
            from.volume = Mathf.Lerp(fromStartVolume, 0f, progress);
            to.volume = Mathf.Lerp(0f, targetVolume, progress);
            yield return null;
        }

        from.Stop();
        from.volume = 0f;
        to.volume = targetVolume;
        activeMusicSource = to;
        standbyMusicSource = from;
        musicSource = activeMusicSource;
        transitionMusicSource = standbyMusicSource;
        musicFadeRoutine = null;
    }

    public void StopMusic()
    {
        StopMusic(defaultStopFadeTime);
    }

    public void StopMusic(float fadeTime)
    {
        if (activeMusicSource == null) return;

        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }

        if (fadeTime > 0f && activeMusicSource.isPlaying)
        {
            musicFadeRoutine = StartCoroutine(FadeOutMusic(fadeTime));
        }
        else
        {
            activeMusicSource.Stop();
            currentMusicClip = null;
        }
    }

    private IEnumerator FadeOutMusic(float time)
    {
        AudioSource source = activeMusicSource;
        float startVolume = source.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(t / time));
            yield return null;
        }

        source.Stop();
        source.volume = musicVolume;
        currentMusicClip = null;
        musicFadeRoutine = null;
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
        if (activeMusicSource != null) activeMusicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    public bool IsMusicPlaying() => activeMusicSource != null && activeMusicSource.isPlaying;
}
