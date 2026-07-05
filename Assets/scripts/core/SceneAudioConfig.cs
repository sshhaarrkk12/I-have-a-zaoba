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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ApplySceneMusic(musicClip, loop, musicVolume);
        }
    }
}
