using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // 不需要在Inspector里赋值，自动创建
    Image fadeOverlay;
    public float FadeAlpha => fadeOverlay != null ? fadeOverlay.color.a : 0f;
    public bool IsFadeVisible => FadeAlpha > 0.01f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeOverlay();
    }

    void CreateFadeOverlay()
    {
        // 自动创建持久的黑色遮罩Canvas
        var go = new GameObject("FadeCanvas");
        DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();

        var imgGo = new GameObject("FadeOverlay");
        imgGo.transform.SetParent(go.transform, false);
        fadeOverlay = imgGo.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.raycastTarget = false;

        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void ShowFade(bool fadeToBlack, float duration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(fadeToBlack ? 1f : 0f, duration));
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        if (fadeOverlay == null) yield break;
        float start = fadeOverlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(start, target, elapsed / duration);
            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, target);
    }
}
