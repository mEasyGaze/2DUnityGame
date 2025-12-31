using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

[CreateAssetMenu(fileName = "EnemyBattleAI", menuName = "Battle System/AI/Enemy AI Controller")]
public class EnemyBattleAI : ScriptableObject
{
    [Header("核心戰鬥邏輯")]
    [Tooltip("【必須指定!】用於定義行動消耗與效果的 BattleActions 檔案。")]
    [SerializeField] private BattleActions battleActions;

    private class ScoredAction
    {
        public ActionPlan Plan { get; }
        public float Score { get; }
        public string Log { get; }
        public ScoredAction(ActionPlan plan, float score, string log) 
        { 
            Plan = plan; 
            Score = score; 
            Log = log;
        }
    }

    public void PlanActions(AIPersonalitySO personality, List<BattleUnit> selfUnits, List<BattleUnit> opponentUnits, TurnActionPlanner planner, BattleRules rules, int turnCount)
    {
        if (battleActions == null)
        {
            Debug.LogError($"AI 控制器 '{this.name}' 未在 Inspector 中指定 'Battle Actions' ScriptableObject！AI 無法行動。");
            BattleLog.Instance.AddLog("錯誤：敵人 AI 配置不完整，無法行動。");
            return;
        }
        Debug.Log($"<color=purple>===== AI TURN {turnCount} PLANNING (Personality: {personality?.name ?? "Fallback"}) =====</color>");
        
        if (personality != null)
        {
            ExecuteSmartAI(personality, selfUnits, opponentUnits, planner, rules, turnCount);
        }
        else
        {
            Debug.Log("<color=orange>No personality assigned for this encounter. Using simple fallback AI.</color>");
            ExecuteSimpleFallbackAI(selfUnits, opponentUnits, planner, rules);
        }
        
        Debug.Log($"<color=purple>===== AI TURN PLANNING COMPLETE =====</color>");
        BattleLog.Instance.AddLog("敵人已完成行動規劃。");
    }
    
    #region 智能AI邏輯
    private void ExecuteSmartAI(AIPersonalitySO personality, List<BattleUnit> selfUnits, List<BattleUnit> opponentUnits, TurnActionPlanner planner, BattleRules rules, int turnCount)
    {
        var staminaTracker = selfUnits.Where(u => !u.IsDead).ToDictionary(u => (IBattleUnit_ReadOnly)u, u => u.CurrentStamina);
        var context = new AIContext(selfUnits, opponentUnits, turnCount);
        foreach (var actor in selfUnits.Where(u => !u.IsDead).OrderBy(u => u.Role))
        {
            int maxActions = (actor.Role == BattleRole.Vanguard) ? 4 : 2;
            int actionsPlannedByThisUnit = 0;

            while(actionsPlannedByThisUnit < maxActions)
            {
                int currentPhaseIndex = actionsPlannedByThisUnit + 1;
                Debug.Log($"\n--- Planning action #{actionsPlannedByThisUnit + 1} for '{actor.UnitName}' (Phase: {currentPhaseIndex}) ---");

                var candidateActions = GenerateCandidateActions(actor, rules, staminaTracker[actor], currentPhaseIndex);
                if (!candidateActions.Any())
                {
                    Debug.Log($"  - Unit '{actor.UnitName}' has no candidate actions for this turn. Breaking loop.");
                    break;
                }

                var allPossibleActions = new List<ScoredAction>();
                foreach (var action in candidateActions)
                {
                    var logBuilder = new StringBuilder();
                    float totalScore = CalculateScoreForAction(personality, action, actor, context, logBuilder);
                    allPossibleActions.Add(new ScoredAction(action, totalScore, logBuilder.ToString()));
                }

                if (allPossibleActions.Any())
                {
                    var bestAction = allPossibleActions.OrderByDescending(sa => sa.Score).First();
                    
                    foreach (var scoredAction in allPossibleActions.OrderByDescending(sa => sa.Score))
                    {
                        bool isWinner = (scoredAction == bestAction);
                        string color = isWinner ? "lime" : "grey";
                        Debug.Log($"<color={color}>{scoredAction.Log}</color>");
                    }
                    Debug.Log($"<b><color=lime>  >> WINNER for {actor.UnitName}'s action: {FormatActionPlan(bestAction.Plan)} with Score: {bestAction.Score:F2}</color></b>");
                    
                    planner.AddPlan(bestAction.Plan, -1);
                    
                    if (staminaTracker.ContainsKey(actor))
                    {
                        staminaTracker[actor] = CalculateResultingStamina(staminaTracker[actor], bestAction.Plan);
                    }
                }
                else
                {
                    Debug.LogWarning($"No valid actions were found for '{actor.UnitName}'.");
                }
                actionsPlannedByThisUnit++;
            }
        }
    }
    #endregion

