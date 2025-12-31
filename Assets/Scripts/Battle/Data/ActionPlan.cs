using System;

public class ActionPlan
{
    public IBattleUnit_ReadOnly Source { get; }
    public IBattleUnit_ReadOnly Target { get; }
    public ActionType Type { get; }
    public int PhaseIndex { get; }
    public BattleRole PlannedForRole { get; }
    public Guid TransactionID { get; }
    public GridPosition TargetPosition { get; }
    public Item ItemUsed { get; }
    public SkillData SkillUsed { get; }

    private ActionPlan(IBattleUnit_ReadOnly source, IBattleUnit_ReadOnly target, ActionType type, int phaseIndex, BattleRole plannedForRole, Guid transactionID, Item item, GridPosition targetPosition, SkillData skill)
    {
        Source = source;
        Target = target;
        Type = type;
        PhaseIndex = phaseIndex;
        PlannedForRole = plannedForRole;
        TransactionID = transactionID;
        ItemUsed = item;
        TargetPosition = targetPosition;
        SkillUsed = skill;
    }

    #region 靜態工廠方法
    // 1: 用於玩家操作 (人事契約)
    public static ActionPlan CreatePlayerAction(IBattleUnit_ReadOnly source, IBattleUnit_ReadOnly target, ActionType type, int phaseIndex, BattleRole plannedForRole, Guid transactionID = default, Item item = null)
    {
        GridPosition position = (target != null) ? target.CurrentPosition : GridPosition.None;
        return new ActionPlan(source, target, type, phaseIndex, plannedForRole, transactionID, item, position, null);
    }

    // 2: 用於 AI 操作 (地域契約)
    public static ActionPlan CreateAIAction(IBattleUnit_ReadOnly source, GridPosition targetPosition, ActionType type, int phaseIndex, BattleRole plannedForRole)
    {
        return new ActionPlan(source, null, type, phaseIndex, plannedForRole, default, null, targetPosition, null);
    }

    // 3: 用於無目標的行動 (如休息、防禦、跳過)
    public static ActionPlan CreateNoTargetAction(IBattleUnit_ReadOnly source, ActionType type, int phaseIndex, BattleRole plannedForRole)
    {
        return new ActionPlan(source, null, type, phaseIndex, plannedForRole, default, null, GridPosition.None, null);
    }
    
    // 4: 用於完全空的行動 (如自動跳過)
    public static ActionPlan CreateEmptyAction(int phaseIndex, BattleRole plannedForRole)
    {
        return new ActionPlan(null, null, ActionType.Skip, phaseIndex, plannedForRole, default, null, GridPosition.None, null);
    }

    // 5: 用於技能行動
    public static ActionPlan CreateSkillAction(IBattleUnit_ReadOnly source, IBattleUnit_ReadOnly target, SkillData skill, int phaseIndex, BattleRole plannedForRole)
    {
        GridPosition position = (target != null) ? target.CurrentPosition : GridPosition.None;
        return new ActionPlan(source, target, ActionType.Skill, phaseIndex, plannedForRole, default, null, position, skill);
    }
    #endregion
}