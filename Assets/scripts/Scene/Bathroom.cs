using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ToiletInteraction : MonoBehaviour
{
    [Header("UI 按钮组件")]
    public Button toiletButton;       // 上厕所按钮

    [Header("一句话对话框组件（保底时使用）")]
    public GameObject dialogueBox;    // 简易对话框物体
    public Text dialogueText;         // 对话框文本组件

    private bool hasInteracted = false; // 标记是否已经交互过（防止连点）

    void Start()
    {
        if (toiletButton != null)
        {
            toiletButton.onClick.AddListener(OnToiletButtonClicked);
        }

        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    /// <summary>
    /// 当玩家点击“上厕所”按钮时触发
    /// </summary>
    private void OnToiletButtonClicked()
    {
        if (hasInteracted) return;
        hasInteracted = true;
        MorningRoutineState.MarkDone("Bathroom");

        // 锁定按钮，防止重复触发
        if (toiletButton != null) toiletButton.interactable = false;

        // 1. 尝试检索并触发“仅限于厕所场景”的事件
        bool eventTriggered = TryTriggerToiletEvent();

        // 2. 如果没有触发任何事件，执行保底逻辑（时间+5分钟，健康+5）
        if (!eventTriggered)
        {
            ExecuteDefaultToiletLogic();
        }
    }

    /// <summary>
    /// 检索事件管理器中属于厕所场景、且满足当前时间/条件限制的事件
    /// </summary>
    /// <returns>是否成功触发了事件</returns>
    private bool TryTriggerToiletEvent()
    {
        if (EventManager.Instance == null || TimeManager.Instance == null)
        {
            Debug.LogWarning("[厕所系统] EventManager 或 TimeManager 未实例化，直接走保底。");
            return false;
        }

        float currentHour = TimeManager.Instance.gameHour;
        int day = PlayerStats.Instance != null ? PlayerStats.Instance.currentDay : 1;
        string currentScene = SceneStateManager.Instance != null ? SceneStateManager.Instance.CurrentScene : "Toilet";
        // 💡 提示：如果你的场景名在系统里叫别的（比如 "Washroom"），请将上面的 "Toilet" 替换掉。

        // 这一步完全复刻你 EventManager 里的筛选逻辑，精准定向到当前场景
        var toiletCandidates = EventManager.Instance.allEvents.Where(e =>
            e.isEnabled &&
            (e.triggerType == EventTriggerType.Random || e.triggerType == EventTriggerType.ExactTime) &&
            day >= e.minDay && day <= e.maxDay &&
            e.IsTimeValid(currentHour) &&
            e.IsSceneValid(currentScene) // 核心限制：仅限于当前厕所场景
        ).ToList();

        // 寻找符合概率或条件可以立刻执行的事件
        foreach (var evt in toiletCandidates)
        {
            // 如果是精准时间触发，或者随机概率中了
            if (evt.triggerType == EventTriggerType.ExactTime || UnityEngine.Random.value < evt.triggerChance)
            {
                // 呼叫你的核心事件触发器，走 DialoguePlayer 那一套（会自己扣时间/弹对话）
                // 之前你的 EventManager 已经改好了，用的是外部传入的 TriggerEvent(evt)
                // 如果 TriggerEvent 是 private，记得去 EventManager.cs 里把它改成 public！
                EventManager.Instance.TriggerEvent(evt);

                Debug.Log($"[厕所系统] 成功检索并拦截！触发了厕所专属事件: {evt.eventTitle}");
                return true;
            }
        }

        return false; // 没找到或者概率没中，返回 false
    }

    /// <summary>
    /// 保底日常逻辑：加5分钟，加5健康，场景不切换
    /// </summary>
    private void ExecuteDefaultToiletLogic()
    {
        Debug.Log("[厕所系统] 未检索到可触发事件，执行保底日常。");

        // 1. 时间增加 5 分钟 (5 / 60f 小时)
        float hoursToSpend = 5f / 60f;
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SpendTime(hoursToSpend);
        }

        

        // 3. 显示保底的一句话提示
        string defaultText = "非常顺畅！感觉整个人都变轻松了。";
        ShowSingleLineDialogue(defaultText);
    }

    private void ShowSingleLineDialogue(string text)
    {
        if (dialogueBox != null && dialogueText != null)
        {
            dialogueBox.SetActive(true);
            dialogueText.text = text;
            StartCoroutine(WaitForDismissDialogue());
        }
    }

    private IEnumerator WaitForDismissDialogue()
    {
        yield return new WaitForSeconds(0.3f);

        // 等待玩家点击屏幕
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        if (dialogueBox != null) dialogueBox.SetActive(false);

        Debug.Log("[厕所系统] 保底流程结束，维持原场景等待玩家后续出门操作。");
    }
}
