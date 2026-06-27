using UnityEngine;
using System;
using System.Collections.Generic;

// ==================== 数据结构 ====================

/// <summary>
/// 单条朋友圈数据（ScriptableObject，在Inspector里预设）
/// 右键 Create → EarlyClass8 → MomentPost 创建
/// </summary>
[CreateAssetMenu(fileName = "NewMoment", menuName = "EarlyClass8/MomentPost")]
public class MomentPost : ScriptableObject
{
    [Header("发布者")]
    public string authorName;           // 好友名字
    public Sprite authorAvatar;         // 头像（可空）

    [Header("内容")]
    [TextArea(2, 5)]
    public string content;              // 朋友圈文字内容
    public Sprite image;                // 配图（可空）

    [Header("触发条件")]
    public int appearDay = 1;           // 第几天开始出现
    public float appearAfterHour = 0f;  // 几点之后出现（0=不限）
    public float appearBeforeHour = 0f; // 几点之前出现（0=不限）
    public bool showOnce = true;        // 只显示一次

    [Header("玩家互动")]
    public bool canLike = true;         // 可以点赞
    public float likesMoodDelta = 2f;   // 点赞后心情变化

    [Header("是否是玩家可发的朋友圈（事件触发后选择发布）")]
    public bool isPlayerPost = false;
    public string playerPostEventTag;   // 对应哪个事件标签
}

/// <summary>
/// 朋友圈系统管理器，挂在 _Mangers 上
/// </summary>
public class MomentsSystem : MonoBehaviour
{
    public static MomentsSystem Instance { get; private set; }

    [Header("所有预设朋友圈（好友发的）")]
    public List<MomentPost> friendPosts = new List<MomentPost>();

    [Header("玩家可发的朋友圈选项")]
    public List<MomentPost> playerPosts = new List<MomentPost>();

    // 当前可见的朋友圈（按时间排序）
    public List<MomentEntry> VisibleMoments { get; private set; } = new List<MomentEntry>();

    // 已点赞的
    HashSet<string> likedPosts = new HashSet<string>();

    // 已显示过的（showOnce用）
    HashSet<string> shownPosts = new HashSet<string>();

    // 玩家已发的
    List<MomentEntry> playerPostedMoments = new List<MomentEntry>();

    public static event Action OnMomentsUpdated;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()  => TimeManager.OnTimeChanged += OnTimeUpdate;
    void OnDisable() => TimeManager.OnTimeChanged -= OnTimeUpdate;

    void OnTimeUpdate(float hour)
    {
        RefreshMoments();
    }

    /// <summary>刷新当前可见的朋友圈列表</summary>
    public void RefreshMoments()
    {
        int day = PlayerStats.Instance?.currentDay ?? 1;
        float hour = TimeManager.Instance?.gameHour ?? 0f;

        var newVisible = new List<MomentEntry>();

        // 好友发的朋友圈
        foreach (var post in friendPosts)
        {
            if (post == null) continue;
            if (post.showOnce && shownPosts.Contains(post.name)) continue;
            if (day < post.appearDay) continue;
            if (post.appearAfterHour > 0 && hour < post.appearAfterHour) continue;
            if (post.appearBeforeHour > 0 && hour > post.appearBeforeHour) continue;

            var entry = new MomentEntry
            {
                post       = post,
                isLiked    = likedPosts.Contains(post.name),
                isPlayer   = false,
                postTime   = $"Day{day} {hour:F1}h"
            };
            newVisible.Add(entry);

            if (post.showOnce) shownPosts.Add(post.name);
        }

        // 玩家发的朋友圈
        newVisible.AddRange(playerPostedMoments);

        // 按发布时间排序（新的在前）
        newVisible.Sort((a, b) => string.Compare(b.postTime, a.postTime));

        VisibleMoments = newVisible;
        OnMomentsUpdated?.Invoke();
    }

    /// <summary>玩家点赞</summary>
    public void LikePost(MomentPost post)
    {
        if (post == null || likedPosts.Contains(post.name)) return;
        likedPosts.Add(post.name);
        PlayerStats.Instance?.ChangeMood(post.likesMoodDelta);
        PlayerStats.Instance?.RecalculateHealth();
        OnMomentsUpdated?.Invoke();
        Debug.Log($"[Moments] 点赞: {post.authorName} 心情+{post.likesMoodDelta}");
    }

    /// <summary>玩家发朋友圈（事件触发后调用）</summary>
    public void PlayerPost(MomentPost post)
    {
        if (post == null) return;
        int day = PlayerStats.Instance?.currentDay ?? 1;
        float hour = TimeManager.Instance?.gameHour ?? 0f;

        var entry = new MomentEntry
        {
            post     = post,
            isLiked  = false,
            isPlayer = true,
            postTime = $"Day{day} {hour:F1}h"
        };

        playerPostedMoments.Insert(0, entry);
        PlayerStats.Instance?.ChangeMood(3f); // 发朋友圈心情+3
        OnMomentsUpdated?.Invoke();
        Debug.Log($"[Moments] 玩家发布朋友圈: {post.content}");
    }

    /// <summary>获取某事件标签对应的可发朋友圈选项</summary>
    public List<MomentPost> GetPlayerPostOptions(string eventTag)
    {
        return playerPosts.FindAll(p =>
            p.isPlayerPost &&
            (string.IsNullOrEmpty(p.playerPostEventTag) ||
             p.playerPostEventTag == eventTag));
    }

    /// <summary>每天重置（showOnce除外）</summary>
    public void OnNewDay()
    {
        // showOnce的不重置，其他的可以重置
        RefreshMoments();
    }
}

/// <summary>朋友圈条目（运行时数据）</summary>
[Serializable]
public class MomentEntry
{
    public MomentPost post;
    public bool isLiked;
    public bool isPlayer;   // 是否是玩家发的
    public string postTime; // 发布时间记录
}
