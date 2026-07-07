using UnityEngine;
using TMPro;

public class GlobalTimeDisplay : MonoBehaviour
{
    public static GlobalTimeDisplay Instance { get; private set; }

    [Header("UI Text References")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
            Destroy(gameObject);
    }

    void OnEnable()
    {
        TimeManager.OnTimeChanged += UpdateTimeUI;
        RefreshFromTimeManager();
    }

    void Start()
    {
        RefreshFromTimeManager();
    }

    void OnDisable()
    {
        TimeManager.OnTimeChanged -= UpdateTimeUI;
    }

    void RefreshFromTimeManager()
    {
        if (TimeManager.Instance != null)
            UpdateTimeUI(TimeManager.Instance.gameHour);
    }

    void UpdateTimeUI(float currentTime)
    {
        if (dayText != null && PlayerStats.Instance != null)
            dayText.text = $"\u7b2c {PlayerStats.Instance.currentDay} \u5929";

        if (timeText != null)
            timeText.text = FormatTime(currentTime);
    }

    static string FormatTime(float hour)
    {
        int totalMinutes = Mathf.RoundToInt(hour * 60f);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }
}
