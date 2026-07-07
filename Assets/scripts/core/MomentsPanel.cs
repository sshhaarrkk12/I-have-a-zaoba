using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 朋友圈面板UI
/// 挂在 WechatPanel/ContentArea 下的 MomentsView GameObject 上
/// </summary>
public class MomentsPanel : MonoBehaviour
{
    [Header("列表容器")]
    public Transform momentListRoot;
    public GameObject momentItemPrefab;

    [Header("发布朋友圈按钮")]
    public Button postBtn;
    public GameObject postChoicePanel;
    public Transform postChoiceRoot;
    public GameObject postChoiceItemPrefab;

    [Header("当前事件标签（由事件系统设置）")]
    public string currentEventTag = "";

    void OnEnable()
    {
        MomentsSystem.OnMomentsUpdated += RefreshUI;
        RefreshUI();
    }

    void OnDisable()
    {
        MomentsSystem.OnMomentsUpdated -= RefreshUI;
    }

    void Start()
    {
        if (postBtn != null)
            postBtn.onClick.AddListener(OnPostBtnClick);
        if (postChoicePanel != null)
            postChoicePanel.SetActive(false);
    }

    void RefreshUI()
    {
        if (momentListRoot == null || MomentsSystem.Instance == null) return;

        foreach (Transform child in momentListRoot)
            Destroy(child.gameObject);

        var moments = MomentsSystem.Instance.VisibleMoments;

        if (moments.Count == 0)
        {
            CreateEmptyHint();
            return;
        }

        foreach (var entry in moments)
            CreateMomentItem(entry);
    }

    void CreateEmptyHint()
    {
        var go = new GameObject("EmptyHint");
        go.transform.SetParent(momentListRoot, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = "暂时没有朋友圈动态";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.6f, 0.6f, 0.6f);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);
    }

    void CreateMomentItem(MomentEntry entry)
    {
        GameObject go;
        if (momentItemPrefab != null)
        {
            go = Instantiate(momentItemPrefab, momentListRoot);
            SetupPrefabItem(go, entry);
            return;
        }

        go = new GameObject("MomentItem");
        go.transform.SetParent(momentListRoot, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 120);

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateHeader(go.transform, entry);

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(go.transform, false);
        var contentTMP = contentGo.AddComponent<TextMeshProUGUI>();
        contentTMP.text = entry.isPlayer ? "【我】" + entry.post.content : entry.post.content;
        contentTMP.fontSize = 16;
        contentTMP.color = Color.white;
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.sizeDelta = new Vector2(0, 50);

        if (entry.post.canLike && !entry.isPlayer)
            CreateLikeBtn(go.transform, entry);

        var divider = new GameObject("Divider");
        divider.transform.SetParent(go.transform, false);
        var divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1, 1, 1, 0.1f);
        var divRT = divider.GetComponent<RectTransform>();
        divRT.sizeDelta = new Vector2(0, 1);
    }

    GameObject CreateHeader(Transform parent, MomentEntry entry)
    {
        var go = new GameObject("Header");
        go.transform.SetParent(parent, false);
        var layout = go.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 36);

        var avatarGo = new GameObject("Avatar");
        avatarGo.transform.SetParent(go.transform, false);
        var avatarImg = avatarGo.AddComponent<Image>();
        avatarImg.color = new Color(0.4f, 0.6f, 0.9f);
        if (entry.post.authorAvatar != null) avatarImg.sprite = entry.post.authorAvatar;
        var avatarRT = avatarGo.GetComponent<RectTransform>();
        avatarRT.sizeDelta = new Vector2(36, 36);

        var infoGo = new GameObject("Info");
        infoGo.transform.SetParent(go.transform, false);
        var infoLayout = infoGo.AddComponent<VerticalLayoutGroup>();
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        var infoRT = infoGo.GetComponent<RectTransform>();
        infoRT.sizeDelta = new Vector2(200, 36);

        var nameTMP = new GameObject("Name").AddComponent<TextMeshProUGUI>();
        nameTMP.transform.SetParent(infoGo.transform, false);
        nameTMP.text = entry.isPlayer ? "我" : entry.post.authorName;
        nameTMP.fontSize = 16;
        nameTMP.fontStyle = FontStyles.Bold;
        nameTMP.color = new Color(0.5f, 0.7f, 1f);

        var timeTMP = new GameObject("Time").AddComponent<TextMeshProUGUI>();
        timeTMP.transform.SetParent(infoGo.transform, false);
        timeTMP.text = entry.postTime;
        timeTMP.fontSize = 12;
        timeTMP.color = new Color(0.6f, 0.6f, 0.6f);

        return go;
    }

    void CreateLikeBtn(Transform parent, MomentEntry entry)
    {
        var go = new GameObject("LikeBtn");
        go.transform.SetParent(parent, false);
        var btn = go.AddComponent<Button>();
        var img = go.AddComponent<Image>();
        img.color = entry.isLiked ? GamePalette.BadTransparent : new Color(1f, 1f, 1f, 0.1f);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 30);

        var tmp = new GameObject("Label").AddComponent<TextMeshProUGUI>();
        tmp.transform.SetParent(go.transform, false);
        tmp.text = entry.isLiked ? "❤ 已点赞" : "♡ 点赞";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = entry.isLiked ? GamePalette.Bad : Color.white;

        var captured = entry;
        btn.onClick.AddListener(() =>
        {
            if (!captured.isLiked)
            {
                MomentsSystem.Instance?.LikePost(captured.post);
                captured.isLiked = true;
            }
        });
    }

    void SetupPrefabItem(GameObject go, MomentEntry entry)
    {
        var tmps = go.GetComponentsInChildren<TextMeshProUGUI>();
        if (tmps.Length > 0) tmps[0].text = entry.isPlayer ? "我" : entry.post.authorName;
        if (tmps.Length > 1) tmps[1].text = entry.post.content;
        if (tmps.Length > 2) tmps[2].text = entry.postTime;

        var btn = go.GetComponentInChildren<Button>();
        if (btn != null)
        {
            var captured = entry;
            btn.onClick.AddListener(() => MomentsSystem.Instance?.LikePost(captured.post));
        }
    }

    void OnPostBtnClick()
    {
        if (postChoicePanel == null) return;

        var options = MomentsSystem.Instance?.GetPlayerPostOptions(currentEventTag) ?? new List<MomentPost>();

        if (options.Count == 0)
        {
            Debug.Log("[Moments] 当前没有可发的朋友圈");
            return;
        }

        BuildPostChoices(options);
        postChoicePanel.SetActive(true);
    }

    void BuildPostChoices(List<MomentPost> options)
    {
        if (postChoiceRoot == null) return;

        foreach (Transform child in postChoiceRoot)
            Destroy(child.gameObject);

        AddChoiceItem("不发了", null);

        foreach (var option in options)
        {
            var captured = option;
            AddChoiceItem(option.content, () =>
            {
                MomentsSystem.Instance?.PlayerPost(captured);
                postChoicePanel.SetActive(false);
            });
        }
    }

    void AddChoiceItem(string text, System.Action onClick)
    {
        GameObject go;
        if (postChoiceItemPrefab != null)
        {
            go = Instantiate(postChoiceItemPrefab, postChoiceRoot);
        }
        else
        {
            go = new GameObject("ChoiceItem");
            go.transform.SetParent(postChoiceRoot, false);
            go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50);
        }

        var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
        tmp.text = text;

        btn.onClick.RemoveAllListeners();
        if (onClick == null)
            btn.onClick.AddListener(() => postChoicePanel.SetActive(false));
        else
            btn.onClick.AddListener(() => onClick());
    }
}