using UnityEngine;
using UnityEngine.UI;

public class PhonePanelManager : MonoBehaviour
{
    [System.Serializable]
    public struct AppItem
    {
        public string appName;        // 方便在 Inspector 里备注名字
        public Button appButton;      // 桌面的 App 按钮
        public GameObject appPanel;   // 对应的功能面板物体
    }

    [Header("核心主界面")]
    public GameObject homeScreen;     // 对应你的 HomeScreen 物体
    public Button homeBackButton;     // 对应底部的返回按钮（或“要做一个返回最上级”）

    [Header("所有 App 映射配置")]
    public AppItem[] apps;            // 在 Inspector 里配置数组长度为 6

    void Start()
    {
        // 1. 动态循环绑定所有 App 按钮点击事件
        for (int i = 0; i < apps.Length; i++)
        {
            // 🚨 修复闭包问题：必须用局部变量存一下当前的 Panel
            GameObject targetPanel = apps[i].appPanel;
            Button targetButton = apps[i].appButton;

            if (targetButton != null && targetPanel != null)
            {
                targetButton.onClick.AddListener(() => OpenAppPanel(targetPanel));
            }
        }

        // 2. 绑定底部返回/主页键事件
        if (homeBackButton != null)
        {
            homeBackButton.onClick.AddListener(BackToHomeScreen);
        }

        // 3. 初始化：确保一上来处于手机主界面
        BackToHomeScreen();
    }

    /// <summary>
    /// 点开某一个 App 面板
    /// </summary>
    public void OpenAppPanel(GameObject targetPanel)
    {
        // 隐藏主屏幕桌面
        if (homeScreen != null) homeScreen.SetActive(false);

        // 先把所有 App 面板全部隐藏（防止从微信跳到别的面板时重叠）
        HideAllAppPanels();

        // 显示被点击的 App 面板
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            Debug.Log($"[手机系统] 成功打开面板: {targetPanel.name}");
        }
    }

    /// <summary>
    /// 返回手机主桌面
    /// </summary>
    public void BackToHomeScreen()
    {
        // 隐藏所有打开的 App 界面
        HideAllAppPanels();

        // 重新显示主桌面
        if (homeScreen != null)
        {
            homeScreen.SetActive(true);
        }
        
        Debug.Log("[手机系统] 已返回主界面。");
    }

    /// <summary>
    /// 辅助工具：一键隐藏所有应用面板
    /// </summary>
    private void HideAllAppPanels()
    {
        foreach (var app in apps)
        {
            if (app.appPanel != null)
            {
                app.appPanel.SetActive(false);
            }
        }
    }
}