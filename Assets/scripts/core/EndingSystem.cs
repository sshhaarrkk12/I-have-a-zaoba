using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ==================== 结局数据定义 ====================

[CreateAssetMenu(fileName = "NewEnding", menuName = "EarlyClass8/Ending")]
public class EndingData : ScriptableObject
{
    [Header("基础信息")]
    public string endingId;
    public string endingTitle;          // 结局标题，如"完美毕业"
    [TextArea(2, 4)]
    public string endingDescription;    // 结局描述文字

    [Header("触发方式（可多选，满足任意一个即触发）")]
    public bool triggerOnDayEnd = false; // 30天结束时触发
    public bool triggerOnStatBreak = false; // 属性爆掉时触发
    public bool triggerOnEventTag = false; // 特定事件标签触发

    [Header("30天结束触发条件（triggerOnDayEnd=true时有效）")]
    [Tooltip("30天结束后检查属性是否满足这些条件才触发此结局")]
    public List<StatCondition> dayEndConditions = new List<StatCondition>();

    [Header("属性爆掉触发条件（triggerOnStatBreak=true时有效）")]
    public StatsEventType breakStat;        // 哪个属性爆掉
    public ConditionCompare breakCompare;   // 高于还是低于
    public float breakValue = 0f;           // 临界值

    [Header("事件标签触发（triggerOnEventTag=true时有效）")]
    public string triggerTag;               // 触发此结局的事件标签

    [Header("优先级（数字越大越优先，相同条件时高优先级胜出）")]
    public int priority = 0;

    [Header("结局内容")]
    // ⚠️ 修改 1：将 DialogueSequence 改为 DialogueNode
    public DialogueNode endingDialogue;     // 结局对话/剧情 (可以只配一两句话，也可以直接留空)
    public Sprite endingCG;                 // 结局CG图（可空）

    [Header("结局类型")]
    public EndingType endingType = EndingType.Normal;

    /// <summary>检查30天结束时的属性条件是否全部满足</summary>
    public bool CheckDayEndConditions()
    {
        if (dayEndConditions == null || dayEndConditions.Count == 0) return true;
        foreach (var cond in dayEndConditions)
            if (!cond.IsMet()) return false;
        return true;
    }
}

public enum EndingType
{
    Good,    // 好结局
    Normal,  // 普通结局
    Bad,     // 坏结局
    Secret   // 隐藏结局
}

/// <summary>属性条件（结局触发时检查）</summary>
[Serializable]
public class StatCondition
{
    public StatsEventType stat;
    public ConditionCompare compare;
    public float value;

    public bool IsMet()
    {
        var p = PlayerStats.Instance;
        if (p == null) return false;

        float current = stat switch
        {
            StatsEventType.Mood => p.mood,
            StatsEventType.InstantStamina => p.instantStamina,
            StatsEventType.CoreStamina => p.coreStamina,
            StatsEventType.Stress => p.instantStress,
            StatsEventType.Fatigue => p.fatigue,
            StatsEventType.Health => p.health,
            StatsEventType.Academic => p.academic,
            _ => 0f
        };

        return compare == ConditionCompare.LessThan
            ? current < value
            : current > value;
    }
}

// ==================== 结局管理器 ====================

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

    // ==================== 触发方式1：30天结束 ====================

    void OnDayEnd()
    {
        if (endingTriggered) return;
        if (PlayerStats.Instance.currentDay < PlayerStats.MAX_DAYS) return;

        var candidates = allEndings
            .Where(e => e.triggerOnDayEnd && e.CheckDayEndConditions())
            .OrderByDescending(e => e.priority)
            .ToList();

        var ending = candidates.Count > 0 ? candidates[0] : defaultEnding;
        if (ending != null) TriggerEnding(ending);
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

    void TriggerEnding(EndingData ending)
    {
        if (endingTriggered) return;
        endingTriggered = true;

        // 记录下来，方便下一个场景读取
        CurrentEnding = ending;

        Debug.Log($"[Ending] 触发结局: {ending.endingTitle} ({ending.endingType})");
        OnEndingTriggered?.Invoke(ending);

        if (TimeManager.Instance != null)
            TimeManager.Instance.isPaused = true;

        StartCoroutine(PlayEnding(ending));
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