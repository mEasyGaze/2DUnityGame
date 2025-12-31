using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PositionalStrategy
{
    [Tooltip("此位置單位在決策時會參考的所有戰術文件。")]
    public List<AITacticSO> tactics;
}

[CreateAssetMenu(fileName = "NewAIPersonality", menuName = "Battle System/AI/AI Personality")]
public class AIPersonalitySO : ScriptableObject
{
    [Header("各位置戰術文件夾")]
    [Tooltip("前衛單位的戰術文件。")]
    public PositionalStrategy vanguardStrategy;

    [Tooltip("遠程1號位單位的戰術文件。")]
    public PositionalStrategy ranged1Strategy;

    [Tooltip("遠程2號位單位的戰術文件。")]
    public PositionalStrategy ranged2Strategy;
    
    [Tooltip("後勤單位的戰術文件 (主要用於指揮官技能)。")]
    public PositionalStrategy supportStrategy;

    public PositionalStrategy GetStrategyForRole(BattleRole role)
    {
        switch (role)
        {
            case BattleRole.Vanguard: return vanguardStrategy;
            case BattleRole.Ranged1:  return ranged1Strategy;
            case BattleRole.Ranged2:  return ranged2Strategy;
            case BattleRole.Support:  return supportStrategy;
            default:                  return null;
        }
    }
}