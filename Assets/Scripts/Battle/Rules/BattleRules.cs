using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "BattleRules", menuName = "Battle System/Battle Rules")]
public class BattleRules : ScriptableObject
{
    public List<IBattleUnit_ReadOnly> GetEligibleActorsForStep(int stepIndex, List<CharacterStateRule.UnitStateSnapshot> currentState, TurnActionPlanner planner)
    {
        var eligibleActors = new List<IBattleUnit_ReadOnly>();
        bool isVanguardStep = (stepIndex % 2 == 0);
        var potentialActorSnaps = currentState.Where(s => s.Unit.IsPlayerTeam && !s.Unit.IsDead && s.Role != BattleRole.Support).ToList();

        foreach (var snap in potentialActorSnaps)
        {
            bool isUnitVanguardInSnapshot = (snap.Role == BattleRole.Vanguard);
            if ((isVanguardStep && !isUnitVanguardInSnapshot) || (!isVanguardStep && isUnitVanguardInSnapshot)) continue;

            int maxActions = (snap.Role == BattleRole.Vanguard) ? 4 : 2;

            if (planner.GetActionCountForUnit(snap.Unit) < maxActions)
            {
                eligibleActors.Add(snap.Unit);
            }
        }
        return eligibleActors;
    }

    private bool CanUnitAct(IBattleUnit_ReadOnly unit, TurnActionPlanner planner, int maxActions)
    {
        if (unit == null || unit.IsDead) return false;
        return planner.GetActionCountForUnit(unit) < maxActions;
    }
    
    public Dictionary<ActionType, bool> GetActionFeasibility(IBattleUnit_ReadOnly actor, CharacterStateRule.BattleStateSnapshot currentState, TurnActionPlanner planner)
    {
        var feasibility = new Dictionary<ActionType, bool>();
        var unitSnaps = currentState.UnitSnapshots;
        var actorSnap = unitSnaps.FirstOrDefault(s => s.Unit == actor);
        if (actorSnap == null) 
        {
            foreach (ActionType type in System.Enum.GetValues(typeof(ActionType)))
            {
                feasibility[type] = false;
            }
            return feasibility;
        }

        feasibility[ActionType.Attack] = GetValidTargets(actor, unitSnaps).Any() && actorSnap.Stamina >= 3;
        feasibility[ActionType.Exchange] = GetValidExchangeTargets(actor,unitSnaps).Any() && actorSnap.Stamina >= 2; 
        feasibility[ActionType.Item] = currentState.InventorySnapshot.HasAnyConsumables();
        
        bool hasAnyUsableSkill = false;
        if (actor.Skills != null && actor.Skills.Count > 0)
        {
            foreach (var skill in actor.Skills)
            {
                if (IsSkillUsable(actor, skill, currentState))
                {
                    hasAnyUsableSkill = true;
                    break;
                }
            }
        }
        feasibility[ActionType.Skill] = hasAnyUsableSkill;
        feasibility[ActionType.Defend] = true;
        feasibility[ActionType.Rest] = true;
        feasibility[ActionType.Skip] = true;
        
        return feasibility;
    }

    public List<IBattleUnit_ReadOnly> GetValidTargets(IBattleUnit_ReadOnly attacker, List<CharacterStateRule.UnitStateSnapshot> currentState)
    {
        var validTargets = new List<IBattleUnit_ReadOnly>();
        if (attacker == null || attacker.IsDead) return validTargets;

        var attackerSnap = currentState.First(s => s.Unit == attacker);
        var opponentSnaps = currentState.Where(s => s.Unit.IsPlayerTeam != attacker.IsPlayerTeam && !s.Unit.IsDead && s.Role != BattleRole.Support);

        var enemyVanguardSnap = opponentSnaps.FirstOrDefault(s => s.Role == BattleRole.Vanguard);

        IEnumerable<CharacterStateRule.UnitStateSnapshot> potentialTargets = (enemyVanguardSnap != null) 
            ? new List<CharacterStateRule.UnitStateSnapshot> { enemyVanguardSnap } 
            : opponentSnaps;

        foreach (var targetSnap in potentialTargets)
        {
            int distance = Mathf.Abs((int)attackerSnap.Position - (int)targetSnap.Position);
            if (distance <= attacker.AttackRange)
            {
                validTargets.Add(targetSnap.Unit);
            }
        }
        return validTargets;
    }
    
    public bool IsExchangeValidNow(IBattleUnit_ReadOnly unitA, IBattleUnit_ReadOnly unitB)
    {
        if (unitA == null || unitB == null || unitA.IsDead || unitB.IsDead) return false;
        if (unitA.Role == BattleRole.Support || unitB.Role == BattleRole.Support) return false;
        var roleA = unitA.Role;
        var roleB = unitB.Role;
        switch (roleA)
        {
            case BattleRole.Vanguard:
                return roleB == BattleRole.Ranged1;
            case BattleRole.Ranged1:
                return roleB == BattleRole.Vanguard || roleB == BattleRole.Ranged2;
            case BattleRole.Ranged2:
                return roleB == BattleRole.Ranged1;
            default:
                return false;
        }
    }