    #region 評分核心邏輯
    private float CalculateScoreForAction(AIPersonalitySO personality, ActionPlan action, IBattleUnit_ReadOnly actor, AIContext context, StringBuilder logBuilder)
    {
        float totalScore = 0f;
        logBuilder.AppendLine($"  Evaluating action for '{actor.UnitName}': <b>{FormatActionPlan(action)}</b>");

        if (action.Type == ActionType.Exchange && action.Target != null)
        {
            logBuilder.AppendLine("    (Exchange action - scoring both participants)");
            float actorTacticScore = GetScoreFromTactics(personality, action, actor, context, logBuilder, "Actor");
            float targetTacticScore = GetScoreFromTactics(personality, action, action.Target, context, logBuilder, "Target");
            float actorPositionalBonus = GetPositionalBonusScore(action, actor);
            float targetPositionalBonus = GetPositionalBonusScore(action, action.Target);

            if (actorPositionalBonus != 0) logBuilder.AppendLine($"    - [Universal] Positional Bonus (Actor): {actorPositionalBonus:F2}");
            if (targetPositionalBonus != 0) logBuilder.AppendLine($"    - [Universal] Positional Bonus (Target): {targetPositionalBonus:F2}");
            
            totalScore = actorTacticScore + targetTacticScore + actorPositionalBonus + targetPositionalBonus;
        }
        else
        {
            float scoreFromTactics = GetScoreFromTactics(personality, action, actor, context, logBuilder, "Actor");
            float positionalBonus = GetPositionalBonusScore(action, actor);
            if (positionalBonus != 0) logBuilder.AppendLine($"    - [Universal] Positional Bonus: {positionalBonus:F2}");
            
            totalScore = scoreFromTactics + positionalBonus;
        }

        logBuilder.AppendLine($"    <color=#ADD8E6>  -> Subtotal Score: {totalScore:F2}</color>");
        return totalScore;
    }
    
    private float GetScoreFromTactics(AIPersonalitySO personality, ActionPlan action, IBattleUnit_ReadOnly unit, AIContext context, StringBuilder logBuilder, string participantLabel)
    {
        float score = 0f;
        var strategy = personality.GetStrategyForRole(unit.Role);
        if (strategy == null) return 0f;

        foreach (var tactic in strategy.tactics)
        {
            foreach (var scorer in tactic.scoringModifiers)
            {
                float singleScore = scorer.CalculateScore(action, unit, context);
                if (singleScore != 0)
                {
                    score += singleScore;
                    logBuilder.AppendLine($"    - ({participantLabel}) Tactic '{tactic.name}' -> Scorer '{scorer.name}': {singleScore:F2}");
                }
            }
        }
        return score;
    }
    #endregion

    #region 通用位置評分功能
    private float GetPositionalBonusScore(ActionPlan action, IBattleUnit_ReadOnly actor)
    {
        if (actor == null) return 0f;

        switch (actor.Role)
        {
            case BattleRole.Vanguard:
                if (actor.AttackRange >= 2)
                {
                    switch (action.Type)
                    {
                        case ActionType.Attack: return 20f;
                        case ActionType.Defend: return 20f;
                        case ActionType.Exchange:
                            if (action.Target != null && action.Target.Role == BattleRole.Ranged1)
                                return 50f;
                            break;
                    }
                }
                break;

            case BattleRole.Ranged1:
                if (actor.AttackRange == 1)
                {
                    switch (action.Type)
                    {
                        case ActionType.Attack: return -50f;
                        case ActionType.Exchange:
                            if (action.Target != null)
                            {
                                if (action.Target.Role == BattleRole.Vanguard) return 50f;
                                if (action.Target.Role == BattleRole.Ranged2) return -100f;
                            }
                            break;
                    }
                }
                break;

            case BattleRole.Ranged2:
                if (actor.AttackRange <= 2)
                {
                     switch (action.Type)
                    {
                        case ActionType.Attack: return -50f;
                        case ActionType.Exchange:
                            if (action.Target != null && action.Target.Role == BattleRole.Ranged1)
                                return 50f;
                            break;
                    }
                }
                break;
        }
        return 0f;
    }
    #endregion

