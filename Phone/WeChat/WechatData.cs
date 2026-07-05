using UnityEngine;
using System.Collections.Generic;

// --- 1. 选项数据 ---
[System.Serializable]
public class ChatChoice
{
    public string choiceText;
    public float moodDelta;
    public float stressDelta;
    public float fatigueDelta;
    public float timeCostMinutes = 5f; // 默认消耗5分钟
}

// --- 2. 消息数据 ---
[System.Serializable]
public class ChatMessage
{
    [TextArea(2, 5)]
    public string content;
    public bool isFromPlayer;
    public List<ChatChoice> choices; // 如果是玩家消息，此处填写选项
}

// --- 3. 联系人数据 (ScriptableObject) ---
[CreateAssetMenu(fileName = "NewChatContact", menuName = "Wechat/Chat Contact")]
public class ChatContact : ScriptableObject
{
    public string contactName;
    public Sprite avatar;
    public Color avatarColor = Color.white;

    public List<ChatMessage> messages = new List<ChatMessage>();

    // 当前读到第几条（注意：如果在游戏运行中修改此值，ScriptableObject会永久保存。
    // 正式发布时建议在游戏开始时将其重置，或者将此字段剥离到存档系统中）
    public int currentMessageIndex = 0;

    // 是否有未读消息
    public bool HasUnread => currentMessageIndex < messages.Count;
}