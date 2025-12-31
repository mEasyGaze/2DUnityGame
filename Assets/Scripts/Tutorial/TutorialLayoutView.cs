using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialLayoutView : MonoBehaviour
{
    [Header("佈局插槽 (可選)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image tutorialImage;

    void Start()
    {
        UISoundAutoHook.HookEntireScene();
    }
    
    public void Populate(TutorialStep step)
    {
        if (titleText != null)
        {
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(step.title));
            titleText.text = step.title;
        }
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(step.description));
            descriptionText.text = step.description;
        }
        if (tutorialImage != null)
        {
            tutorialImage.gameObject.SetActive(step.image != null);
            tutorialImage.sprite = step.image;
        }
    }
}