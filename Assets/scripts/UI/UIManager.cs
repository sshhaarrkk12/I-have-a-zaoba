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
        if (duration <= 0f)
        {
            SetFadeAlpha(fadeToBlack ? 1f : 0f);
            return;
        }

        StartCoroutine(FadeRoutine(fadeToBlack ? 1f : 0f, duration));
    }

    public void HideFadeImmediate()
    {
        StopAllCoroutines();
        SetFadeAlpha(0f);
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        if (fadeOverlay == null) yield break;
        float start = fadeOverlay.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float a = Mathf.Lerp(start, target, p);
            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }
        SetFadeAlpha(target);
    }

    void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null) return;
        fadeOverlay.color = new Color(0, 0, 0, alpha);
    }
}
