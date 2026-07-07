using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingSceneView : MonoBehaviour
{
    [Header("Ending Content")]
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] Image cgImage;

    [Header("Buttons")]
    [SerializeField] Button backToMenuButton;
    [SerializeField] string menuSceneName = "Start";

    void Start()
    {
        RenderEnding();

        if (backToMenuButton != null)
        {
            backToMenuButton.onClick.RemoveAllListeners();
            backToMenuButton.onClick.AddListener(BackToMenu);
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowFade(false, 0.5f);
    }

    void RenderEnding()
    {
        EndingData ending = EndingSystem.Instance != null
            ? EndingSystem.Instance.CurrentEnding
            : null;

        if (ending == null)
        {
            SetText(titleText, "No Ending");
            SetText(descriptionText, "No ending data is available. Trigger an ending through gameplay or call EndingSystem.ForceEnding for testing.");
            SetCg(null);
            return;
        }

        SetText(titleText, ending.endingTitle);
        SetText(descriptionText, ending.endingDescription);
        SetCg(ending.endingCG);
    }

    void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrEmpty(value) ? string.Empty : value;
    }

    void SetCg(Sprite sprite)
    {
        if (cgImage == null) return;
        if (sprite == null)
        {
            cgImage.enabled = true;
            cgImage.preserveAspect = true;
            return;
        }

        cgImage.sprite = sprite;
        cgImage.enabled = true;
        cgImage.preserveAspect = true;
    }

    void BackToMenu()
    {
        if (!string.IsNullOrEmpty(menuSceneName))
            SceneManager.LoadScene(menuSceneName);
    }
}
