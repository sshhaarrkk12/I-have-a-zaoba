using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DressingManager : MonoBehaviour
{
    [Header("UI 按钮组件")]
    public Button casualButton;      // 随便穿
    public Button weatherButton;     // 根据天气挑
    public Button gorgeousButton;    // 精心打扮

    [Header("一句话对话框组件")]
    public GameObject dialogueBox;   // 简易对话框物体
    public Text dialogueText;        // 对话框文本组件

    private bool hasChosen = false;  // 标记是否已经选过


    //呱：进行选项对应字幕演出的补丁
    [Header("遮罩")]
    [SerializeField] private Image mask;
    [SerializeField] private float lastingTime = 2f;
    [SerializeField] private float duration = 0.5f;

    //呱：管理神秘的三个按钮
    [Tooltip("这里请拖入三个过场选择按钮")]
    [Header("按钮")]
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    private Coroutine transitionRoutine;

    void Start()
    {
        //呱：开始先隐藏遮罩
        if (mask != null) mask.gameObject.SetActive(false);

        SetButtonState(button1,false);
        SetButtonState(button2, false);
        SetButtonState(button3, false);

        // 1. 绑定按钮点击事件
        if (casualButton != null) casualButton.onClick.AddListener(() => OnOptionSelected(1));
        if (weatherButton != null) weatherButton.onClick.AddListener(() => OnOptionSelected(2));
        if (gorgeousButton != null) gorgeousButton.onClick.AddListener(() => OnOptionSelected(3));

        // 2. 初始时隐藏对话框
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    /// <summary>
    /// 选择不同穿衣风格的核心逻辑
    /// </summary>
    /// <param name="optionType">1: 随便穿, 2: 根据天气挑, 3: 精心打扮</param>
    private void OnOptionSelected(int optionType)
    {
        if (hasChosen) return; // 拦截连点
        hasChosen = true;
        MorningRoutineState.MarkDone("Dressing");

        // 锁定所有交互按钮
        SetActionButtonsInteractable(false);
        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();

        int minutesToSpend = 0;
        float moodReward = 0f;
        float healthReward = 0f;
        string reviewText = "";

        switch (optionType)
        {
            case 1:
                minutesToSpend = 3;
                moodReward = 0f;

                // 🚨 策划案14底层硬核博弈：随便穿有 50% 概率和当天天气不匹配
                if (Random.value < 0.5f)
                {
                    healthReward = 5f;
                    reviewText = "今天随便挑了一套，\n没想到歪打正着，\n体感很舒服！";
                    moodReward = 2f; // 穿对了追加2点小开心
                }
                else
                {
                    healthReward = -5f;
                    reviewText = "今天随便穿了一套就出门了。\n结果外面风大得很，冻得我直打哆嗦……";
                }
                break;

            case 2:
                minutesToSpend = 5;
                moodReward = 5f;
                healthReward = 5f; // 看了天气预报，稳稳获得健康加成
                reviewText = "看了一下天气预报，\n今天温度挺合适的，\n保暖最重要。";
                break;

            case 3:
                minutesToSpend = 15;
                moodReward = 15f;
                healthReward = 0f;
                reviewText = "衣柜里的衣服试了又试，\n配了一身超级好看的穿搭！";
                break;
        }

        // 🚨 核心时间转换：分钟 / 60f 换算为 TimeManager 识别的小时单位
        float hoursToSpend = minutesToSpend / 60f;

        // 1. 推动时间（调用 SpendTime 顺便计算疲劳）
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SpendTime(hoursToSpend);
            Debug.Log($"[穿衣系统] 消耗 {minutesToSpend} 分钟，当前时间: {TimeManager.Instance.GetFormattedTime()}");
        }

        // 2. 更新心情属性
        if (PlayerStats.Instance != null && moodReward != 0)
        {
            PlayerStats.Instance.ChangeMood(moodReward);
        }

        if (PlayerStats.Instance != null && healthReward != 0)
        {
            PlayerStats.Instance.health = Mathf.Clamp(PlayerStats.Instance.health + healthReward, 0f, 100f);
        }

        // 4. 显示对应的一句话反馈
        ShowSingleLineDialogue(reviewText, StatsChangeSummary.Build(beforeStats));
    }

    private void ShowSingleLineDialogue(string text, string statText)
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(PlayBlackTextSequence(text, statText));
    }

    private IEnumerator PlayBlackTextSequence(string text, string statText)
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        yield return StartCoroutine(BlackScreenText.Play(this, text, duration, lastingTime));

        StatsChangeSummary.Show(statText);

        SetButtonState(button1, true);
        SetButtonState(button2, true);
        SetButtonState(button3, true);

        transitionRoutine = null;
        Debug.Log("[穿衣系统] 对话框关闭，等待玩家通过导航系统前往下一个场景（如：整理书包/出门）。");
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (casualButton != null)
        {
            casualButton.interactable = state;
            casualButton.gameObject.SetActive(state);
        }
        if (weatherButton != null) 
        {
            weatherButton.interactable = state;
            weatherButton.gameObject.SetActive(state);
        }
        if (gorgeousButton != null)
        {
            gorgeousButton.interactable = state;
            gorgeousButton.gameObject.SetActive(state);
        }

        
    }


    IEnumerator FadeMask(float targetAlpha, float fadeDuration)
    {
        if (mask == null) yield break;

        mask.gameObject.SetActive(true);
        float startAlpha = mask.color.a;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, fadeDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            SetMaskAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / safeDuration));
            yield return null;
        }

        SetMaskAlpha(targetAlpha);
        mask.gameObject.SetActive(targetAlpha > 0.01f);
    }

    void SetMaskAlpha(float alpha)
    {
        if (mask == null) return;
        Color color = mask.color;
        mask.color = new Color(color.r, color.g, color.b, alpha);
    }

    void ClearDialogue()
    {
        if (dialogueText != null) dialogueText.text = "";
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    void EnsureDialogueAboveMask()
    {
        if (mask != null) mask.transform.SetAsLastSibling();
        if (dialogueBox != null) dialogueBox.transform.SetAsLastSibling();
    }

    void SetButtonState(Button button,bool state)
    {
        if (button == null) return;
        button.gameObject.SetActive(state);
    }
}
