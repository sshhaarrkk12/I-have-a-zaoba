using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class Menu : MonoBehaviour
{
    private const string GameStartScene = "Wakeup";

    [Header("视频过渡")]
    public VideoPlayer videoPlayer;
    public float videoDelay = 0.2f;

    private RawImage videoRawImage;
    private GameObject videoPanel;
    private RenderTexture videoRenderTexture;

    public void Gamestart()
    {
        StartCoroutine(PlayIntroAndLoadScene());
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private IEnumerator PlayIntroAndLoadScene()
    {
        PrepareVideoUI();

        if (videoPlayer != null)
        {
            if (videoPanel != null)
                videoPanel.SetActive(true);

            if (videoRenderTexture != null)
            {
                videoPlayer.targetTexture = videoRenderTexture;
            }

            videoPlayer.Stop();
            videoPlayer.time = 0f;
            videoPlayer.Play();
            yield return new WaitForSeconds(videoDelay);

            while (videoPlayer.isPlaying)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(videoDelay);
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
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        }
    }
}
