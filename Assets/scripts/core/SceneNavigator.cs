using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    [Header("时间显示（可选）")]
    public TextMeshProUGUI timeText;

    [Header("场景跳转按钮（不需要的留空）")]
    public Button toWakeUp;
    public Button toDormHub;
    public Button toWashing;
    public Button toDressing;
    public Button toPacking;
    public Button toGoOut;
    public Button toCorridor;
    public Button toCanteen;
    public Button toClassroom;
    public Button toPhoneUI;
    public Button toDressUP; 
    public Button toBathroom;

    bool navigationLocked = false;

    void Start()
    {
        MorningRoutineState.SyncDay();

        Bind(toWakeUp, "Wakeup");
        Bind(toDormHub, "DormHub");
        Bind(toWashing, "Washing");
        Bind(toDressing, "Dressing");
        Bind(toPacking, "Packing");
        Bind(toGoOut, "GoOut");
        Bind(toCorridor, "Corridor");
        Bind(toCanteen, "Canteen");
        Bind(toClassroom, "Classroom");
        Bind(toPhoneUI, "PhoneUI");
        Bind(toBathroom, "Bathroom");

        if (timeText != null)
        {
            TimeManager.OnTimeChanged += UpdateTime;
            UpdateTime(TimeManager.Instance != null ? TimeManager.Instance.gameHour : 0f);
        }
    }

    void OnDestroy()
    {
        if (timeText != null)
            TimeManager.OnTimeChanged -= UpdateTime;
    }

    void UpdateTime(float h)
    {
        if (timeText != null && TimeManager.Instance != null)
            timeText.text = TimeManager.Instance.GetFormattedTime();
    }

    void Bind(Button btn, string sceneName)
    {
        if (btn == null) return;

        btn.interactable = !MorningRoutineState.IsDone(sceneName);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (navigationLocked || MorningRoutineState.IsDone(sceneName)) return;

            navigationLocked = true;
            btn.interactable = false;
            if (sceneName != "Bathroom")
                MorningRoutineState.MarkDone(sceneName);
            SceneStateManager.Instance?.SetCurrentScene(sceneName);

            Debug.Log($"[Navigator] 跳转到 {sceneName}");
            SceneManager.LoadScene(sceneName);
        });
    }
}

static class MorningRoutineState
{
    static int activeDay = -1;

    public static bool WashingDone { get; private set; }
    public static bool DressingDone { get; private set; }
    public static bool PackingDone { get; private set; }
    public static bool BathroomDone { get; private set; }

    public static void SyncDay()
    {
        int day = PlayerStats.Instance != null ? PlayerStats.Instance.currentDay : 1;
        if (activeDay == day) return;

        activeDay = day;
        WashingDone = false;
        DressingDone = false;
        PackingDone = false;
        BathroomDone = false;
    }

    public static bool IsDone(string sceneName)
    {
        SyncDay();

        switch (sceneName)
        {
            case "Washing": return WashingDone;
            case "Dressing": return DressingDone;
            case "Packing": return PackingDone;
            case "Bathroom": return BathroomDone;
            default: return false;
        }
    }

    public static void MarkDone(string sceneName)
    {
        SyncDay();

        switch (sceneName)
        {
            case "Washing":
                WashingDone = true;
                break;
            case "Dressing":
                DressingDone = true;
                break;
            case "Packing":
                PackingDone = true;
                break;
            case "Bathroom":
                BathroomDone = true;
                break;
        }
    }
}
