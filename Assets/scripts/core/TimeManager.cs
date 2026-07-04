using UnityEngine;
using System;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    public static event Action OnAlarm;

    [Header("时间设置")]
    public float gameHour = 6.5f;
    public float realSecondsPerGameHour = 60f;
    public bool isPaused = false;

    [Header("关键时间点")]
    public float classStartTime = 8.0f;
    public float noonTime = 12.0f;
    public float sleepTime = 23.0f;

    public static event Action<float> OnTimeChanged;
    public static event Action OnClassWarning;   // 上课前预警（仅通知，不暂停）
    public static event Action OnClassStart;
    public static event Action OnNoon;
    public static event Action OnSleep;

    bool classWarningFired = false;
    bool classStartFired = false;
    bool noonFired = false;
    bool sleepFired = false;
    bool alarmFired = false;

    float _debugTimer = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // PhoneUI 是叠加在主场景上的 UI，不重置暂停状态
        // 其余所有场景加载完成后，时间默认恢复运行
        if (scene.name != "PhoneUI")
            isPaused = false;

        Debug.Log($"[Time] 场景: {scene.name} 时间: {GetFormattedTime()} isPaused={isPaused}");
    }

    void Update()
    {
        _debugTimer += Time.deltaTime;
        if (_debugTimer >= 1f)
        {
            _debugTimer = 0f;
            Debug.Log($"[Time] gameHour={gameHour:F2} isPaused={isPaused}");
        }

        if (isPaused) return;
        AdvanceTime(Time.deltaTime / realSecondsPerGameHour);
    }

    public void AdvanceTime(float hours)
    {
        gameHour += hours;
        OnTimeChanged?.Invoke(gameHour);

        // ── 上课预警（距上课 15 分钟）：只发通知，绝不暂停时间 ──
        if (!classWarningFired && gameHour >= classStartTime - 0.25f)
        {
            classWarningFired = true;
            OnClassWarning?.Invoke();
        }

        // ── 正式上课 ──
        if (!classStartFired && gameHour >= classStartTime)
        {
            classStartFired = true;
            OnClassStart?.Invoke();
            HandleLateness();
        }

        // ── 中午：暂停时间，交由 GameManager 处理场景跳转 ──
        if (!noonFired && gameHour >= noonTime)
        {
            noonFired = true;
            isPaused = true;
            OnNoon?.Invoke();
            GameManager.Instance?.TransitionToNoonPhase();
        }

        // ── 睡眠时间 ──
        if (!sleepFired && gameHour >= sleepTime)
        {
            sleepFired = true;
            isPaused = true;
            OnSleep?.Invoke();
        }

        // ── 闹钟 ──
        // 只在早晨场景触发：Classroom 场景里闹钟已经没有意义，
        // 且 alarmTime 默认 7.5f 会导致课堂时间莫名暂停
        bool inMorningScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Classroom";
        if (!alarmFired
            && inMorningScene
            && PlayerStats.Instance != null
            && PlayerStats.Instance.alarmSet
            && gameHour >= PlayerStats.Instance.alarmTime)
        {
            alarmFired = true;
            isPaused = true;
            OnAlarm?.Invoke();
        }
    }

    public void SpendTime(float hours)
    {
        AdvanceTime(hours);
        PlayerStats.Instance?.AddFatigue(hours * 3f);
    }

    public string GetFormattedTime()
    {
        int h = (int)gameHour;
        int m = (int)((gameHour - h) * 60);
        return $"{h:D2}:{m:D2}";
    }

    public float MinutesToClass() => (classStartTime - gameHour) * 60f;

    public void ResetForNewDay(float startHour)
    {
        gameHour = startHour;
        classWarningFired = false;
        classStartFired = false;
        noonFired = false;
        sleepFired = false;   // 修复：原来重复写了两次 sleepFired = false
        alarmFired = false;
        isPaused = false;
    }

    void HandleLateness()
    {
        var player = PlayerStats.Instance;
        if (player == null) return;

        bool isLate = SceneStateManager.Instance != null &&
                      !SceneStateManager.Instance.IsInClassroom;
        if (isLate)
        {
            player.AddStress(15f);
            player.ChangeMood(-10f);
            EventManager.Instance?.TriggerEventByTag("late_to_class");
            Debug.Log("迟到了！压力+15，心情-10");
        }
    }
}