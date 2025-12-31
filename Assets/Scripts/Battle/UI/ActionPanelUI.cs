using UnityEngine;
using System.Collections.Generic;

public class ActionPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private ActionButtonUI buttonPrefab;
    
    private List<ActionButtonUI> spawnedButtons = new List<ActionButtonUI>();
    private Dictionary<ActionType, ActionButtonUI> buttonMap = new Dictionary<ActionType, ActionButtonUI>();

    public void ShowPanel(BattleUnit unit, System.Action<ActionType> onActionSelected)
    {
        ClearButtons();
        
        List<ActionType> availableActions = GetActionsForRole(unit.Role);

        foreach (var actionType in availableActions)
        {
            ActionButtonUI newButton = Instantiate(buttonPrefab, buttonContainer);
            newButton.Setup(actionType, onActionSelected);
            spawnedButtons.Add(newButton);
            buttonMap.Add(actionType, newButton);
        }
        panel.SetActive(true);
    }

    public void UpdateButtonStates(Dictionary<ActionType, bool> feasibility)
    {
        foreach (var pair in feasibility)
        {
            if (buttonMap.TryGetValue(pair.Key, out ActionButtonUI button))
            {
                button.SetInteractable(pair.Value);
            }
        }
    }
    
    private List<ActionType> GetActionsForRole(BattleRole role)
    {
        switch (role)
        {
            case BattleRole.Vanguard:
            case BattleRole.Ranged1:
            case BattleRole.Ranged2:
                return new List<ActionType>
                {
                    ActionType.Attack,
                    ActionType.Defend,
                    ActionType.Skill,
                    ActionType.Rest,
                    ActionType.Item,
                    ActionType.Exchange,
                    ActionType.Skip
                };
            // case BattleRole.Support: // 後勤的行動可以在這裡定義
            default:
                return new List<ActionType>();
        }
    }

    public void UpdateButtonStates(BattleUnit unit, BattleRules rules)
    {
        foreach (var btnUI in spawnedButtons)
        {
            // 簡化：這裡可以加入更複雜的規則，例如體力檢查
            // bool isInteractable = rules.CanPerformAction(unit, btnUI.ActionType);
            // btnUI.SetInteractable(isInteractable);
        }
    }

    public void HidePanel()
    {
        panel.SetActive(false);
        ClearButtons();
    }
    
    private void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
        buttonMap.Clear();
    }
}