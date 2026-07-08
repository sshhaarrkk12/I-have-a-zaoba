using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class BlackScreenText
{
    const string PreferredFontName = "\u5999\u59997000\u5B57";
    static TMP_FontAsset cachedPreferredFont;

    public static IEnumerator Play(MonoBehaviour owner, string text, float fadeTime = 0.5f, float holdTime = 2f)
    {
        if (owner == null) yield break;

        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("AutoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject overlay = new GameObject("BlackScreenText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvas.transform, false);
        overlay.transform.SetAsLastSibling();

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;

        CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = true;

        TextMeshProUGUI textComponent = CreateText(overlay.transform);
        textComponent.text = text;

        yield return Fade(overlayGroup, 0f, 1f, fadeTime);

        float timer = 0f;
        while (timer < holdTime)
        {
            timer += Time.unscaledDeltaTime;
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) break;
            yield return null;
        }

        yield return Fade(overlayGroup, 1f, 0f, fadeTime);

        Object.Destroy(overlay);
    }

    static TextMeshProUGUI CreateText(Transform parent)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
        textComponent.fontSize = 40f;
        textComponent.enableWordWrapping = true;
        ApplyPreferredFont(textComponent);

        RectTransform textRect = textComponent.rectTransform;
        textRect.anchorMin = new Vector2(0.12f, 0.32f);
        textRect.anchorMax = new Vector2(0.88f, 0.68f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return textComponent;
    }

    public static void ApplyPreferredFont(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        TMP_FontAsset font = GetPreferredFont();
        if (font == null) return;

        textComponent.font = font;
        if (font.material != null)
            textComponent.fontSharedMaterial = font.material;
    }

    static TMP_FontAsset GetPreferredFont()
    {
        if (cachedPreferredFont != null) return cachedPreferredFont;

        TextMeshProUGUI[] sceneTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI sceneText in sceneTexts)
        {
            if (sceneText != null && sceneText.font != null && sceneText.font.name == PreferredFontName)
            {
                cachedPreferredFont = sceneText.font;
                return cachedPreferredFont;
            }
        }

        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (TMP_FontAsset font in loadedFonts)
        {
            if (font != null && font.name == PreferredFontName)
            {
                cachedPreferredFont = font;
                return cachedPreferredFont;
            }
        }

        foreach (TextMeshProUGUI sceneText in sceneTexts)
        {
            if (sceneText != null && sceneText.font != null)
                return sceneText.font;
        }

        return null;
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        float safeDuration = Mathf.Max(0.0001f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / safeDuration));
            yield return null;
        }

        group.alpha = to;
    }
}
