using UnityEngine;
using UnityEngine.UI;
using TMPro; // ���� TextMeshPro
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class PhoneUIManager : MonoBehaviour
{
    [Header("UI �������� (����)")]
    [Tooltip("������õ��������� _SDF �ļ��ϵ�����")]
    public TMP_FontAsset chineseFont;

    [Header("����UI")]
    public Toggle alarmToggle;
    public Slider alarmTimeSlider;
    public TextMeshProUGUI alarmTimeText;

    [Header("�ճ��б�")]
    public Transform activityListRoot;

    [Header("ͳ������Ϣ")]
    public TextMeshProUGUI totalTimeText;
    public TextMeshProUGUI dayText;
    public TextMeshProUGUI statusText;

    [Header("ȷ�ϰ�ť")]
    public Button confirmButton;

    private List<ActivityData> selectedActivities = new List<ActivityData>();
    private Dictionary<ActivityData, Toggle> activityToggles = new Dictionary<ActivityData, Toggle>();

    void Start()
    {
        Debug.Log("[PhoneUI] Start()");
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PhoneUI] GameManagerΪnull");
            return;
        }

        InitUIState();
        BuildActivityList();

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirm);
        }

        if (statusText != null) statusText.text = "���ź�������ճ̣�Ȼ��ȥ˯����";
    }

    private void InitUIState()
    {
        if (dayText != null && PlayerStats.Instance != null)
            dayText.text = $"Day {PlayerStats.Instance.currentDay} / {PlayerStats.MAX_DAYS}";

        if (alarmTimeSlider != null)
        {
            alarmTimeSlider.minValue = 6f;
            alarmTimeSlider.maxValue = 9f;
            alarmTimeSlider.value = PlayerStats.Instance?.alarmTime ?? 7.5f;
            alarmTimeSlider.onValueChanged.AddListener(UpdateAlarmText);
            UpdateAlarmText(alarmTimeSlider.value);
        }

        if (alarmToggle != null)
            alarmToggle.isOn = PlayerStats.Instance?.alarmSet ?? true;
    }

    void BuildActivityList()
    {
        // 1. ��ȫ��飺����������������ڣ�ֱ����������ֹ����
        if (activityListRoot == null) return;

        var schedule = ScheduleSystem.Instance;
        if (schedule == null || schedule.availableActivities == null)
        {
            Debug.LogWarning("[PhoneUI] ScheduleSystem ���б�δ��ʼ���������б�������");
            return;
        }

        // 2. �����ɶ���
        foreach (Transform child in activityListRoot)
            Destroy(child.gameObject);

        activityToggles.Clear();
        selectedActivities.Clear();

        int day = PlayerStats.Instance?.currentDay ?? 1;

        // 3. �����б������ӶԵ��� activity �� null ���
        foreach (var activity in schedule.availableActivities)
        {
            if (activity == null) continue; // ����������
            if (day < activity.unlockDay) continue;

            var item = CreateActivityItem(activity);
            if (item == null) continue;

            var toggle = item.GetComponentInChildren<Toggle>();
            if (toggle == null) continue;

            toggle.isOn = activity.isFixed;
            toggle.interactable = !activity.isFixed;

            // ��ȫ�����ӵ��б�
            if (activity.isFixed) selectedActivities.Add(activity);

            var captured = activity;
            // ��ǰ����������ֹ�ظ��߼�
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    if (!selectedActivities.Contains(captured)) selectedActivities.Add(captured);
                }
                else
                {
                    selectedActivities.Remove(captured);
                }
                UpdateTotalTime();
            });

            activityToggles[activity] = toggle;
        }

        UpdateTotalTime();
    }

    /// <summary>
    /// �����붯̬���� UI Ԫ��
    /// </summary>
    GameObject CreateActivityItem(ActivityData activity)
    {
        var go = new GameObject(activity.activityId);
        go.transform.SetParent(activityListRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 50);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childForceExpandWidth = false;
        layout.padding = new RectOffset(10, 10, 5, 5);

        var tGo = new GameObject("Toggle");
        tGo.transform.SetParent(go.transform, false);
        var tRT = tGo.AddComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(30, 30);
        var toggle = tGo.AddComponent<Toggle>();

        var bg = new GameObject("BG");
        bg.transform.SetParent(tGo.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.sizeDelta = new Vector2(30, 30);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.35f);
        toggle.targetGraphic = bgImg;

        var ck = new GameObject("Check");
        ck.transform.SetParent(tGo.transform, false);
        var ckRT = ck.AddComponent<RectTransform>();
        ckRT.sizeDelta = new Vector2(22, 22);
        var ckImg = ck.AddComponent<Image>();
        ckImg.color = new Color(0.2f, 0.85f, 0.45f);
        toggle.graphic = ckImg;

        // ====== �����޸Ĳ��� ======
        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRT = txtGo.AddComponent<RectTransform>();
        txtRT.sizeDelta = new Vector2(500, 50);
        var tmp = txtGo.AddComponent<TextMeshProUGUI>();

        // ���ؼ�����������������帳ֵ���ı�
        if (chineseFont != null)
        {
            tmp.font = chineseFont;
        }
        else
        {
            Debug.LogWarning("[PhoneUI] û��ָ���������壡UI���ܻ���ʾ�ɷ��顣");
        }

        tmp.text = BuildLabel(activity);
        tmp.fontSize = 18;
        tmp.color = activity.isFixed ? new Color(0.7f, 0.7f, 0.7f) : Color.white;

        return go;
    }

    string BuildLabel(ActivityData a)
    {
        string slot = a.slot switch
        {
            ActivitySlot.Morning => "����",
            ActivitySlot.Afternoon => "����",
            ActivitySlot.Evening => "����",
            _ => "ȫ��"
        };

        StringBuilder sb = new StringBuilder();
        sb.Append($"{a.displayName} [{slot} {a.duration}h]");

        if (a.moodDelta != 0) sb.Append($" ����{a.moodDelta:+0;-0}");
        if (a.staminaDelta != 0) sb.Append($" ����{a.staminaDelta:+0;-0}");
        if (a.stressDelta != 0) sb.Append($" ѹ��{a.stressDelta:+0;-0}");
        if (a.fatigueDelta != 0) sb.Append($" ƣ��{a.fatigueDelta:+0;-0}");
        if (a.academicDelta != 0) sb.Append($" ѧҵ{a.academicDelta:+0;-0}");
        if (a.socialDelta != 0) sb.Append($" �罻{a.socialDelta:+0;-0}");
        if (a.healthDelta != 0) sb.Append($" ����{a.healthDelta:+0;-0}");

        if (a.isFixed) sb.Append("  ��̶�");

        return sb.ToString();
    }

    void UpdateAlarmText(float val)
    {
        int h = (int)val;
        int m = (int)((val - h) * 60);
        if (alarmTimeText != null) alarmTimeText.text = $"���ӣ�{h:D2}:{m:D2}";
    }

    void UpdateTotalTime()
    {
        float total = 0f;
        foreach (var a in selectedActivities) total += a.duration;
        if (totalTimeText != null) totalTimeText.text = $"�Ѱ��ţ�{total:F1} Сʱ";
    }

    public void OnConfirm()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.alarmSet = alarmToggle != null && alarmToggle.isOn;
            PlayerStats.Instance.alarmTime = alarmTimeSlider != null ? alarmTimeSlider.value : 7.5f;
        }

        ScheduleSystem.Instance?.SetTomorrowSchedule(selectedActivities);

        var schedule = new DailySchedule
        {
            alarmSet = PlayerStats.Instance?.alarmSet ?? true,
            alarmTime = PlayerStats.Instance?.alarmTime ?? 7.5f
        };

        if (statusText != null) statusText.text = "��������";
        if (confirmButton != null) confirmButton.interactable = false;

        StartCoroutine(ConfirmDelay(schedule));
    }

    IEnumerator ConfirmDelay(DailySchedule schedule)
    {
        yield return new WaitForSeconds(0.8f);
        GameManager.Instance?.ConfirmTomorrowPlan(schedule);
    }
}
