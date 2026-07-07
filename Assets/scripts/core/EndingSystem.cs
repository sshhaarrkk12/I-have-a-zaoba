using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EndingSystem : MonoBehaviour
{
    public static EndingSystem Instance { get; private set; }

    [Header("所有结局（拖入即可，按priority排序）")]
    public List<EndingData> allEndings = new List<EndingData>();

    [Header("默认结局（其他条件都不满足时触发）")]
    public EndingData defaultEnding;

    [Header("结局场景名称")]
    public string endingSceneName = "Ending";

    bool endingTriggered = false;

    // ⚠️ 修改 2：新增对外暴露的当前结局数据，供结局场景的 UI 读取！
    public EndingData CurrentEnding { get; private set; }
    public bool HasTriggered => endingTriggered;

    public static event Action<EndingData> OnEndingTriggered;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        PlayerStats.OnDayEnd += OnDayEnd;
        PlayerStats.OnCriticalThreshold += OnStatThreshold;
        EventManager.OnEventFinished += OnEventFinished;
    }

    void OnDisable()
    {
        PlayerStats.OnDayEnd -= OnDayEnd;
        PlayerStats.OnCriticalThreshold -= OnStatThreshold;
        EventManager.OnEventFinished -= OnEventFinished;
    }

    // ==================== 触发方式1：最终日结束 ====================

    void OnDayEnd()
    {
        if (endingTriggered) return;
        if (PlayerStats.Instance.currentDay <= PlayerStats.MAX_DAYS) return;

        var candidates = allEndings
            .Where(e => e != null && e.triggerOnDayEnd && IsDayEndMatch(e))
            .OrderByDescending(e => e.priority)
            .ToList();

        var ending = candidates.Count > 0 ? candidates[0] : defaultEnding;
        if (ending != null) TriggerEnding(ending);
    }

    bool IsDayEndMatch(EndingData ending)
    {
        if (ending.dayEndConditions != null && ending.dayEndConditions.Count > 0)
            return ending.CheckDayEndConditions();

        // Compatibility: some existing final-day endings store a single stat
        // condition in breakStat/breakValue without enabling triggerOnStatBreak.
        if (ending.breakValue > 0f)
            return CheckStatCondition(ending.breakStat, ending.breakCompare, ending.breakValue);

        return false;
    }

    // ==================== 触发方式2：属性爆掉 ====================

    void OnStatThreshold(StatsEventType statType, float value)
    {
        if (endingTriggered) return;

        var candidates = allEndings
            .Where(e =>
                e.triggerOnStatBreak &&
                e.breakStat == statType &&
                (e.breakCompare == ConditionCompare.LessThan
                    ? value < e.breakValue
                    : value > e.breakValue))
            .OrderByDescending(e => e.priority)
            .ToList();

        if (candidates.Count > 0)
            TriggerEnding(candidates[0]);
    }

    // ==================== 触发方式3：事件标签 ====================

    void OnEventFinished(GameEventData evt)
    {
        if (endingTriggered) return;
        if (evt == null || evt.requiredTags == null) return;

        var candidates = allEndings
            .Where(e =>
                e.triggerOnEventTag &&
                !string.IsNullOrEmpty(e.triggerTag) &&
                Array.Exists(evt.requiredTags, t => t == e.triggerTag))
            .OrderByDescending(e => e.priority)
            .ToList();

        if (candidates.Count > 0)
            TriggerEnding(candidates[0]);
    }

    // ==================== 核心触发逻辑 ====================

    public void TriggerEnding(EndingData ending)
    {
        if (endingTriggered) return;
        if (ending == null) return;
        endingTriggered = true;

        // 记录下来，方便下一个场景读取
        CurrentEnding = ending;

        Debug.Log($"[Ending] 触发结局: {ending.endingTitle} ({ending.endingType})");
        OnEndingTriggered?.Invoke(ending);

        if (TimeManager.Instance != null)
            TimeManager.Instance.isPaused = true;

        StartCoroutine(PlayEnding(ending));
    }

    public void ForceEnding(int index = 0)
    {
        if (allEndings == null || allEndings.Count == 0)
        {
            Debug.LogWarning("[Ending] No ending is configured");
            return;
        }

        index = Mathf.Clamp(index, 0, allEndings.Count - 1);
        TriggerEnding(allEndings[index]);
    }

    public void ForceEndingByTitle(string title)
    {
        if (string.IsNullOrEmpty(title) || title.Trim().Length == 0)
        {
            ForceEnding();
            return;
        }

        var ending = allEndings.FirstOrDefault(e => e != null && e.endingTitle == title);
        if (ending == null)
        {
            Debug.LogWarning($"[Ending] Ending not found: {title}");
            return;
        }

        TriggerEnding(ending);
    }

    [ContextMenu("Debug/Force First Ending")]
    void DebugForceFirstEnding()
    {
        ForceEnding();
    }

    [ContextMenu("Debug/Run Final Day End Check")]
    void DebugRunFinalDayEndCheck()
    {
        if (PlayerStats.Instance == null)
        {
            ForceEnding();
            return;
        }

        PlayerStats.Instance.currentDay = PlayerStats.MAX_DAYS;
        PlayerStats.Instance.EndOfDayUpdate();
    }

    public static bool CheckStatCondition(StatsEventType stat, ConditionCompare compare, float value)
    {
        if (!TryGetStatValue(stat, out float current)) return false;

        return compare == ConditionCompare.LessThan
            ? current < value
            : current > value;
    }

    public static bool TryGetStatValue(StatsEventType stat, out float value)
    {
        var p = PlayerStats.Instance;
        value = 0f;
        if (p == null) return false;

        value = stat switch
        {
            StatsEventType.Mood => p.mood,
            StatsEventType.InstantStamina => p.instantStamina,
            StatsEventType.CoreStamina => p.coreStamina,
            StatsEventType.Stress => p.instantStress,
            StatsEventType.CoreStress => p.coreStress,
            StatsEventType.Fatigue => p.fatigue,
            StatsEventType.Health => p.health,
            StatsEventType.Academic => p.academic,
            StatsEventType.Social => p.social,
            _ => 0f
        };

        return true;
    }

    IEnumerator PlayEnding(EndingData ending)
    {
        // 淡入黑幕
        if (UIManager.Instance != null) UIManager.Instance.ShowFade(true, 1.5f);

        yield return new WaitForSeconds(1.5f); // 等待黑幕完全遮住屏幕

        // 播放结局对话前奏（可选，比如主角临死前的遗言，或者画外音）
        if (ending.endingDialogue != null && DialoguePlayer.Instance != null)
        {
            bool dialogueDone = false;
            DialoguePlayer.Instance.Play(ending.endingDialogue, () => dialogueDone = true);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 跳转结局专属场景（纯展示 CG、标题、描述文本和返回主菜单按钮）
        if (!string.IsNullOrEmpty(endingSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(endingSceneName);
        }
    }
}
