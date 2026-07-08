using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AcademicBuildingInteraction : MonoBehaviour
{
    [Header("登楼方式选择按钮")]
    public Button walkStairsButton;  // 走楼梯
    public Button runStairsButton;   // 跑楼梯
    public Button elevatorButton;    // 坐电梯

    [Header("疲惫值消耗设置（可在Inspector自由调整）")]
    public float walkFatigueCost = 10f;
    public float runFatigueCost = 35f;
    public float elevatorNormalFatigueCost = 0f;
    public float elevatorCongestedFatigueCost = 5f; // 拥堵站着等可能微耗体力

    [Header("一句话对话框组件")]
    public GameObject dialogueBox;   // 简易对话框物体
    public Text dialogueText;         // 对话框文本组件

    private bool hasFinalChosen = false;       // 标记是否已经完成了最终结算
    private bool elevatorWarningActive = false; // 标记是否正处于电梯拥堵警告状态

    void Start()
    {
        // 1. 动态绑定按钮事件
        if (walkStairsButton != null) walkStairsButton.onClick.AddListener(OnWalkClicked);
        if (runStairsButton != null) runStairsButton.onClick.AddListener(OnRunClicked);
        if (elevatorButton != null) elevatorButton.onClick.AddListener(OnElevatorClicked);

        // 2. 初始时隐藏对话框
        if (dialogueBox != null) dialogueBox.SetActive(false);
    }

    private void OnWalkClicked()
    {
        // 走楼梯：6分钟
        ExecuteMovement(6, walkFatigueCost, "一步一个台阶地走上楼，虽然有点腿酸，但胜在稳健。", true);
    }

    private void OnRunClicked()
    {
        // 跑楼梯：3分钟
        ExecuteMovement(3, runStairsButtonCostFix(), "一口气狂飙冲上高楼层，感觉心肺都要炸了，疯狂喘粗气！", true);
    }

    private void OnElevatorClicked()
    {
        if (TimeManager.Instance == null) return;

        // 🚨 核心逻辑：判断当前游戏时间是否在 7:50 之后
        // 7小时 + (50分钟 / 60分钟) = 7.83333f
        float rushHourStart = 7f + (50f / 60f);

        if (TimeManager.Instance.gameHour >= rushHourStart)
        {
            // 如果是在7:50之后，且玩家还没有被警告过
            if (!elevatorWarningActive)
            {
                elevatorWarningActive = true;

                // 弹出拥堵警告，注意最后的参数是 false，代表看完对白后“不跳转场景”，让玩家重新选
                string warningStr = "糟糕！电梯口挤满了赶早八的人，等一趟居然要10分钟！我是硬等，还是改走楼梯或跑步？";
                ShowSingleLineDialogue(warningStr, false);
                return;
            }
            else
            {
                // 如果已经被警告过，玩家依然执意第二次点击电梯：触发 10 分钟拥堵惩罚
                ExecuteMovement(10, elevatorCongestedFatigueCost, "硬是在电梯门口挤了10分钟才上去，早八要顶不住了！", true);
            }
        }
        else
        {
            // 7:50之前的黄金时间：1分钟直接上楼
            ExecuteMovement(1, elevatorNormalFatigueCost, "大厅空无一人，丝滑地坐上电梯，1分钟轻松直达教室层。", true);
        }
    }

    /// <summary>
    /// 统一执行时间、属性扣除，并控制是否转场
    /// </summary>
    private void ExecuteMovement(int minutesToSpend, float targetFatigueAdd, string reviewText, bool shouldTransition)
    {
        if (hasFinalChosen) return;

        if (shouldTransition)
        {
            hasFinalChosen = true;
            SetActionButtonsInteractable(false); // 锁死全部按钮
        }

        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();

        // 1. 核心时间单位转换并推进
        float hoursToSpend = minutesToSpend / 60f;
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SpendTime(hoursToSpend);

            // 针对 SpendTime 内部自带的 (hours * 3) 疲劳奖励进行差额补齐
            float autoFatigueAdded = hoursToSpend * 3f;
            float missingFatigue = targetFatigueAdd - autoFatigueAdded;

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.AddFatigue(missingFatigue);
            }

            Debug.Log($"[登楼系统] 消耗时间: {minutesToSpend} 分钟，总计追加疲劳: {targetFatigueAdd}");
        }

        // 2. 显示对应的一句话反馈
        ShowSingleLineDialogue(reviewText, shouldTransition, StatsChangeSummary.Build(beforeStats));
    }

    private void ShowSingleLineDialogue(string text, bool willLoadScene, string statText = null)
    {
        StartCoroutine(WaitForDismiss(text, willLoadScene, statText));
    }

    private IEnumerator WaitForDismiss(string text, bool willLoadScene, string statText)
    {
        if (dialogueBox != null) dialogueBox.SetActive(false);
        yield return StartCoroutine(BlackScreenText.Play(this, text));

        bool statDone = false;
        StatsChangeSummary.Show(statText, () => statDone = true);
        yield return new WaitUntil(() => statDone || this == null);
        if (this == null) yield break;

        // 🚨 判断是直接进教室，还是留在原地重新选
        if (willLoadScene)
        {
            LoadClassroomScene();
        }
        else
        {
            Debug.Log("[登楼系统] 拥堵警告结束，玩家现在可以重新在界面上点击‘走楼梯’或‘跑楼梯’。");
        }
    }

    private void LoadClassroomScene()
    {
        string targetScene = "Classroom";
        Debug.Log($"[登楼系统] 最终抉择完成，直接加载场景: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    private void SetActionButtonsInteractable(bool state)
    {
        if (walkStairsButton != null) walkStairsButton.interactable = state;
        if (runStairsButton != null) runStairsButton.interactable = state;
        if (elevatorButton != null) elevatorButton.interactable = state;
    }

    // 辅助防报错小工具
    private float runStairsButtonCostFix() => runFatigueCost;
}
