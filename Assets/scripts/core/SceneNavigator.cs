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

    void Start()
    {
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
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            Debug.Log($"[Navigator] 跳转到 {sceneName}");
            SceneManager.LoadScene(sceneName);
        });
    }
}