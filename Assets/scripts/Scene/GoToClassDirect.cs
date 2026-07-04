using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GoToClassDirect : MonoBehaviour
{
    [Header("交通工具选择按钮")]
    public Button walkButton;        // 走路
    public Button bikeButton;        // 骑车
    public Button runButton;         // 跑步

    [Header("一句话对话框组件")]
    public GameObject dialogueBox;   // 简易对话框物体
    public Text dialogueText;        // 对话框文本组件

    private bool hasChosen = false;  // 标记是否已经做出选择

    void Start()
    {
        // 1. 动态绑定交通工具按钮事件
        if (walkButton != null) walkButton.onClick.AddListener(() => OnTransportSelected(1));
        if (bikeButton != null) bikeButton.onClick.AddListener(() => OnTransportSelected(2));
        if (runButton != null) runButton.onClick.AddListener(() => OnTransportSelected(3));

        // 2. 初始时隐藏对话框
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    /// <summary>
    /// 选择不同交通工具的核心业务逻辑
    /// </summary>
    /// <param name="type">1: 走路, 2: 骑车, 3: 跑步</param>
    private void OnTransportSelected(int type)
    {
        if (hasChosen) return; // 防连点拦截
        hasChosen = true;

        // 锁定所有选择按钮，防止玩家在播对白时乱点
        SetActionButtonsInteractable(false);

        int minutesToSpend = 0;
        float targetFatigueAdd = 0f;
        string reviewText = "";

        switch (type)
        {
            case 1:
                minutesToSpend = 20;
                targetFatigueAdd = 20f;
                reviewText = "一路上看着校园的风景，不紧不慢地朝着教学楼走去。";
                break;
            case 2:
                minutesToSpend = 5;
                targetFatigueAdd = 5f;
                reviewText = "踩上脚踏车一路飞驰，风在耳边呼呼作响，速度拉满！";
                break;
            case 3:
                minutesToSpend = 10;
                targetFatigueAdd = 40f;
                reviewText = "坏了要迟到了！我直接开始全速狂奔，感觉肺都要炸了！";
                break;
        }

        // 🚨 1. 核心时间单位转换 (分钟 -> 小时小数)
        float hoursToSpend = minutesToSpend / 60f;

        // 🚨 2. 扣除时间并精准处理疲劳值
        if (TimeManager.Instance != null)
        {
            // 先通过 SpendTime 推进时间
            TimeManager.Instance.SpendTime(hoursToSpend);

            // 针对 SpendTime 内部自带的 (hours * 3) 疲劳奖励进行差额多退少补
            float autoFatigueAdded = hoursToSpend * 3f;
            float missingFatigue = targetFatigueAdd - autoFatigueAdded;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddFatigue(missingFatigue);
            }

            Debug.Log($"[上课系统] 消耗 {minutesToSpend} 分钟，总共增加疲劳：{targetFatigueAdd}");
        }

        // 3. 显示反馈文本并开启链式跳转检测
        ShowSingleLineDialogue(reviewText);
    }

    private void ShowSingleLineDialogue(string text)
    {
        if (dialogueBox != null && dialogueText != null)
        {
            dialogueBox.SetActive(true);
            dialogueText.text = text;
            StartCoroutine(WaitForDismissAndLoadScene());
        }
        else
        {
            // 防御代码：如果你没有挂载对话框 UI，就直接进行转场，防止卡死
            Load教学楼Scene();
        }
    }

    /// <summary>
    /// 核心链式逻辑：玩家看完一句话对白，点击屏幕后“直接跳转”到教室场景
    /// </summary>
    private IEnumerator WaitForDismissAndLoadScene()
    {
        yield return new WaitForSeconds(0.3f);

        // 等待玩家点击屏幕确认这一句话
        while (!Input.GetMouseButtonDown(0))
        {
            yield return null;
        }

        // 关闭对话框（虽然马上切场景了，但顺手关闭是个好习惯）
        if (dialogueBox != null) dialogueBox.SetActive(false);

        // 🚨 4. 直接执行链式跳转！一步到位进入 教学楼
        Load教学楼Scene();
    }

    private void Load教学楼Scene()
    {
        string targetScene = "教学楼";
        Debug.Log($"[上课系统] 链式触发成功！正在直接切换到目标场景: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (walkButton != null) walkButton.interactable = state;
        if (bikeButton != null) bikeButton.interactable = state;
        if (runButton != null) runButton.interactable = state;
    }
}