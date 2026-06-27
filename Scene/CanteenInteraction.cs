using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanteenInteraction : MonoBehaviour
{
    [Header("食物档次选择按钮")]
    public Button lowCostButton;      // 低档（随便吃点）
    public Button midCostButton;      // 中档（标准套餐）
    public Button highCostButton;     // 高档（豪华大餐）

    [Header("一句话对话框组件")]
    public GameObject dialogueBox;    // 简易对话框物体
    public Text dialogueText;         // 对话框文本组件

    private bool hasChosen = false;   // 标记是否已经做出选择

    void Start()
    {
        // 1. 动态绑定三个档次的饮食按钮事件
        if (lowCostButton != null) lowCostButton.onClick.AddListener(() => OnFoodSelected(1));
        if (midCostButton != null) midCostButton.onClick.AddListener(() => OnFoodSelected(2));
        if (highCostButton != null) highCostButton.onClick.AddListener(() => OnFoodSelected(3));

        // 2. 初始时隐藏对话框
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    /// <summary>
    /// 选择不同档次食物的核心业务逻辑
    /// </summary>
    /// <param name="tier">1: 低档, 2: 中档, 3: 高档</param>
    private void OnFoodSelected(int tier)
    {
        if (hasChosen) return; // 防连点拦截
        hasChosen = true;

        // 锁定所有食物按钮，防止重复点击
        SetActionButtonsInteractable(false);

        int minutesToSpend = 0;
        float moodChange = 0f;
        string reviewText = "";

        // 📊 策划案里的数值平衡配置
        switch (tier)
        {
            case 1:
                minutesToSpend = 10;
                moodChange = -5f;      // 心情下降                reviewText = "随便打了一份剩菜凑活了一下，钱包没怎么缩水，但肚子和心情都有些空落落的。";
                break;
            case 2:
                minutesToSpend = 20;
                moodChange = 5f;       // 心情上升
                reviewText = "点了食堂招牌的双拼饭，味道中规中矩，吃完感觉整个人都恢复了元气。";
                break;
            case 3:
                minutesToSpend = 35;
                moodChange = 20f;      // 极度快乐
                reviewText = "今天大饱口福！去二楼开了个豪华小灶，虽然钱包在流血，但这也太好吃了巴适！";
                break;
        }

        // 🚨 1. 核心时间单位转换 (分钟 -> 小时小数)
        float hoursToSpend = minutesToSpend / 60f;

        // 🚨 2. 推进时间（使用 SpendTime，自动追加疲劳结算）
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SpendTime(hoursToSpend);
        }

        // 🚨 3. 改变玩家底层数值（金钱、心情、健康）
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ChangeMood(moodChange);

            Debug.Log($"[食堂系统] 购买成功：消耗时间 {minutesToSpend} 分钟");
        }

        // 4. 显示对应的反馈文本
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

    /// <summary>
    /// 仅关闭对话框，留在当前场景不跳转
    /// </summary>
    private IEnumerator WaitForDismissDialogue()
    {
        yield return new WaitForSeconds(0.3f);

        // 等待玩家鼠标点击确认这一句话
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        // 隐去对话框
        if (dialogueBox != null) dialogueBox.SetActive(false);

        Debug.Log("[食堂系统] 对话框已关闭。不执行任何转场逻辑，原地等待玩家点击侧边栏或地图上的场景按钮。");
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (lowCostButton != null) lowCostButton.interactable = state;
        if (midCostButton != null) midCostButton.interactable = state;
        if (highCostButton != null) highCostButton.interactable = state;
    }
}