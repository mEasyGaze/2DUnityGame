using System;

public enum BattleRole
{Vanguard,Ranged1,Ranged2,Support}

public enum ActionType
{Attack,Defend,Rest,Skill,Item,Exchange,Skip}

public enum UICommandType
{Back,EndTurn,ResetAll,CancelSingleAction,UseCommanderSkill,CancelCommanderSkill}

public enum BattleState
{Setup,PlayerPlanning,EnemyTurn,ActionExecution,Won,Lost}

public enum PlanningSubState
{
    None,                   // 無操作
    SelectingRangedUnit,    // 正在等待玩家選擇行動的遠程單位
    SelectingAction,        // 正在等待玩家為已選單位選擇行動
    SelectingTarget,        // 正在等待玩家為已選行動選擇目標
    SelectingExchangeTarget,// 正在等待玩家選擇交換位置的目標
    SelectingItem,          // 正在等待玩家選擇使用道具的目標
    SelectingItemTarget,    // 正在等待玩家選擇使用道具的目標
    SelectingSkill,         // 正在等待玩家選擇要使用的技能
    SelectingSkillTarget    // 正在等待玩家為已選技能選擇目標
}

[Serializable]
public enum GridPosition
{
    PlayerSupport = 0,
    PlayerRanged2 = 1,
    PlayerRanged1 = 2,
    PlayerVanguard = 3,
    EnemyVanguard = 4,
    EnemyRanged1 = 5,
    EnemyRanged2 = 6,
    EnemySupport = 7,
    None = -1
}

public static class ActionTypeExtensions
{
    public static string ToActionName(this ActionType type)
    {
        switch (type)
        {
            case ActionType.Attack:   return "攻擊";
            case ActionType.Defend:   return "防禦";
            case ActionType.Rest:     return "休息";
            case ActionType.Skill:    return "技能";
            case ActionType.Item:     return "道具";
            case ActionType.Exchange: return "交換";
            case ActionType.Skip:     return "跳過";
            default:                  return type.ToString();
        }
    }
}