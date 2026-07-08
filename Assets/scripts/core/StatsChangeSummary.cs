using System.Collections.Generic;
using System;
using UnityEngine;

public struct StatsChangeSnapshot
{
    public bool hasStats;
    public bool hasTime;
    public float gameHour;
    public float mood;
    public float instantStamina;
    public float instantStress;
    public float fatigue;
    public float academic;
    public float social;
    public float health;
}

public static class StatsChangeSummary
{
    const float Epsilon = 0.05f;

    public static StatsChangeSnapshot Capture()
    {
        PlayerStats stats = PlayerStats.Instance;
        TimeManager time = TimeManager.Instance;

        return new StatsChangeSnapshot
        {
            hasStats = stats != null,
            hasTime = time != null,
            gameHour = time != null ? time.gameHour : 0f,
            mood = stats != null ? stats.mood : 0f,
            instantStamina = stats != null ? stats.instantStamina : 0f,
            instantStress = stats != null ? stats.instantStress : 0f,
            fatigue = stats != null ? stats.fatigue : 0f,
            academic = stats != null ? stats.academic : 0f,
            social = stats != null ? stats.social : 0f,
            health = stats != null ? stats.health : 0f
        };
    }

    public static string Build(StatsChangeSnapshot before)
    {
        List<string> lines = new List<string>();


        PlayerStats stats = PlayerStats.Instance;
        if (before.hasStats && stats != null)
        {
            AddDelta(lines, "心情", stats.mood - before.mood);
            AddDelta(lines, "体力", stats.instantStamina - before.instantStamina);
            AddDelta(lines, "压力", stats.instantStress - before.instantStress);
            AddDelta(lines, "疲劳", stats.fatigue - before.fatigue);
            AddDelta(lines, "学业", stats.academic - before.academic);
            AddDelta(lines, "人际关系", stats.social - before.social);
            AddDelta(lines, "健康", stats.health - before.health);
        }

        return lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
    }

    public static void Show(string content, Action onDone = null)
    {
        if (string.IsNullOrEmpty(content) || DialogueManager.Instance == null)
        {
            onDone?.Invoke();
            return;
        }

        DialogueManager.Instance.ShowStatPopup(content, onDone);
    }

    static void AddDelta(List<string> lines, string label, float delta)
    {
        if (Mathf.Abs(delta) < Epsilon) return;

        string sign = delta > 0f ? "+" : string.Empty;
        lines.Add($"{label} {sign}{FormatNumber(delta)}");
    }

    static string FormatNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.#");
    }
}
