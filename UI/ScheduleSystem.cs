using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

[CreateAssetMenu(fileName = "NewActivity", menuName = "EarlyClass8/Activity")]
public class ActivityData : ScriptableObject
{
    [Header("--- 基础信息 ---")]
    public string activityId;
    public string displayName;
    [TextArea(2, 4)]
    public string description;

    [Header("--- 安排限制 ---")]
    public ActivitySlot slot;
    public float duration = 1f;
    [Tooltip("解锁天数")] public int unlockDay = 1;
    [Tooltip("是否为每日固定活动")] public bool isFixed = false;
    [Tooltip("每周最大次数限制")] public int maxPerWeek = 7;

    [Header("--- 属性影响 (结算时生效) ---")]
    public float moodDelta;       // 心情
    public float staminaDelta;    // 体力
    public float stressDelta;     // 压力
    public float fatigueDelta;    // 疲惫
    public float academicDelta;   // 学业
    public float socialDelta;     // 社交
    public float healthDelta;     // 健康

    [Header("--- 表现设置 ---")]
    [Tooltip("留空则自动生成属性增减文本")]
    [TextArea(2, 4)]
    public string resultText;
}

public enum ActivitySlot { Morning, Afternoon, Evening, Any }

/// <summary>
/// 日程系统
/// 玩家在手机里安排明天的活动，睡觉时结算属性，第二天起床获取文本
/// </summary>
public class ScheduleSystem : MonoBehaviour
{
    public static ScheduleSystem Instance { get; private set; }

    [Header("活动配置")]
    public List<ActivityData> availableActivities = new List<ActivityData>();
    public List<ActivityData> fixedActivities = new List<ActivityData>();

    public List<ActivityData> TomorrowSchedule { get; private set; } = new List<ActivityData>();
    public List<string> YesterdayResults { get; private set; } = new List<string>();

    public static event Action<List<string>> OnScheduleSettled;

    private void Awake()
    {
        // 优化单例模式，防止切换场景时出现多个实例导致的异常
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>设置明天的日程（PhoneUI调用）</summary>
    public void SetTomorrowSchedule(List<ActivityData> selectedActivities)
    {
        ClearTomorrowSchedule(); // 先清空并加入固定活动

        foreach (var activity in selectedActivities)
        {
            if (activity != null && !TomorrowSchedule.Contains(activity))
            {
                TomorrowSchedule.Add(activity);
            }
        }
        Debug.Log($"[ScheduleSystem] 明天共安排 {TomorrowSchedule.Count} 个活动");
    }

    /// <summary>清空明日日程，并重新填入固定活动</summary>
    public void ClearTomorrowSchedule()
    {
        TomorrowSchedule.Clear();
        TomorrowSchedule.AddRange(fixedActivities); // 使用 AddRange 更高效
    }

    /// <summary>
    /// 结算日程（GameManager在SleepTransition里调用）
    /// </summary>
    public void SettleSchedule()
    {
        var player = PlayerStats.Instance;
        if (player == null)
        {
            Debug.LogError("[ScheduleSystem] 找不到 PlayerStats 实例，结算失败！");
            return;
        }

        YesterdayResults.Clear();

        foreach (var activity in TomorrowSchedule)
        {
            if (activity == null) continue; // 防空指针

            ApplyStatChanges(player, activity);

            string result = BuildResultText(activity);
            if (!string.IsNullOrEmpty(result))
            {
                YesterdayResults.Add(result);
            }
            Debug.Log($"[ScheduleSystem] 已结算: {activity.displayName}");
        }

        player.RecalculateHealth();
        OnScheduleSettled?.Invoke(YesterdayResults);
    }

    /// <summary>将属性应用逻辑单独提取，使 SettleSchedule 方法更清爽</summary>
    private void ApplyStatChanges(PlayerStats player, ActivityData activity)
    {
        // 使用 != 0 代替 Mathf.Abs() > 0，性能微小提升且逻辑更直接
        if (activity.moodDelta != 0) player.ChangeMood(activity.moodDelta);
        if (activity.staminaDelta != 0) player.ConsumeInstantStamina(-activity.staminaDelta);
        if (activity.stressDelta != 0) player.AddStress(activity.stressDelta);
        if (activity.fatigueDelta != 0) player.AddFatigue(activity.fatigueDelta);

        if (activity.academicDelta != 0) player.academic = Mathf.Clamp(player.academic + activity.academicDelta, 0f, 100f);
        if (activity.socialDelta != 0) player.social = Mathf.Clamp(player.social + activity.socialDelta, 0f, 100f);
        if (activity.healthDelta != 0) player.health = Mathf.Clamp(player.health + activity.healthDelta, 0f, 100f);
    }

    /// <summary>生成表现文本</summary>
    private string BuildResultText(ActivityData a)
    {
        if (!string.IsNullOrEmpty(a.resultText))
            return a.resultText;

        // 使用 StringBuilder 代替 string += 拼接，减少垃圾回收(GC)带来的卡顿风险
        StringBuilder sb = new StringBuilder();

        AppendStatText(sb, "心情", a.moodDelta);
        AppendStatText(sb, "体力", a.staminaDelta);
        AppendStatText(sb, "压力", a.stressDelta);
        AppendStatText(sb, "疲惫", a.fatigueDelta);
        AppendStatText(sb, "学业", a.academicDelta);
        AppendStatText(sb, "社交", a.socialDelta);
        AppendStatText(sb, "健康", a.healthDelta);

        if (sb.Length == 0) return string.Empty;

        return $"{a.displayName}：{sb.ToString().TrimEnd()}";
    }

    /// <summary>辅助拼接字符串</summary>
    private void AppendStatText(StringBuilder sb, string statName, float delta)
    {
        if (delta != 0)
        {
            sb.Append($"{statName}{delta:+0;-0} ");
        }
    }

    /// <summary>获取昨天结果的合并文字（用于StatPopup）</summary>
    public string GetYesterdayResultSummary()
    {
        return YesterdayResults.Count > 0 ? string.Join("\n", YesterdayResults) : string.Empty;
    }
}