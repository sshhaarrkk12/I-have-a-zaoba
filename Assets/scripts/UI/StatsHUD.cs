using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class StatBar
{
    public string label;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI valueText;
    public Image barFill;
    public bool isReversed;
    public bool fixedColor;
    public Color fixedBarColor = Color.white;
    [HideInInspector] public float displayValue;
}

public class StatsHUD : MonoBehaviour
{
    [Header("属性栏")]
    public StatBar mood;
    public StatBar instantStamina;
    public StatBar stress;
    public StatBar fatigue;
    public StatBar health;
    public StatBar academic;
    public StatBar social;

    [Header("自动读取")]
    [Tooltip("为空时直接读取 StatsHUD 自己下面的直系子物体")]
    public Transform barsRoot;
    [Tooltip("子物体不足时优先复制该模板；为空则复制第一个已有属性条")]
    public GameObject barTemplate;
    public CanvasGroup visibilityGroup;
    public bool hideDuringDialogue = true;
    public bool hideDuringBlackScreen = true;
    public bool keepOnTop = true;
    public bool useOwnCanvasOrder = true;
    public int sortingOrder = 40;
    public bool lockToBottomLeft = true;
    public Vector2 bottomLeftOffset = Vector2.zero;

    [Header("其他 UI")]
    public TextMeshProUGUI dayText;
    public Transform floatRoot;

    [Header("特效设置")]
    public GameObject floatTextPrefab;

    [Header("刷新设置")]
    public float smoothSpeed = 8f;

    static readonly Color ColorGood = new Color(0.27f, 0.85f, 0.40f);
    static readonly Color ColorMid = new Color(0.98f, 0.75f, 0.14f);
    static readonly Color ColorBad = new Color(0.94f, 0.27f, 0.27f);
    static readonly Color ColorHealth = new Color(0.55f, 0.36f, 0.96f);
    static readonly Color ColorAcademic = new Color(0.20f, 0.60f, 0.98f);
    static readonly Color ColorSocial = new Color(1.00f, 0.60f, 0.40f);

    float prevMood, prevStamina, prevStress, prevFatigue, prevHealth, prevAcademic, prevSocial;
    int lastCachedDay = -1;
    RectTransform screenCanvasRect;

    void Awake()
    {
        BuildBarsFromChildren();
        FindAuxiliaryUI();
        EnsureVisibilityGroup();
        EnsureScreenSpaceCanvas();
        EnsureTopLayer();
        RefreshVisibility();
    }

    void OnEnable() { PlayerStats.OnCriticalThreshold += OnCritical; }
    void OnDisable() { PlayerStats.OnCriticalThreshold -= OnCritical; }

    void Start()
    {
        BuildBarsFromChildren();
        FindAuxiliaryUI();
        EnsureVisibilityGroup();
        EnsureScreenSpaceCanvas();
        EnsureTopLayer();
        RefreshVisibility();

        if (PlayerStats.Instance == null) return;
        var p = PlayerStats.Instance;

        mood.displayValue = p.mood;
        instantStamina.displayValue = p.instantStamina;
        stress.displayValue = p.instantStress;
        fatigue.displayValue = p.fatigue;
        health.displayValue = p.health;
        academic.displayValue = p.academic;
        social.displayValue = p.social;

        prevMood = p.mood;
        prevStamina = p.instantStamina;
        prevStress = p.instantStress;
        prevFatigue = p.fatigue;
        prevHealth = p.health;
        prevAcademic = p.academic;
        prevSocial = p.social;

        RefreshDay(true);
        DrawAllBars();
    }

