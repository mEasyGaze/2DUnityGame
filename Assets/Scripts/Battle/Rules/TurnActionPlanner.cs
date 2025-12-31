using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TurnActionPlanner
{
    private const int TOTAL_PLAYER_STEPS = 8;
    private int turnNumber = 0;
    private Dictionary<int, ActionPlan> playerPlansByStep = new Dictionary<int, ActionPlan>();
    private List<ActionPlan> enemyPlans = new List<ActionPlan>();

    public int GetPlayerPlanCount() => playerPlansByStep.Count;
    public bool IsPlanningFinished() => playerPlansByStep.Count >= TOTAL_PLAYER_STEPS;
    public bool IsStepPlanned(int stepIndex) => playerPlansByStep.ContainsKey(stepIndex);

    public int GetNextPlanningStepIndex()
    {
        for (int i = 0; i < TOTAL_PLAYER_STEPS; i++)
        {
            if (!playerPlansByStep.ContainsKey(i))
            {
                return i;
            }
        }
        return TOTAL_PLAYER_STEPS;
    }
    
    public int FindNextAvailableStep(BattleRole role, int startIndex = 0, List<int> excludeIndices = null)
    {
        bool isLookingForVanguard = (role == BattleRole.Vanguard);
        excludeIndices = excludeIndices ?? new List<int>();

        for (int i = startIndex; i < TOTAL_PLAYER_STEPS; i++)
        {
            if (playerPlansByStep.ContainsKey(i) || excludeIndices.Contains(i)) continue;

            bool isVanguardStep = (i % 2 == 0);

            if ((isLookingForVanguard && isVanguardStep) || (!isLookingForVanguard && !isVanguardStep)) return i;
        }
        return -1;
    }

    public void AddPlan(ActionPlan plan, int stepIndex)
    {
        if (plan.Type == ActionType.Skip && plan.Source == null)
        {
            if (stepIndex >= 0 && stepIndex < TOTAL_PLAYER_STEPS)
            {
                playerPlansByStep[stepIndex] = plan;
            }
            return;
        }

        if (plan.Source.IsPlayerTeam)
        {
            if (stepIndex >= 0 && stepIndex < TOTAL_PLAYER_STEPS)
            {
                playerPlansByStep[stepIndex] = plan;
            }
            else
            {
                Debug.LogError($"嘗試為玩家計畫添加無效的步驟索引: {stepIndex}");
            }
        }
        else
        {
            enemyPlans.Add(plan);
        }
    }

    public ActionPlan GetPlanAtStep(int stepIndex)
    {
        playerPlansByStep.TryGetValue(stepIndex, out ActionPlan plan);
        return plan;
    }
    
    public int GetLastPlayerPlanStepIndex()
    {
        if (playerPlansByStep.Count == 0) return -1;
        
        for (int i = TOTAL_PLAYER_STEPS - 1; i >= 0; i--)
        {
            if (playerPlansByStep.TryGetValue(i, out ActionPlan plan))
            {
                if (plan.Source != null)
                {
                    return i;
                }
            }
        }
        return -1;
    }
    
    public void RemovePlansFromStepOnward(int startStepIndex)
    {
        if (startStepIndex < 0) return;

        var keysToRemove = playerPlansByStep.Keys.Where(k => k >= startStepIndex).ToList();
        foreach (var key in keysToRemove)
        {
            playerPlansByStep.Remove(key);
        }
    }

    public int GetStepIndexOfPlan(ActionPlan plan)
    {
        if (plan == null) return -1;
        foreach (var pair in playerPlansByStep)
        {
            if (pair.Value == plan)
            {
                return pair.Key;
            }
        }
        return -1;
    }
    
    public int GetLastPlanStepIndex()
    {
        if (playerPlansByStep.Count == 0) return -1;
        return playerPlansByStep.Keys.Max();
    }

    public void RemovePlansFromStep(int stepIndex)
    {
        if (playerPlansByStep.TryGetValue(stepIndex, out ActionPlan planToRemove))
        {
            if (planToRemove.TransactionID != Guid.Empty)
            {
                var keysToRemove = playerPlansByStep
                    .Where(pair => pair.Value.TransactionID == planToRemove.TransactionID)
                    .Select(pair => pair.Key)
                    .ToList();
                
                foreach (var key in keysToRemove)
                {
                    playerPlansByStep.Remove(key);
                }
            }
            else
            {
                playerPlansByStep.Remove(stepIndex);
            }
        }
    }
    
    public void ClearPlayerPlans()
    {
        playerPlansByStep.Clear();
    }
    
    public void PrepareForNewTurn()
    {
        var allUnitsWithDefense = new HashSet<IBattleUnit_ReadOnly>();
        foreach(var plan in playerPlansByStep.Values)
        {
            if (plan.Type == ActionType.Defend) allUnitsWithDefense.Add(plan.Source);
        }
        foreach(var plan in enemyPlans)
        {
            if (plan.Type == ActionType.Defend) allUnitsWithDefense.Add(plan.Source);
        }
        foreach(var unit in allUnitsWithDefense)
        {
            unit.GetMonoBehaviour().SetDefenseState(false);
        }
        turnNumber++;
        playerPlansByStep.Clear();
        enemyPlans.Clear();
    }

    public int GetCurrentTurnNumber()
    {
        return turnNumber;
    }

    public ActionPlan GetActionForRole(int phase, bool isPlayer, BattleRole role)
    {
        if (isPlayer)
        {
            for (int i = 0; i < 8; i++)
            {
                if (playerPlansByStep.TryGetValue(i, out ActionPlan plan))
                {
                    bool isVanguardStep = (plan.PlannedForRole == BattleRole.Vanguard);
                    bool roleMatch = (role == BattleRole.Vanguard) ? isVanguardStep : !isVanguardStep;

                    if (plan.PhaseIndex == phase && roleMatch) return plan;
                }
            }
            return null;
        }
        else
        {
            return enemyPlans.FirstOrDefault(p =>
                p.PhaseIndex == phase &&
                (p.Source == null || !p.Source.IsPlayerTeam) && 
                (
                    (role == BattleRole.Vanguard && p.PlannedForRole == BattleRole.Vanguard) ||
                    (role != BattleRole.Vanguard && (p.PlannedForRole == BattleRole.Ranged1 || p.PlannedForRole == BattleRole.Ranged2))
                )
            );
        }
    }
    
    public int GetActionCountForUnit(IBattleUnit_ReadOnly unit)
    {
        return playerPlansByStep.Values.Count(p => p.Source == unit) + enemyPlans.Count(p => p.Source == unit);
    }
    
    public void RemovePlansFromDeadUnits(List<BattleUnit> allUnits)
    {
        var deadUnits = allUnits.Where(u => u.IsDead).Cast<IBattleUnit_ReadOnly>().ToList();
        if (deadUnits.Any())
        {
            var playerKeysToRemove = playerPlansByStep
                .Where(pair => deadUnits.Contains(pair.Value.Source) || (pair.Value.Target != null && deadUnits.Contains(pair.Value.Target)))
                .Select(pair => pair.Key)
                .ToList();
                
            foreach (var key in playerKeysToRemove)
            {
                playerPlansByStep.Remove(key);
            }
            
            int removedEnemyCount = enemyPlans.RemoveAll(p => deadUnits.Contains(p.Source) || (p.Target != null && deadUnits.Contains(p.Target)));

            if (playerKeysToRemove.Count > 0 || removedEnemyCount > 0)
            {
                BattleLog.Instance.AddLog($"因單位陣亡，移除了 {playerKeysToRemove.Count + removedEnemyCount} 個無效的行動計畫。");
            }
        }
    }
    
    public void ValidateAndCleanupInvalidPlans(BattleRules rules, int currentPhase)
    {
        var keysToRemove = new List<int>();
        var exchangeTransactions = playerPlansByStep
            .Where(pair => pair.Value.PhaseIndex > currentPhase && 
                        pair.Value.Type == ActionType.Exchange && 
                        pair.Value.TransactionID != Guid.Empty)
            .GroupBy(pair => pair.Value.TransactionID);

        foreach (var transactionGroup in exchangeTransactions)
        {
            var plansInTransaction = transactionGroup.Select(pair => pair.Value).ToList();
            if (plansInTransaction.Count < 2) continue;

            var unitA = plansInTransaction[0].Source;
            var unitB = plansInTransaction[0].Target;
            
            if (unitA == null || unitB == null || unitA.IsDead || unitB.IsDead) continue;
            
            bool isStillValid = rules.IsExchangeValidNow(unitA, unitB);

            if (!isStillValid)
            {
                BattleLog.Instance.AddLog($"因戰局變化，{unitA.UnitName} 與 {unitB.UnitName} 的【未來】交換計畫已失效並被取消。");
                foreach (var pair in transactionGroup)
                {
                    keysToRemove.Add(pair.Key);
                }
            }
        }

        if (keysToRemove.Any())
        {
            foreach (var key in keysToRemove)
            {
                playerPlansByStep.Remove(key);
            }
        }
    }
}