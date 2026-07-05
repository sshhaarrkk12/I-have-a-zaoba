using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
    [Header("七条属性栏")]
    public StatBar mood;
    public StatBar instantStamina;
    public StatBar stress;
    public StatBar fatigue;
    public StatBar health;
    public StatBar academic;
    public StatBar social; // 💡 新增：人际关系属性栏

    [Header("其他UI")]
    public TextMeshProUGUI dayText;
    public Transform floatRoot;

    [Header("特效设置")]
    public GameObject floatTextPrefab; // 漂浮字预制体（可选）

    [Header("刷新设置")]
    public float smoothSpeed = 8f;

    static readonly Color ColorGood = new Color(0.27f, 0.85f, 0.40f);
    static readonly Color ColorMid = new Color(0.98f, 0.75f, 0.14f);
    static readonly Color ColorBad = new Color(0.94f, 0.27f, 0.27f);
    static readonly Color ColorHealth = new Color(0.55f, 0.36f, 0.96f);
    static readonly Color ColorAcademic = new Color(0.20f, 0.60f, 0.98f);
    static readonly Color ColorSocial = new Color(1.00f, 0.60f, 0.40f); // 💡 为人际关系设定一个温暖的橙粉色

    float prevMood, prevStamina, prevStress, prevFatigue, prevHealth, prevAcademic, prevSocial;
    int lastCachedDay = -1;

    void OnEnable() { PlayerStats.OnCriticalThreshold += OnCritical; }
    void OnDisable() { PlayerStats.OnCriticalThreshold -= OnCritical; }

    void Start()
    {
        if (PlayerStats.Instance == null) return;
        var p = PlayerStats.Instance;

        mood.displayValue = p.mood;
        instantStamina.displayValue = p.instantStamina;
        stress.displayValue = p.instantStress;
        fatigue.displayValue = p.fatigue;
        health.displayValue = p.health;
        academic.displayValue = p.academic;
        social.displayValue = p.social; // 💡 初始化人际关系

        prevMood = p.mood;
        prevStamina = p.instantStamina;
        prevStress = p.instantStress;
        prevFatigue = p.fatigue;
        prevHealth = p.health;
        prevAcademic = p.academic;
        prevSocial = p.social; // 💡 记录人际关系初始值

        // 颜色固定配置
        health.fixedColor = true;
        health.fixedBarColor = ColorHealth;
        academic.fixedColor = true;
        academic.fixedBarColor = ColorAcademic;

        // 💡 如果你希望人际关系一直显示橙色，可以开启下面两句；
        // 💡 如果希望它像心情一样高绿低红，就注释掉这两句。
        social.fixedColor = true;
        social.fixedBarColor = ColorSocial;

        stress.isReversed = true;
        fatigue.isReversed = true;

        RefreshDay(true);
    }

    void Update()
    {
        if (PlayerStats.Instance == null) return;
        var p = PlayerStats.Instance;

        // 1. 捕捉属性变化，触发漂浮字
        TryFloat(p.mood, prevMood, mood);
        TryFloat(p.instantStamina, prevStamina, instantStamina);
        TryFloat(p.instantStress, prevStress, stress);
        TryFloat(p.fatigue, prevFatigue, fatigue);
        TryFloat(p.health, prevHealth, health);
        TryFloat(p.academic, prevAcademic, academic);
        TryFloat(p.social, prevSocial, social); // 💡 人际关系漂浮字

        prevMood = p.mood;
        prevStamina = p.instantStamina;
        prevStress = p.instantStress;
        prevFatigue = p.fatigue;
        prevHealth = p.health;
        prevAcademic = p.academic;
        prevSocial = p.social; // 💡 缓存本帧人际关系

        // 2. 属性条平滑插值
        float dt = Time.deltaTime * smoothSpeed;
        mood.displayValue = Mathf.Lerp(mood.displayValue, p.mood, dt);
        instantStamina.displayValue = Mathf.Lerp(instantStamina.displayValue, p.instantStamina, dt);
        stress.displayValue = Mathf.Lerp(stress.displayValue, p.instantStress, dt);
        fatigue.displayValue = Mathf.Lerp(fatigue.displayValue, p.fatigue, dt);
        health.displayValue = Mathf.Lerp(health.displayValue, p.health, dt);
        academic.displayValue = Mathf.Lerp(academic.displayValue, p.academic, dt);
        social.displayValue = Mathf.Lerp(social.displayValue, p.social, dt); // 💡 人际关系平滑过渡

        // 3. 渲染 UI
        DrawBar(mood);
        DrawBar(instantStamina);
        DrawBar(stress);
        DrawBar(fatigue);
        DrawBar(health);
        DrawBar(academic);
        DrawBar(social); // 💡 渲染人际关系条

        // 4. 优化：按需刷新天数
        if (p.currentDay != lastCachedDay)
        {
            RefreshDay(false);
        }
    }

    void DrawBar(StatBar bar)
    {
        float v = bar.displayValue;
        if (bar.valueText != null) bar.valueText.text = Mathf.RoundToInt(v).ToString();
        if (bar.barFill != null)
        {
            bar.barFill.fillAmount = v / 100f;
            bar.barFill.color = bar.fixedColor ? bar.fixedBarColor : GetColor(v, bar.isReversed);
        }
    }

    Color GetColor(float v, bool reversed)
    {
        if (reversed) return v >= 70f ? ColorBad : v >= 40f ? ColorMid : ColorGood;
        else return v >= 70f ? ColorGood : v >= 40f ? ColorMid : ColorBad;
    }

    void TryFloat(float cur, float prev, StatBar bar)
    {
        float delta = cur - prev;
        if (Mathf.Abs(delta) < 0.5f || floatRoot == null || bar.valueText == null) return;

        GameObject go;
        TextMeshProUGUI tmp;

        if (floatTextPrefab != null)
        {
            go = Instantiate(floatTextPrefab, floatRoot, false);
            tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        }
        else
        {
            go = new GameObject("FloatDelta");
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
        rt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(55f, 0f);

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