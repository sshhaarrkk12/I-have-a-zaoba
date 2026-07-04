using UnityEngine;

[DisallowMultipleComponent]
public class SceneAudioConfig : MonoBehaviour
{
    [Header("Scene Audio")]
    public AudioClip musicClip;
    public bool loop = true;
    [Range(0f,1f)] public float musicVolume = 1f;
    public bool playOnStart = true;

    private void Start()
    {
        if (!playOnStart) return;
        if (musicClip == null) return;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.PlayMusic(musicClip, loop);
        }
    }
}
