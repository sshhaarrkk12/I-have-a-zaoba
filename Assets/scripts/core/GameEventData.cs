using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "EarlyClass8/GameEvent")]
public class GameEventData : ScriptableObject
{
    [Header("基础信息")]
    public string eventId;
    public string eventTitle;
    public bool isEnabled = true;
    [TextArea(1, 2)] public string editorNote;

    [Header("触发方式")]
    public EventTriggerType triggerType = EventTriggerType.Random;

    // ── Random 专属 ──────────────────────────────────────
    [Header("Random 触发条件")]
    public EventTiming timing = EventTiming.Any;
    [Range(0f, 1f)] public float triggerChance = 0.3f;
    public string[] requiredTags;

    [Header("触发时间范围（0 = 不限制，支持午夜 0 点请填 0.01）")]
    [Tooltip("游戏小时，如 7.5 = 07:30")]
    public float triggerAfterHour = 0f;
    public float triggerBeforeHour = 0f;

    // ── ExactTime 专属 ───────────────────────────────────
    [Header("ExactTime 触发时刻")]
    [Tooltip("到达此游戏小时后立即触发，如 8.0 = 08:00")]
    public float exactTriggerHour = 8.0f;

    // ── Condition 专属 ───────────────────────────────────
    [Header("Condition 触发条件")]
    public StatsEventType conditionStat;
    public ConditionCompare conditionCompare = ConditionCompare.LessThan;
    public float conditionValue = 20f;

    // ── 通用限制 ─────────────────────────────────────────
    [Header("日期 & 场景限制")]
    public int minDay = 1;
    public int maxDay = 7;
    [Tooltip("留空 = 所有场景均可触发")]
    public string[] triggerScenes;

    [Header("触发次数")]
    public bool triggerOnce = true;

    // ── 内容 ─────────────────────────────────────────────
    [Header("对话与演出")]
    [Tooltip("将配置好的 DialogueNode（对话树根节点）拖到这里")]
    public DialogueNode dialogue; // <--- 这里已修改为 DialogueNode

    [Header("后续事件（可选）")]
    [Tooltip("对话结束后自动触发的下一个事件 ID")]
    public string followUpEventId;

    // ── 验证方法 ──────────────────────────────────────────

    /// <summary>当前游戏小时是否在触发时间窗口内</summary>
    public bool IsTimeValid(float currentHour)
    {
        // 用 <= 0 而非 == 0，避免浮点误差；0 点用 0.01 表示
        if (triggerAfterHour > 0f && currentHour < triggerAfterHour) return false;
        if (triggerBeforeHour > 0f && currentHour > triggerBeforeHour) return false;
        return true;
    }

    /// <summary>当前场景是否允许触发</summary>
    public bool IsSceneValid(string currentScene)
    {
        if (triggerScenes == null || triggerScenes.Length == 0) return true;
        foreach (var s in triggerScenes)
        {
            if (string.Equals(s.Trim(), currentScene, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 自动补全 eventId（用文件名兜底，避免忘填）
        if (string.IsNullOrEmpty(eventId))
            eventId = name;

        // 日期范围保护
        if (maxDay < minDay) maxDay = minDay;

        // triggerChance 已有 Range 特性，但 ExactTime/Condition 用不到，给个提示
        if (triggerType != EventTriggerType.Random && triggerType != EventTriggerType.Tag)
            triggerChance = 1f; // 非随机类型强制 100%，Inspector 看起来不会迷惑
    }
#endif
}

public enum EventTriggerType { Random, Condition, Tag, ExactTime }
public enum ConditionCompare { LessThan, GreaterThan }
public enum EventTiming { Morning, Class, Noon, Afternoon, Evening, Any, Trigger }
