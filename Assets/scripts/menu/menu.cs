using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Menu : MonoBehaviour
{
    private const string GameStartScene = "Wakeup";
    private const float PrepareTimeout = 5f;
    private const float StartTimeout = 3f;
    private const float PlaybackFallbackTimeout = 30f;

    [Header("视频过渡")]
    public VideoPlayer videoPlayer;
    public float videoDelay = 0.2f;

    private RawImage videoRawImage;
    private GameObject videoPanel;
    private RenderTexture videoRenderTexture;
    private bool isStarting;

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }
    }

    public void Gamestart()
    {
        if (isStarting) return;
        StartCoroutine(PlayIntroAndLoadScene());
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private IEnumerator PlayIntroAndLoadScene()
    {
        isStarting = true;
        PrepareVideoUI();

        if (videoPlayer != null)
        {
            if (videoPanel != null)
                videoPanel.SetActive(true);

            videoPlayer.Stop();
            videoPlayer.time = 0f;
            videoPlayer.Prepare();

            float prepareElapsed = 0f;
            while (!videoPlayer.isPrepared && prepareElapsed < PrepareTimeout)
            {
                prepareElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!videoPlayer.isPrepared)
                Debug.LogWarning("[Menu] Video prepare timed out, trying to play anyway.");

            bool reachedEnd = false;
            VideoPlayer.EventHandler onLoopPointReached = _ => reachedEnd = true;
            videoPlayer.loopPointReached += onLoopPointReached;

            videoPlayer.Play();

            float startElapsed = 0f;
            while (!videoPlayer.isPlaying && videoPlayer.frame <= 0 && startElapsed < StartTimeout)
            {
                startElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!videoPlayer.isPlaying && videoPlayer.frame <= 0)
            {
                Debug.LogWarning("[Menu] Video did not start, skipping intro video.");
                videoPlayer.loopPointReached -= onLoopPointReached;
                SceneManager.LoadScene(GameStartScene);
                yield break;
            }

            yield return new WaitForSecondsRealtime(videoDelay);

            float maxPlaybackSeconds = videoPlayer.length > 0
                ? (float)videoPlayer.length + 2f
                : PlaybackFallbackTimeout;
            float playbackElapsed = 0f;

            while (!reachedEnd && playbackElapsed < maxPlaybackSeconds)
            {
                playbackElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            videoPlayer.loopPointReached -= onLoopPointReached;
            videoPlayer.Stop();
        }
        else
        {
            yield return new WaitForSecondsRealtime(videoDelay);
        }

        SceneManager.LoadScene(GameStartScene);
    }

    private void PrepareVideoUI()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        if (videoPanel == null)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("VideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            videoPanel = new GameObject("VideoPanel", typeof(RectTransform));
            videoPanel.transform.SetParent(canvas.transform, false);

            var panelRect = videoPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = videoPanel.AddComponent<Image>();
            panelImage.color = Color.black;

            var raw = new GameObject("VideoRawImage", typeof(RectTransform));
            raw.transform.SetParent(videoPanel.transform, false);
            videoRawImage = raw.AddComponent<RawImage>();
            var rawRect = videoRawImage.GetComponent<RectTransform>();
            rawRect.anchorMin = Vector2.zero;
            rawRect.anchorMax = Vector2.one;
            rawRect.offsetMin = Vector2.zero;
            rawRect.offsetMax = Vector2.zero;

            videoRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            videoRenderTexture.Create();
            videoRawImage.texture = videoRenderTexture;

            videoPlayer.targetCamera = null;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }

        if (videoRawImage != null && videoPlayer != null)
        {
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
                videoRenderTexture.Create();
            }

            videoRawImage.texture = videoRenderTexture;
            videoPlayer.targetCamera = null;
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = false;
            videoPlayer.skipOnDrop = true;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }
    }

    private void OnDestroy()
    {
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }
    }
}
