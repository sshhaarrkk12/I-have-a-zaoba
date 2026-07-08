using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 手机系统管理器
/// 挂在 _Managers 上
/// Tab键 或 场景内手机按钮 → 打开/关闭
/// </summary>
public class PhoneManager : MonoBehaviour
{
    public static PhoneManager Instance { get; private set; }

    [Header("手机根Canvas")]
    public Canvas phoneCanvas;

    [Header("各页面Panel（在Inspector拖入，默认全部隐藏）")]
    public GameObject homeScreen;
    public GameObject socialPanel;
    public GameObject checkinPanel;
    public GameObject shoppingPanel;
    public GameObject entertainPanel;
    public GameObject schedulePanel;
    public GameObject alarmPanel;

    [Header("状态栏")]
    public TextMeshProUGUI statusBarTime;

    [Header("桌面 - 闹钟预览")]
    public TextMeshProUGUI alarmPreviewText;   // 显示"明日闹钟 07:30"

    [Header("闹钟面板")]
    public Toggle alarmToggle;
    public Slider alarmHourSlider;             // min=5 max=9
    public Slider alarmMinuteSlider;           // min=0 max=59 step=5
    public TextMeshProUGUI alarmTimeDisplay;   // 显示当前设置的时间
    public Button alarmConfirmBtn;

    [Header("签到面板")]
    public TextMeshProUGUI checkinStatusText;
    public Button checkinBtn;

    [Header("快捷键")]
    public KeyCode toggleKey = KeyCode.Tab;

    public bool IsOpen => isOpen;
    bool isOpen = false;
    bool checkedInToday = false;
    GameObject currentPanel;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (phoneCanvas != null)
            DontDestroyOnLoad(phoneCanvas.gameObject);
    }

    void Start()
    {
        // 初始化闹钟滑块
        if (alarmHourSlider != null)
        {
            alarmHourSlider.minValue = 5; alarmHourSlider.maxValue = 9;
            alarmHourSlider.value = (int)PlayerStats.Instance.alarmTime;
            alarmHourSlider.wholeNumbers = true;
            alarmHourSlider.onValueChanged.AddListener(_ => RefreshAlarmDisplay());
        }
        if (alarmMinuteSlider != null)
        {
            alarmMinuteSlider.minValue = 0; alarmMinuteSlider.maxValue = 55;
            alarmMinuteSlider.value = 30; alarmMinuteSlider.wholeNumbers = true;
            alarmMinuteSlider.onValueChanged.AddListener(_ => RefreshAlarmDisplay());
        }
        if (alarmToggle != null)
            alarmToggle.isOn = PlayerStats.Instance.alarmSet;
        if (alarmConfirmBtn != null)
            alarmConfirmBtn.onClick.AddListener(SaveAlarm);
        if (checkinBtn != null)
            checkinBtn.onClick.AddListener(DoCheckin);

        PlayerStats.OnDayEnd += OnNewDay;
        ClosePhone();
    }

    void OnDestroy() => PlayerStats.OnDayEnd -= OnNewDay;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) TogglePhone();
        if (isOpen && statusBarTime != null && TimeManager.Instance != null)
            statusBarTime.text = TimeManager.Instance.GetFormattedTime();
    }

    // ==================== 开关手机 ====================

    public void TogglePhone() { if (isOpen) ClosePhone(); else OpenPhone(); }

    public void OpenPhone()
    {
        isOpen = true;
        phoneCanvas.gameObject.SetActive(true);
        if (TimeManager.Instance != null) TimeManager.Instance.isPaused = true;
        ShowHome();
        DialogueManager.Instance?.RefreshClassCountdown();
    }

    public void ClosePhone()
    {
        isOpen = false;
        if (phoneCanvas != null) phoneCanvas.gameObject.SetActive(false);
        if (TimeManager.Instance != null) TimeManager.Instance.isPaused = false;
        DialogueManager.Instance?.RefreshClassCountdown();
    }

    // ==================== 页面切换 ====================

    public void ShowHome() => Show(homeScreen);
    public void ShowSocial() => Show(socialPanel);
    public void ShowCheckin() { Show(checkinPanel); RefreshCheckin(); }
    public void ShowShopping() => Show(shoppingPanel);
    public void ShowEntertain() => Show(entertainPanel);
    public void ShowSchedule() => Show(schedulePanel);
    public void ShowAlarm() { Show(alarmPanel); RefreshAlarmDisplay(); }

    void Show(GameObject panel)
    {
        HideAll();
        if (panel != null) panel.SetActive(true);
        currentPanel = panel;
        DialogueManager.Instance?.RefreshClassCountdown();
    }

    void HideAll()
    {
        SetActive(homeScreen, false);
        SetActive(socialPanel, false);
        SetActive(checkinPanel, false);
        SetActive(shoppingPanel, false);
        SetActive(entertainPanel, false);
        SetActive(schedulePanel, false);
        SetActive(alarmPanel, false);
    }

    void SetActive(GameObject go, bool v) { if (go != null) go.SetActive(v); }

    // ==================== 闹钟 ====================

    void RefreshAlarmDisplay()
    {
        int h = alarmHourSlider != null ? (int)alarmHourSlider.value : 7;
        int m = alarmMinuteSlider != null ? (int)alarmMinuteSlider.value : 30;
        if (alarmTimeDisplay != null)
            alarmTimeDisplay.text = $"{h:D2}:{m:D2}";
        if (alarmPreviewText != null)
            alarmPreviewText.text = $"明日闹钟  {h:D2}:{m:D2}";
    }

    void SaveAlarm()
    {
        int h = alarmHourSlider != null ? (int)alarmHourSlider.value : 7;
        int m = alarmMinuteSlider != null ? (int)alarmMinuteSlider.value : 30;
        float alarmTime = h + m / 60f;

        PlayerStats.Instance.alarmSet = alarmToggle != null && alarmToggle.isOn;
        PlayerStats.Instance.alarmTime = alarmTime;

        RefreshAlarmDisplay();
        if (alarmTimeDisplay != null)
            alarmTimeDisplay.text = $"{h:D2}:{m:D2}  已保存 ✓";

        Debug.Log($"[Phone] 闹钟设置：{h:D2}:{m:D2} 开启={PlayerStats.Instance.alarmSet}");
    }

    // ==================== 签到 ====================

    void RefreshCheckin()
    {
        if (checkinStatusText == null) return;
        checkinStatusText.text = checkedInToday ? "今日已签到 ✓" : "今天还没签到哦";
        if (checkinBtn != null) checkinBtn.interactable = !checkedInToday;
    }

    void DoCheckin()
    {
        if (checkedInToday) return;
        checkedInToday = true;
        PlayerStats.Instance.ChangeMood(5f);
        PlayerStats.Instance.AddStress(-3f);
        RefreshCheckin();
        Debug.Log("[Phone] 签到成功 心情+5 压力-3");
    }

    void OnNewDay() => checkedInToday = false;
}
