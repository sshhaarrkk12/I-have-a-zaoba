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
                reviewText = "刷牙洗脸一条龙，虽然素面朝天，但胜在速度够快，出发！";
                break;
            case 2:
                minutesToSpend = 15;
                moodReward = 10f;
                reviewText = "稍微描个眉、涂个口红，整个人看起来气色好多了。";
                break;
            case 3:
                minutesToSpend = 40;
                moodReward = 20f;
                reviewText = "粉底、眼影、高光、修容……搞定！镜子里的我今天简直完美！";
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
        if (washButton != null) washButton.interactable = state;
        if (lightMakeupButton != null) lightMakeupButton.interactable = state;
        if (fullMakeupButton != null) fullMakeupButton.interactable = state;
    }
}