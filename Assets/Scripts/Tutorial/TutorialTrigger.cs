using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("觸發設定")]
    [Tooltip("要觸發的教學的唯一ID。")]
    [SerializeField] private string tutorialID;
    [Tooltip("勾選此項，當玩家進入觸發器範圍時自動觸發。")]
    [SerializeField] private bool triggerOnEnter = true;
    [Tooltip("勾選此項，觸發器觸發一次後即失效。")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnEnter && other.CompareTag("Player"))
        {
            Trigger();
        }
    }

    public void Trigger()
    {
        if (triggerOnce && hasTriggered) return;
        
        TutorialManager.Instance.ShowTutorial(tutorialID);
        hasTriggered = true;
        
        if (triggerOnce)
        {
            gameObject.SetActive(false);
        }
    }
}