    #region AI 輔助方法
    private List<ActionPlan> GenerateCandidateActions(BattleUnit actor, BattleRules rules, int currentStamina, int phaseIndex)
    {
        var candidates = new List<ActionPlan>();
        var allUnitsSnap = BattleManager.Instance.GetAllUnits().Select(u => new CharacterStateRule.UnitStateSnapshot(u)).ToList();
        var context = new CharacterStateRule.BattleStateSnapshot(allUnitsSnap, null);

        if (currentStamina >= battleActions.GetAttackStaminaCost())
        {
            var attackTargets = rules.GetValidTargets(actor, context.UnitSnapshots);
            foreach (var target in attackTargets)
                candidates.Add(ActionPlan.CreateAIAction(actor, target.CurrentPosition, ActionType.Attack, phaseIndex, actor.Role));
        }
        candidates.Add(ActionPlan.CreateNoTargetAction(actor, ActionType.Defend, phaseIndex, actor.Role));
        candidates.Add(ActionPlan.CreateNoTargetAction(actor, ActionType.Rest, phaseIndex, actor.Role));
        
        if (currentStamina >= battleActions.GetExchangeStaminaCost())
        {
            var exchangeTargets = rules.GetValidExchangeTargets(actor, context.UnitSnapshots);
            foreach(var target in exchangeTargets)
                candidates.Add(ActionPlan.CreatePlayerAction(actor, target, ActionType.Exchange, phaseIndex, actor.Role));
        }
        
        return candidates;
    }
    
    private int CalculateResultingStamina(int currentStamina, ActionPlan plan)
    {
        switch (plan.Type)
        {
            case ActionType.Attack: return currentStamina - battleActions.GetAttackStaminaCost();
            case ActionType.Rest: return Mathf.Min(plan.Source.MaxStamina, currentStamina + battleActions.GetRestStaminaRecovery());
            case ActionType.Exchange: return currentStamina - battleActions.GetExchangeStaminaCost();
            case ActionType.Skill: return (plan.SkillUsed != null) ? currentStamina - plan.SkillUsed.staminaCost : currentStamina;
            default: return currentStamina;
        }
    }
    
    private string FormatActionPlan(ActionPlan plan)
    {
        if (plan == null) return "NULL PLAN";
        if (plan.Source == null) return "Empty Action";
        string sourceName = plan.Source.UnitName;
        string actionName = plan.Type.ToActionName();
        string targetName = "N/A";
        if (plan.Target != null)
        {
            targetName = plan.Target.UnitName;
        }
        else if (plan.TargetPosition != GridPosition.None)
        {
            var unitAtPos = BattleManager.Instance?.GetUnitAtPosition(plan.TargetPosition);
            targetName = unitAtPos != null ? unitAtPos.UnitName : plan.TargetPosition.ToString();
        }
        return $"{sourceName} -> {actionName} -> {targetName}";
    }
    #endregion

    #region 通用後備 AI
    private void ExecuteSimpleFallbackAI(List<BattleUnit> selfUnits, List<BattleUnit> opponentUnits, TurnActionPlanner planner, BattleRules rules) 
    {
        foreach (var unit in selfUnits.Where(u => !u.IsDead))
        {
            int maxActions = (unit.Role == BattleRole.Vanguard) ? 4 : 2;
            int actionsPlanned = 0;
            int currentStamina = unit.CurrentStamina;
            int attackCost = (this.battleActions != null) ? this.battleActions.GetAttackStaminaCost() : 3;
            while (actionsPlanned < maxActions)
            {
                int phaseIndex = actionsPlanned + 1; 
                var allUnitsSnap = BattleManager.Instance.GetAllUnits().Select(u => new CharacterStateRule.UnitStateSnapshot(u)).ToList();
                var targets = rules.GetValidTargets(unit, allUnitsSnap);
                if (targets.Any() && currentStamina >= attackCost)
                {
                    planner.AddPlan(ActionPlan.CreateAIAction(unit, targets.First().CurrentPosition, ActionType.Attack, phaseIndex, unit.Role), -1);
                    currentStamina -= attackCost;
                }
                else
                {
                    planner.AddPlan(ActionPlan.CreateNoTargetAction(unit, ActionType.Defend, phaseIndex, unit.Role), -1);
                }
                actionsPlanned++;
            }
        }
    }
    #endregion
}