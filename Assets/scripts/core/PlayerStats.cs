using UnityEngine;
using System;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "EarlyClass8/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    public static PlayerStats Instance { get; private set; }

    [Header("=== 心情 Mood ===")]
    [Range(0, 100)] public float mood = 70f;
    public float moodDecayPerHour = 1f;

    [Header("=== 体力 Stamina ===")]
    [Range(0, 100)] public float instantStamina = 100f;
    [Range(0, 100)] public float coreStamina = 80f;
    public float staminaRecoveryBase = 5f;
    public float instantStaminaRecoveryRate = 0.5f;

    [Header("=== 压力 Stress ===")]
    [Range(0, 100)] public float instantStress = 30f;
    [Range(0, 100)] public float coreStress = 20f;
    public float stressRecoveryBase = 3f;

    [Header("=== 疲惫 Fatigue ===")]
    [Range(0, 100)] public float fatigue = 40f;
    public int consecutiveHighFatigueDays = 0;
    public const float FATIGUE_CRITICAL = 100f;
    public const int CONSECUTIVE_FATIGUE_THRESHOLD = 3;

    [Header("=== 学业 Academic ===")]
    [Range(0, 100)] public float academic = 60f;

    [Header("=== 人际关系 Social ===")]
    [Range(0, 100)] public float social = 50f; // 初始默认 50 关系度

    [Header("=== 健康 Health ===")]
    [Range(0, 100)] public float health = 80f;
    // 权重：心情0.2 核心体力0.3 压力(反)0.2 疲惫(反)0.2 学业0.1

    [Header("=== 睡眠相关 ===")]
    public float sleepQuality = 0.8f;
    public bool alarmSet = false;
    public float alarmTime = 7.5f;
    public float wakeUpTime = 7.5f;

    [Header("=== 日期 ===")]
    public int currentDay = 1;
    public const int MAX_DAYS = 7;
    public int week = 1;

    public static event Action<StatsEventType, float> OnCriticalThreshold;
    public static event Action OnHealthCritical;
    public static event Action OnDayEnd;

    void OnEnable() { Instance = this; }

    public void NewDayReset()
    {
        float recoveryRate = coreStamina / 100f;
        instantStamina = Mathf.Clamp(instantStamina + staminaRecoveryBase * 8f * recoveryRate, 0, 100);
        instantStress = Mathf.Clamp(instantStress - stressRecoveryBase * 8f * (1f - coreStress / 100f), 0, 100);
        float fatigueLoss = sleepQuality * 50f;
        fatigue = Mathf.Clamp(fatigue - fatigueLoss, 0, 100);
        RecalculateHealth();
    }

    public void EndOfDayUpdate()
    {
        if (fatigue >= FATIGUE_CRITICAL)
        {
            consecutiveHighFatigueDays++;
            if (consecutiveHighFatigueDays >= CONSECUTIVE_FATIGUE_THRESHOLD)
            {
                health = Mathf.Max(0, health - 15f);
                OnHealthCritical?.Invoke();
            }
        }
        else
        {
            consecutiveHighFatigueDays = 0;
        }

        currentDay++;
        
        
        
        OnDayEnd?.Invoke();
        CheckAllThresholds();
    }

    public void RecalculateHealth()
    {
        float moodScore = mood * 0.2f;
        float staminaScore = coreStamina * 0.3f;
        float stressScore = (100f - coreStress) * 0.2f;
        float fatigueScore = (100f - fatigue) * 0.2f;
        float academicScore = academic * 0.1f;
        health = Mathf.Clamp(moodScore + staminaScore + stressScore + fatigueScore + academicScore, 0, 100);
        if (health <= 20f) OnHealthCritical?.Invoke();
    }

    public void AddAcademic(float amount)
    {
        academic = Mathf.Clamp(academic + amount, 0, 100);
        CheckThreshold(StatsEventType.Academic, academic);
        RecalculateHealth();
    }

    public void ConsumeInstantStamina(float amount)
    {
        instantStamina = Mathf.Max(0, instantStamina - amount);
        if (instantStamina < 20f)
            coreStamina = Mathf.Max(0, coreStamina - amount * 0.1f);
        CheckThreshold(StatsEventType.InstantStamina, instantStamina);
    }

    public void RecoverInstantStamina(float amount)
    {
        float rate = coreStamina / 100f;
        instantStamina = Mathf.Min(100f, instantStamina + amount * rate);
    }

    public void AddStress(float amount)
    {
        float multiplier = 1f + coreStress / 100f;
        instantStress = Mathf.Min(100, instantStress + amount * multiplier);
        coreStress = Mathf.Min(100, coreStress + amount * 0.05f);
        CheckThreshold(StatsEventType.Stress, instantStress);
        RecalculateHealth();
    }

    public void AddFatigue(float amount)
    {
        fatigue = Mathf.Min(100, fatigue + amount);
        CheckThreshold(StatsEventType.Fatigue, fatigue);
        RecalculateHealth();
    }

    public void ChangeMood(float delta)
    {
        mood = Mathf.Clamp(mood + delta, 0, 100);
        CheckThreshold(StatsEventType.Mood, mood);
        RecalculateHealth();
    }
    public void ChangeSocial(float delta)
    {
        social = Mathf.Clamp(social + delta, 0, 100);
        CheckThreshold(StatsEventType.Social, social);
        // 人际关系通常不直接影响生死，所以这里可以选择不调用 RecalculateHealth()，或者你自己决定是否加入公式
    }
    // 💡 补充修改压力的方法
    public void ChangeStress(float delta)
    {
        // 注意：这里的 instantStress 请替换为你实际使用的压力变量名
        instantStress = Mathf.Clamp(instantStress + delta, 0, 100);

        // 触发临界值检测和 UI 刷新
        CheckThreshold(StatsEventType.Stress, instantStress);
    }

    // 💡 补充修改疲惫的方法（顺手补上，防止等会儿 Fatigue 也报错）
    public void ChangeFatigue(float delta)
    {
        fatigue = Mathf.Clamp(fatigue + delta, 0, 100);
        CheckThreshold(StatsEventType.Fatigue, fatigue);
    }

    public float GetMovementSpeed(bool running)
    {
        float baseSpeed = running ? 2.0f : 1.0f;
        float staminaFactor = Mathf.Lerp(0.3f, 1.0f, instantStamina / 100f);
        return baseSpeed * staminaFactor;
    }

    public float GetAlarmWakeChance()
    {
        if (!alarmSet) return 0f;
        float fatiguePenalty = Mathf.Lerp(1.0f, 0.1f, fatigue / 100f);
        float coreFactor = Mathf.Lerp(0.5f, 1.0f, coreStamina / 100f);
        return Mathf.Clamp01(fatiguePenalty * coreFactor);
    }

    public bool IsGroggAfterWakeup()
    {
        float groggChance = (1f - sleepQuality) * 0.5f + (fatigue / 100f) * 0.5f;
        return UnityEngine.Random.value < groggChance;
    }

    void CheckAllThresholds()
    {
        CheckThreshold(StatsEventType.Mood, mood);
        CheckThreshold(StatsEventType.CoreStamina, coreStamina);
        CheckThreshold(StatsEventType.Stress, instantStress);
        CheckThreshold(StatsEventType.Fatigue, fatigue);
        CheckThreshold(StatsEventType.Health, health);
        CheckThreshold(StatsEventType.Academic, academic);
    }

    void CheckThreshold(StatsEventType type, float value)
    {
        if (value <= 20f || value >= 80f)
            OnCriticalThreshold?.Invoke(type, value);
    }
}

public enum StatsEventType
{
    Mood, InstantStamina, CoreStamina, Stress, CoreStress, Fatigue, Health, Academic, Social
}
