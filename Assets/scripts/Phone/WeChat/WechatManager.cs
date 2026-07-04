using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class WechatManager : MonoBehaviour
{
    [Header("整体结构 Views")]
    public GameObject chatView;
    public GameObject momentsView; // 已做的朋友圈View
    public GameObject profileView;
    public GameObject chatDetailView;

    [Header("底栏 Tabs")]
    public Image chatTabIcon;
    public Image momentTabIcon;
    public Image profileTabIcon;
    public Color activeTabColor = Color.green;
    public Color inactiveTabColor = Color.gray;

    [Header("联系人数据")]
    public List<ChatContact> contacts;
    public Transform contactListRoot;
    public GameObject contactItemPrefab;

    [Header("聊天详情页组件")]
    public TextMeshProUGUI detailTitleText;
    public Transform chatHistoryRoot; // 气泡的父节点
    public GameObject npcBubblePrefab;
    public GameObject playerBubblePrefab;

    [Header("选项区域组件")]
    public GameObject choicesPanel; // 选项按钮的父容器
    public GameObject choiceButtonPrefab;
    public Transform choicesRoot;

    [Header("个人中心组件")]
    public TextMeshProUGUI profileNameText;
    public TextMeshProUGUI profileHealthText;
    public TextMeshProUGUI profileMoodText;
    public TextMeshProUGUI profileAcademicText;

    private ChatContact currentChattingContact;
    private Coroutine interactionCoroutine;

    void OnEnable()
    {
        // 每次打开微信默认切到Chat视图并刷新
        SwitchTab(0);
    }

    // ================== Tab 切换逻辑 ==================
    public void SwitchTab(int tabIndex)
    {
        chatView.SetActive(tabIndex == 0);
        momentsView.SetActive(tabIndex == 1);
        profileView.SetActive(tabIndex == 2);
        chatDetailView.SetActive(false); // 切换Tab时隐藏聊天详情

        chatTabIcon.color = tabIndex == 0 ? activeTabColor : inactiveTabColor;
        momentTabIcon.color = tabIndex == 1 ? activeTabColor : inactiveTabColor;
        profileTabIcon.color = tabIndex == 2 ? activeTabColor : inactiveTabColor;

        if (tabIndex == 0) RefreshChatList();
        if (tabIndex == 1) RefreshMoments();
        if (tabIndex == 2) RefreshProfile();
    }

    // ================== 1. ChatView 逻辑 ==================
    private void RefreshChatList()
    {
        // 清理旧列表
        foreach (Transform child in contactListRoot) Destroy(child.gameObject);

        // 生成新列表
        foreach (var contact in contacts)
        {
            GameObject go = Instantiate(contactItemPrefab, contactListRoot);
            go.GetComponent<ChatContactItemUI>().Setup(contact, OpenChatDetail);
        }
    }

    // ================== 2. ChatDetailView 逻辑 (核心) ==================
    private void OpenChatDetail(ChatContact contact)
    {
        currentChattingContact = contact;
        detailTitleText.text = contact.contactName;
        chatDetailView.SetActive(true);
        choicesPanel.SetActive(false);

        // 清理旧气泡
        foreach (Transform child in chatHistoryRoot) Destroy(child.gameObject);

        // 1. 瞬间生成已经读过的历史消息
        for (int i = 0; i < contact.currentMessageIndex; i++)
        {
            CreateBubble(contact.messages[i]);
        }

        // 2. 开始处理未读消息交互
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        interactionCoroutine = StartCoroutine(ShowNextInteraction());
    }

    private IEnumerator ShowNextInteraction()
    {
        // 检查是否还有未读消息
        if (currentChattingContact.currentMessageIndex >= currentChattingContact.messages.Count)
        {
            yield break; // 消息已读完，结束
        }

        // 获取当前要处理的消息
        ChatMessage currentMsg = currentChattingContact.messages[currentChattingContact.currentMessageIndex];

        if (!currentMsg.isFromPlayer)
        {
            // --- 对方发送消息 ---
            yield return new WaitForSeconds(0.5f); // 模拟真实打字延迟
            CreateBubble(currentMsg);
            currentChattingContact.currentMessageIndex++;

            // 继续下一条
            interactionCoroutine = StartCoroutine(ShowNextInteraction());
        }
        else
        {
            // --- 玩家发送消息 ---
            if (currentMsg.choices != null && currentMsg.choices.Count > 0)
            {
                // 显示选项面板
                choicesPanel.SetActive(true);
                foreach (Transform child in choicesRoot) Destroy(child.gameObject);

                foreach (var choice in currentMsg.choices)
                {
                    GameObject btnObj = Instantiate(choiceButtonPrefab, choicesRoot);
                    btnObj.GetComponent<ChatChoiceUI>().Setup(choice, OnChoiceClicked);
                }
            }
            else
            {
                // 如果是玩家发的但没配选项，默认直接发出去（或者你可以设定必须有选项）
                CreateBubble(currentMsg);
                currentChattingContact.currentMessageIndex++;
                interactionCoroutine = StartCoroutine(ShowNextInteraction());
            }
        }
    }

    private void OnChoiceClicked(ChatChoice selectedChoice)
    {
        choicesPanel.SetActive(false);

        // 生成玩家发出的气泡 (这里用选择的文本代替原本的content)
        ChatMessage fakeMsg = new ChatMessage { content = selectedChoice.choiceText, isFromPlayer = true };
        CreateBubble(fakeMsg);

        // 💡 联动：应用属性变化和时间消耗 (替换为你实际的方法)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ChangeMood(selectedChoice.moodDelta);
            PlayerStats.Instance.ChangeStress(selectedChoice.stressDelta);
            PlayerStats.Instance.ChangeFatigue(selectedChoice.fatigueDelta);
            // 假设TimeManager有消耗分钟的方法
            // TimeManager.Instance.SpendMinutes(selectedChoice.timeCostMinutes); 
        }

        // 索引增加，延迟0.5秒后继续下一条
        currentChattingContact.currentMessageIndex++;
        StartCoroutine(WaitAndContinue());
    }

    private IEnumerator WaitAndContinue()
    {
        yield return new WaitForSeconds(0.5f);
        interactionCoroutine = StartCoroutine(ShowNextInteraction());
    }

    private void CreateBubble(ChatMessage msg)
    {
        GameObject prefab = msg.isFromPlayer ? playerBubblePrefab : npcBubblePrefab;
        GameObject bubble = Instantiate(prefab, chatHistoryRoot);
        bubble.GetComponent<ChatBubbleUI>().Setup(msg.content, msg.isFromPlayer);

        // 💡 提示：这里通常还需要调用 LayoutRebuilder.ForceRebuildLayoutImmediate(chatHistoryRoot.GetComponent<RectTransform>()); 
        // 并且滚动条拉到最底部。
    }

    // 关闭聊天详情 (返回按钮调用)
    public void CloseChatDetail()
    {
        if (interactionCoroutine != null) StopCoroutine(interactionCoroutine);
        chatDetailView.SetActive(false);
        RefreshChatList(); // 返回时刷新列表以更新最后一条消息和红点
    }

    // ================== 3. MomentsView 逻辑 ==================
    private void RefreshMoments()
    {
        // 你说已做，所以这里预留接口
        // MomentsSystem.Instance.RefreshVisibleMoments();
    }

    // ================== 4. ProfileView 逻辑 ==================
    private void RefreshProfile()
    {
        if (PlayerStats.Instance != null)
        {
            // profileNameText.text = PlayerStats.Instance.playerName; // 如果有的话
            profileHealthText.text = "健康: " + PlayerStats.Instance.health;
            profileMoodText.text = "心情: " + PlayerStats.Instance.mood;
            profileAcademicText.text = "学业: " + PlayerStats.Instance.academic;
        }
    }
}