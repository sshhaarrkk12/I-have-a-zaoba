using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("所有游戏事件（拖入即可）")]
    public List<GameEventData> allEvents = new List<GameEventData>();

    [Header("每天最多触发随机事件数")]
    public int maxDailyEvents = 2;

    private HashSet<string> todayTriggeredIds = new HashSet<string>();
    private HashSet<string> permanentTriggeredIds = new HashSet<string>();
    private int todayEventCount = 0;

    public static event Action<GameEventData> OnEventFinished;

    // 改为 int，用于记录上一次检查的整点时间，防止 Update 里疯狂触发
    int lastCheckedHour = -1;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        PlayerStats.OnDayEnd += ResetDailyEvents;
        PlayerStats.OnCriticalThreshold += OnStatThreshold;
    }

    void OnDisable()
    {
        PlayerStats.OnDayEnd -= ResetDailyEvents;
        PlayerStats.OnCriticalThreshold -= OnStatThreshold;
    }

    // ==================== Update 时间检查 ====================

    void Update()
    {
        if (TimeManager.Instance == null) return;

        // 关键防护 1：如果当前正在播放对话，暂停所有的自动事件检测！
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        float hour = TimeManager.Instance.gameHour;
        int currentIntHour = Mathf.FloorToInt(hour); // 向下取整，比如 8.5 点视为 8 点

        // 关键防护 2：只有当时间跨越到下一个整点时，才进行一次事件扫描
        if (currentIntHour <= lastCheckedHour) return;
        lastCheckedHour = currentIntHour;

        CheckTimeTriggeredEvents(hour);
    }

    void CheckTimeTriggeredEvents(float currentHour)
    {
        int day = PlayerStats.Instance?.currentDay ?? 1;
        string scene = SceneStateManager.Instance?.CurrentScene ?? "";

        foreach (var evt in allEvents)
        {
            if (!evt.isEnabled) continue;
            if (IsAlreadyTriggered(evt)) continue;
            if (day < evt.minDay || day > evt.maxDay) continue;
            if (!evt.IsSceneValid(scene)) continue;

            if (evt.triggerType == EventTriggerType.ExactTime)
            {
                // 精确时间触发：当前小时大于设定时间，立即触发
                if (currentHour >= evt.exactTriggerHour)
                {
                    TriggerEvent(evt, consumeDailyQuota: false);
                    return; // 触发了一个事件后直接 return，防止同一整点连发多个事件
                }
            }
            else if (evt.triggerType == EventTriggerType.Random && evt.timing == EventTiming.Any)
            {
                if (todayEventCount >= maxDailyEvents) continue;
                if (evt.IsTimeValid(currentHour) && UnityEngine.Random.value < evt.triggerChance)
                {
                    TriggerEvent(evt, consumeDailyQuota: true);
                    return; // 同理，命中一个随机事件后本小时不再触发其他的
                }
            }
        }
    }

    // ==================== 场景切换后的随机触发入口 ====================

    public void TriggerDailyEvents(EventTiming timing)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (todayEventCount >= maxDailyEvents) return;

        int day = PlayerStats.Instance.currentDay;
        float currentHour = TimeManager.Instance != null ? TimeManager.Instance.gameHour : 0f;
        string scene = SceneStateManager.Instance?.CurrentScene ?? "";

        // 稍微打乱一下事件列表，避免排在前面的事件永远优先触发
        var shuffledEvents = allEvents.OrderBy(x => Guid.NewGuid()).ToList();

        foreach (var evt in shuffledEvents)
        {
            if (todayEventCount >= maxDailyEvents) break;
            if (!evt.isEnabled) continue;
            if (evt.triggerType != EventTriggerType.Random) continue;
            if (IsAlreadyTriggered(evt)) continue;
            if (evt.timing != timing && evt.timing != EventTiming.Any) continue;
            if (evt.requiredTags != null && evt.requiredTags.Length > 0) continue;
            if (day < evt.minDay || day > evt.maxDay) continue;
            if (!evt.IsTimeValid(currentHour)) continue;
            if (!evt.IsSceneValid(scene)) continue;

            if (UnityEngine.Random.value < evt.triggerChance)
            {
                TriggerEvent(evt, consumeDailyQuota: true);
                return; // 每次进场景最多触发 1 个随机事件即可
            }
        }
    }

    // ==================== 标签 / ID / 条件触发（保持原样，增加阻塞判定） ====================

    public void TriggerEventByTag(string tag)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        var evt = allEvents.FirstOrDefault(e =>
            e.isEnabled && e.triggerType == EventTriggerType.Tag &&
            e.requiredTags != null && Array.Exists(e.requiredTags, t => t == tag));

        if (evt != null) TriggerEvent(evt, consumeDailyQuota: false);
    }

    public void TriggerEventById(string id)
    {
        var evt = allEvents.FirstOrDefault(e => e.eventId == id);
        if (evt != null) TriggerEvent(evt, consumeDailyQuota: false);
    }

    void OnStatThreshold(StatsEventType statType, float value)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        foreach (var evt in allEvents)
        {
            if (!evt.isEnabled || evt.triggerType != EventTriggerType.Condition) continue;
            if (evt.conditionStat != statType || IsAlreadyTriggered(evt)) continue;

            bool met = evt.conditionCompare == ConditionCompare.LessThan
                ? value < evt.conditionValue
                : value > evt.conditionValue;

            if (met)
            {
                TriggerEvent(evt, consumeDailyQuota: false);
                break;
            }
        }
    }

    // ==================== 核心触发 ====================

    public void TriggerEvent(GameEventData evt, bool consumeDailyQuota = true)
    {
        if (todayTriggeredIds.Contains(evt.eventId)) return;

        todayTriggeredIds.Add(evt.eventId);
        if (consumeDailyQuota) todayEventCount++;

        if (evt.triggerOnce) permanentTriggeredIds.Add(evt.eventId);

        Debug.Log($"[Event] 触发: {evt.eventTitle} 时间:{TimeManager.Instance?.GetFormattedTime()}");

        // ⚠️ 请确保你的 GameEventData 里，把 dialogue 字段改成了 public DialogueNode dialogue;
        if (evt.dialogue != null)
        {
            DialoguePlayer.Instance.Play(evt.dialogue, () =>
            {
                if (!string.IsNullOrEmpty(evt.followUpEventId))
                    TriggerEventById(evt.followUpEventId);

                OnEventFinished?.Invoke(evt);
            });
        }
        else
        {
            if (!string.IsNullOrEmpty(evt.followUpEventId))
                TriggerEventById(evt.followUpEventId);

            OnEventFinished?.Invoke(evt);
        }
    }

    // ==================== 工具方法 ====================

    bool IsAlreadyTriggered(GameEventData evt)
    {
        if (todayTriggeredIds.Contains(evt.eventId)) return true;
        if (evt.triggerOnce && permanentTriggeredIds.Contains(evt.eventId)) return true;
        return false;
    }

    void ResetDailyEvents()
    {
        todayTriggeredIds.Clear();
        todayEventCount = 0;
        // 跨天时重置检查时间，保证第二天早上能正常触发
        lastCheckedHour = -1;
    }
}