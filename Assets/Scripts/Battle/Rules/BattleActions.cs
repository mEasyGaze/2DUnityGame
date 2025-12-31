using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BattleActions", menuName = "Battle System/Battle Actions")]
public class BattleActions : ScriptableObject
{
    [Header("行動數值")]
    [SerializeField] private int attackStaminaCost = 3;
    [SerializeField] private int exchangeStaminaCost = 2;
    [SerializeField] private int restStaminaRecovery = 5;

    public int GetAttackStaminaCost() => attackStaminaCost;
    public int GetExchangeStaminaCost() => exchangeStaminaCost;
    public int GetRestStaminaRecovery() => restStaminaRecovery;

    public void Execute(ActionPlan plan, TurnActionPlanner planner, HashSet<Guid> processedTransactions, BattleManager battleManager)
    {
        BattleUnit sourceUnit = plan.Source?.GetMonoBehaviour();
        if (sourceUnit == null || sourceUnit.IsDead) return;
        // 處理關聯行動的唯一性 (交換位置)
        if (plan.Type == ActionType.Exchange && plan.TransactionID != Guid.Empty)
        {
            if (processedTransactions.Contains(plan.TransactionID)) return;
            processedTransactions.Add(plan.TransactionID);
        }
        ActionType finalActionType = plan.Type;
        BattleUnit finalTargetUnit = null;

        switch (plan.Type)
        {
            // 地域契約
            case ActionType.Attack:
                finalTargetUnit = battleManager.GetUnitAtPosition(plan.TargetPosition);
                
                // 指令失效判斷：如果目標位置上沒人，或者站著自己人
                if (finalTargetUnit == null || finalTargetUnit.IsPlayerTeam == sourceUnit.IsPlayerTeam)
                {
                    finalActionType = (sourceUnit.Role == BattleRole.Vanguard) ? ActionType.Defend : ActionType.Rest;
                    BattleLog.Instance.AddLog($"{sourceUnit.UnitName} 的攻擊位置 [{plan.TargetPosition}] 已無有效目標，行動變更為 [{finalActionType.ToActionName()}]！");
                    finalTargetUnit = null;
                }
                break;

            // 人事契約
            case ActionType.Exchange:
                // 直接從契約獲取乙方本人
                finalTargetUnit = plan.Target?.GetMonoBehaviour();
                // 指令失效判斷：如果契約乙方不存在或已陣亡
                if (finalTargetUnit == null || finalTargetUnit.IsDead)
                {
                    finalActionType = ActionType.Rest;
                    BattleLog.Instance.AddLog($"{sourceUnit.UnitName} 的交換目標 [{plan.Target.UnitName}] 已消失，行動變更為 [{finalActionType.ToActionName()}]！");
                    finalTargetUnit = null;
                }
                break;
            
            // 人事契約
            case ActionType.Item:
                finalTargetUnit = plan.Target?.GetMonoBehaviour();
                // 指令失效判斷：如果目標已陣亡或道具沒了
                if (finalTargetUnit == null || finalTargetUnit.IsDead || plan.ItemUsed == null)
                {
                    finalActionType = ActionType.Rest;
                    finalTargetUnit = null;
                    BattleLog.Instance.AddLog($"{plan.Source.UnitName} 的道具目標已消失，行動變更為休息！");
                }
                break;

            // 無目標行動
            case ActionType.Defend:
            case ActionType.Rest:
            case ActionType.Skip:
                finalTargetUnit = null;
                break;
        }
        
        switch (finalActionType)
        {
            case ActionType.Attack:
                ExecuteAttack(sourceUnit, finalTargetUnit);
                break;
            case ActionType.Defend:
                ExecuteDefend(sourceUnit);
                break;
            case ActionType.Rest:
                ExecuteRest(sourceUnit);
                break;
            case ActionType.Exchange:
                ExecuteExchange(sourceUnit, finalTargetUnit, battleManager);
                break;
            case ActionType.Item:
                ExecuteItem(plan.Source.GetMonoBehaviour(), finalTargetUnit, plan.ItemUsed);
                break;
            case ActionType.Skip:
                BattleLog.Instance.AddLog($"{sourceUnit.UnitName} 選擇跳過行動。");
                break;
            case ActionType.Skill:
                ExecuteSkill(plan, battleManager);
                break;
        }
    }

    private void ExecuteAttack(BattleUnit source, BattleUnit target)
    {
        source.ConsumeStamina(attackStaminaCost);
        BattleLog.Instance.AddLog($"{source.UnitName} 對 {target.UnitName} 發動攻擊！");
        target.TakeDamage(source.CurrentAttack);
    }
    
    private void ExecuteDefend(BattleUnit source)
    {
        source.SetDefenseState(true);
        BattleLog.Instance.AddLog($"{source.UnitName} 進入防禦姿態。");
    }

