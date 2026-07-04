using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    public static event Action OnAlarm;

    [Header("时间设置")]
    public float gameHour = 6.5f;
    public float realSecondsPerGameHour = 60f;
    public bool isPaused = true;

    [Header("关键时间点")]
    public float classStartTime = 8.0f;
    public float noonTime = 12.0f;
    public float sleepTime = 23.0f;

    [Header("推进规则")]
    [Tooltip("关闭后，时间只会在交互/场景切换时由脚本手动推进")]
    public bool autoAdvanceEnabled = false;
    [Tooltip("默认场景切换时推进的小时数")]
    public float defaultSceneTransitionHours = 0.08f;
    public List<SceneTransitionTimeRule> sceneTransitionRules = new List<SceneTransitionTimeRule>();

    public static event Action<float> OnTimeChanged;
    public static event Action OnClassWarning;
    public static event Action OnClassStart;
    public static event Action OnNoon;
    public static event Action OnSleep;

    bool classWarningFired = false;
    bool classStartFired = false;
    bool noonFired = false;
    bool sleepFired = false;
    bool alarmFired = false;

    string previousSceneName = string.Empty;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        isPaused = true;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isPaused = true;

        if (string.IsNullOrEmpty(previousSceneName))
        {
            previousSceneName = scene.name;
            return;
        }

        if (previousSceneName != scene.name)
        {
            float hours = GetTransitionHours(previousSceneName, scene.name);
            if (hours > 0f)
            {
                AdvanceTime(hours);
                Debug.Log($"[Time] 场景切换 {previousSceneName} -> {scene.name}，推进 {hours:F2} 小时");
            }
        }

        previousSceneName = scene.name;
    }

    void Update()
    {
        if (isPaused) return;
        if (autoAdvanceEnabled)
            AdvanceTime(Time.deltaTime / realSecondsPerGameHour);
    }

    public void AdvanceTime(float hours)
    {
        gameHour += hours;
        OnTimeChanged?.Invoke(gameHour);

        if (!classWarningFired && gameHour >= classStartTime - 0.25f)
        {
            classWarningFired = true;
            OnClassWarning?.Invoke();
        }

        if (!classStartFired && gameHour >= classStartTime)
        {
            classStartFired = true;
            OnClassStart?.Invoke();
            HandleLateness();
        }

        if (!noonFired && gameHour >= noonTime)
        {
            noonFired = true;
            isPaused = true;
            OnNoon?.Invoke();
            GameManager.Instance?.TransitionToNoonPhase();
        }

        if (!sleepFired && gameHour >= sleepTime)
        {
            sleepFired = true;
            isPaused = true;
            OnSleep?.Invoke();
        }

        bool inMorningScene = SceneManager.GetActiveScene().name != "Classroom";
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
        sleepFired = false;
        alarmFired = false;
        isPaused = true;
        previousSceneName = string.Empty;
    }

    float GetTransitionHours(string fromScene, string toScene)
    {
        foreach (var rule in sceneTransitionRules)
        {
            if (rule == null) continue;
            if (string.Equals(rule.fromScene, fromScene, StringComparison.OrdinalIgnoreCase)
                && string.Equals(rule.toScene, toScene, StringComparison.OrdinalIgnoreCase))
                return rule.hours;
        }

        return defaultSceneTransitionHours;
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

[Serializable]
public class SceneTransitionTimeRule
{
    public string fromScene;
    public string toScene;
    [Range(0f, 2f)] public float hours = 0.08f;
}