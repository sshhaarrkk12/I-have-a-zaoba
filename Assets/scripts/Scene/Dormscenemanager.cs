using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DormSceneManager : MonoBehaviour
{
    [Header("UI 面板")]
    public GameObject mainMenuPanel;
    public GameObject washPanel;
    public GameObject dressingPanel;
    public GameObject packingPanel;
    public GameObject navigationPanel;

    [Header("状态显示")]
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI statusBar;
    public StatsHUD statsHUD;

    [Header("宿舍状态")]
    public bool isWashed = false;
    public bool isDressed = false;
    public bool isPacked = false;

    [Header("图标提示（可选，不赋值也不报错）")]
    public GameObject washIcon;
    public GameObject dressIcon;
    public GameObject packIcon;

    void Start()
    {
        EnsureStatusBarAssigned();
        MorningRoutineState.SyncDay();
        isWashed = MorningRoutineState.WashingDone;
        isDressed = MorningRoutineState.DressingDone;
        isPacked = MorningRoutineState.PackingDone;

        TimeManager.OnTimeChanged += OnTimeUpdate;
        if (TimeManager.Instance != null)
            OnTimeUpdate(TimeManager.Instance.gameHour);
        RefreshUI();
    }

    void OnDestroy()
    {
        TimeManager.OnTimeChanged -= OnTimeUpdate;
    }

    void OnTimeUpdate(float hour)
    {
        if (TimeManager.Instance == null) return;

        if (timeText != null)
            timeText.text = TimeManager.Instance.GetFormattedTime();

        int minutes = Mathf.CeilToInt(TimeManager.Instance.MinutesToClass());
        if (statusBar != null)
        {
            if (minutes > 0)
                statusBar.text = $"距离上课还有 {minutes} 分钟！";
        }

        if (minutes <= 15 && minutes > 0)
        {
            if (timeText != null) timeText.color = GamePalette.Bad;
        }
    }

    void EnsureStatusBarAssigned()
    {
        if (statusBar != null) return;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null) continue;

            string objectName = text.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("status") || objectName.Contains("tip") || objectName.Contains("hint"))
            {
                statusBar = text;
                return;
            }
        }
    }

    void RefreshUI()
    {
        if (washIcon != null) washIcon.SetActive(!isWashed);
        if (dressIcon != null) dressIcon.SetActive(!isDressed);
        if (packIcon != null) packIcon.SetActive(!isPacked);
    }

    public void ShowWashPanel() { if (isWashed) return; SetMain(false); if (washPanel != null) washPanel.SetActive(true); }
    public void ShowDressPanel() { if (isDressed) return; SetMain(false); if (dressingPanel != null) dressingPanel.SetActive(true); }
    public void ShowPackingPanel() { if (isPacked) return; SetMain(false); if (packingPanel != null) packingPanel.SetActive(true); }
    public void ShowNavPanel() { SetMain(false); if (navigationPanel != null) navigationPanel.SetActive(true); }

    void SetMain(bool on) { if (mainMenuPanel != null) mainMenuPanel.SetActive(on); }

    public void BackToMain()
    {
        if (washPanel != null) washPanel.SetActive(false);
        if (dressingPanel != null) dressingPanel.SetActive(false);
        if (packingPanel != null) packingPanel.SetActive(false);
        if (navigationPanel != null) navigationPanel.SetActive(false);
        SetMain(true);
        RefreshUI();
    }

    public void DoWashQuick() => DoWash(true);
    public void DoWashNormal() => DoWash(false);

    void DoWash(bool isQuick)
    {
        if (isWashed) return;

        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();
        TimeManager.Instance.SpendTime(isQuick ? 0.083f : 0.25f);
        PlayerStats.Instance.ChangeMood(isQuick ? 2f : 8f);
        PlayerStats.Instance.AddFatigue(isQuick ? -2f : -8f);
        isWashed = true;
        MorningRoutineState.MarkDone("Washing");
        ShowStatus(isQuick ? "快速洗漱完毕" : "认真洗漱，神清气爽");
        BackToMain();
        ShowStatChange(beforeStats);
    }

    public void DoChangeQuick() => DoChangeClothes("quick");
    public void DoChangeNormal() => DoChangeClothes("normal");
    public void DoChangeElaborate() => DoChangeClothes("elaborate");

    void DoChangeClothes(string option)
    {
        if (isDressed) return;

        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();
        if (option == "quick") { TimeManager.Instance.SpendTime(0.05f); }
        else if (option == "normal") { TimeManager.Instance.SpendTime(0.1f); PlayerStats.Instance.ChangeMood(5f); }
        else { TimeManager.Instance.SpendTime(0.25f); PlayerStats.Instance.ChangeMood(12f); PlayerStats.Instance.AddFatigue(3f); }
        isDressed = true;
        MorningRoutineState.MarkDone("Dressing");
        ShowStatus("换好衣服了");
        BackToMain();
        ShowStatChange(beforeStats);
    }

    public void DoPackCareful() => DoPackBag(true);
    public void DoPackQuick() => DoPackBag(false);

    void DoPackBag(bool careful)
    {
        if (isPacked) return;

        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();
        string resultText;
        TimeManager.Instance.SpendTime(careful ? 0.1f : 0.05f);
        if (!careful && Random.value < 0.3f)
        {
            string[] items = { "课本", "水杯", "雨伞", "作业" };
            resultText = $"忘带{items[Random.Range(0, items.Length)]}了！压力+8";
            PlayerStats.Instance.AddStress(8f);
            PlayerStats.Instance.ChangeMood(-5f);
        }
        else { resultText = "书包收拾好了"; }
        isPacked = true;
        MorningRoutineState.MarkDone("Packing");
        ShowStatus(resultText);
        BackToMain();
        ShowStatChange(beforeStats);
    }

    public void GoToCanteen()
    {
        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();
        PlayerStats.Instance.ConsumeInstantStamina(3f);
        PlayerStats.Instance.AddFatigue(2f);
        ShowStatChange(beforeStats, () => GameManager.Instance.GoToCanteen());
    }

    public void GoOutDirect()
    {
        StatsChangeSnapshot beforeStats = StatsChangeSummary.Capture();
        PlayerStats.Instance.ConsumeInstantStamina(3f);
        PlayerStats.Instance.AddFatigue(2f);
        if (!isWashed) PlayerStats.Instance.ChangeMood(-5f);
        if (!isPacked) PlayerStats.Instance.AddStress(5f);
        ShowStatChange(beforeStats, () => GameManager.Instance.GoToCorridor());
    }

    void ShowStatus(string msg)
    {
        if (statusBar != null) statusBar.text = msg;
        else Debug.Log(msg);
    }

    void ShowStatChange(StatsChangeSnapshot beforeStats, System.Action onDone = null)
    {
        StatsChangeSummary.Show(StatsChangeSummary.Build(beforeStats), onDone);
    }
}
