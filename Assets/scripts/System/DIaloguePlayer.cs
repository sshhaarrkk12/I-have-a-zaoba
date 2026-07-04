using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 对话播放器，挂在 _Managers 上
/// </summary>
public class DialoguePlayer : MonoBehaviour
{
    public static DialoguePlayer Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>从根节点开始播放对话树</summary>
    public void Play(DialogueNode root, Action onComplete = null)
    {
        if (root == null) { onComplete?.Invoke(); return; }
        StartCoroutine(PlayNode(root, onComplete));
    }

    /// <summary>快速播放一段纯文字，不需要创建节点</summary>
    public void PlaySimple(string text, string charName = "我", Action onComplete = null)
    {
        if (DialogueManager.Instance == null) { onComplete?.Invoke(); return; }
        DialogueManager.Instance.Show(text, onComplete, charName);
    }

    // ── 核心递归：播放节点 → 递归下一节点 ──────────────

    IEnumerator PlayNode(DialogueNode node, Action onComplete)
    {
        // 到达节点：应用属性变化 & 触发事件
        ApplyStatsDelta(node.statsDelta);
        if (!string.IsNullOrEmpty(node.triggerEventId))
            EventManager.Instance?.TriggerEventById(node.triggerEventId);

        bool hasChoices = node.choices != null && node.choices.Count > 0;

        if (hasChoices)
        {
            // 有选项：等玩家选择，然后递归进入对应分支
            DialogueNode chosenNext = null;
            StatsDelta chosenDelta = null;
            string chosenEventId = null;
            bool chosen = false;

            var choiceList = new List<DialogueChoice>();
            foreach (var c in node.choices)
            {
                var captured = c;
                choiceList.Add(new DialogueChoice
                {
                    label = captured.label,
                    onChoose = () =>
                    {
                        chosenNext = captured.next;
                        chosenDelta = captured.statsDelta;
                        chosenEventId = captured.triggerEventId;
                        chosen = true;
                    }
                });
            }

            DialogueManager.Instance.ShowWithChoices(
                node.text, choiceList, node.characterName, node.portrait);

            yield return new WaitUntil(() => chosen);

            ApplyStatsDelta(chosenDelta);
            if (!string.IsNullOrEmpty(chosenEventId))
                EventManager.Instance?.TriggerEventById(chosenEventId);

            if (chosenNext != null)
                yield return StartCoroutine(PlayNode(chosenNext, onComplete));
            else
            {
                DialogueManager.Instance?.Hide();
                onComplete?.Invoke();
            }
        }
        else
        {
            // 无选项：显示文字，点击后跳 next
            bool clicked = false;
            DialogueManager.Instance.Show(
                new DialogueLine
                {
                    characterName = node.characterName,
                    text = node.text,
                    portrait = node.portrait
                },
                () => clicked = true
            );

            yield return new WaitUntil(() => clicked);

            if (node.next != null)
                yield return StartCoroutine(PlayNode(node.next, onComplete));
            else
            {
                DialogueManager.Instance?.Hide();
                onComplete?.Invoke();
            }
        }
    }

    // ── 属性应用 ────────────────────────────────────────

    void ApplyStatsDelta(StatsDelta delta)
    {
        if (delta == null || delta.IsEmpty()) return;
        var p = PlayerStats.Instance;
        if (p == null) return;

        if (delta.mood != 0) p.ChangeMood(delta.mood);
        if (delta.stamina != 0) p.ConsumeInstantStamina(-delta.stamina);
        if (delta.stress != 0) p.AddStress(delta.stress);
        if (delta.fatigue != 0) p.AddFatigue(delta.fatigue);
        if (delta.academic != 0) p.academic = Mathf.Clamp(p.academic + delta.academic, 0f, 100f);
        if (delta.social != 0) p.social = Mathf.Clamp(p.social + delta.social, 0f, 100f);

        p.RecalculateHealth();
    }
}