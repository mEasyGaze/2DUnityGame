using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ExploreProgressBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI actionText;

    private Coroutine fillCoroutine;

    public void StartProgress(float duration, string text)
    {
        if (slider == null || actionText == null)
        {
            Debug.LogError("進度條UI元件未正確連結！");
            gameObject.SetActive(false);
            return;
        }

        actionText.text = text;
        
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }
        fillCoroutine = StartCoroutine(FillBarCoroutine(duration));
    }

    public void Cancel()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator FillBarCoroutine(float duration)
    {
        float elapsedTime = 0f;
        slider.value = 0;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            slider.value = elapsedTime / duration;
            yield return null;
        }
        slider.value = 1;
        
        ExplorationUIManager.Instance.OnProgressComplete();
        
        gameObject.SetActive(false);
        fillCoroutine = null;
    }
}