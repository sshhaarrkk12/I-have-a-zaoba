using UnityEngine;
using UnityEngine.UI;
using TMPro; // 包含 TextMeshPro
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class PhoneUIManager : MonoBehaviour
{
    [Header("UI 字体设置 (必填)")]
    [Tooltip("请把做好的中文字体 _SDF 文件拖到这里")]
    public TMP_FontAsset chineseFont;

    [Header("闹钟UI")]
    public Toggle alarmToggle;
    public Slider alarmTimeSlider;
    public TextMeshProUGUI alarmTimeText;

    [Header("日程列表")]
    public Transform activityListRoot;

    [Header("统计与信息")]
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI statusText;

    [Header("确认按钮")]
    public Button confirmButton;

    private List<ActivityData> selectedActivities = new List<ActivityData>();
    private Dictionary<ActivityData, Toggle> activityToggles = new Dictionary<ActivityData, Toggle>();

    void Start()
    {
        Debug.Log("[PhoneUI] Start()");
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PhoneUI] GameManager为null");
            return;
        }

        InitUIState();
        BuildActivityList();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }

        if (statusText != null) statusText.text = "安排好明天的日程，然后去睡觉吧";
    }

    private void InitUIState()
    {
        if (dayText != null && PlayerStats.Instance != null)
            dayText.text = $"Day {PlayerStats.Instance.currentDay} / 30";

        if (alarmTimeSlider != null)
        {
            alarmTimeSlider.minValue = 6f;
            alarmTimeSlider.maxValue = 9f;
            alarmTimeSlider.value = PlayerStats.Instance?.alarmTime ?? 7.5f;
            alarmTimeSlider.onValueChanged.AddListener(UpdateAlarmText);
            UpdateAlarmText(alarmTimeSlider.value);
        }

        if (alarmToggle != null)
            alarmToggle.isOn = PlayerStats.Instance?.alarmSet ?? true;
    }

    void BuildActivityList()
    {
        // 1. 安全检查：如果核心依赖不存在，直接跳过，防止报错
        if (activityListRoot == null) return;

        var schedule = ScheduleSystem.Instance;
        if (schedule == null || schedule.availableActivities == null)
        {
            Debug.LogWarning("[PhoneUI] ScheduleSystem 或活动列表未初始化，跳过列表构建。");
            return;
        }

        // 2. 清理旧对象
        foreach (Transform child in activityListRoot)
            Destroy(child.gameObject);

        activityToggles.Clear();
        selectedActivities.Clear();

        int day = PlayerStats.Instance?.currentDay ?? 1;

        // 3. 遍历列表，增加对单个 activity 的 null 检查
        foreach (var activity in schedule.availableActivities)
        {
            if (activity == null) continue; // 防御空数据
            if (day < activity.unlockDay) continue;

            var item = CreateActivityItem(activity);
            if (item == null) continue;

            var toggle = item.GetComponentInChildren<Toggle>();
            if (toggle == null) continue;

            toggle.isOn = activity.isFixed;
            toggle.interactable = !activity.isFixed;

            // 安全地添加到列表
            if (activity.isFixed) selectedActivities.Add(activity);

            var captured = activity;
            // 绑定前先清理，防止重复逻辑
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if (!selectedActivities.Contains(captured)) selectedActivities.Add(captured);
                }
                else
                {
                    selectedActivities.Remove(captured);
                }
                UpdateTotalTime();
            });

            activityToggles[activity] = toggle;
        }

        UpdateTotalTime();
    }

    /// <summary>
    /// 纯代码动态创建 UI 元素
    /// </summary>
    GameObject CreateActivityItem(ActivityData activity)
    {
        var go = new GameObject(activity.activityId);
        go.transform.SetParent(activityListRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 50);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childForceExpandWidth = false;
        layout.padding = new RectOffset(10, 10, 5, 5);

        var tGo = new GameObject("Toggle");
        tGo.transform.SetParent(go.transform, false);
        var tRT = tGo.AddComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(30, 30);
        var toggle = tGo.AddComponent<Toggle>();

        var bg = new GameObject("BG");
        bg.transform.SetParent(tGo.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(30, 30);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.35f);
        toggle.targetGraphic = bgImg;

        var ck = new GameObject("Check");
        ck.transform.SetParent(tGo.transform, false);
        var ckRT = ck.AddComponent<RectTransform>();
        ckRT.sizeDelta = new Vector2(22, 22);
        var ckImg = ck.AddComponent<Image>();
        ckImg.color = new Color(0.2f, 0.85f, 0.45f);
        toggle.graphic = ckImg;

        // ====== 字体修改部分 ======
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRT = txtGo.AddComponent<RectTransform>();
        txtRT.sizeDelta = new Vector2(500, 50);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();

        // 【关键】将传入的中文字体赋值给文本
        if (chineseFont != null)
        {
            tmp.font = chineseFont;
        }
        else
        {
            Debug.LogWarning("[PhoneUI] 没有指派中文字体！UI可能会显示成方块。");
        }

        tmp.text = BuildLabel(activity);
        tmp.fontSize = 18;
        tmp.color = activity.isFixed ? new Color(0.7f, 0.7f, 0.7f) : Color.white;

        return go;
    }

    string BuildLabel(ActivityData a)
    {
        string slot = a.slot switch
        {
            ActivitySlot.Morning => "上午",
            ActivitySlot.Afternoon => "下午",
            ActivitySlot.Evening => "晚上",
            _ => "全天"
        };

        StringBuilder sb = new StringBuilder();
        sb.Append($"{a.displayName} [{slot} {a.duration}h]");

        if (a.moodDelta != 0) sb.Append($" 心情{a.moodDelta:+0;-0}");
        if (a.staminaDelta != 0) sb.Append($" 体力{a.staminaDelta:+0;-0}");
        if (a.stressDelta != 0) sb.Append($" 压力{a.stressDelta:+0;-0}");
        if (a.fatigueDelta != 0) sb.Append($" 疲惫{a.fatigueDelta:+0;-0}");
        if (a.academicDelta != 0) sb.Append($" 学业{a.academicDelta:+0;-0}");
        if (a.socialDelta != 0) sb.Append($" 社交{a.socialDelta:+0;-0}");
        if (a.healthDelta != 0) sb.Append($" 健康{a.healthDelta:+0;-0}");

        if (a.isFixed) sb.Append("  ★固定");

        return sb.ToString();
    }

    void UpdateAlarmText(float val)
    {
        int h = (int)val;
        int m = (int)((val - h) * 60);
        if (alarmTimeText != null) alarmTimeText.text = $"闹钟：{h:D2}:{m:D2}";
    }

    void UpdateTotalTime()
    {
        float total = 0f;
        foreach (var a in selectedActivities) total += a.duration;
        if (totalTimeText != null) totalTimeText.text = $"已安排：{total:F1} 小时";
    }

    public void OnConfirm()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.alarmSet = alarmToggle != null && alarmToggle.isOn;
            PlayerStats.Instance.alarmTime = alarmTimeSlider != null ? alarmTimeSlider.value : 7.5f;
        }

        ScheduleSystem.Instance?.SetTomorrowSchedule(selectedActivities);

        var schedule = new DailySchedule
        {
            alarmSet = PlayerStats.Instance?.alarmSet ?? true,
            alarmTime = PlayerStats.Instance?.alarmTime ?? 7.5f
        };

        if (statusText != null) statusText.text = "晚安……";
        if (confirmButton != null) confirmButton.interactable = false;

        StartCoroutine(ConfirmDelay(schedule));
    }

    IEnumerator ConfirmDelay(DailySchedule schedule)
    {
        yield return new WaitForSeconds(0.8f);
        GameManager.Instance?.ConfirmTomorrowPlan(schedule);
    }
}