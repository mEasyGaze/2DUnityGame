using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PuzzleMechanismState
{
    public bool isActive;
    public bool hasBeenUsed;
}

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueObjectIdentifier))] // 確保有 ID
public class PuzzleMechanism : MonoBehaviour, IInteractable, ISceneSaveable
{
    public enum TriggerType{Toggle, ActivateOnly, DeactivateOnly, TriggerEveryTime}

    [Header("機關行為設定")]
    [Tooltip("選擇此機關的交互行為模式")]
    [SerializeField] private TriggerType triggerType = TriggerType.Toggle;
    [Tooltip("勾選此項，機關在遊戲開始時處於 '激活' 狀態")]
    [SerializeField] private bool startsActive = false;

    [Header("狀態視覺化")]
    [Tooltip("當機關處於 '激活' 狀態時顯示的物件 (例如：亮的燈)")]
    [SerializeField] private GameObject activeVisual;
    [Tooltip("當機關處於 '未激活' 狀態時顯示的物件 (例如：暗的燈)")]
    [SerializeField] private GameObject inactiveVisual;

    [Header("觸發事件")]
    [Tooltip("當機關被 '激活' (開啟) 時觸發")]
    public UnityEvent onActivate;
    [Tooltip("當機關被 '取消激活' (關閉) 時觸發")]
    public UnityEvent onDeactivate;

    // --- 未來工作 ---
    // [Header("音效")]
    // [SerializeField] private AudioClip activateSound;
    // [SerializeField] private AudioClip deactivateSound;
    // private AudioSource audioSource;

    private bool isActive;
    private bool hasBeenUsed = false;

    void Start()
    {
        if (!SaveManager.Instance)
        {
            isActive = startsActive;
            UpdateVisualState();
        }
        else if (!hasBeenUsed){}
        UpdateVisualState();
    }

    public void Interact()
    {
        switch (triggerType)
        {
            case TriggerType.Toggle:
                ToggleState();
                break;

            case TriggerType.ActivateOnly:
                if (!hasBeenUsed && !isActive)
                {
                    Activate();
                    hasBeenUsed = true;
                }
                break;

            case TriggerType.DeactivateOnly:
                if (!hasBeenUsed && isActive)
                {
                    Deactivate();
                    hasBeenUsed = true;
                }
                break;

            case TriggerType.TriggerEveryTime:
                Debug.Log($"觸發了重複性機關: {gameObject.name}");
                onActivate.Invoke();
                // PlaySound(activateSound);
                break;
        }
    }

    private void ToggleState()
    {
        if (isActive) Deactivate();
        else Activate();
    }

    private void Activate()
    {
        isActive = true;
        onActivate.Invoke();
        UpdateVisualState();
    }

    private void Deactivate()
    {
        isActive = false;
        onDeactivate.Invoke();
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (activeVisual != null) activeVisual.SetActive(isActive);
        if (inactiveVisual != null) inactiveVisual.SetActive(!isActive);
    }
    
    // 輔助方法：播放音效 (未來工作)
    // private void PlaySound(AudioClip clip)
    // {
    //     if (audioSource != null && clip != null)
    //     {
    //         audioSource.PlayOneShot(clip);
    //     }
    // }

    #region ISceneSaveable
    public object CaptureState()
    {
        return new PuzzleMechanismState
        {
            isActive = this.isActive,
            hasBeenUsed = this.hasBeenUsed
        };
    }

    public void RestoreState(object stateData)
    {
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<PuzzleMechanismState>(stateJson);
            this.isActive = state.isActive;
            this.hasBeenUsed = state.hasBeenUsed;
            UpdateVisualState();
        }
    }
    #endregion
}