using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WakeUpSceneManager : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI clickCountText;
    public Button wakeUpButton;
    public Slider groggProgressBar;
    public Image screenOverlay;

    [Header("迷糊设置")]
    public int clicksRequired = 15;
    public float groggTimeout = 8f;

    [Header("第一天专属对话（Inspector里可直接修改文字）")]
    [TextArea(2, 4)] public string day1Line1 = "我醒了...？现在几点了?";
    [TextArea(2, 4)] public string day1Line2 = "开学第一天还真是醒的很早啊，今天有早八，不过这么早的话要不要再睡一会儿呢？";
    public string day1Choice1 = "马上起床";
    public string day1Choice2 = "再睡一会儿";

    [Header("疲劳影响设置")]
    [Tooltip("超过此值才有睡过闹钟的可能")]
    public float fatigueThreshold = 80f;
    [Tooltip("疲劳100时睡过闹钟的最大概率")]
    [Range(0f, 1f)] public float maxMissAlarmChance = 0.9f;
    [Tooltip("起床后疲劳回复：疲劳值 * 此系数")]
    public float fatigueRecoveryRate = 0.15f;

    [Header("第一天再睡分支")]
    public float sleepMoreDurationHours = 0.5f;
    public float sleepMoreLateExtraHours = 0.5f;
    public float sleepMoreFadeDuration = 0.45f;
    public float sleepMoreBlackHoldDuration = 0.8f;

    int currentClicks = 0;
    float timeoutTimer = 0f;
    bool isGroggy = false;
    bool wakeUpSuccess = false;
    bool waitingForInput = false;

    void Start()
    {
        SetOverlayAlpha(0f);
        UIManager.Instance?.HideFadeImmediate();

        TimeManager.OnTimeChanged += OnTimeUpdate;
        TimeManager.OnAlarm += OnAlarmRing;

        var stats = PlayerStats.Instance;
        clicksRequired = Mathf.RoundToInt(Mathf.Lerp(8f, 20f, stats.fatigue / 100f));

        if (wakeUpButton != null)
        {
            wakeUpButton.onClick.RemoveAllListeners();
            wakeUpButton.onClick.AddListener(OnButtonClick);
            wakeUpButton.interactable = false;
        }

        if (groggProgressBar != null) groggProgressBar.value = 0f;

        float currentHour = GetCurrentHour();
        bool reachedCalculatedWakeTime = currentHour >= stats.wakeUpTime - 0.01f;

        // 新一天开始时 GameManager 已经把时间重置到计算出的实际醒来时间。
        // 如果已经到点，直接进入起床流程，避免继续等待一个不会再次触发的闹钟事件。
        if (stats.currentDay == 1 || !stats.alarmSet || reachedCalculatedWakeTime)
        {
            TriggerWakeUp();
        }
        else
        {
            SetStatus("Zzz...");
            if (groggProgressBar != null) groggProgressBar.gameObject.SetActive(false);
            if (clickCountText != null) clickCountText.gameObject.SetActive(false);
            if (TimeManager.Instance != null) TimeManager.Instance.isPaused = false;
        }
    }

    void OnDestroy()
    {
        TimeManager.OnTimeChanged -= OnTimeUpdate;
        TimeManager.OnAlarm -= OnAlarmRing;
    }

    void OnTimeUpdate(float hour) { }

    // ==================== 闹钟回调 ====================

    void OnAlarmRing()
    {
        var stats = PlayerStats.Instance;
        float fatigue = stats.fatigue;

        if (fatigue <= fatigueThreshold)
        {
            TriggerWakeUp();
            return;
        }

        // 疲劳80→0%，疲劳100→maxMissAlarmChance
        float overRatio = (fatigue - fatigueThreshold) / (100f - fatigueThreshold);
        float missChance = overRatio * maxMissAlarmChance;

        if (Random.value > missChance)
        {
            TriggerWakeUp();
            return;
        }

        StartCoroutine(SleepThroughAlarm(fatigue));
    }

    IEnumerator SleepThroughAlarm(float fatigue)
    {
        float overRatio = (fatigue - fatigueThreshold) / (100f - fatigueThreshold);
        float extraSleep = Mathf.Lerp(0.5f, 5f, overRatio);
        extraSleep += Random.Range(-0.5f, 0.5f);
        extraSleep = Mathf.Max(0.5f, extraSleep);

        float wakeHour = TimeManager.Instance.gameHour + extraSleep;

        SetOverlayAlpha(1f);
        SetStatus("闹钟？管它呢……");
        yield return new WaitForSeconds(2f);
        SetOverlayAlpha(0f);

        TimeManager.Instance.SetTime(wakeHour);

        if (wakeHour >= 12f)
        {
            SetStatus("……已经下午了。");
            yield return new WaitForSeconds(1.5f);
            HideStatusText();
            PlayerStats.Instance.ChangeMood(-10f);
            bool pop = false;
            DialogueManager.Instance.ShowStatPopup("睡过头了！\n心情 -10\n今天的课全没了", () => pop = true);
            yield return new WaitUntil(() => pop || this == null);
            if (this == null) yield break;
            ApplyWakeUpFatigueRecovery();
            GameManager.Instance.GoToPhoneUI();
        }
        else
        {
            TriggerWakeUp();
        }
    }

    // ==================== 起床流程入口 ====================

    void TriggerWakeUp()
    {
        var stats = PlayerStats.Instance;
        clicksRequired = Mathf.RoundToInt(Mathf.Lerp(8f, 20f, stats.fatigue / 100f));
        isGroggy = stats.IsGroggAfterWakeup();
        ApplyWakeUpFatigueRecovery();

        if (wakeUpButton != null) wakeUpButton.interactable = true;
        if (isGroggy) StartGroggy();
        else StartNormal();
    }

    void ApplyWakeUpFatigueRecovery()
    {
        var stats = PlayerStats.Instance;
        float recovery = stats.fatigue * fatigueRecoveryRate;
        stats.AddFatigue(-recovery);
    }

    // ==================== 起床交互 ====================

    void StartNormal()
    {
        waitingForInput = true;
        if (wakeUpButton != null) wakeUpButton.interactable = true;
        SetStatus(GetWakeUpStatusText());
        SetButtonText("起床");
        if (groggProgressBar != null) groggProgressBar.gameObject.SetActive(false);
        if (clickCountText != null) clickCountText.gameObject.SetActive(false);
    }

    void StartGroggy()
    {
        currentClicks = 0; timeoutTimer = 0f; waitingForInput = true;
        if (wakeUpButton != null) wakeUpButton.interactable = true;
        if (groggProgressBar != null) { groggProgressBar.gameObject.SetActive(true); groggProgressBar.value = 0f; }
        if (clickCountText != null) { clickCountText.gameObject.SetActive(true); clickCountText.text = $"0/{clicksRequired}"; }
        SetStatus("还没睡醒……使劲点击起床！");
        SetButtonText("点！");
    }

    void Update()
    {
        if (wakeUpSuccess || !waitingForInput || !isGroggy) return;
        timeoutTimer += Time.deltaTime;
        if (timeoutTimer >= groggTimeout) SleepAgain();
    }

    void OnButtonClick()
    {
        if (wakeUpSuccess || !waitingForInput) return;
        if (!isGroggy)
        {
            waitingForInput = false;
            if (wakeUpButton != null) wakeUpButton.interactable = false;
            HideGroggyUI();
            StartCoroutine(PlayWakeUpDialogue());
            return;
        }
        currentClicks++; timeoutTimer = 0f;
        if (groggProgressBar != null) groggProgressBar.value = (float)currentClicks / clicksRequired;
        if (clickCountText != null) clickCountText.text = $"{currentClicks}/{clicksRequired}";
        string[] texts = { "加油！", "快了！", "撑住！", "别睡了！" };
        SetStatus(texts[Random.Range(0, texts.Length)]);
        if (currentClicks >= clicksRequired)
        {
            waitingForInput = false;
            if (wakeUpButton != null) wakeUpButton.interactable = false;
            HideGroggyUI();
            StartCoroutine(PlayWakeUpDialogue());
        }
    }

    void SleepAgain()
    {
        waitingForInput = false;
        if (wakeUpButton != null) wakeUpButton.interactable = false;
        SetStatus("又睡着了……时间在流逝！");
        TimeManager.Instance.AdvanceTime(Random.Range(0.25f, 0.5f));
        PlayerStats.Instance.AddFatigue(5f);
        clicksRequired = Mathf.Min(clicksRequired + 3, 30);
        StartCoroutine(RestartAfterDelay(1.5f));
    }

    IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!wakeUpSuccess) StartGroggy();
    }

    // ==================== 对话主入口 ====================

    IEnumerator PlayWakeUpDialogue()
    {
        if (this == null) yield break;
        if (wakeUpButton != null) wakeUpButton.gameObject.SetActive(false);
        HideGroggyUI();
        HideStatusText();

        if (PlayerStats.Instance.currentDay == 1)
            yield return StartCoroutine(PlayDay1Dialogue());
        else
            yield return StartCoroutine(PlayNormalDayWakeup());
    }

    // ==================== 昨日活动结果 ====================

    IEnumerator ShowYesterdayResults()
    {
        if (ScheduleSystem.Instance == null) yield break;
        string summary = ScheduleSystem.Instance.GetYesterdayResultSummary();
        if (string.IsNullOrEmpty(summary)) yield break;

        bool pop = false;
        DialogueManager.Instance.ShowStatPopup(summary, () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
    }

    // ==================== 第一天固定剧情 ====================

    IEnumerator PlayDay1Dialogue()
    {
        bool d1 = false;
        DialogueManager.Instance.Show(day1Line1, () => d1 = true);
        yield return new WaitUntil(() => d1 || this == null);
        if (this == null) yield break;

        DialogueManager.Instance.ShowWithChoices(day1Line2,
            new List<DialogueChoice>
            {
                new DialogueChoice { label = day1Choice1, onChoose = () => StartCoroutine(ChooseGetUp()) },
                new DialogueChoice { label = day1Choice2, onChoose = () => StartCoroutine(ChooseSleepMore()) }
            });
    }

    IEnumerator ChooseGetUp()
    {
        bool done = false;
        DialogueManager.Instance.Show("还是马上起来吧，万一睡过了就不好了", () => done = true);
        yield return new WaitUntil(() => done || this == null);
        if (this == null) yield break;
        GoToDorm();
    }

    IEnumerator ChooseSleepMore()
    {
        bool done = false;
        DialogueManager.Instance.Show("还有这么长时间，那还是再睡一会儿吧", () => done = true);
        yield return new WaitUntil(() => done || this == null);
        if (this == null) yield break;

        float plannedWakeHour = GetCurrentHour() + sleepMoreDurationHours;
        int roll = Random.Range(0, 3);
        if (roll == 0) StartCoroutine(EventWakeShort(plannedWakeHour));
        else if (roll == 1) StartCoroutine(EventRoommateWake(plannedWakeHour));
        else StartCoroutine(EventWakeLate(plannedWakeHour + sleepMoreLateExtraHours));
    }

    IEnumerator EventWakeShort(float wakeHour)
    {
        yield return StartCoroutine(PlaySleepMoreBlackTransition());
        MoveTimeTo(wakeHour);
        ChangeFatigueSafely(-8f);
        bool done = false;
        DialogueManager.Instance.Show($"{GetCurrentTimeText()}……时间正好，也睡饱了，那就起床吧！", () => done = true);
        yield return new WaitUntil(() => done || this == null);
        if (this == null) yield break;
        PlayerStats.Instance.ChangeMood(2f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 +2\n疲劳 -8", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;
        GoToDorm();
    }

    IEnumerator EventRoommateWake(float wakeHour)
    {
        yield return StartCoroutine(PlaySleepMoreBlackTransition());
        MoveTimeTo(wakeHour);
        ChangeFatigueSafely(-5f);
        bool d1 = false;
        DialogueManager.Instance.Show($"小A：快醒醒，{GetCurrentTimeText()}了，该起床了！", () => d1 = true, "小A");
        yield return new WaitUntil(() => d1 || this == null);
        if (this == null) yield break;
        bool d2 = false;
        DialogueManager.Instance.Show($"唔…已经{GetCurrentTimeText()}了吗？谢谢你小A", () => d2 = true);
        yield return new WaitUntil(() => d2 || this == null);
        if (this == null) yield break;
        PlayerStats.Instance.ChangeMood(1f);
        PlayerStats.Instance.ChangeSocial(3f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 +1\n人际关系 +3\n疲劳 -5", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;
        GoToDorm();
    }

    IEnumerator EventWakeLate(float wakeHour)
    {
        yield return StartCoroutine(PlaySleepMoreBlackTransition());

        MoveTimeTo(wakeHour);

        bool d1 = false;
        DialogueManager.Instance.Show("感觉睡了好久，几点了？", () => d1 = true);
        yield return new WaitUntil(() => d1 || this == null);
        if (this == null) yield break;

        bool d2 = false;
        DialogueManager.Instance.Show("？！！！已经50分了？怎么没人叫我", () => d2 = true);
        yield return new WaitUntil(() => d2 || this == null);
        if (this == null) yield break;

        PlayerStats.Instance.ChangeMood(-5f);
        PlayerStats.Instance.ChangeSocial(-5f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 -5\n人际关系 -5", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;

        DialogueManager.Instance.ShowWithChoices("这么晚了还要去上课吗……",
            new List<DialogueChoice>
            {
                new DialogueChoice { label = "于是我张开双手允许学分流走",
                    onChoose = () => StartCoroutine(SkipClass()) },
                new DialogueChoice { label = "不想去但舍不得学分，找舍友帮忙",
                    onChoose = () => StartCoroutine(AskRoommate()) }
            });
    }

    IEnumerator SkipClass()
    {
        PlayerStats.Instance.ChangeMood(2f);
        PlayerStats.Instance.AddAcademic(-5f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 +2\n学业 -5", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;
        GoToDorm();
    }

    IEnumerator AskRoommate()
    {
        bool done = false;
        DialogueManager.Instance.Show("（发消息给舍友：江湖救急！能帮我签到吗，下午请你喝奶茶）",
            () => done = true);
        yield return new WaitUntil(() => done || this == null);
        if (this == null) yield break;

        // 随机：0=拒绝，1=同意
        if (Random.Range(0, 2) == 0)
            yield return StartCoroutine(RoommateRefuse());
        else
            yield return StartCoroutine(RoommateAgree());
    }

    IEnumerator RoommateRefuse()
    {
        bool d1 = false;
        DialogueManager.Instance.Show("你什么态度啊？滚远点", () => d1 = true, "舍友");
        yield return new WaitUntil(() => d1 || this == null);
        if (this == null) yield break;

        bool d2 = false;
        DialogueManager.Instance.Show("呃……居然被骂了。算了我也不差这次签到！", () => d2 = true);
        yield return new WaitUntil(() => d2 || this == null);
        if (this == null) yield break;

        PlayerStats.Instance.ChangeMood(-10f);
        PlayerStats.Instance.ChangeSocial(-5f);
        PlayerStats.Instance.AddAcademic(-5f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 -10\n人际关系 -5\n学业 -5", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;

        GoToDorm();
    }

    IEnumerator RoommateAgree()
    {
        bool d1 = false;
        DialogueManager.Instance.Show("好的", () => d1 = true, "舍友");
        yield return new WaitUntil(() => d1 || this == null);
        if (this == null) yield break;

        bool d2 = false;
        DialogueManager.Instance.Show("太好了！爽玩一早上！", () => d2 = true);
        yield return new WaitUntil(() => d2 || this == null);
        if (this == null) yield break;

        PlayerStats.Instance.ChangeMood(5f);
        PlayerStats.Instance.ChangeSocial(1f);
        PlayerStats.Instance.AddAcademic(5f);
        bool pop = false;
        DialogueManager.Instance.ShowStatPopup("心情 +5\n人际关系 +1\n学业 +5", () => pop = true);
        yield return new WaitUntil(() => pop || this == null);
        if (this == null) yield break;

        GoToDorm();
    }

    // ==================== 第二天起：随机事件 ====================

    IEnumerator PlayNormalDayWakeup()
    {
        yield return StartCoroutine(ShowYesterdayResults());
        if (this == null) yield break;

        bool eventFinished = false;
        bool eventTriggered = false;

        System.Action<GameEventData> listener = (evt) =>
        {
            eventTriggered = true;
            eventFinished = true;
        };

        EventManager.OnEventFinished += listener;
        EventManager.Instance?.TriggerDailyEvents(EventTiming.Morning);

        yield return null;

        if (!eventTriggered)
        {
            EventManager.OnEventFinished -= listener;
            GoToDorm();
            yield break;
        }

        eventFinished = false;
        yield return new WaitUntil(() => eventFinished || this == null);
        EventManager.OnEventFinished -= listener;

        if (this != null) GoToDorm();
    }

    // ==================== 工具 ====================

    void GoToDorm()
    {
        wakeUpSuccess = true;
        HideStatusText();
        DialogueManager.Instance?.Hide();
        GameManager.Instance.OnPlayerWokeUp();
    }

    void SetStatus(string msg)
    {
        if (statusText == null) return;

        if (!statusText.gameObject.activeSelf)
            statusText.gameObject.SetActive(true);
        statusText.text = msg;
    }

    void HideStatusText()
    {
        if (statusText == null) return;

        statusText.text = string.Empty;
        statusText.gameObject.SetActive(false);
    }

    void HideGroggyUI()
    {
        if (groggProgressBar != null)
        {
            groggProgressBar.value = 0f;
            groggProgressBar.gameObject.SetActive(false);
        }

        if (clickCountText != null)
        {
            clickCountText.text = string.Empty;
            clickCountText.gameObject.SetActive(false);
        }
    }

    void SetButtonText(string txt)
    {
        if (wakeUpButton == null) return;
        var tmp = wakeUpButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = txt;
    }

    void SetOverlayAlpha(float alpha)
    {
        if (screenOverlay == null) return;
        var c = screenOverlay.color;
        screenOverlay.color = new Color(c.r, c.g, c.b, alpha);
        screenOverlay.raycastTarget = alpha > 0.5f;
    }

    IEnumerator PlaySleepMoreBlackTransition()
    {
        yield return StartCoroutine(FadeOverlay(1f, sleepMoreFadeDuration));
        yield return new WaitForSeconds(sleepMoreBlackHoldDuration);
        yield return StartCoroutine(FadeOverlay(0f, sleepMoreFadeDuration));
    }

    IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (screenOverlay == null)
            yield break;

        float startAlpha = screenOverlay.color.a;
        if (duration <= 0f)
        {
            SetOverlayAlpha(targetAlpha);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, p);
            SetOverlayAlpha(Mathf.Lerp(startAlpha, targetAlpha, eased));
            yield return null;
        }

        SetOverlayAlpha(targetAlpha);
    }

    void MoveTimeTo(float targetHour)
    {
        if (TimeManager.Instance == null) return;

        TimeManager.Instance.SetTime(Mathf.Max(TimeManager.Instance.gameHour, targetHour));
    }

    float GetCurrentHour()
    {
        return TimeManager.Instance != null ? TimeManager.Instance.gameHour : 0f;
    }

    string GetCurrentTimeText()
    {
        return TimeManager.Instance != null ? TimeManager.Instance.GetFormattedTime() : "--:--";
    }

    string GetWakeUpStatusText()
    {
        PlayerStats stats = PlayerStats.Instance;
        if (stats == null || !stats.alarmSet) return "自然醒了……";

        float currentHour = GetCurrentHour();
        if (currentHour > stats.alarmTime + 0.01f) return "睡过头了，快起床！";

        return "闹钟响了，起床！";
    }

    void ChangeFatigueSafely(float delta)
    {
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.ChangeFatigue(delta);
        PlayerStats.Instance.RecalculateHealth();
    }

    /*
    public void ClearWakeUpText()
    {
        Debug.Log($"[Clear] 游戏对象：{gameObject.name}，实例ID：{GetInstanceID()}");
        Debug.Log($"[Clear] 清空前文字：\"{statusText.text}\"");
        statusText.text = "";
        Debug.Log($"[Clear] 清空后文字：\"{statusText.text}\"");
    }
    */
}
