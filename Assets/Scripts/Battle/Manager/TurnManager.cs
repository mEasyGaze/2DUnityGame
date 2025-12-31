using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    #region 屬性與狀態變數 (Properties & State Variables)
    public BattleState CurrentState { get; private set; }
    private BattleManager battleManager;
    private BattleUIManager uiManager;
    private TurnActionPlanner actionPlanner;
    private BattleRules battleRules;
    private BattleActions battleActions;
    private EnemyBattleAI enemyAI;
    private CharacterStateRule characterStateRule;
    private BattlefieldStateSimulator stateSimulator;
    private SkillData selectedSkill;
    private AIPersonalitySO currentEnemyPersonality;

    private int currentPlanningStepIndex;
    private PlanningSubState currentSubState;
    private BattleUnit selectedUnitForAction;
    private ActionType selectedActionType;
    private Item selectedItem;
    private List<IBattleUnit_ReadOnly> currentEligibleActors;
    private Stack<PlanningSubState> planningHistory = new Stack<PlanningSubState>();
    private ActionPlan plannedCommanderSkill = null;
    private bool hasCommanderSkillBeenUsedThisBattle = false;
    private BattleUnit supportUnit = null;

    private int currentTurnNumber = 0;
    #endregion

    #region 初始化與啟動 (Initialization & Startup)
    public void Initialize(BattleManager bm, BattleUIManager uim, TurnActionPlanner ap, BattleRules br, BattleActions ba, EnemyBattleAI eai, CharacterStateRule csr, BattlefieldStateSimulator sim, AIPersonalitySO personality)
    {
        battleManager = bm;
        uiManager = uim;
        actionPlanner = ap;
        battleRules = br;
        battleActions = ba;
        enemyAI = eai;
        characterStateRule = csr;
        stateSimulator = sim;
        currentEnemyPersonality = personality;

        supportUnit = battleManager.PlayerUnits.FirstOrDefault(u => u.Role == BattleRole.Support);
        currentEligibleActors = new List<IBattleUnit_ReadOnly>();
    }

    public void StartBattle()
    {
        BattleLog.Instance.AddLog("戰鬥開始！");
        SetState(BattleState.PlayerPlanning);
    }
    #endregion

    #region 宏觀狀態機管理 (Macro State Machine Management)
    private void SetState(BattleState newState)
    {
        if (CurrentState == newState) return;     
        CurrentState = newState;
        StartCoroutine(OnEnterState(newState));
    }
    
    private IEnumerator OnEnterState(BattleState state)
    {
        switch (state)
        {
            case BattleState.PlayerPlanning:
                HandleEnterPlayerPlanning();
                break;
            case BattleState.EnemyTurn:
                yield return StartCoroutine(HandleEnterEnemyTurn());
                if (plannedCommanderSkill != null)
                {
                    yield return StartCoroutine(ExecuteCommanderSkill());
                }
                SetState(BattleState.ActionExecution);
                break;
            case BattleState.ActionExecution:
                HandleEnterActionExecution();
                break;
            case BattleState.Won:
                HandleEnterWon();
                break;
            case BattleState.Lost:
                HandleEnterLost();
                break;
        }
    }
    private void HandleEnterPlayerPlanning()
    {
        currentTurnNumber++;
        BattleLog.Instance.AddLog($"==== 第 {currentTurnNumber} 回合：玩家規劃階段 ====");
        BuffManager.Instance.TickAllBuffsOnAllUnits(battleManager.GetAllUnits());
        UpdateAuraEffects();
        if (uiManager != null)
        {
            uiManager.SetResetButtonVisible(true);
            uiManager.SetEndTurnButtonInteractable(false);
            uiManager.SetBackButtonVisible(false);
            BuffManager.Instance.TickAllBuffsOnAllUnits(battleManager.GetAllUnits());
        }
        actionPlanner.PrepareForNewTurn();
        characterStateRule.RestoreAllUnitStamina(battleManager.GetAllUnits());
        UpdateCommanderSkillUI();
        CommitResetAllPlans();
    }

    private IEnumerator HandleEnterEnemyTurn()
    {
        BattleLog.Instance.AddLog("==== 敵人回合 ====");
        uiManager.EnterEnemyTurnState();
        enemyAI.PlanActions(currentEnemyPersonality, battleManager.EnemyUnits, battleManager.PlayerUnits, actionPlanner, battleRules, currentTurnNumber);
        yield return new WaitForSeconds(1f);
        if (plannedCommanderSkill != null)
        {
            yield return StartCoroutine(ExecuteCommanderSkill());
        }
        SetState(BattleState.ActionExecution);
    }

    private void HandleEnterActionExecution()
    {
        BattleLog.Instance.AddLog("==== 行動執行階段 ====");
        uiManager.EnterActionExecutionState();
        StartCoroutine(ExecuteActions());
    }

    private void HandleEnterWon()
    {
        BattleLog.Instance.AddLog("★★ 戰鬥勝利 ★★");
        uiManager.ShowVictoryScreen(battleManager.EncounterData.totalGoldReward);
    }

    private void HandleEnterLost()
    {
        BattleLog.Instance.AddLog("戰鬥失敗...");
        uiManager.ShowDefeatScreen();
    }
    #endregion

    #region 玩家輸入處理 (Player Input Handling)
    public void OnUnitClicked(BattleUnit unit)
    {
        if (CurrentState != BattleState.PlayerPlanning) return;

        switch(currentSubState)
        {
            case PlanningSubState.SelectingRangedUnit:
                if (currentEligibleActors.Any(u => u.GetMonoBehaviour() == unit))
                {
                    selectedUnitForAction = unit;
                    GoToPlanningSubState(PlanningSubState.SelectingAction);
                }
                break;
                
            case PlanningSubState.SelectingTarget:
                var validTargets = battleRules.GetValidTargets(selectedUnitForAction, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                if (validTargets.Any(t => t.GetMonoBehaviour() == unit))
                {
                    CommitNewPlan(selectedUnitForAction, selectedActionType, unit);
                }
                break;

            case PlanningSubState.SelectingExchangeTarget:
                var validExchangeTargets = battleRules.GetValidExchangeTargets(selectedUnitForAction, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                if (validExchangeTargets.Any(t => t.GetMonoBehaviour() == unit))
                {
                    CommitNewExchangePlan(selectedUnitForAction, unit);
                }
                break;

            case PlanningSubState.SelectingItemTarget:
                var validItemTargets = battleRules.GetValidItemTargets(selectedUnitForAction);
                if (validItemTargets.Any(u => u.GetMonoBehaviour() == unit))
                {
                    CommitNewItemPlan(selectedUnitForAction, selectedItem, unit);
                }
                break;
            case PlanningSubState.SelectingSkillTarget:
                var validSkillTargets = battleRules.GetValidSkillTargets(selectedUnitForAction, selectedSkill, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                if (validSkillTargets.Any(t => t.GetMonoBehaviour() == unit))
                {
                    CommitNewSkillPlan(selectedUnitForAction, unit, selectedSkill);
                }
                break;
        }
    }
    
    public void OnActionSelected(ActionType type)
    {
        if (CurrentState != BattleState.PlayerPlanning || currentSubState != PlanningSubState.SelectingAction) return;
        
        selectedActionType = type;
        
        if (type == ActionType.Skill)
        {
            GoToPlanningSubState(PlanningSubState.SelectingSkill);
        }
        else if (type == ActionType.Attack)
        {
            GoToPlanningSubState(PlanningSubState.SelectingTarget);
        }
        else if (type == ActionType.Exchange)
        {
            GoToPlanningSubState(PlanningSubState.SelectingExchangeTarget);
        }
        else if (type == ActionType.Item)
        {
            GoToPlanningSubState(PlanningSubState.SelectingItem);
        }
        else
        {
            CommitNewPlan(selectedUnitForAction, type, null);
        }
    }

    public void OnItemSelected(Item item)
    {
        if (CurrentState != BattleState.PlayerPlanning || currentSubState != PlanningSubState.SelectingItem) return;
        selectedItem = item;        
        GoToPlanningSubState(PlanningSubState.SelectingItemTarget);
    }

    public void OnSkillSelected(SkillData skill)
    {
        if (CurrentState != BattleState.PlayerPlanning || currentSubState != PlanningSubState.SelectingSkill) return;

        selectedSkill = skill;
        stateSimulator.ShowTemporaryStaminaPreview(selectedUnitForAction, skill.staminaCost);

        switch (skill.targetType)
        {
            case SkillTargetType.None:
            case SkillTargetType.Self:
            case SkillTargetType.Ally_All:
            case SkillTargetType.Enemy_All:
            case SkillTargetType.Enemy_Penetrate:
                CommitNewSkillPlan(selectedUnitForAction, null, selectedSkill);
                break;
            case SkillTargetType.Enemy_Single:
            case SkillTargetType.Ally_Single:
                GoToPlanningSubState(PlanningSubState.SelectingSkillTarget);
                break;
        }
    }

    public void OnUICommand(UICommandType command, ActionPlan planData = null)
    {
        if (CurrentState != BattleState.PlayerPlanning)
        {
            if (command == UICommandType.EndTurn) SetState(BattleState.EnemyTurn);
            return;
        }

        if (currentSubState == PlanningSubState.SelectingItem)
        {
            if (command == UICommandType.Back || command == UICommandType.ResetAll)
            {
                BattleLog.Instance.AddLog("從道具選擇返回。");
                uiManager.HideItemSelectionPanel();
                GoToPlanningSubState(PlanningSubState.SelectingAction);
                return;
            }
        }

        if (currentSubState == PlanningSubState.SelectingItemTarget)
        {
            if (command == UICommandType.Back)
            {
                CommitGoBack(); 
                return;
            }
        }

        switch (command)
        {
            case UICommandType.EndTurn: SetState(BattleState.EnemyTurn); break;
            case UICommandType.ResetAll: CommitResetAllPlans(); break;
            case UICommandType.CancelSingleAction: CommitCancelLastPlan(); break;
            case UICommandType.Back: CommitGoBack(); break;
            case UICommandType.UseCommanderSkill: HandleUseCommanderSkill(); break;
            case UICommandType.CancelCommanderSkill: HandleCancelCommanderSkill(); break;
        }
    }
    #endregion

    #region 事務性操作 (Transactional Operations)
    private void CommitNewPlan(IBattleUnit_ReadOnly actor, ActionType type, IBattleUnit_ReadOnly target)
    {
        planningHistory.Clear();
        int phaseIndex = (currentPlanningStepIndex / 2) + 1;
        BattleRole stepRole = GetRoleForStep(currentPlanningStepIndex);
        
        ActionPlan newPlan = ActionPlan.CreatePlayerAction(actor, target, type, phaseIndex, stepRole);

        actionPlanner.AddPlan(newPlan, currentPlanningStepIndex);
        characterStateRule.GenerateAndStoreNextSnapshot(newPlan, battleActions);
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        AdvancePlanningStep();
    }

    private void CommitNewNoTargetPlan(IBattleUnit_ReadOnly actor, ActionType type)
    {
        planningHistory.Clear();
        int phaseIndex = (currentPlanningStepIndex / 2) + 1;
        BattleRole stepRole = GetRoleForStep(currentPlanningStepIndex);
        
        ActionPlan newPlan = ActionPlan.CreateNoTargetAction(actor, type, phaseIndex, stepRole);

        actionPlanner.AddPlan(newPlan, currentPlanningStepIndex);
        characterStateRule.GenerateAndStoreNextSnapshot(newPlan, battleActions);
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        AdvancePlanningStep();
    }
    
    private void CommitNewExchangePlan(IBattleUnit_ReadOnly actor, IBattleUnit_ReadOnly target)
    {
        planningHistory.Clear();
        var latestSnapshot = characterStateRule.GetLatestSnapshot();
        var actorSnap = latestSnapshot.UnitSnapshots.First(s => s.Unit == actor);
        var targetSnap = latestSnapshot.UnitSnapshots.First(s => s.Unit == target);
        if (actorSnap.Stamina < battleActions.GetExchangeStaminaCost())
        {
            BattleLog.Instance.AddLog($"交換失敗：{actor.UnitName} 體力不足！");
            return;
        }
        int actorStepIndex = currentPlanningStepIndex;
        var excludeList = new List<int> { actorStepIndex };
        int targetStepIndex = actionPlanner.FindNextAvailableStep(targetSnap.Role, 0, excludeList);
        if (targetStepIndex == -1)
        {
            BattleLog.Instance.AddLog($"交換失敗：沒有可用的行動格給 {target.UnitName}！");
            return;
        }
        int phaseIndex = (actorStepIndex / 2) + 1; 
        Guid transactionID = Guid.NewGuid();

        BattleRole actorStepRole = GetRoleForStep(actorStepIndex);
        BattleRole targetStepRole = GetRoleForStep(targetStepIndex);

        var planA = ActionPlan.CreatePlayerAction(actor, target, ActionType.Exchange, phaseIndex, actorStepRole, transactionID);
        var planB = ActionPlan.CreatePlayerAction(target, actor, ActionType.Exchange, phaseIndex, targetStepRole, transactionID);
        
        actionPlanner.AddPlan(planA, actorStepIndex);
        actionPlanner.AddPlan(planB, targetStepIndex);
        
        characterStateRule.GenerateAndStoreNextSnapshot(planA, battleActions);
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        AdvancePlanningStep();
    }
    
    private void CommitCancelLastPlan()
    {
        uiManager.HideSkillSelectionPanel();
        planningHistory.Clear();
        int lastPlayerPlanStep = actionPlanner.GetLastPlayerPlanStepIndex();
        if (lastPlayerPlanStep == -1)
        {
            BattleLog.Instance.AddLog("沒有任何玩家規劃的行動可以取消。");
            return;
        }
        
        ActionPlan lastPlayerPlan = actionPlanner.GetPlanAtStep(lastPlayerPlanStep);
        if (lastPlayerPlan != null && lastPlayerPlan.Source != null)
        {
            BattleLog.Instance.AddLog($"正在撤銷行動: {lastPlayerPlan.Source.UnitName} -> {lastPlayerPlan.Type.ToActionName()}");
        }
        actionPlanner.RemovePlansFromStepOnward(lastPlayerPlanStep);
        characterStateRule.PruneSnapshotsToCount(actionPlanner.GetPlayerPlanCount() + 1);
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        currentPlanningStepIndex = lastPlayerPlanStep;
        AdvancePlanningStep(false);
    }

    private void CommitGoBack()
    {
        if (currentSubState == PlanningSubState.SelectingSkill)
        {
            uiManager.HideSkillSelectionPanel();
        }
        if (planningHistory.Count > 0)
        {
            PlanningSubState previousState = planningHistory.Pop();
            
            BattleLog.Instance.AddLog("返回上一步操作。");
            stateSimulator.ClearTemporaryPreviews();
            GoToPlanningSubState(previousState, false); 
        }
        else
        {
            Debug.LogWarning("沒有可返回的微觀操作歷史。");
        }
    }
    
    private void CommitResetAllPlans()
    {
        uiManager.HideSkillSelectionPanel();
        planningHistory.Clear();
        actionPlanner.ClearPlayerPlans();
        characterStateRule.InitializeSnapshots(battleManager.GetAllUnits());
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        uiManager.SetEndTurnButtonInteractable(false);
        StartCoroutine(StartGuidedPlanning());
        BattleLog.Instance.AddLog("已重置所有規劃。");
    }

    private void CommitNewItemPlan(IBattleUnit_ReadOnly actor, Item item, IBattleUnit_ReadOnly target)
    {
        planningHistory.Clear();

        var latestSnapshot = characterStateRule.GetLatestSnapshot();
        if (latestSnapshot == null || !latestSnapshot.InventorySnapshot.HasItem(item.uniqueItemID))
        {
            BattleLog.Instance.AddLog($"預演失敗：模擬背包中已無 {item.itemName}，請選擇其他道具或行動。");
            GoToPlanningSubState(PlanningSubState.SelectingItem); 
            return;
        }
        int phaseIndex = (currentPlanningStepIndex / 2) + 1;
        BattleRole stepRole = GetRoleForStep(currentPlanningStepIndex);
        ActionPlan newPlan = ActionPlan.CreatePlayerAction(actor, target, ActionType.Item, phaseIndex, stepRole, default, item);
        
        actionPlanner.AddPlan(newPlan, currentPlanningStepIndex);
        characterStateRule.GenerateAndStoreNextSnapshot(newPlan, battleActions);
        
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        AdvancePlanningStep();
    }

    private void CommitNewSkillPlan(IBattleUnit_ReadOnly actor, IBattleUnit_ReadOnly target, SkillData skill)
    {
        planningHistory.Clear();
        int phaseIndex = (currentPlanningStepIndex / 2) + 1;
        BattleRole stepRole = GetRoleForStep(currentPlanningStepIndex);
        
        ActionPlan newPlan = ActionPlan.CreateSkillAction(actor, target, skill, phaseIndex, stepRole);

        actionPlanner.AddPlan(newPlan, currentPlanningStepIndex);
        characterStateRule.GenerateAndStoreNextSnapshot(newPlan, battleActions);
        stateSimulator.ShowStateFromSnapshot(characterStateRule.GetLatestSnapshot().UnitSnapshots);
        
        AdvancePlanningStep();
    }

    private void HandleUseCommanderSkill()
    {
        if (supportUnit == null || hasCommanderSkillBeenUsedThisBattle) return;

        SkillData commanderSkill = supportUnit.Skills.FirstOrDefault(s => s.isCommanderSkill_OneTimeUse);
        if (commanderSkill == null)
        {
            BattleLog.Instance.AddLog(LogFormatter.System("錯誤：後勤單位沒有找到指揮官技能！"));
            return;
        }

        plannedCommanderSkill = ActionPlan.CreateSkillAction(supportUnit, null, commanderSkill, 0, BattleRole.Support);
        
        BattleLog.Instance.AddLog($"{LogFormatter.Unit(supportUnit)} 已準備施放指揮官技能 [{commanderSkill.skillName}]。");
        UpdateCommanderSkillUI();
    }
    
    private void HandleCancelCommanderSkill()
    {
        if (plannedCommanderSkill == null) return;
        
        string skillName = plannedCommanderSkill.SkillUsed.skillName;
        plannedCommanderSkill = null;
        
        BattleLog.Instance.AddLog($"取消了指揮官技能 [{skillName}] 的施放。");
        UpdateCommanderSkillUI();
    }

    #endregion
    
    #region 規劃階段核心邏輯 (Planning Phase Core Logic)
    private IEnumerator StartGuidedPlanning()
    {
        selectedUnitForAction = null;
        selectedActionType = default;
        currentSubState = PlanningSubState.None;
        
        currentPlanningStepIndex = 0;
        AdvancePlanningStep();
        yield return null;
    }

    private void AdvancePlanningStep(bool isPlayerInitiated = true)
    {
        uiManager.UpdateActionSlots(actionPlanner);
        if (isPlayerInitiated)
        {
            planningHistory.Clear();
        }
        if (actionPlanner.IsPlanningFinished())
        {
            GoToPlanningSubState(PlanningSubState.None);
            return;
        }
        while (currentPlanningStepIndex < 8)
        {
            currentPlanningStepIndex = actionPlanner.GetNextPlanningStepIndex();
            if (currentPlanningStepIndex >= 8)
            {
                GoToPlanningSubState(PlanningSubState.None);
                return;
            }
            
            var latestSnapshotUnits = characterStateRule.GetLatestSnapshot().UnitSnapshots;
            currentEligibleActors = battleRules.GetEligibleActorsForStep(currentPlanningStepIndex, latestSnapshotUnits, actionPlanner);

            if (currentEligibleActors.Count > 0)
            {
                if (currentEligibleActors.Count == 1)
                {
                    selectedUnitForAction = currentEligibleActors[0].GetMonoBehaviour();
                    planningHistory.Push(PlanningSubState.None);
                    GoToPlanningSubState(PlanningSubState.SelectingAction);
                }
                else
                {
                    GoToPlanningSubState(PlanningSubState.SelectingRangedUnit);
                }
                return;
            }
            else
            {
                BattleLog.Instance.AddLog($"在階段 {currentPlanningStepIndex + 1} 沒有可行動的單位，自動跳過。");
                
                int phaseIndex = (currentPlanningStepIndex / 2) + 1;
                BattleRole stepRole = GetRoleForStep(currentPlanningStepIndex);
                ActionPlan skipPlan = ActionPlan.CreateEmptyAction(phaseIndex, stepRole);

                actionPlanner.AddPlan(skipPlan, currentPlanningStepIndex);
                characterStateRule.GenerateAndStoreNextSnapshot(skipPlan, battleActions);
                uiManager.UpdateActionSlots(actionPlanner);
            }
        }
    }
    
    private void GoToNextStep()
    {
        selectedUnitForAction = null;
        selectedActionType = default;
        currentSubState = PlanningSubState.None;
        
        AdvancePlanningStep();
    }
    #endregion

    #region 規劃階段輔助方法 (Planning Phase Helper Methods)
    private void GoToPlanningSubState(PlanningSubState nextState, bool isForwardStep = true)
    {
        if (currentSubState == PlanningSubState.SelectingItemTarget && nextState != PlanningSubState.SelectingItemTarget)
        {
            if (CursorManager.Instance != null) 
            {
                CursorManager.Instance.ResetCursor();
                CursorManager.Instance.HideTooltip();
            }
        }
        currentSubState = nextState;
        
        bool canGoBack = planningHistory.Count > 0 && nextState != PlanningSubState.SelectingRangedUnit && nextState != PlanningSubState.None;
        uiManager.SetBackButtonVisible(planningHistory.Count > 0);

        switch (nextState)
        {
            case PlanningSubState.None:
                if (!isForwardStep)
                {
                    BattleLog.Instance.AddLog("已返回至行動格規劃起點。");
                    selectedUnitForAction = null;
                    selectedActionType = default;
                    AdvancePlanningStep();
                }
                else
                {
                    uiManager.ShowPlanningFinishedState();
                }
                break;
                
            case PlanningSubState.SelectingRangedUnit:
                selectedUnitForAction = null;
                selectedActionType = default;
                uiManager.ShowRangedUnitSelection(ConvertFromReadOnlyList(currentEligibleActors), currentPlanningStepIndex);
                break;

            case PlanningSubState.SelectingAction:
                selectedActionType = default;
                var feasibility = battleRules.GetActionFeasibility(selectedUnitForAction, characterStateRule.GetLatestSnapshot(), actionPlanner);
                uiManager.ShowActionSelectionFor(selectedUnitForAction, currentPlanningStepIndex, feasibility);
                break;

            case PlanningSubState.SelectingTarget:
            {
                var validTargets = battleRules.GetValidTargets(selectedUnitForAction, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                uiManager.ShowTargetSelection(ConvertFromReadOnlyList(validTargets));
                break;
            }

            case PlanningSubState.SelectingExchangeTarget:
                var validExchangeTargets = battleRules.GetValidExchangeTargets(selectedUnitForAction, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                uiManager.ShowTargetSelection(ConvertFromReadOnlyList(validExchangeTargets));
                break;
            
            case PlanningSubState.SelectingItem:
                selectedItem = null;
                uiManager.ShowItemSelection(OnItemSelected);
                break;

            case PlanningSubState.SelectingItemTarget:
                if (CursorManager.Instance != null && selectedItem != null)
                {
                    CursorManager.Instance.SetCursorIcon(selectedItem.icon);
                    int count = InventoryManager.Instance.GetItemCount(selectedItem);
                    CursorManager.Instance.SetQuantityText(count.ToString());
                    CursorManager.Instance.HideTooltip();
                }
                var validItemTargets = battleRules.GetValidItemTargets(selectedUnitForAction);
                uiManager.ShowItemTargetSelection(ConvertFromReadOnlyList(validItemTargets));
                break;

            case PlanningSubState.SelectingSkill:
                selectedSkill = null;
                var skillFeasibility = new Dictionary<SkillData, bool>();
                var latestSnapshot = characterStateRule.GetLatestSnapshot();
                
                foreach (var skill in selectedUnitForAction.Skills)
                {
                    bool isUsable = battleRules.IsSkillUsable(selectedUnitForAction, skill, latestSnapshot);
                    skillFeasibility.Add(skill, isUsable);
                }
                uiManager.ShowSkillSelection(skillFeasibility, OnSkillSelected);
                break;

            case PlanningSubState.SelectingSkillTarget:
            {
                var validTargets = battleRules.GetValidSkillTargets(selectedUnitForAction, selectedSkill, characterStateRule.GetLatestSnapshot().UnitSnapshots);
                uiManager.ShowTargetSelection(ConvertFromReadOnlyList(validTargets));
                break;
            }
        }
    }

    private List<BattleUnit> ConvertFromReadOnlyList(List<IBattleUnit_ReadOnly> readOnlyList)
    {
        return readOnlyList.Select(u => u.GetMonoBehaviour()).ToList();
    }

    private void ShowActionPanelForUnit(BattleUnit unit)
    {
        var feasibility = battleRules.GetActionFeasibility(unit, characterStateRule.GetLatestSnapshot(), actionPlanner);
        uiManager.ShowActionSelectionFor(unit, currentPlanningStepIndex, feasibility);
    }
    
    private BattleRole GetRoleForStep(int stepIndex)
    {
        return (stepIndex % 2 == 0) ? BattleRole.Vanguard : BattleRole.Ranged1;
    }

    private void UpdateCommanderSkillUI()
    {
        if (uiManager == null) return;
        if (supportUnit == null || supportUnit.IsDead)
        {
            uiManager.SetCommanderSkillButtons(false, false, false);
            return;
        }

        if (!supportUnit.Skills.Any(s => s.isCommanderSkill_OneTimeUse))
        {
            uiManager.SetCommanderSkillButtons(false, false, false);
            return;
        }

        bool canUse = !hasCommanderSkillBeenUsedThisBattle;
        bool isPlanned = (plannedCommanderSkill != null);

        if (hasCommanderSkillBeenUsedThisBattle)
        {
            uiManager.SetCommanderSkillButtons(false, false, false);
        }
        else if (!isPlanned)
        {
            uiManager.SetCommanderSkillButtons(true, false, canUse);
        }
        else
        {
            uiManager.SetCommanderSkillButtons(false, true, true);
        }
    }
    #endregion
    
    #region 行動執行階段 (Action Execution Phase)
    private IEnumerator ExecuteActions()
    {
        foreach(var unit in battleManager.GetAllUnits())
        {
            unit.ResetVisualsToCoreState();
        }

        var processedTransactions = new HashSet<Guid>();
        for (int phase = 1; phase <= 4; phase++)
        {
            var executionOrder = new[]
            {
                new { IsPlayer = true, Role = BattleRole.Vanguard },
                new { IsPlayer = false, Role = BattleRole.Vanguard },
                new { IsPlayer = true, Role = BattleRole.Ranged1 },
                new { IsPlayer = false, Role = BattleRole.Ranged1 }
            };

            foreach(var step in executionOrder)
            {
                yield return StartCoroutine(ExecuteStep(phase, step.IsPlayer, step.Role, processedTransactions));
                if (CheckBattleEnd(phase)) yield break; 
            }
        }
        UpdateAuraEffects();

        if (CurrentState == BattleState.ActionExecution)
        {
            SetState(BattleState.PlayerPlanning);
        }
    }
    
    private IEnumerator ExecuteStep(int phase, bool isPlayer, BattleRole role, HashSet<Guid> processedTransactions)
    {
        ActionPlan plan = actionPlanner.GetActionForRole(phase, isPlayer, role);
        if (plan != null && plan.Type != ActionType.Skip)
        {
            if (plan.Source != null && !plan.Source.IsDead)
            {
                battleActions.Execute(plan, actionPlanner, processedTransactions, battleManager);
                if (plan.Type == ActionType.Exchange && plan.Target != null && !plan.Target.IsDead)
                {
                    BattleUnit sourceUnit = plan.Source.GetMonoBehaviour();
                    BattleUnit targetUnit = plan.Target.GetMonoBehaviour();

                    yield return StartCoroutine(battleManager.AnimateExchangeCoroutine(sourceUnit, targetUnit, 1.5f));
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
    }

    private IEnumerator ExecuteCommanderSkill()
    {
        if (plannedCommanderSkill == null) yield break;

        BattleLog.Instance.AddLog(LogFormatter.System("==== 指揮官技能發動！ ===="));
        battleActions.Execute(plannedCommanderSkill, null, null, battleManager);
        
        hasCommanderSkillBeenUsedThisBattle = true;
        plannedCommanderSkill = null;

        UpdateCommanderSkillUI();

        yield return new WaitForSeconds(1.5f);
    }

    private bool CheckBattleEnd(int currentPhase)
    {
        actionPlanner.RemovePlansFromDeadUnits(battleManager.GetAllUnits());
        battleManager.HandleTeamPromotions();
        actionPlanner.ValidateAndCleanupInvalidPlans(battleRules, currentPhase);
        uiManager.UpdateActionSlots(actionPlanner);
        
        if (battleRules.IsVictory(battleManager.EnemyUnits))
        {
            SetState(BattleState.Won);
            return true;
        }
        if (battleRules.IsDefeat(battleManager.PlayerUnits))
        {
            SetState(BattleState.Lost);
            return true;
        }
        return false;
    }
    #endregion

    #region 光環效果管理 (Aura Effect Management)
    private void UpdateAuraEffects()
    {
        foreach (var unit in battleManager.GetAllUnits())
        {
            if (unit != null)
            {
                unit.GetComponent<BuffController>()?.ClearAllAuras();
            }
        }
        ApplyAurasForTeam(true);
        ApplyAurasForTeam(false);
    }

    private void ApplyAurasForTeam(bool isPlayerTeam)
    {
        GridPosition supportPos = isPlayerTeam ? GridPosition.PlayerSupport : GridPosition.EnemySupport;
        BattleUnit supportUnit = battleManager.GetUnitAtPosition(supportPos);

        if (supportUnit == null || supportUnit.IsDead) return;

        foreach (var skill in supportUnit.Skills)
        {
            if (skill.skillType == SkillType.CommanderPassive)
            {
                foreach (var effect in skill.effects)
                {
                    if (effect is SkillEffect_ApplyBuff buffEffect && buffEffect.isCommanderPassiveAura)
                    {
                        BattleLog.Instance.AddLog($"{supportUnit.UnitName} 的指揮官被動技能 [{skill.skillName}] 正在發動！");
                        
                        List<BattleUnit> targetTeam;
                        if (skill.targetType == SkillTargetType.Ally_All)
                        {
                            targetTeam = battleManager.GetSameTeam(supportUnit);
                        }
                        else if (skill.targetType == SkillTargetType.Enemy_All)
                        {
                            targetTeam = battleManager.GetOpposingTeam(supportUnit);
                        }
                        else continue;

                        foreach (var buffDef in buffEffect.BuffsToApply)
                        {
                            foreach (var targetUnit in targetTeam)
                            {
                                if (targetUnit != null && !targetUnit.IsDead)
                                {
                                    targetUnit.GetComponent<BuffController>()?.ApplyAura(buffDef, supportUnit);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion
}