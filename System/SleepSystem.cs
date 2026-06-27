using UnityEngine;

/// <summary>
/// 睡眠系统
/// 计算起床时间、睡眠质量、迷糊状态
/// </summary>
public static class SleepSystem
{
    /// <summary>
    /// 计算今天的实际起床时间
    /// 影响因素：前一天疲惫、睡眠质量、有无闹钟
    /// </summary>
    public static float CalculateWakeUpTime(PlayerStats stats)
    {
        // 第一天固定6:50醒来
        if (stats.currentDay == 1)
        {
            stats.wakeUpTime = 6.833f; // 06:50
            return stats.wakeUpTime;
        }
        float naturalWakeTime = 8.5f; // 自然醒时间（无闹钟）

        // 疲惫越高，自然醒越晚
        float fatigueSleep = Mathf.Lerp(0f, 2f, stats.fatigue / 100f);
        naturalWakeTime += fatigueSleep;

        // 睡眠质量影响（睡得好起得早）
        float qualityBonus = Mathf.Lerp(0.5f, -0.5f, stats.sleepQuality);
        naturalWakeTime += qualityBonus;

        if (!stats.alarmSet)
        {
            // 没闹钟：自然醒
            stats.wakeUpTime = Mathf.Min(naturalWakeTime, 9.5f);
            return stats.wakeUpTime;
        }

        // 有闹钟：看能不能被叫醒
        float alarmChance = stats.GetAlarmWakeChance();
        bool wokenByAlarm = Random.value < alarmChance;

        if (wokenByAlarm)
        {
            stats.wakeUpTime = stats.alarmTime;
            Debug.Log($"⏰ 闹钟叫醒，起床时间 {FormatTime(stats.alarmTime)}");
        }
        else
        {
            // 没被叫醒，睡过去了
            float oversleepAmount = Random.Range(0.25f, 1.5f);
            stats.wakeUpTime = Mathf.Min(stats.alarmTime + oversleepAmount, 9.5f);
            Debug.Log($"😴 闹钟没叫醒，睡过了！起床时间 {FormatTime(stats.wakeUpTime)}");
            // 睡过头压力+
            stats.AddStress(10f);
        }

        return stats.wakeUpTime;
    }

    /// <summary>
    /// 计算睡眠质量（0~1）
    /// 受当天疲惫、压力、环境（室友）影响
    /// </summary>
    public static float CalculateSleepQuality(PlayerStats stats)
    {
        float base_quality = 0.8f;

        // 压力影响睡眠
        float stressPenalty = (stats.instantStress / 100f) * 0.3f;

        // 疲惫过高反而睡不好
        float fatiguePenalty = stats.fatigue > 80f ? (stats.fatigue - 80f) / 100f * 0.2f : 0f;

        // 基础心情影响
        float moodBonus = ((stats.mood - 50f) / 100f) * 0.15f;

        float quality = base_quality - stressPenalty - fatiguePenalty + moodBonus;
        quality = Mathf.Clamp01(quality);

        // 随机波动（环境因素）
        quality += Random.Range(-0.1f, 0.1f);
        quality = Mathf.Clamp01(quality);

        Debug.Log($"💤 今晚睡眠质量: {quality:F2}");
        return quality;
    }

    static string FormatTime(float h)
    {
        int hh = (int)h;
        int mm = (int)((h - hh) * 60);
        return $"{hh:D2}:{mm:D2}";
    }
}