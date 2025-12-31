using UnityEngine;

[CreateAssetMenu(fileName = "SM_Outcome", menuName = "Battle System/AI/Scorer/Outcome Prediction Scorer")]
public class SM_OutcomeSO : SM_BaseSO
{
    [Header("後果評分")]
    [Tooltip("如果此行動能擊殺目標，給予的分數。")]
    public float killTargetScore = 200f;
    
    [Tooltip("如果執行此行動會導致自己死亡（例如反傷），給予的負分。")]
    public float actorDiesScore = -500f;

    public override float CalculateScore(ActionPlan candidateAction, IBattleUnit_ReadOnly actor, AIContext context)
    {
        if (candidateAction.Type != ActionType.Attack || candidateAction.Target == null)
        {
            return 0;
        }
        if (candidateAction.Target.CurrentHP <= actor.CurrentAttack)
        {
            return killTargetScore * weight;
        }
        return 0;
    }
}