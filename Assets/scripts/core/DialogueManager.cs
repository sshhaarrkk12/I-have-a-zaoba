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
    public List<Button> choiceButtons;
    public List<TextMeshProUGUI> choiceLabels;
    

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

    bool isTyping = false;
    bool skipRequested = false;
    Action onDialogueDone;
    Queue<DialogueLine> lineQueue = new Queue<DialogueLine>();

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

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            var btn = choiceButtons[i];
            if (btn == null) continue;
            if (i < choices.Count)
            {
                btn.gameObject.SetActive(true);
                btn.onClick.RemoveAllListeners();
                if (i < choiceLabels.Count && choiceLabels[i] != null)
                    choiceLabels[i].text = choices[i].label;
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