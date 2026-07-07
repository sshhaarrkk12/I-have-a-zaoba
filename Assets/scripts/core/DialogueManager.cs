using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

// ─────────────────────────────────────────────
//  DialogueLine：单行对话数据（全局唯一定义）
// ─────────────────────────────────────────────
[Serializable]
public class DialogueLine
{
    public string characterName;
    [TextArea(2, 5)]
    public string text;
    public Sprite portrait;
    public float typingSpeed = 30f;
}

// ─────────────────────────────────────────────
//  DialogueChoice：选项数据（全局唯一定义）
// ─────────────────────────────────────────────
[Serializable]
public class DialogueChoice
{
    public string label;
    public Action onChoose;
}

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("对话框根物体")]
    public GameObject dialogueRoot;

    [Header("对话区")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image portraitImage;
    public GameObject portraitRoot;

    [Header("选项区")]
    public GameObject choicesRoot;

    [Header("Choice Auto Size")]
    public bool autoResizeChoiceButtons = true;
    public Vector2 choiceButtonPadding = new Vector2(320f, 90f);
    public Vector2 choiceButtonMinSize = new Vector2(420f, 160f);
    public float choiceButtonLineHeight = 160f;
    public float choiceButtonExtraWidth = 200f;
    

    [Header("其他")]
    public GameObject clickHint;
    public GameObject statPopup;
    public TextMeshProUGUI statPopupText;

    [Header("设置")]
    public float defaultTypingSpeed = 30f;
    public float autoCloseDelay = 2f;
    public Sprite defaultPortrait;

    [Header("补丁")]
    public GameObject wakeUpMGRGameObject;
    public WakeUpSceneManager wakeUpSceneManager;

    public bool IsDialogueActive => dialogueRoot != null && dialogueRoot.activeSelf;
    public bool IsChoicesActive => choicesRoot != null && choicesRoot.activeSelf;
    public bool IsStatPopupActive => statPopup != null && statPopup.activeSelf;
    public bool IsOverlayActive => IsDialogueActive || IsChoicesActive || IsStatPopupActive;

    bool isTyping = false;
    bool skipRequested = false;
    Action onDialogueDone;
    Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();
    const string ChoicesRootName = "ChoicesRoot";
    const string ButtonGroupName = "ButtonGroup";

    [Header("DialogueCanvas 根物体（顶层，DontDestroyOnLoad）")]
    public GameObject dialogueCanvas;

    void Start()
    {

        wakeUpSceneManager = wakeUpMGRGameObject.GetComponent<WakeUpSceneManager>();
    }

    void Awake()
    {
        if (Instance != null)
        {
            if (dialogueCanvas != null)
                Destroy(dialogueCanvas);
            else if (dialogueRoot != null)
                Destroy(dialogueRoot.transform.root.gameObject);
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (dialogueCanvas != null)
            DontDestroyOnLoad(dialogueCanvas);
        else if (dialogueRoot != null)
            DontDestroyOnLoad(dialogueRoot.transform.root.gameObject);
        Hide();
    }

    // ==================== 公开 API ====================

    public void Show(string text, Action onDone = null, string charName = "", Sprite portrait = null)
    {
        Show(new DialogueLine
        {
            characterName = charName,
            text = text,
            portrait = portrait,
            typingSpeed = defaultTypingSpeed
        }, onDone);
    }

    public void Show(DialogueLine line, Action onDone = null)
    {
        onDialogueDone = onDone;
        lineQueue.Clear();
        StopAllCoroutines();
        StartCoroutine(PlayLine(line, null));
    }

    public void ShowQueue(List<DialogueLine> lines, Action onAllDone = null)
    {
        lineQueue.Clear();
        foreach (var l in lines) lineQueue.Enqueue(l);
        onDialogueDone = onAllDone;
        StopAllCoroutines();
        StartCoroutine(PlayQueue());
    }

    public void ShowWithChoices(string text, List<DialogueChoice> choices,
                                string charName = "", Sprite portrait = null)
    {
        StopAllCoroutines();
        StartCoroutine(PlayLineWithChoices(new DialogueLine
        {
            characterName = charName,
            text = text,
            portrait = portrait,
            typingSpeed = defaultTypingSpeed
        }, choices));
    }

    public void ShowStatPopup(string content, Action onDone = null)
    {
        if (statPopup == null || statPopupText == null) { onDone?.Invoke(); return; }
        statPopupText.text = content;
        statPopup.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(WaitThenClose(statPopup, 2f, onDone));
    }

    public void Hide()
    {
        StopAllCoroutines();
        CacheChoicesRoot();
        SetActive(dialogueRoot, false);
        SetActive(choicesRoot, false);
        SetActive(statPopup, false);
    }

    // ==================== 内部协程 ====================

    IEnumerator PlayQueue()
    {
        while (lineQueue.Count > 0)
        {
            var line = lineQueue.Dequeue();
            bool lineDone = false;
            yield return StartCoroutine(PlayLine(line, () => lineDone = true));
            yield return new WaitUntil(() => lineDone);
        }
        SetActive(dialogueRoot, false);
        onDialogueDone?.Invoke();
    }

    IEnumerator PlayLine(DialogueLine line, Action onLineDone)
    {
        SetActive(dialogueRoot, true);
        SetActive(choicesRoot, false);
        SetActive(clickHint, false);

        if (characterNameText != null)
            characterNameText.text = string.IsNullOrEmpty(line.characterName) ? "我" : line.characterName;

        if (portraitImage != null)
        {
            Sprite s = line.portrait != null ? line.portrait : defaultPortrait;
            if (s != null) { portraitImage.sprite = s; SetActive(portraitRoot, true); }
            else SetActive(portraitRoot, false);
        }

        yield return StartCoroutine(TypeText(line.text, line.typingSpeed));
        SetActive(clickHint, true);

        float timer = 0f;
        skipRequested = false;
        while (!skipRequested && timer < autoCloseDelay)
        {
            timer += Time.deltaTime;
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) skipRequested = true;
            yield return null;
        }
        SetActive(clickHint, false);

        if (onLineDone != null) onLineDone.Invoke();
        else { SetActive(dialogueRoot, false); onDialogueDone?.Invoke(); }
    }

    IEnumerator PlayLineWithChoices(DialogueLine line, List<DialogueChoice> choices)
    {
        CacheChoicesRoot();
        SetActive(dialogueRoot, true);
        SetActive(choicesRoot, false);

        if (characterNameText != null)
            characterNameText.text = string.IsNullOrEmpty(line.characterName) ? "我" : line.characterName;

        if (portraitImage != null)
        {
            Sprite s = line.portrait != null ? line.portrait : defaultPortrait;
            if (s != null) { portraitImage.sprite = s; SetActive(portraitRoot, true); }
            else SetActive(portraitRoot, false);
        }

        yield return StartCoroutine(TypeText(line.text, line.typingSpeed));
        SetActive(choicesRoot, true);

        List<Button> buttons = GetRuntimeChoiceButtons();
        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;
            if (i < choices.Count)
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                TextMeshProUGUI label = GetChoiceLabel(i, btn);
                if (label != null)
                    label.text = choices[i].label;
                var c = choices[i];
                btn.onClick.AddListener(() =>
                {
                    SetActive(dialogueRoot, false);
                    SetActive(choicesRoot, false);
                    c.onChoose?.Invoke();
                });
            }
            else btn.gameObject.SetActive(false);
        }

        yield return StartCoroutine(RefreshChoiceLayoutNextFrame());
    }

    IEnumerator RefreshChoiceLayoutNextFrame()
    {
        if (!autoResizeChoiceButtons) yield break;

        Canvas.ForceUpdateCanvases();
        yield return null;
        Canvas.ForceUpdateCanvases();

        ResizeChoiceButtonsToText();
        RebuildButtonGroupOnly();
    }

    void ResizeChoiceButtonsToText()
    {
        List<Button> buttons = GetRuntimeChoiceButtons();
        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            TextMeshProUGUI label = GetChoiceLabel(i, button);
            if (button == null || label == null || !button.gameObject.activeSelf) continue;

            label.ForceMeshUpdate();
            RectTransform labelRect = label.rectTransform;

            Vector2 textSize = label.GetPreferredValues(label.text, Mathf.Infinity, Mathf.Infinity);
            if (textSize.x <= 0f || textSize.y <= 0f)
                textSize = labelRect.rect.size;

            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textSize.x);
            labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textSize.y);

            float scaledTextWidth = textSize.x * Mathf.Abs(labelRect.localScale.x);
            int lineCount = Mathf.Max(1, label.textInfo != null ? label.textInfo.lineCount : 1);

            Vector2 targetSize = new Vector2(
                Mathf.Max(choiceButtonMinSize.x, scaledTextWidth + choiceButtonPadding.x + choiceButtonExtraWidth),
                Mathf.Max(choiceButtonMinSize.y, choiceButtonLineHeight * lineCount)
            );

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);

            LayoutElement layoutElement = button.GetComponent<LayoutElement>();
            if (layoutElement == null)
                layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = targetSize.x;
            layoutElement.preferredWidth = targetSize.x;
            layoutElement.minHeight = targetSize.y;
            layoutElement.preferredHeight = targetSize.y;

            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonRect);
            labelRect.anchoredPosition = Vector2.zero;
        }
    }

    void RebuildButtonGroupOnly()
    {
        RectTransform buttonGroup = null;
        List<Button> buttons = GetRuntimeChoiceButtons();
        foreach (var button in buttons)
        {
            if (button == null || button.transform.parent == null) continue;
            buttonGroup = button.transform.parent as RectTransform;
            if (buttonGroup != null) break;
        }

        if (buttonGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonGroup);

        Canvas.ForceUpdateCanvases();
    }

    List<Button> GetRuntimeChoiceButtons()
    {
        List<Button> buttons = new List<Button>();
        HashSet<Button> used = new HashSet<Button>();

        CacheChoicesRoot();
        Transform buttonGroup = choicesRoot != null ? choicesRoot.transform.Find(ButtonGroupName) : null;
        if (buttonGroup != null)
        {
            for (int i = 0; i < buttonGroup.childCount; i++)
            {
                if (buttons.Count >= 2) break;
                AddChoiceButton(buttonGroup.GetChild(i).GetComponent<Button>(), buttons, used);
            }
        }

        if (buttons.Count == 0 && buttonGroup != null)
        {
            Button[] childButtons = buttonGroup.GetComponentsInChildren<Button>(true);
            foreach (var button in childButtons)
                AddChoiceButton(button, buttons, used);
        }

        return buttons;
    }

    void AddChoiceButton(Button button, List<Button> buttons, HashSet<Button> used)
    {
        if (button == null || used.Contains(button)) return;
        used.Add(button);
        buttons.Add(button);
    }

    TextMeshProUGUI GetChoiceLabel(int index, Button button)
    {
        return button != null ? button.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    void CacheChoicesRoot()
    {
        if (choicesRoot != null) return;

        Transform found = null;
        if (dialogueRoot != null)
            found = FindChildByName(dialogueRoot.transform, ChoicesRootName);
        if (found == null && dialogueCanvas != null)
            found = FindChildByName(dialogueCanvas.transform, ChoicesRootName);
        if (found == null)
            found = FindChildByName(transform, ChoicesRootName);

        if (found != null)
            choicesRoot = found.gameObject;
    }

    Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    bool ResizeRectToPreferredLayout(RectTransform rect)
    {
        float preferredWidth = LayoutUtility.GetPreferredWidth(rect);
        float preferredHeight = LayoutUtility.GetPreferredHeight(rect);
        bool resized = false;

        if (preferredWidth > 0f)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth);
            resized = true;
        }
        if (preferredHeight > 0f)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
            resized = true;
        }

        return resized;
    }

    void ResizeRectToActiveChildren(RectTransform rect)
    {
        if (rect == null) return;

        bool hasBounds = false;
        Vector2 min = Vector2.zero;
        Vector2 max = Vector2.zero;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < rect.childCount; i++)
        {
            RectTransform child = rect.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            child.GetWorldCorners(corners);
            for (int j = 0; j < corners.Length; j++)
            {
                Vector2 local = rect.InverseTransformPoint(corners[j]);
                if (!hasBounds)
                {
                    min = local;
                    max = local;
                    hasBounds = true;
                }
                else
                {
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
            }
        }

        if (!hasBounds) return;

        Vector2 size = max - min;
        if (size.x > 0f)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        if (size.y > 0f)
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
    }

    IEnumerator TypeText(string text, float speed)
    {
        if (dialogueText == null) yield break;
        isTyping = true;
        skipRequested = false;
        dialogueText.text = "";
        float interval = 1f / Mathf.Max(speed, 1f);
        for (int i = 0; i < text.Length; i++)
        {
            if (skipRequested) { dialogueText.text = text; break; }
            dialogueText.text += text[i];
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) skipRequested = true;
            yield return new WaitForSeconds(interval);
        }
        isTyping = false;
    }

    IEnumerator WaitThenClose(GameObject target, float delay, Action onDone)
    {
        float t = 0f;
        while (t < delay)
        {
            t += Time.deltaTime;
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0) break;
            yield return null;
        }
        SetActive(target, false);
        onDone?.Invoke();
    }

    void SetActive(GameObject go, bool active) { if (go != null) go.SetActive(active); }

    //呱：这个是我打的补丁 给一开始消失不掉的状态文字搞的
    public void OnButtonClick()
    {
       
       // wakeUpSceneManager.ClearWakeUpText();
     
    }
}
