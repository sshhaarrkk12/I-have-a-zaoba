using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单场景管理器
/// 功能：开始游戏、退出、制作人员
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("主菜单面板")]
    public GameObject mainMenuPanel;
    public Button startButton;
    public Button creditsButton;
    public Button quitButton;

    [Header("制作人员面板")]
    public GameObject creditsPanel;
    public Button creditsBackButton;
    public TextMeshProUGUI creditsText;

    [Header("制作人员内容（直接在Inspector里填）")]
    [TextArea(5, 20)]
    public string creditsContent = 
        "早八模拟器\n\n" +
        "制作团队\n\n" +
        "策划\n" +
        "XXX\n\n" +
        "程序\n" +
        "XXX\n\n" +
        "美术\n" +
        "XXX\n\n" +
        "音效\n" +
        "XXX\n\n" +
        "特别感谢\n" +
        "所有为早八奋斗过的同学们";

    [Header("过渡设置")]
    public string gameSceneName = "Wakeup";
    public float fadeTime = 1f;
    public Image fadeOverlay;           // 全屏黑色遮罩

    void Start()
    {
        // 初始化面板
        ShowMainMenu();

        // 绑定按钮
        if (startButton   != null) startButton.onClick.AddListener(OnStartGame);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnShowCredits);
        if (quitButton    != null) quitButton.onClick.AddListener(OnQuit);
        if (creditsBackButton != null) creditsBackButton.onClick.AddListener(ShowMainMenu);

        // 填入制作人员文字
        if (creditsText != null) creditsText.text = creditsContent;

        // 淡入显示
        StartCoroutine(FadeIn());
    }

    // ==================== 按钮响应 ====================

    void OnStartGame()
    {
        startButton.interactable   = false;
        creditsButton.interactable = false;
        quitButton.interactable    = false;
        StartCoroutine(StartGameTransition());
    }

    void OnShowCredits()
    {
        mainMenuPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (creditsPanel  != null) creditsPanel.SetActive(false);
    }

    void OnQuit()
    {
        StartCoroutine(QuitTransition());
    }

    // ==================== 过渡动画 ====================

    IEnumerator StartGameTransition()
    {
        yield return StartCoroutine(FadeOut());
        SceneManager.LoadScene(gameSceneName);
    }

    IEnumerator QuitTransition()
    {
        yield return StartCoroutine(FadeOut());
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, 0);
        fadeOverlay.gameObject.SetActive(false);
    }

    IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;
        fadeOverlay.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, elapsed / fadeTime);
            fadeOverlay.color = new Color(0, 0, 0, a);
            yield return null;
        }
        fadeOverlay.color = new Color(0, 0, 0, 1);
    }
}