    public List<IBattleUnit_ReadOnly> GetValidExchangeTargets(IBattleUnit_ReadOnly actor, List<CharacterStateRule.UnitStateSnapshot> currentState)
    {
        var validTargets = new List<IBattleUnit_ReadOnly>();
        if (actor == null || actor.IsDead || actor.Role == BattleRole.Support) return validTargets;
        
        var actorSnap = currentState.First(s => s.Unit == actor);
        var teammateSnaps = currentState.Where(s => s.Unit.IsPlayerTeam == actor.IsPlayerTeam && s.Unit != actor && !s.Unit.IsDead && s.Role != BattleRole.Support);

        foreach (var mateSnap in teammateSnaps)
        {
            bool canSwap = false;
            switch (actorSnap.Role)
            {
                case BattleRole.Vanguard: canSwap = (mateSnap.Role == BattleRole.Ranged1); break;
                case BattleRole.Ranged1: canSwap = (mateSnap.Role == BattleRole.Vanguard || mateSnap.Role == BattleRole.Ranged2); break;
                case BattleRole.Ranged2: canSwap = (mateSnap.Role == BattleRole.Ranged1); break;
            }
            if(canSwap) validTargets.Add(mateSnap.Unit);
        }
        return validTargets;
    }

    public List<IBattleUnit_ReadOnly> GetValidItemTargets(IBattleUnit_ReadOnly user)
    {
        if (user == null || user.IsDead) return new List<IBattleUnit_ReadOnly>();

        var teammates = user.IsPlayerTeam ? BattleManager.Instance.PlayerUnits : BattleManager.Instance.EnemyUnits;
        return teammates.Where(u => !u.IsDead).Cast<IBattleUnit_ReadOnly>().ToList();
    }

    public List<IBattleUnit_ReadOnly> GetValidSkillTargets(IBattleUnit_ReadOnly source, SkillData skill, List<CharacterStateRule.UnitStateSnapshot> currentState)
    {
        var validTargets = new List<IBattleUnit_ReadOnly>();
        if (source == null || skill == null) return validTargets;

        var sourceSnap = currentState.First(s => s.Unit == source);

        switch (skill.targetType)
        {
            case SkillTargetType.Enemy_Single:
                var opponentSnaps = currentState.Where(s => s.Unit.IsPlayerTeam != source.IsPlayerTeam && !s.Unit.IsDead);
                foreach (var targetSnap in opponentSnaps)
                {
                    int distance = Mathf.Abs((int)sourceSnap.Position - (int)targetSnap.Position);
                    if (skill.range == 0 || distance <= skill.range)
                    {
                        validTargets.Add(targetSnap.Unit);
                    }
                }
                break;

            case SkillTargetType.Ally_Single:
                var allySnaps = currentState.Where(s => s.Unit.IsPlayerTeam == source.IsPlayerTeam && !s.Unit.IsDead && s.Unit != source);
                foreach (var targetSnap in allySnaps)
                {
                    int distance = Mathf.Abs((int)sourceSnap.Position - (int)targetSnap.Position);
                    if (skill.range == 0 || distance <= skill.range)
                    {
                        validTargets.Add(targetSnap.Unit);
                    }
                }
                break;
            case SkillTargetType.None:
            case SkillTargetType.Self:
            case SkillTargetType.Ally_All:
            case SkillTargetType.Enemy_All:
            case SkillTargetType.Enemy_Penetrate:
            default:
                break;
        }
        return validTargets;
    }
    
    private bool HasConsumableItems()
    {
        if (InventoryManager.Instance == null || InventoryManager.Instance.playerInventoryData == null)
        {
            return false;
        }
        return InventoryManager.Instance.playerInventoryData.slots.Any(s => !s.IsEmpty() && s.item.itemType == ItemType.Consumable);
    }

    public bool IsVictory(List<BattleUnit> enemyUnits) => enemyUnits.All(u => u.IsDead);
    public bool IsDefeat(List<BattleUnit> playerUnits) => playerUnits.All(u => u.IsDead);
    public bool IsSkillUsable(IBattleUnit_ReadOnly source, SkillData skill, CharacterStateRule.BattleStateSnapshot currentState)
    {
        if (source == null || skill == null || currentState == null) return false;

        // 1. 檢查體力是否足夠
        var sourceSnap = currentState.UnitSnapshots.FirstOrDefault(s => s.Unit == source);
        if (sourceSnap == null || sourceSnap.Stamina < skill.staminaCost)
        {
            return false;
        }
        
        // 2. 檢查指揮官技能的使用次數 (未來擴充點)
        // if (skill.isCommanderSkill_OneTimeUse && battleManager.HasCommanderSkillBeenUsed())
        // {
        //     return false;
        // }

        // (未來可擴充) 3. 檢查是否有沉默等特殊狀態
        // if (source.IsSilenced) return false;

        // 所有檢查都通過，技能可用
        return true;
    }
}