    void Update()
    {
        EnsureScreenSpaceCanvas();
        EnsureTopLayer();
        RefreshVisibility();

        if (PlayerStats.Instance == null) return;
        var p = PlayerStats.Instance;

        TryFloat(p.mood, prevMood, mood);
        TryFloat(p.instantStamina, prevStamina, instantStamina);
        TryFloat(p.instantStress, prevStress, stress);
        TryFloat(p.fatigue, prevFatigue, fatigue);
        TryFloat(p.health, prevHealth, health);
        TryFloat(p.academic, prevAcademic, academic);
        TryFloat(p.social, prevSocial, social);

        prevMood = p.mood;
        prevStamina = p.instantStamina;
        prevStress = p.instantStress;
        prevFatigue = p.fatigue;
        prevHealth = p.health;
        prevAcademic = p.academic;
        prevSocial = p.social;

        float dt = Time.deltaTime * smoothSpeed;
        mood.displayValue = Mathf.Lerp(mood.displayValue, p.mood, dt);
        instantStamina.displayValue = Mathf.Lerp(instantStamina.displayValue, p.instantStamina, dt);
        stress.displayValue = Mathf.Lerp(stress.displayValue, p.instantStress, dt);
        fatigue.displayValue = Mathf.Lerp(fatigue.displayValue, p.fatigue, dt);
        health.displayValue = Mathf.Lerp(health.displayValue, p.health, dt);
        academic.displayValue = Mathf.Lerp(academic.displayValue, p.academic, dt);
        social.displayValue = Mathf.Lerp(social.displayValue, p.social, dt);

        DrawAllBars();

        if (p.currentDay != lastCachedDay)
            RefreshDay(false);
    }

    void BuildBarsFromChildren()
    {
        Transform root = barsRoot != null ? barsRoot : transform;
        var directChildren = GetDirectBarChildren(root);
        EnsureChildCount(root, directChildren, 7);

        mood = BindBar(mood, directChildren[0], "MoodBar", "心情", false, false, Color.white);
        instantStamina = BindBar(instantStamina, directChildren[1], "StaminaBar", "体力", false, false, Color.white);
        stress = BindBar(stress, directChildren[2], "StressBar", "压力", true, false, Color.white);
        fatigue = BindBar(fatigue, directChildren[3], "FatigueBar", "疲惫", true, false, Color.white);
        health = BindBar(health, directChildren[4], "HealthBar", "健康", false, true, ColorHealth);
        academic = BindBar(academic, directChildren[5], "AcademicBar", "学业", false, true, ColorAcademic);
        social = BindBar(social, directChildren[6], "SocialBar", "人际", false, true, ColorSocial);
    }

    void EnsureVisibilityGroup()
    {
        if (visibilityGroup != null && visibilityGroup.gameObject == gameObject) return;

        visibilityGroup = GetComponent<CanvasGroup>();
        if (visibilityGroup == null)
            visibilityGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void EnsureTopLayer()
    {
        if (!keepOnTop) return;

        transform.SetAsLastSibling();

        if (!useOwnCanvasOrder) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            EnsureScreenSpaceCanvas();
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }
    }

