using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GamePhase currentPhase { get; private set; }

    [Header("场景名称 - 必须和 Build Settings 完全一致")]
    public string wakeUpScene = "Wakeup";
    public string dormHubScene = "DormHub";
    public string washingScene = "Washing";
    public string dressingScene = "Dressing";
    public string packingScene = "Packing";
    public string goOutScene = "GoOut";
    public string corridorScene = "Corridor";
    public string canteenScene = "Canteen";
    public string classroomScene = "Classroom";
    public string phoneUIScene = "PhoneUI";

    [Header("把 PlayerStats SO 拖进来")]
    public PlayerStats playerStats;

    public DailySchedule tomorrowSchedule = new DailySchedule();
    public static event Action<GamePhase> OnPhaseChanged;

    void Awake()
    {
        Debug.Log($"[GM] Awake, parent={transform.parent?.name ?? "null(根级)"}");
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GM] DontDestroyOnLoad 已设置");
    }

    void Start()
    {
        if (playerStats == null)
            playerStats = Resources.Load<PlayerStats>("PlayerStats");
        if (playerStats == null) { Debug.LogError("[GM] PlayerStats未找到"); return; }

        ResetPlayerStats();
        StartDay(SceneManager.GetActiveScene().name != wakeUpScene);
    }

    void ResetPlayerStats()
    {
        playerStats.currentDay = 1;
        playerStats.week = 1;
        playerStats.mood = 70f;
        playerStats.instantStamina = 100f;
        playerStats.coreStamina = 80f;
        playerStats.instantStress = 30f;
        playerStats.coreStress = 20f;
        playerStats.fatigue = 40f;
        playerStats.health = 80f;
        playerStats.alarmSet = true;
        playerStats.alarmTime = 7.5f;
        playerStats.consecutiveHighFatigueDays = 0;
        Debug.Log("[GM] PlayerStats 已重置");
    }

    // ==================== 天循环 ====================

    public void StartDay()
    {
        StartDay(true);
    }

    void StartDay(bool loadWakeUpScene)
    {
        Debug.Log($"[GM] === Day {playerStats.currentDay} ===");
        ChangePhase(GamePhase.WakeUp);
        float wakeTime = SleepSystem.CalculateWakeUpTime(playerStats);
        playerStats.wakeUpTime = wakeTime;
        TimeManager.Instance?.ResetForNewDay(wakeTime);
        playerStats.NewDayReset();

        if (loadWakeUpScene)
            LoadScene(wakeUpScene);
        else
            SceneStateManager.Instance?.SetCurrentScene(wakeUpScene);

        UIManager.Instance?.ShowFade(false, 1f);
    }

    public void OnPlayerWokeUp()
    {
        ChangePhase(GamePhase.Morning);
        // 注意：早晨随机事件由 WakeUpSceneManager 负责触发
        LoadScene(dormHubScene);
    }

    public void TransitionToNoonPhase()
    {
        Debug.Log("[GM] → 中午手机");
        ChangePhase(GamePhase.Noon);
        playerStats.AddFatigue(20f);
        playerStats.RecalculateHealth();
        LoadScene(phoneUIScene);
    }

    public void ConfirmTomorrowPlan(DailySchedule schedule)
    {
        tomorrowSchedule = schedule;
        ChangePhase(GamePhase.Night);
        StartCoroutine(SleepTransition());
    }

    IEnumerator SleepTransition()
    {
        // 淡入黑幕
        UIManager.Instance?.ShowFade(true, 1f);
        yield return new WaitForSeconds(1.5f);

        // 结算日程活动属性
        ScheduleSystem.Instance?.SettleSchedule();

        // 结算天数
        playerStats.EndOfDayUpdate();
        if (EndingSystem.Instance != null && EndingSystem.Instance.HasTriggered)
            yield break;

        playerStats.sleepQuality = SleepSystem.CalculateSleepQuality(playerStats);

        yield return new WaitForSeconds(0.5f);
        StartDay();
    }

    // ==================== 场景跳转 ====================

    public void GoToDorm() => LoadScene(dormHubScene);
    public void GoToDormHub() => LoadScene(dormHubScene);
    public void GoToWashing() => LoadScene(washingScene);
    public void GoToDressing() => LoadScene(dressingScene);
    public void GoToPacking() => LoadScene(packingScene);
    public void GoToGoOut() => LoadScene(goOutScene);
    public void GoToCorridor() => LoadScene(corridorScene);
    public void GoToPhoneUI() => LoadScene(phoneUIScene);

    public void GoToClassroom()
    {
        ChangePhase(GamePhase.InClass);
        LoadScene(classroomScene);
    }

    public void GoToCanteen()
    {
        if (Application.CanStreamedLevelBeLoaded(canteenScene))
            LoadScene(canteenScene);
        else
        {
            Debug.LogWarning("[GM] Canteen场景不存在，跳Corridor代替");
            LoadScene(corridorScene);
        }
    }

    void LoadScene(string name)
    {
        Debug.Log($"[GM] LoadScene: {name}");
        SceneStateManager.Instance?.SetCurrentScene(name);
        SceneManager.LoadScene(name);
    }

    void ChangePhase(GamePhase p)
    {
        currentPhase = p;
        OnPhaseChanged?.Invoke(p);
        Debug.Log($"[GM] Phase → {p}");
    }

    
}

public enum GamePhase { WakeUp, Morning, InClass, Noon, Night }

[Serializable]
public class DailySchedule
{
    public List<ScheduledActivity> activities = new List<ScheduledActivity>();
    public bool alarmSet = true;
    public float alarmTime = 7.5f;
}

[Serializable]
public class ScheduledActivity
{
    public string activityId;
    public string displayName;
    public float startTime;
    public float duration;
    public float moodDelta;
    public float staminaDelta;
    public float stressDelta;
    public float fatigueDelta;
    public string location;
    public bool isFixed;
}