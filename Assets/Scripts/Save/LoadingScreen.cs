using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour
{
    [Header("UI 元素連結")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI statusText;

    private CanvasGroup canvasGroup;
    private bool isInitialized = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        isInitialized = true;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        if (!isInitialized)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            isInitialized = true;
        }
        UpdateProgress(0f, "正在準備...");
        gameObject.SetActive(true); 
        canvasGroup.alpha = 1f;
    }

    public void UpdateProgress(float progress, string text)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public IEnumerator FadeOut(float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1.0f - (elapsedTime / duration);
            yield return null;
        }
        Hide();
    }
}