    void EnsureScreenSpaceCanvas()
    {
        RectTransform hudRect = GetComponent<RectTransform>();
        if (hudRect == null) return;

        Transform parent = transform.parent;
        if (parent == null || parent.name != "StatsHUDScreenCanvas")
        {
            GameObject canvasGo = new GameObject("StatsHUDScreenCanvas", typeof(RectTransform));
            screenCanvasRect = canvasGo.GetComponent<RectTransform>();
            screenCanvasRect.SetParent(null, false);
            transform.SetParent(screenCanvasRect, false);
            DontDestroyOnLoad(canvasGo);
        }
        else
        {
            screenCanvasRect = parent as RectTransform;
        }

        GameObject canvasObject = screenCanvasRect.gameObject;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
            canvas = canvasObject.AddComponent<Canvas>();

        canvas.enabled = true;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvasObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            canvasObject.AddComponent<GraphicRaycaster>();

        RemoveLocalCanvasComponents();

        if (lockToBottomLeft)
            AnchorHudToBottomLeft(hudRect);

        SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));
    }

    void RemoveLocalCanvasComponents()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) Destroy(canvas);

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null) Destroy(scaler);

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null) Destroy(raycaster);
    }

    void AnchorHudToBottomLeft(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = bottomLeftOffset;
    }

    void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null || layer < 0) return;

        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    void RefreshVisibility()
    {
        if (visibilityGroup == null)
            EnsureVisibilityGroup();

        bool shouldHideForDialogue = hideDuringDialogue && IsDialogueOverlayActive();

        bool shouldHideForBlackScreen = hideDuringBlackScreen
            && IsBlackScreenActive();

        bool shouldHide = shouldHideForDialogue || shouldHideForBlackScreen;

        visibilityGroup.alpha = shouldHide ? 0f : 1f;
        visibilityGroup.interactable = !shouldHide;
        visibilityGroup.blocksRaycasts = !shouldHide;
    }

    bool IsDialogueOverlayActive()
    {
        DialogueManager dialogue = DialogueManager.Instance;
        if (dialogue == null) return false;
        if (dialogue.IsOverlayActive) return true;

        return IsActiveInHierarchy(dialogue.dialogueRoot)
            || IsActiveInHierarchy(dialogue.choicesRoot)
            || IsActiveInHierarchy(dialogue.statPopup);
    }

    bool IsActiveInHierarchy(GameObject go)
    {
        return go != null && go.activeInHierarchy;
    }

    bool IsBlackScreenActive()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsFadeVisible)
            return true;

        Image[] images = FindObjectsOfType<Image>(true);
        foreach (var image in images)
        {
            if (image == null || image.transform.IsChildOf(transform) || !image.gameObject.activeInHierarchy)
                continue;

            string n = image.name.ToLowerInvariant();
            bool likelyMask = n.Contains("mask") || n.Contains("black") || n.Contains("fade") || n.Contains("overlay");
            bool darkEnough = image.color.a > 0.05f && image.color.r < 0.08f && image.color.g < 0.08f && image.color.b < 0.08f;
            if (likelyMask && darkEnough)
                return true;
        }

        return false;
    }

    List<Transform> GetDirectBarChildren(Transform root)
    {
        var children = new List<Transform>();
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null || child == floatRoot) continue;
            if (!IsLikelyBarRoot(child)) continue;
            children.Add(child);
        }
        return children;
    }

    bool IsLikelyBarRoot(Transform child)
    {
        string n = child.name.ToLowerInvariant();
        if (n.Contains("day") || n.Contains("date") || n.Contains("float"))
            return false;

        if (child.GetComponentsInChildren<Image>(true).Length > 0)
            return true;

        return child.GetComponentsInChildren<TextMeshProUGUI>(true).Length >= 2;
    }

    void EnsureChildCount(Transform root, List<Transform> children, int requiredCount)
    {
        GameObject template = barTemplate != null ? barTemplate : (children.Count > 0 ? children[0].gameObject : null);

        while (children.Count < requiredCount)
        {
            GameObject go = template != null
                ? Instantiate(template, root, false)
                : CreateBarObject(root);

            go.SetActive(true);
            children.Add(go.transform);
        }
    }

    GameObject CreateBarObject(Transform parent)
    {
        var root = new GameObject("StatBar", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        CreateText(root.transform, "Name");
        CreateText(root.transform, "Value");

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(root.transform, false);
        var image = fill.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;

        return root;
    }

    TextMeshProUGUI CreateText(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    StatBar BindBar(StatBar bar, Transform root, string objectName, string label, bool reversed, bool fixedColor, Color fixedBarColor)
    {
        if (bar == null) bar = new StatBar();

        root.name = objectName;
        bar.label = label;
        bar.isReversed = reversed;
        bar.fixedColor = fixedColor;
        bar.fixedBarColor = fixedBarColor;
        bar.nameText = FindNameText(root);
        bar.valueText = FindValueText(root, bar.nameText);
        bar.barFill = FindFillImage(root);

        if (bar.nameText == null) bar.nameText = CreateText(root, "Name");
        if (bar.valueText == null) bar.valueText = CreateText(root, "Value");
        if (bar.barFill == null)
        {
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(root, false);
            bar.barFill = fill.GetComponent<Image>();
            bar.barFill.type = Image.Type.Filled;
            bar.barFill.fillMethod = Image.FillMethod.Horizontal;
        }

        bar.nameText.text = label;
        bar.valueText.text = "0";
        return bar;
    }

    TextMeshProUGUI FindNameText(Transform root)
    {
        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            string n = text.name.ToLowerInvariant();
            if (n.Contains("name") || n.Contains("label") || n.Contains("title"))
                return text;
        }
        return texts.Length > 0 ? texts[0] : null;
    }

    TextMeshProUGUI FindValueText(Transform root, TextMeshProUGUI nameText)
    {
        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (text == nameText) continue;
            string n = text.name.ToLowerInvariant();
            if (n.Contains("value") || n.Contains("num") || n.Contains("amount"))
                return text;
        }

        foreach (var text in texts)
        {
            if (text != nameText)
                return text;
        }

        return null;
    }

    Image FindFillImage(Transform root)
    {
        var images = root.GetComponentsInChildren<Image>(true);
        Image fallback = null;
        foreach (var image in images)
        {
            if (image.transform == root) continue;

            string n = image.name.ToLowerInvariant();
            if (image.type == Image.Type.Filled || n.Contains("fill") || n.Contains("bar"))
                return image;
            fallback = image;
        }
        return fallback;
    }

    void FindAuxiliaryUI()
    {
        if (floatRoot == null)
        {
            Transform found = FindChildByName(transform, "float");
            floatRoot = found != null ? found : transform;
        }

        if (dayText != null) return;

        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            string n = text.name.ToLowerInvariant();
            if (n.Contains("day") || n.Contains("date"))
            {
                dayText = text;
                return;
            }
        }
    }

    Transform FindChildByName(Transform root, string keyword)
    {
        string lowerKeyword = keyword.ToLowerInvariant();
        foreach (Transform child in root)
        {
            if (child.name.ToLowerInvariant().Contains(lowerKeyword))
                return child;

            Transform nested = FindChildByName(child, lowerKeyword);
            if (nested != null)
                return nested;
        }
        return null;
    }

    void DrawAllBars()
    {
        DrawBar(mood);
        DrawBar(instantStamina);
        DrawBar(stress);
        DrawBar(fatigue);
        DrawBar(health);
        DrawBar(academic);
        DrawBar(social);
    }

    void DrawBar(StatBar bar)
    {
        if (bar == null) return;

        float v = bar.displayValue;
        if (bar.valueText != null) bar.valueText.text = Mathf.RoundToInt(v).ToString();
        if (bar.barFill != null)
        {
            bar.barFill.fillAmount = Mathf.Clamp01(v / 100f);
            bar.barFill.color = bar.fixedColor ? bar.fixedBarColor : GetColor(v, bar.isReversed);
        }
    }

    Color GetColor(float v, bool reversed)
    {
        if (reversed) return v >= 70f ? ColorBad : v >= 40f ? ColorMid : ColorGood;
        return v >= 70f ? ColorGood : v >= 40f ? ColorMid : ColorBad;
    }

    void TryFloat(float cur, float prev, StatBar bar)
    {
        float delta = cur - prev;
        if (bar == null || Mathf.Abs(delta) < 0.5f || floatRoot == null || bar.valueText == null) return;

        GameObject go;
        TextMeshProUGUI tmp;

        if (floatTextPrefab != null)
        {
            go = Instantiate(floatTextPrefab, floatRoot, false);
            tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        }
        else
        {
            go = new GameObject("FloatDelta", typeof(RectTransform));
            go.transform.SetParent(floatRoot, false);
            tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 18f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            var rtEx = go.GetComponent<RectTransform>();
            rtEx.sizeDelta = new Vector2(80, 30);
        }

        bool isBad = bar.isReversed ? delta > 0 : delta < 0;
        tmp.text = (delta > 0 ? "+" : "") + Mathf.RoundToInt(delta);
        tmp.color = isBad ? ColorBad : ColorGood;

        var rt = go.GetComponent<RectTransform>();
        var sourceRt = bar.valueText.GetComponent<RectTransform>();
        rt.position = sourceRt.position;
        rt.anchoredPosition += new Vector2(55f, 0f);

        StartCoroutine(FloatFade(go, tmp));
    }

    IEnumerator FloatFade(GameObject go, TextMeshProUGUI tmp)
    {
        Vector3 startLocalPos = go.transform.localPosition;
        float t = 0f, dur = 1.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            go.transform.localPosition = startLocalPos + new Vector3(0, p * 55f, 0);
            float a = p < 0.5f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.5f) * 2f);
            tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, a);
            yield return null;
        }
        Destroy(go);
    }

    void OnCritical(StatsEventType type, float value)
    {
        Debug.Log($"[HUD] 临界: {type} = {value:F0}");
    }

    void RefreshDay(bool force)
    {
        if (PlayerStats.Instance == null || dayText == null) return;

        lastCachedDay = PlayerStats.Instance.currentDay;
        dayText.text = $"Day {lastCachedDay} / {PlayerStats.MAX_DAYS}";
    }
}
