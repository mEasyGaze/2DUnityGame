using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    private ActionType actionType;
    
    public void Setup(ActionType type, System.Action<ActionType> onClickCallback)
    {
        buttonText.text = type.ToActionName();
        
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback(type));
    }
    
    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }
}