using UnityEngine;
using System.Collections.Generic;

public class AIContext
{
    public readonly List<BattleUnit> SelfTeam;
    public readonly List<BattleUnit> OpponentTeam;
    public readonly int TurnCount;

    public AIContext(List<BattleUnit> self, List<BattleUnit> opponents, int turn)
    {
        SelfTeam = self;
        OpponentTeam = opponents;
        TurnCount = turn;
    }
}

public abstract class SM_BaseSO : ScriptableObject
{
    [Tooltip("此評分規則的權重，會乘以計算出的分數。")]
    public float weight = 1.0f;
    public abstract float CalculateScore(ActionPlan candidateAction, IBattleUnit_ReadOnly actor, AIContext context);
}