using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// 简单的打包场景交互控制器：点击按钮后黑屏+字幕并修改 PlayerStats
[DisallowMultipleComponent]
public class PackingController : MonoBehaviour
{
    [Header("打包选项按钮")]
    public Button button1; // 带很多东西选项
    public Button button2; // 什么都没带选项

    [Header("场景跳转按钮")]
    public Button dormButton;
    public Button toiletButton;
    public Button goOutButton;
    public Button[] postActionButtons;

    [Header("遮罩设置")]
    public GameObject maskObject; // 可选：自定义遮罩（Image + CanvasGroup）
    public TextMeshProUGUI maskText; // 可选：显示字幕的文本（如果不填写会自动创建）

    [Header("动画时长（秒）")]
    // 默认：0.5s 渐显，2.2s 保持，0.5s 渐隐
    public float fadeTime = 0.5f; // 渐显/渐隐时间
    public float holdTime = 2.2f;   // 持续时间

    CanvasGroup maskGroup;
    GameObject createdMask;
    TextMeshProUGUI createdText;

    void Start()
    {
        if (button1 != null) button1.onClick.AddListener(OnButton1Clicked);
        if (button2 != null) button2.onClick.AddListener(OnButton2Clicked);

        if (dormButton != null) dormButton.onClick.AddListener(OnDormButtonClicked);
        if (toiletButton != null) toiletButton.onClick.AddListener(OnToiletButtonClicked);
        if (goOutButton != null) goOutButton.onClick.AddListener(OnGoOutButtonClicked);

        EnsureMask();
        SetMaskActive(false);
        HidePostActionButtons();
    }

    void HidePostActionButtons()
    {
        if (postActionButtons != null)
        {
            foreach (var b in postActionButtons)
            {
                if (b != null) b.gameObject.SetActive(false);
            }
        }

        if (dormButton != null) dormButton.gameObject.SetActive(false);
        if (toiletButton != null) toiletButton.gameObject.SetActive(false);
        if (goOutButton != null) goOutButton.gameObject.SetActive(false);
    }

    void ShowPostActionButtons()
    {
        if (postActionButtons != null)
        {
            foreach (var b in postActionButtons)
            {
                if (b != null) b.gameObject.SetActive(true);
            }
        }

        if (dormButton != null) dormButton.gameObject.SetActive(true);
        if (toiletButton != null) toiletButton.gameObject.SetActive(true);
        if (goOutButton != null) goOutButton.gameObject.SetActive(true);
    }

    void EnsureMask()
    {
        if (maskObject != null)
        {
            maskGroup = maskObject.GetComponent<CanvasGroup>();
            if (maskGroup == null) maskGroup = maskObject.AddComponent<CanvasGroup>();
            if (maskText == null)
            {
                var t = maskObject.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null) maskText = t;
            }
            return;
        }

        // 自动创建全屏遮罩
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            var cg = new GameObject("AutoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = cg.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        createdMask = new GameObject("PackingMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        createdMask.transform.SetParent(canvas.transform, false);
        var img = createdMask.GetComponent<Image>();
        img.color = Color.black;

        var rt = createdMask.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        maskGroup = createdMask.AddComponent<CanvasGroup>();

        var textGO = new GameObject("MaskText", typeof(RectTransform));
        textGO.transform.SetParent(createdMask.transform, false);
        createdText = textGO.AddComponent<TextMeshProUGUI>();
        createdText.alignment = TextAlignmentOptions.Center;
        createdText.color = Color.white;
        createdText.fontSize = 28;
        createdText.enableWordWrapping = true;

        var tr = createdText.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0.1f, 0.4f);
        tr.anchorMax = new Vector2(0.9f, 0.6f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        // expose to inspector fields so other code can reference if needed
        maskObject = createdMask;
        maskText = createdText;
    }

    void SetMaskActive(bool active)
    {
        if (maskObject != null) maskObject.SetActive(active);
        if (maskGroup != null) maskGroup.alpha = active ? 1f : 0f;
    }

    void OnButton1Clicked()
    {
        SetPackChoiceButtonsActive(false);

        var p = PlayerStats.Instance;
        if (p != null)
        {
            p.ConsumeInstantStamina(5f); // 体力 -5
            p.AddFatigue(5f); // 疲惫 +5
        }

        StartCoroutine(PlayMaskSequence("带的东西太多了好累啊...", true));
    }

    void OnButton2Clicked()
    {
        SetPackChoiceButtonsActive(false);

        var p = PlayerStats.Instance;
        if (p != null)
        {
            p.AddStress(5f); // 压力 +5
            p.RecoverInstantStamina(5f); // 体力 +5
        }

        StartCoroutine(PlayMaskSequence("什么都没带，身体好轻松，但是心理压力好大啊...", true));
    }

    void SetPackChoiceButtonsActive(bool state)
    {
        if (button1 != null) button1.gameObject.SetActive(state);
        if (button2 != null) button2.gameObject.SetActive(state);
    }

    private IEnumerator PlayMaskSequence(string message, bool showPostButtonsWhenDone)
    {
        if (maskText != null) maskText.text = message;
        if (maskGroup == null) yield break;

        SetMaskActive(true);
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            maskGroup.alpha = Mathf.Lerp(0f, 1f, t / Mathf.Max(0.0001f, fadeTime));
            yield return null;
        }
        maskGroup.alpha = 1f;

        if (holdTime > 0f) yield return new WaitForSecondsRealtime(holdTime);

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            maskGroup.alpha = Mathf.Lerp(1f, 0f, t / Mathf.Max(0.0001f, fadeTime));
            yield return null;
        }
        maskGroup.alpha = 0f;
        SetMaskActive(false);

        if (showPostButtonsWhenDone)
            ShowPostActionButtons();
    }

    void OnDormButtonClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToDormHub();
        else
            SceneManager.LoadScene("DormHub");
    }

    void OnToiletButtonClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToBathroom();
        else
            SceneManager.LoadScene("Bathroom");
    }

    void OnGoOutButtonClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToGoOut();
        else
            SceneManager.LoadScene("GoOut");
    }
}
