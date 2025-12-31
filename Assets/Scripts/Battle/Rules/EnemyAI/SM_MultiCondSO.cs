using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class StatCondition
{
    public enum StatType { HealthPercent, Stamina, AttackRange }

    public StatType stat;
    [Tooltip("條件檢查的值範圍 (X: 最小值, Y: 最大值)。例如血量在 20% 到 60% 之間，就填 (0.2, 0.6)。")]
    public Vector2 valueRange = new Vector2(0, 1);

    public bool IsMet(IBattleUnit_ReadOnly actor)
    {
        if (actor == null) return false;

        float subjectValue = 0;
        switch (stat)
        {
            case StatType.HealthPercent:
                subjectValue = (actor.MaxHP > 0) ? (float)actor.CurrentHP / actor.MaxHP : 0;
                break;
            case StatType.Stamina:
                subjectValue = actor.CurrentStamina;
                break;
            case StatType.AttackRange:
                subjectValue = actor.AttackRange;
                break;
        }
        return subjectValue >= valueRange.x && subjectValue <= valueRange.y;
    }
}

[System.Serializable]
public class ActionScore
{
    public ActionType action;
    public float scoreAdjustment;
}

[CreateAssetMenu(fileName = "SM_MultiCond", menuName = "Battle System/AI/Scorer/Multi-Condition Scorer")]
public class SM_MultiCondSO : SM_BaseSO
{
    [Header("觸發條件 (必須全部滿足)")]
    public List<StatCondition> conditions;

    [Header("滿足條件後的分數調整")]
    public List<ActionScore> scores;

    public override float CalculateScore(ActionPlan candidateAction, IBattleUnit_ReadOnly actor, AIContext context)
    {
        // 1. 檢查所有條件是否都對行動者(actor)滿足
        foreach (var condition in conditions)
        {
            if (!condition.IsMet(actor))
            {
                return 0;
            }
        }

        // 2. 如果所有條件都滿足，則查找對應行動的分數
        var scoreEntry = scores.FirstOrDefault(s => s.action == candidateAction.Type);
        if (scoreEntry != null)
        {
            return scoreEntry.scoreAdjustment * weight;
        }
        return 0;
    }
}