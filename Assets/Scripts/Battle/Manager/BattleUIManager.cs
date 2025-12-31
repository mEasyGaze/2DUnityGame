using UnityEngine;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    #region 引用與初始化
    private TurnManager turnManager;

    [Header("UI 面板")]
    [SerializeField] private ActionPanelUI actionPanel;
    [SerializeField] private ActionSlotPanelUI actionSlotPanel;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private BattleEndUI battleEndUI;
    [SerializeField] private BattleItemUI battleItemPanel;
    [SerializeField] private SkillSelectionPanelUI skillSelectionPanel;
    
    public void Initialize(TurnManager tm)
    {
        turnManager = tm;
        battleUI.Setup(cmd => OnUICommand(cmd, null));
        actionSlotPanel.Initialize((cmd, plan) => OnUICommand(cmd, plan));
        UISoundAutoHook.HookEntireScene();
    }
    #endregion
    
    #region 主狀態UI控制
    public void EnterPlayerPlanningState(TurnActionPlanner planner)
    {
        HideAllActionPanels();
        actionSlotPanel.UpdatePanel(planner);
        actionSlotPanel.gameObject.SetActive(true);
        battleUI.SetEndTurnButtonInteractable(false);
        battleUI.SetResetButtonVisible(true);
        battleUI.SetBackButtonVisible(false);
    }

    public void EnterEnemyTurnState()
    {
        HideAllActionPanels();
        battleUI.SetEndTurnButtonInteractable(false);
        battleUI.SetResetButtonVisible(false);
        battleUI.SetBackButtonVisible(false);
    }

    public void EnterActionExecutionState()
    {
        HideAllActionPanels();
    }
    #endregion
    
    #region 引導式規劃UI
    public void ShowRangedUnitSelection(List<BattleUnit> units, int stepIndex)
    {
        HideAllActionPanels();
        DeselectAllHighlights();
        actionSlotPanel.SetPlanningHighlight(stepIndex);
        units.ForEach(u => u.SetHighlight(true));
        battleUI.SetBackButtonVisible(false);
        BattleLog.Instance.AddLog("請選擇一位遠程單位來規劃行動。");
    }

    public void ShowActionSelectionFor(BattleUnit unit, int stepIndex, Dictionary<ActionType, bool> feasibility)
    {
        DeselectAllHighlights();
        unit.SetHighlight(true);
        
        actionPanel.ShowPanel(unit, turnManager.OnActionSelected);
        actionPanel.UpdateButtonStates(feasibility);
        
        actionSlotPanel.SetPlanningHighlight(stepIndex);
        battleUI.SetBackButtonVisible(true);
    }
    
    public void ShowTargetSelection(List<BattleUnit> validTargets)
    {
        actionPanel.HidePanel();
        DeselectAllHighlights();
        validTargets.ForEach(t => t.SetHighlight(true));
        BattleLog.Instance.AddLog("請選擇一個目標。");
    }

    public void ShowItemSelection(System.Action<Item> onItemSelected)
    {
        HideAllActionPanels();
        DeselectAllHighlights();
        battleItemPanel.ShowPanel(onItemSelected);
        BattleLog.Instance.AddLog("請選擇要使用的道具。");
    }

    public void ShowItemTargetSelection(List<BattleUnit> validTargets)
    {
        if (battleItemPanel != null) battleItemPanel.HidePanel();
        DeselectAllHighlights();
        validTargets.ForEach(t => t.SetHighlight(true));
        BattleLog.Instance.AddLog("請選擇使用對象。");
    }

    public void ShowPlanningFinishedState()
    {
        HideAllActionPanels();
        actionSlotPanel.SetPlanningHighlight(-1);
        battleUI.SetEndTurnButtonInteractable(true);
        battleUI.SetBackButtonVisible(false);
    }

    public void ShowSkillSelection(Dictionary<SkillData, bool> skillFeasibility, System.Action<SkillData> onSkillSelectedCallback)
    {
        HideAllActionPanels();
        DeselectAllHighlights();

        if (skillSelectionPanel != null)
        {
            skillSelectionPanel.ShowPanel(skillFeasibility, onSkillSelectedCallback);
        }
        else
        {
            Debug.LogError("BattleUIManager 未指定 SkillSelectionPanelUI！");
        }
    }
    
    public void UpdateActionSlots(TurnActionPlanner planner)
    {
        actionSlotPanel.UpdatePanel(planner);
    }
    #endregion

    #region 通用UI控制
    public void ShowVictoryScreen(int gold) { battleEndUI.ShowVictory(gold); }
    public void ShowDefeatScreen() { battleEndUI.ShowDefeat(); }
    public void SetResetButtonVisible(bool visible)
    {
        if (battleUI != null)
        {
            battleUI.SetResetButtonVisible(visible);
        }
    }
    public void SetEndTurnButtonInteractable(bool interactable)
    {
        if (battleUI != null)
        {
            battleUI.SetEndTurnButtonInteractable(interactable);
        }
    }
    public void SetBackButtonVisible(bool visible)
    {
        if (battleUI != null)
        {
            battleUI.SetBackButtonVisible(visible);
        }
    }

    public void SetCommanderSkillButtons(bool showUseButton, bool showCancelButton, bool isInteractable)
    {
        if (battleUI != null)
        {
            battleUI.SetCommanderSkillButtons(showUseButton, showCancelButton, isInteractable);
        }
        else
        {
            Debug.LogWarning("BattleUIManager 無法控制指揮官技能按鈕，因為 BattleUI 未指定！");
        }
    }
    
    private void HideAllActionPanels()
    {
        actionPanel.HidePanel();
        if (battleItemPanel != null) battleItemPanel.HidePanel();
        DeselectAllHighlights();
        actionSlotPanel.SetPlanningHighlight(-1);
    }

    public void HideItemSelectionPanel()
    {
        if (battleItemPanel != null)
        {
            battleItemPanel.HidePanel();
        }
    }
    
    public void HideSkillSelectionPanel()
    {
        if (skillSelectionPanel != null)
        {
            skillSelectionPanel.HidePanel();
        }
    }

    private void DeselectAllHighlights()
    {
        if (BattleManager.Instance == null) return;
        foreach(var unit in BattleManager.Instance.PlayerUnits) unit.SetHighlight(false);
        foreach(var unit in BattleManager.Instance.EnemyUnits) unit.SetHighlight(false);
    }
    #endregion
    
    #region 事件傳遞
    private void OnUICommand(UICommandType command, ActionPlan planData = null)
    {
        turnManager.OnUICommand(command, planData);
    }
    #endregion
}