using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// 走廊/室外移动场景
/// 处理：走路 vs 跑步，楼梯体力消耗，到达目的地
/// </summary>
public class CorridorSceneManager : MonoBehaviour
{
    public enum Destination { Classroom, Canteen, Dorm }

    [Header("UI")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI destinationText;
    public Slider staminaBar;
    public Button walkButton;
    public Button runButton;
    public Button goUpStairsButton;
    public Button takeElevatorButton;
    public StatsHUD statsHUD;

    [Header("移动设置")]
    public Destination destination = Destination.Classroom;
    public float walkTimeMinutes = 8f;    // 走路时间（分钟）
    public float runTimeMinutes = 4f;     // 跑步时间（分钟）
    public float stairsFloors = 4f;       // 教学楼层数

    [Header("动画")]
    public Transform characterSprite;
    public Transform destinationMarker;

    private bool isMoving = false;
    private bool isRunning = false;
    private Coroutine moveRoutine;

    void Start()
    {
        destinationText.text = GetDestinationName();
        UpdateUI();
    }

    string GetDestinationName()
    {
        switch (destination)
        {
            case Destination.Classroom: return "教室 (4楼)";
            case Destination.Canteen: return "食堂";
            case Destination.Dorm: return "宿舍";
            default: return "未知";
        }
    }

    // ==================== 移动选择 ====================

    public void ChooseWalk()
    {
        isRunning = false;
        statusText.text = "慢悠悠地走过去……";
        StartMoving();
    }

    public void ChooseRun()
    {
        if (PlayerStats.Instance.instantStamina < 15f)
        {
            statusText.text = "体力不足，跑不动了……";
            return;
        }
        isRunning = true;
        statusText.text = "快跑！！！";
        StartMoving();
    }

    void StartMoving()
    {
        if (isMoving) return;
        isMoving = true;
        walkButton.interactable = false;
        runButton.interactable = false;

        // 判断是否需要上楼
        if (destination == Destination.Classroom)
        {
            ShowStairsChoice();
        }
        else
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveToDestination());
        }
    }

    // ==================== 楼梯/电梯选择 ====================

    void ShowStairsChoice()
    {
        goUpStairsButton.gameObject.SetActive(true);
        takeElevatorButton.gameObject.SetActive(true);
        statusText.text = "到楼梯口了，怎么上去？";

        goUpStairsButton.onClick.RemoveAllListeners();
        goUpStairsButton.onClick.AddListener(TakeStairs);
        takeElevatorButton.onClick.RemoveAllListeners();
        takeElevatorButton.onClick.AddListener(TakeElevator);
    }

    public void TakeStairs()
    {
        goUpStairsButton.gameObject.SetActive(false);
        takeElevatorButton.gameObject.SetActive(false);

        // 爬楼消耗体力（跑上去更多）
        float staminaCost = isRunning ? stairsFloors * 6f : stairsFloors * 3f;
        PlayerStats.Instance.ConsumeInstantStamina(staminaCost);
        PlayerStats.Instance.AddFatigue(stairsFloors * 1.5f);

        statusText.text = isRunning ? "拼命爬楼……气喘吁吁" : "一步一步爬上去……";

        // 喘气动画
        characterSprite.DOShakePosition(0.5f, 5f).OnComplete(() =>
        {
            StartCoroutine(MoveToDestination());
        });
    }

    public void TakeElevator()
    {
        goUpStairsButton.gameObject.SetActive(false);
        takeElevatorButton.gameObject.SetActive(false);

        // 电梯额外花1~3分钟等待
        float waitTime = Random.Range(1f, 3f) / 60f;
        TimeManager.Instance.SpendTime(waitTime);
        statusText.text = "等电梯……";

        StartCoroutine(MoveToDestination());
    }

    // ==================== 移动协程 ====================

    IEnumerator MoveToDestination()
    {
        var stats = PlayerStats.Instance;
        float speed = stats.GetMovementSpeed(isRunning);
        float baseTime = isRunning ? runTimeMinutes : walkTimeMinutes;

        // 速度越低花的时间越多
        float actualTime = baseTime / speed;
        float gameHours = actualTime / 60f;

        // 角色移动动画
        float dist = Vector3.Distance(characterSprite.position, destinationMarker.position);
        characterSprite.DOMove(destinationMarker.position, actualTime * 0.5f);

        // 移动期间消耗体力
        float elapsed = 0f;
        while (elapsed < actualTime)
        {
            elapsed += Time.deltaTime;
            float staminaDrain = isRunning ? 0.05f : 0.01f;
            stats.ConsumeInstantStamina(staminaDrain * Time.deltaTime);
            staminaBar.value = stats.instantStamina / 100f;
            timeText.text = TimeManager.Instance.GetFormattedTime();
            yield return null;
        }

        // 消耗时间
        TimeManager.Instance.SpendTime(gameHours);

        // 跑步心情小惩罚（赶时间的痛苦）
        if (isRunning)
        {
            stats.AddStress(5f);
            stats.AddFatigue(5f);
        }

        OnArrived();
    }

    void OnArrived()
    {
        statusText.text = $"到达{GetDestinationName()}！";
        PlayerStats.Instance.RecoverInstantStamina(5f); // 到达后稍微喘口气

        StartCoroutine(DelayedTransition(1.5f));
    }

    IEnumerator DelayedTransition(float delay)
    {
        yield return new WaitForSeconds(delay);
        switch (destination)
        {
            case Destination.Classroom: GameManager.Instance.GoToClassroom(); break;
            case Destination.Canteen: GameManager.Instance.GoToCanteen(); break;
            case Destination.Dorm: GameManager.Instance.GoToDorm(); break;
        }
    }

    void UpdateUI()
    {
        staminaBar.value = PlayerStats.Instance.instantStamina / 100f;
    }
}