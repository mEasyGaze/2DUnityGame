using UnityEngine;
using UnityEngine.UI;

public class BattleUI : MonoBehaviour
{
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;

    [Header("指揮官技能按鈕")]
    [SerializeField] private Button commanderSkillButton;
    [SerializeField] private Button cancelCommanderSkillButton;

    public void Setup(System.Action<UICommandType> onCommand)
    {
        endTurnButton.onClick.RemoveAllListeners();
        endTurnButton.onClick.AddListener(() => onCommand(UICommandType.EndTurn));
        
        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(() => onCommand(UICommandType.ResetAll));
        
        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() => onCommand(UICommandType.Back));

        if (commanderSkillButton != null)
        {
            commanderSkillButton.onClick.RemoveAllListeners();
            commanderSkillButton.onClick.AddListener(() => onCommand(UICommandType.UseCommanderSkill));
        }
        if (cancelCommanderSkillButton != null)
        {
            cancelCommanderSkillButton.onClick.RemoveAllListeners();
            cancelCommanderSkillButton.onClick.AddListener(() => onCommand(UICommandType.CancelCommanderSkill));
        }
    }
    
    public void SetEndTurnButtonInteractable(bool interactable)
    {
        endTurnButton.interactable = interactable;
    }
    
    public void SetBackButtonVisible(bool visible)
    {
        backButton.gameObject.SetActive(visible);
    }

    public void SetResetButtonVisible(bool visible)
    {
        resetButton.gameObject.SetActive(visible);
    }

    public void SetCommanderSkillButtons(bool showUseButton, bool showCancelButton, bool isInteractable)
    {
        if (commanderSkillButton != null)
        {
            commanderSkillButton.gameObject.SetActive(showUseButton);
            commanderSkillButton.interactable = isInteractable;
        }
        if (cancelCommanderSkillButton != null)
        {
            cancelCommanderSkillButton.gameObject.SetActive(showCancelButton);
        }
    }
}