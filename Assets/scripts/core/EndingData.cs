using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnding", menuName = "EarlyClass8/Ending")]
public class EndingData : ScriptableObject
{
    [Header("Basic Info")]
    public string endingId;
    public string endingTitle;
    [TextArea(2, 4)]
    public string endingDescription;

    [Header("Trigger Types")]
    public bool triggerOnDayEnd = false;
    public bool triggerOnStatBreak = false;
    public bool triggerOnEventTag = false;

    [Header("Day End Conditions")]
    public List<StatCondition> dayEndConditions = new List<StatCondition>();

    [Header("Stat Break Condition")]
    public StatsEventType breakStat;
    public ConditionCompare breakCompare;
    public float breakValue = 0f;

    [Header("Event Tag Trigger")]
    public string triggerTag;

    [Header("Priority")]
    public int priority = 0;

    [Header("Ending Content")]
    public DialogueNode endingDialogue;
    public Sprite endingCG;

    [Header("Ending Type")]
    public EndingType endingType = EndingType.Normal;

    public bool CheckDayEndConditions()
    {
        if (dayEndConditions == null || dayEndConditions.Count == 0) return true;
        foreach (var condition in dayEndConditions)
        {
            if (condition == null || !condition.IsMet())
                return false;
        }

        return true;
    }
}

public enum EndingType
{
    Good,
    Normal,
    Bad,
    Secret
}

[Serializable]
public class StatCondition
{
    public StatsEventType stat;
    public ConditionCompare compare;
    public float value;

    public bool IsMet()
    {
        return EndingSystem.CheckStatCondition(stat, compare, value);
    }
}
