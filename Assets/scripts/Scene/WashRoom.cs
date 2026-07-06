using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WashroomManager : MonoBehaviour
{
    [Header("UI 按钮组件")]
    public Button washButton;
    public Button lightMakeupButton;
    public Button fullMakeupButton;

    [Header("一句话对话框组件")]
    public GameObject dialogueBox;
    public TextMeshProUGUI dialogueText;

    private bool hasChosen = false;



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
        if (washButton != null) washButton.onClick.AddListener(() => OnOptionSelected(1));
        if (lightMakeupButton != null) lightMakeupButton.onClick.AddListener(() => OnOptionSelected(2));
        if (fullMakeupButton != null) fullMakeupButton.onClick.AddListener(() => OnOptionSelected(3));

        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    private void OnOptionSelected(int optionType)
    {
        if (hasChosen) return;
        hasChosen = true;

        SetActionButtonsInteractable(false);

        int minutesToSpend = 0;
        float moodReward = 0f;
        string reviewText = "";

        switch (optionType)
        {
            case 1:
                minutesToSpend = 5;
                moodReward = 5f;
                reviewText = "刷牙洗脸一条龙，虽然素面朝天，\n但胜在速度够快，出发！";
                break;
            case 2:
                minutesToSpend = 15;
                moodReward = 10f;
                reviewText = "稍微描个眉、涂个口红，\n整个人看起来气色好多了。";
                break;
            case 3:
                minutesToSpend = 40;
                moodReward = 20f;
                reviewText = "粉底、眼影、高光、修容……搞定！\n镜子里的我今天简直完美！";
                break;
        }

        float hoursToSpend = minutesToSpend / 60f;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SpendTime(hoursToSpend);
            Debug.Log($"[洗漱系统] 消耗了 {minutesToSpend} 分钟，当前时间：{TimeManager.Instance.GetFormattedTime()}");
        }
        else
        {
            Debug.LogError("[洗漱系统] 找不到 TimeManager 实例！");
        }

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ChangeMood(moodReward);
        else
            Debug.LogWarning("[洗漱系统] 未找到 PlayerStats 实例。");

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

        while (!Input.GetMouseButtonDown(0))
            yield return null;

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        // 7:30 后触发限定在 Washroom 场景的事件
        if (TimeManager.Instance != null && TimeManager.Instance.gameHour >= 7.5f)
        {
            EventManager.Instance?.TriggerDailyEvents(EventTiming.Any);
        }

        Debug.Log("[洗漱系统] 对话已关闭。");
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (washButton != null)
        {
            washButton.interactable = state;
            washButton.gameObject.SetActive(state);
            StartMask(state);
        }
            
        if (lightMakeupButton != null)
        {
            lightMakeupButton.interactable = state;
            lightMakeupButton.gameObject.SetActive(state);
            StartMask(state);
        }
            
        if (fullMakeupButton != null)
        {
            fullMakeupButton.interactable = state;
            fullMakeupButton.gameObject.SetActive(state);
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
        yield return Emerge(mask, duration);


        //呱：等待字幕显示
        yield return new WaitForSeconds(lastingTime);


        //呱：渐隐黑幕
        yield return Vanish(mask, duration);


        SetButtonState(button1, true);
        SetButtonState(button2, true);
        SetButtonState(button3, true);
    }

    //呱：渐显
    IEnumerator Emerge(Image image, float duration)
    {

        float duringTime = 0;
        while (duringTime < duration)
        {
            duringTime += Time.deltaTime;
            image.color = new Color(0, 0, 0, duringTime / duration);
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
            image.color = new Color(0, 0, 0, 1 - (duringTime / duration));
            yield return null;
        }

        image.color = new Color(0, 0, 0, 0);
        mask.gameObject.SetActive(false);
        dialogueText.text = "";
    }


    void SetButtonState(Button button, bool state)
    {
        button.gameObject.SetActive(state);
    }
}