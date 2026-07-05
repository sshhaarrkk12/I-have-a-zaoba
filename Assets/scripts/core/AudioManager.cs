using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private bool playMusicOnStart = false;
    [SerializeField] private string startMusicName = "";

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private float musicVolume = 1f;
    private float sfxVolume = 1f;

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
        LoadClipsFromResources();
    }

    private void Start()
    {
        if (playMusicOnStart && !string.IsNullOrEmpty(startMusicName))
            PlayMusic(startMusicName, true);
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

    private void LoadClipsFromResources()
    {
        var loaded = Resources.LoadAll<AudioClip>("Audio");
        foreach (var clip in loaded)
        {
            if (clip == null) continue;
            if (!clips.ContainsKey(clip.name)) clips.Add(clip.name, clip);
        }
    }

    public bool RegisterClip(AudioClip clip)
    {
        if (clip == null) return false;
        if (clips.ContainsKey(clip.name)) return false;
        clips.Add(clip.name, clip);
        return true;
    }

    public bool PlayMusic(string name, bool loop = true, float fadeTime = 0.0f)
    {
        if (!clips.TryGetValue(name, out var clip)) return false;
        if (musicSource.clip == clip && musicSource.isPlaying) return true;
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

    public bool PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 0.0f)
    {
        if (clip == null) return false;
        if (musicSource.clip == clip && musicSource.isPlaying) return true;
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
        musicSource.Stop();
    }

    public bool PlaySFX(string name, float volume = 1f)
    {
        if (!clips.TryGetValue(name, out var clip)) return false;
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
