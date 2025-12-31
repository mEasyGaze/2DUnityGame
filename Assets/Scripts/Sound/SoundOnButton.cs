using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class SoundOnButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("音效設定")]
    [Tooltip("滑鼠懸停時的音效 ID (留空則不播放)")]
    [SerializeField] private string hoverSoundID = "UI_Hover";
    
    [Tooltip("點擊時的音效 ID")]
    [SerializeField] private string clickSoundID = "UI_Click";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    public void Setup(string hoverID, string clickID)
    {
        this.hoverSoundID = hoverID;
        this.clickSoundID = clickID;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button == null) button = GetComponent<Button>();
        if (button.interactable && !string.IsNullOrEmpty(hoverSoundID))
        {
            AudioManager.Instance?.PlayUI(hoverSoundID);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null) button = GetComponent<Button>();
        if (button.interactable && !string.IsNullOrEmpty(clickSoundID))
        {
            AudioManager.Instance?.PlayUI(clickSoundID);
        }
    }
}