    private void ExecuteRest(BattleUnit source)
    {
        source.RestoreStamina(restStaminaRecovery);
        BattleLog.Instance.AddLog($"{source.UnitName} 休息，恢復 {restStaminaRecovery} 體力。");
    }

    private void ExecuteExchange(BattleUnit source, BattleUnit target, BattleManager battleManager)
    {
        source.ConsumeStamina(exchangeStaminaCost);
        BattleLog.Instance.AddLog($"{source.UnitName} 與 {target.UnitName} 進行位置交換！");

        // 獲取雙方【當前】的核心數據
        GridPosition posA = source.CurrentPosition;
        GridPosition posB = target.CurrentPosition;
        BattleRole roleA = source.Role;
        BattleRole roleB = target.Role;

        // --- 執行核心數據交換 ---
        source.SetNewPosition(posB);
        target.SetNewPosition(posA);
        source.SetRole(roleB);
        target.SetRole(roleA);

        // 移除視覺交換邏輯，這部分將交由 TurnManager 的協程處理
        // if (battleManager.GridSpawns[(int)posB] != null)
        // {
        //     source.transform.position = battleManager.GridSpawns[(int)posB].position;
        // }
        // if (battleManager.GridSpawns[(int)posA] != null)
        // {
        //     target.transform.position = battleManager.GridSpawns[(int)posA].position;
        // }
    }

    private void ExecuteItem(BattleUnit source, BattleUnit target, Item item)
    {
        if (item.itemType != ItemType.Consumable) return;
        bool removed = InventoryManager.Instance.RemoveItem(item, 1);
        if (!removed)
        {
            BattleLog.Instance.AddLog($"行動失敗：背包中找不到 {item.itemName}！行動變更為休息。");
            ExecuteRest(source);
            return;
        }
        BattleLog.Instance.AddLog($"{source.UnitName} 對 {target.UnitName} 使用了 {item.itemName}！");

        // 根據道具效果修改目標狀態
        if (item.healAmount > 0)
        {
            target.Heal(item.healAmount);
        }
        // 未來可以在這裡擴充其他道具效果，如解除負面狀態、增加攻擊力等
    }

    private void ExecuteSkill(ActionPlan plan, BattleManager battleManager)
    {
        IBattleUnit_ReadOnly source = plan.Source;
        SkillData skill = plan.SkillUsed;

        if (source == null || source.IsDead || skill == null) return;

        BattleLog.Instance.AddLog($"{source.UnitName} 施放技能 [{skill.skillName}]！");

        // 1. 使用優化後的 GetTargetsForSkill 方法獲取所有受影響的目標
        List<IBattleUnit_ReadOnly> allTargets = GetTargetsForSkill(source, plan.Target, skill.targetType, battleManager);
        
        // 2. 消耗施法者的體力
        source.GetMonoBehaviour().ConsumeStamina(skill.staminaCost);

        // 3. 遍歷技能的所有效果並執行
        foreach(var effect in skill.effects)
        {
            if(effect != null)
            {
                effect.Execute(source, allTargets, battleManager);
            }
        }
    }

    private List<IBattleUnit_ReadOnly> GetTargetsForSkill(IBattleUnit_ReadOnly source, IBattleUnit_ReadOnly manualTarget, SkillTargetType targetType, BattleManager bm)
    {
        var targets = new List<IBattleUnit_ReadOnly>();
        
        var playerUnits = bm.PlayerUnits.Where(u => u != null && !u.IsDead).Cast<IBattleUnit_ReadOnly>().ToList();
        var enemyUnits = bm.EnemyUnits.Where(u => u != null && !u.IsDead).Cast<IBattleUnit_ReadOnly>().ToList();
        
        switch (targetType)
        {
            case SkillTargetType.Self:
                targets.Add(source);
                break;

            case SkillTargetType.Ally_Single:
            case SkillTargetType.Enemy_Single:
                if (manualTarget != null && !manualTarget.IsDead)
                {
                    targets.Add(manualTarget);
                }
                break;

            case SkillTargetType.Ally_All:
                targets.AddRange(source.IsPlayerTeam ? playerUnits : enemyUnits);
                break;

            case SkillTargetType.Enemy_All:
                targets.AddRange(source.IsPlayerTeam ? enemyUnits : playerUnits);
                break;

            case SkillTargetType.Enemy_Penetrate:
                var opponents = source.IsPlayerTeam ? enemyUnits : playerUnits;
                var vanguard = opponents.FirstOrDefault(u => u.Role == BattleRole.Vanguard);
                var ranged1 = opponents.FirstOrDefault(u => u.Role == BattleRole.Ranged1);
                if (vanguard != null) targets.Add(vanguard);
                if (ranged1 != null) targets.Add(ranged1);
                break;
                
            case SkillTargetType.None:
            default:
                break;
        }
        return targets;
    }
}