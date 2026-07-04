using UnityEngine;
using UnityEngine.UI;

public class UI_StatusBarSync : MonoBehaviour
{
    [Header("属性条 UI 组件 (完全对应你的层级树)")]
    public Slider moodSlider;       // 对应 MoodBar
    public Slider studySlider;      // 对应 StudyBar
    public Slider staminaSlider;    // 对应 StaminaBar
    public Slider stressSlider;     // 对应 StressBar
    public Slider fatigueSlider;    // 对应 FatigueBar
    public Slider healthSlider;     // 对应 HealthBar
    public Slider socialSlider;     // 对应 SocialBar

    [Header("其他常驻 UI (可选)")]
    public Text moneyText;          // 金钱文本（如果用的是TextMeshPro，可以改成 TextMeshProUGUI）

    void Update()
    {
        // 每帧安全同步所有数据
        UpdateAllStatusBars();
    }

    private void UpdateAllStatusBars()
    {
        // 防御性拦截：确保底层数据单例已经生成，防止切场景时报错
        if (PlayerStats.Instance == null) return;

        // 🚨 核心逻辑：读取单例数值并同步给各自对应的 Slider（假设满值均为 100）

        // 1. 心情 (MoodBar)
        if (moodSlider != null)
            moodSlider.value = PlayerStats.Instance.mood / 100f;

        // 2. 学习 (StudyBar)
        if (studySlider != null)
            studySlider.value = PlayerStats.Instance.academic / 100f;

        // 3. 体力 (StaminaBar)
        if (staminaSlider != null)
            staminaSlider.value = PlayerStats.Instance.instantStamina / 100f;

        // 4. 压力 (StressBar)
        if (stressSlider != null)
            stressSlider.value = PlayerStats.Instance.instantStress / 100f;

        // 5. 疲惫 (FatigueBar)
        if (fatigueSlider != null)
            fatigueSlider.value = PlayerStats.Instance.fatigue / 100f;

        // 6. 健康 (HealthBar)
        if (healthSlider != null)
            healthSlider.value = PlayerStats.Instance.health / 100f;

        // 7. 社交 (SocialBar)
        if (socialSlider != null)
            socialSlider.value = PlayerStats.Instance.social / 100f;

       
    }
}