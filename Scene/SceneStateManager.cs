using UnityEngine;

/// <summary>
/// 场景状态管理器 - 记录玩家当前在哪个场景
/// 挂在常驻GameObject上（DontDestroyOnLoad）
/// </summary>
public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    public string CurrentScene { get; private set; } = "";

    /// <summary>玩家是否已经在教室里</summary>
    public bool IsInClassroom => CurrentScene == "Classroom";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentScene(string sceneName)
    {
        CurrentScene = sceneName;
        Debug.Log($"[SceneState] 当前场景 → {sceneName}");
    }
}
