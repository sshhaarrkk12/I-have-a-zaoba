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
    [Tooltip("视频结尾前多少秒激活游戏场景，用来避开片尾黑帧和加载黑屏")]
    public float sceneActivationLeadTime = 0.12f;
    [Header("黑屏渐变")]
    public bool directVideoTransition = true;
    public float fadeToVideoDuration = 0.45f;
    public float fadeFromVideoDuration = 0.45f;
    public float fadeToGameDuration = 0.45f;
    public float fadeFromGameDuration = 0.55f;

    private RawImage videoRawImage;
    private Image transitionOverlay;
    private GameObject videoCanvas;
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
        if (!directVideoTransition)
            DontDestroyOnLoad(gameObject);

        PrepareVideoUI();
        HideVideoPanel();
        SetTransitionAlpha(0f);

        if (videoPlayer != null)
        {
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
            bool firstFrameReady = false;
            VideoPlayer.EventHandler onLoopPointReached = _ => reachedEnd = true;
            VideoPlayer.FrameReadyEventHandler onFrameReady = (_, __) => firstFrameReady = true;
            videoPlayer.loopPointReached += onLoopPointReached;
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += onFrameReady;

            videoPlayer.Play();

            float startElapsed = 0f;
            while ((!videoPlayer.isPlaying || (!firstFrameReady && videoPlayer.frame <= 0)) && startElapsed < StartTimeout)
            {
                startElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            videoPlayer.frameReady -= onFrameReady;
            videoPlayer.sendFrameReadyEvents = false;

            if (!videoPlayer.isPlaying)
            {
                Debug.LogWarning("[Menu] Video did not start, skipping intro video.");
                videoPlayer.loopPointReached -= onLoopPointReached;
                if (directVideoTransition)
                    SceneManager.LoadScene(GameStartScene);
                else
                    yield return StartCoroutine(FadeToGameWithoutVideo());
                yield break;
            }

            if (directVideoTransition)
            {
                SetTransitionAlpha(0f);
                ShowVideoPanel();
            }
            else
            {
                videoPlayer.Pause();
                yield return StartCoroutine(FadeTransition(1f, fadeToVideoDuration));
                ShowVideoPanel();
                videoPlayer.Play();
                yield return StartCoroutine(FadeTransition(0f, fadeFromVideoDuration));
            }

            AsyncOperation preloadOperation = BeginPreloadGameScene();

            yield return new WaitForSecondsRealtime(videoDelay);

            float maxPlaybackSeconds = videoPlayer.length > 0
                ? (float)videoPlayer.length + 2f
                : PlaybackFallbackTimeout;
            float playbackElapsed = 0f;

            while (!reachedEnd && playbackElapsed < maxPlaybackSeconds)
            {
                if (ShouldActivatePreloadedScene(preloadOperation))
                    break;

                playbackElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            videoPlayer.loopPointReached -= onLoopPointReached;
            if (!directVideoTransition)
                yield return StartCoroutine(FadeTransition(1f, fadeToGameDuration));

            yield return StartCoroutine(ActivatePreloadedScene(preloadOperation));
            if (directVideoTransition)
                yield break;

            HideVideoPanel();
            yield return null;
            if (!directVideoTransition)
                yield return StartCoroutine(FadeTransition(0f, fadeFromGameDuration));

            CleanupIntroObjects();
            yield break;
        }
        else
        {
            if (directVideoTransition)
                SceneManager.LoadScene(GameStartScene);
            else
                yield return StartCoroutine(FadeToGameWithoutVideo());
            yield break;
        }
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
            videoCanvas = new GameObject("IntroVideoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = videoCanvas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
            if (!directVideoTransition)
                DontDestroyOnLoad(videoCanvas);

            videoPanel = new GameObject("VideoPanel", typeof(RectTransform));
            videoPanel.transform.SetParent(canvas.transform, false);
            videoPanel.SetActive(false);

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

            var overlay = new GameObject("IntroTransitionOverlay", typeof(RectTransform));
            overlay.transform.SetParent(videoCanvas.transform, false);
            transitionOverlay = overlay.AddComponent<Image>();
            transitionOverlay.color = new Color(0f, 0f, 0f, 0f);
            transitionOverlay.raycastTarget = false;
            var overlayRect = transitionOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlay.transform.SetAsLastSibling();
        }

        HideVideoPanel();
    }

    private void ShowVideoPanel()
    {
        if (videoPanel != null)
            videoPanel.SetActive(true);
    }

    private void HideVideoPanel()
    {
        if (videoPanel != null)
            videoPanel.SetActive(false);
    }

    private IEnumerator LoadGameSceneKeepingVideoVisible()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(GameStartScene);
        if (op == null)
        {
            SceneManager.LoadScene(GameStartScene);
            yield break;
        }

        while (!op.isDone)
            yield return null;
    }

    private IEnumerator FadeToGameWithoutVideo()
    {
        yield return StartCoroutine(FadeTransition(1f, fadeToGameDuration));
        yield return StartCoroutine(LoadGameSceneKeepingVideoVisible());
        yield return null;
        yield return StartCoroutine(FadeTransition(0f, fadeFromGameDuration));
        CleanupIntroObjects();
    }

    private IEnumerator FadeTransition(float targetAlpha, float duration)
    {
        if (transitionOverlay == null) yield break;

        transitionOverlay.transform.SetAsLastSibling();
        transitionOverlay.gameObject.SetActive(true);

        float startAlpha = transitionOverlay.color.a;
        if (duration <= 0f)
        {
            SetTransitionAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetTransitionAlpha(Mathf.Lerp(startAlpha, targetAlpha, p));
            yield return null;
        }

        SetTransitionAlpha(targetAlpha);
    }

    private void SetTransitionAlpha(float alpha)
    {
        if (transitionOverlay == null) return;
        transitionOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
    }

    private void CleanupIntroObjects()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoCanvas != null)
            Destroy(videoCanvas);

        Destroy(gameObject);
    }

    private AsyncOperation BeginPreloadGameScene()
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(GameStartScene);
        if (op == null) return null;

        op.allowSceneActivation = false;
        return op;
    }

    private bool ShouldActivatePreloadedScene(AsyncOperation op)
    {
        if (op == null) return true;
        if (op.progress < 0.9f) return false;
        if (videoPlayer == null || videoPlayer.length <= 0) return false;

        double remaining = videoPlayer.length - videoPlayer.time;
        return remaining <= Mathf.Max(0f, sceneActivationLeadTime);
    }

    private IEnumerator ActivatePreloadedScene(AsyncOperation op)
    {
        if (op == null)
        {
            yield return StartCoroutine(LoadGameSceneKeepingVideoVisible());
            yield break;
        }

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;

        while (!op.isDone)
            yield return null;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }
    }
}
