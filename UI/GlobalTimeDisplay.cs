using UnityEngine;
using TMPro; // 如果你使用的是 TextMeshPro，必须保留这行
// using UnityEngine.UI; // 如果你使用的是普通的 Text，请用这行

public class GlobalTimeDisplay : MonoBehaviour
{
    // 1. 单例实例
    public static GlobalTimeDisplay Instance { get; private set; }

    // ⚠️【关键检查点】：检查这两行是不是不小心被删掉了！
    // 如果你的变量名大小写不同，请改成跟你原代码里一模一样的名字（比如 dayText 或 timeText）
    [Header("UI 文本组件引用")]
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI timeText;

    private void Awake()
    {
        // 2. 完美的单例防重影逻辑
        if (Instance == null)
        {
            Instance = this;
            
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // 后面重复生成的直接连物体一起销毁
            return; // 结束执行，防止报错
        }
    }
    private void Start()
    {
        // 【修改点1】：这里改成 TimeManager.Instance.gameHour
        if (TimeManager.Instance != null)
        {
            UpdateTimeUI(TimeManager.Instance.gameHour);
        }
    }

    private void OnEnable()
    {
        TimeManager.OnTimeChanged += UpdateTimeUI;
    }

    private void OnDisable()
    {
        TimeManager.OnTimeChanged -= UpdateTimeUI;
    }

    private void UpdateTimeUI(float currentTime)
    {
        // 1. 更新日期 
        // 【修改点2】：天数应该从 PlayerStats.Instance.currentDay 获取
        if (dayText != null && PlayerStats.Instance != null)
        {
            // 你可以根据喜好修改前缀，比如改成 $"Day {PlayerStats.Instance.currentDay}"
            dayText.text = "第 " + PlayerStats.Instance.currentDay + " 天";
        }

        // 2. 更新时间 (将浮点数转换成 08:30 的格式)
        if (timeText != null)
        {
            int hours = Mathf.FloorToInt(currentTime);
            int minutes = Mathf.FloorToInt((currentTime - hours) * 60);

            timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
    }
}