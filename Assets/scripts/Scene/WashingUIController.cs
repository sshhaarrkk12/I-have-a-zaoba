using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WashingUIController : MonoBehaviour
{
    [Header("Buttons")]
    public Button[] triggerButtons; // e.g. button, button (1), button (2)
    public GameObject[] targetButtons; // the three buttons: 厕所, 宿舍, 出行

    [Header("Mask and Box")]
    public GameObject mask; // full-screen black mask (Image + CanvasGroup)
    public GameObject box;  // the box whose text should be above the mask

    [Header("Timing (seconds)")]
    public float fadeInTime = 0.5f;
    public float holdTime = 2.2f;
    public float fadeOutTime = 0.5f;

    private CanvasGroup maskGroup;
    private bool sequenceRunning = false;

    void Start()
    {
        // Initially hide the three target buttons
        if (targetButtons != null)
        {
            foreach (var go in targetButtons)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // Ensure mask exists and its CanvasGroup
        if (mask == null)
        {
            CreateMask();
        }

        maskGroup = mask.GetComponent<CanvasGroup>();
        if (maskGroup == null) maskGroup = mask.AddComponent<CanvasGroup>();
        maskGroup.alpha = 0f;
        mask.SetActive(false);

        // Ensure box is above mask
        if (box != null)
        {
            // Move mask behind box
            mask.transform.SetAsLastSibling();
            box.transform.SetAsLastSibling();
        }

        // Hook triggers
        if (triggerButtons != null)
        {
            foreach (var b in triggerButtons)
            {
                if (b != null) b.onClick.AddListener(OnTriggerClicked);
            }
        }
    }

    private void CreateMask()
    {
        var canvas = FindObjectOfType<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;
        mask = new GameObject("WashingMask", typeof(RectTransform), typeof(CanvasRenderer));
        mask.transform.SetParent(parent, false);
        var img = mask.AddComponent<Image>();
        img.color = Color.black;
        var rt = mask.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        mask.AddComponent<CanvasGroup>();

        // Place behind other UI; will be adjusted again in Start
        mask.transform.SetAsLastSibling();
    }

    private void OnTriggerClicked()
    {
        if (sequenceRunning) return;
        sequenceRunning = true;

        // Remove listeners so subsequent clicks don't restart
        if (triggerButtons != null)
        {
            foreach (var b in triggerButtons)
            {
                if (b != null) b.onClick.RemoveListener(OnTriggerClicked);
            }
        }

        // Hide the target buttons immediately
        if (targetButtons != null)
        {
            foreach (var go in targetButtons)
            {
                if (go != null) go.SetActive(false);
            }
        }

        // Ensure box is above mask
        if (box != null && mask != null)
        {
            mask.transform.SetAsLastSibling();
            box.transform.SetAsLastSibling();
        }

        StartCoroutine(RunMaskSequence());
    }

    private IEnumerator RunMaskSequence()
    {
        if (mask == null) yield break;

        mask.SetActive(true);
        if (maskGroup == null) maskGroup = mask.GetComponent<CanvasGroup>();
        maskGroup.alpha = 0f;

        float t = 0f;
        // Fade in (unscaled to ignore timeScale)
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            maskGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeInTime));
            yield return null;
        }

        maskGroup.alpha = 1f;

        // Hold
        yield return new WaitForSecondsRealtime(holdTime);

        // Fade out
        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            maskGroup.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.0001f, fadeOutTime));
            yield return null;
        }

        maskGroup.alpha = 0f;
        mask.SetActive(false);

        // After sequence, show the three buttons normally
        if (targetButtons != null)
        {
            foreach (var go in targetButtons)
            {
                if (go != null) go.SetActive(true);
            }
        }

        sequenceRunning = false;
    }
}
