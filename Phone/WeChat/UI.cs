using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// --- 联系人列表项 ---
public class ChatContactItemUI : MonoBehaviour
{
    public Image avatarImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI lastMessageText;
    public GameObject unreadDot;
    public Button clickButton;

    public void Setup(ChatContact contact, Action<ChatContact> onClick)
    {
        nameText.text = contact.contactName;
        if (contact.avatar != null) avatarImage.sprite = contact.avatar;
        avatarImage.color = contact.avatarColor;

        // 显示最后一条消息预览（如果有的话）
        if (contact.messages.Count > 0)
        {
            int previewIndex = Mathf.Min(contact.currentMessageIndex, contact.messages.Count - 1);
            lastMessageText.text = contact.messages[previewIndex].content;
        }

        unreadDot.SetActive(contact.HasUnread);

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(() => onClick(contact));
    }
}

// --- 聊天气泡 ---
public class ChatBubbleUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public RectTransform bubbleBackground; // 气泡背景
    public HorizontalLayoutGroup layoutGroup; // 用于控制左右对齐

    public void Setup(string text, bool isPlayer)
    {
        messageText.text = text;

        // 伪代码：根据是否是玩家调整对齐方式和颜色
        // 你可以在Inspector里设置好PlayerBubble和NPCBubble两个Prefab，这样更简单。
        // 这里提供的是单Prefab控制逻辑：
        layoutGroup.childAlignment = isPlayer ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        bubbleBackground.GetComponent<Image>().color = isPlayer ? new Color(0.6f, 0.9f, 0.6f) : Color.white; // 玩家绿色，NPC白色
    }
}

// --- 玩家选项按钮 ---
public class ChatChoiceUI : MonoBehaviour
{
    public TextMeshProUGUI choiceText;
    public Button choiceButton;

    public void Setup(ChatChoice choice, Action<ChatChoice> onSelected)
    {
        choiceText.text = choice.choiceText;
        choiceButton.onClick.RemoveAllListeners();
        choiceButton.onClick.AddListener(() => onSelected(choice));
    }
}