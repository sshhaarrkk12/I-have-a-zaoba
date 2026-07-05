using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEditor.UI;

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

    void Start()
    {
        //呱：开始先隐藏遮罩
        mask.gameObject.SetActive(false);

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

        // 锁定所有交互按钮
        SetActionButtonsInteractable(false);

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

        

        // 4. 显示对应的一句话反馈
        ShowSingleLineDialogue(reviewText);
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

        // 等待玩家点击鼠标左键或触摸屏幕
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        if (dialogueBox != null) dialogueBox.SetActive(false);

        Debug.Log("[穿衣系统] 对话框关闭，等待玩家通过导航系统前往下一个场景（如：整理书包/出门）。");
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (casualButton != null)
        {
            casualButton.interactable = state;
            casualButton.gameObject.SetActive(state);
            StartMask(state);
        }
        if (weatherButton != null) 
        {
            weatherButton.interactable = state;
            weatherButton.gameObject.SetActive(state);
            StartMask(state);
        }
        if (gorgeousButton != null)
        {
            gorgeousButton.interactable = state;
            gorgeousButton.gameObject.SetActive(state);
            StartMask(state);
        }

        
    }


    void StartMask(bool buttonState)
    {
        //呱：如果没点击按钮就返回
        if (buttonState == true) return;

        mask.color = new Color(0, 0, 0, 0);
        mask.gameObject.SetActive(!buttonState);
        StartCoroutine(WaitAndFade());

    }

    IEnumerator WaitAndFade()
    {
        //呱:渐显黑幕
        yield return Emerge(mask,duration);


        //呱：等待字幕显示
        yield return new WaitForSeconds(lastingTime);
        

        //呱：渐隐黑幕
        yield return Vanish(mask, duration);
        

        SetButtonState(button1, true);
        SetButtonState(button2, true);
        SetButtonState(button3, true);
    }

    //呱：渐显
    IEnumerator Emerge(Image image,float duration)
    {
        
        float duringTime = 0;
        while(duringTime < duration)
        {
            duringTime += Time.deltaTime;
            image.color = new Color(0,0,0, duringTime / duration);
            yield return null;
        }

        image.color = new Color(0, 0, 0, 1);
    }

    //呱：渐隐
    IEnumerator Vanish(Image image, float duration)
    {
        float duringTime = 0;
        while (duringTime < duration)
        {
            duringTime += Time.deltaTime;
            image.color = new Color(0, 0, 0, 1-(duringTime / duration));
            yield return null;
        }

        image.color = new Color(0, 0, 0, 0);
        mask.gameObject.SetActive(false);
        dialogueText.text = "";
    }


    void SetButtonState(Button button,bool state)
    {
        button.gameObject.SetActive(state);
    }
}