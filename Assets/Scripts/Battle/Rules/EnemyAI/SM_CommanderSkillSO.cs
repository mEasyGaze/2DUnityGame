using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "SM_CommanderSkill", menuName = "Battle System/AI/Scorer/Commander Skill Scorer")]
public class SM_CommanderSkillSO : SM_BaseSO
{
    public enum CommanderSkillType { Attack, Defend, Heal, Buff_Debuff }

    [Header("技能評估")]
    [Tooltip("此評分器針對的指揮官技能類型")]
    public CommanderSkillType skillType;
    
    [Tooltip("基礎分數")]
    public float baseScore = 50f;

    [Header("觸發條件與加成")]
    [Tooltip("當敵方團隊平均血量低於此值時，觸發分數加成 (適用於攻擊型技能)")]
    [Range(0,1)] public float enemyHealthThreshold = 0.5f;
    public float enemyHealthBonusScore = 100f;
    
    [Tooltip("當我方團隊平均血量低於此值時，觸發分數加成 (適用於防禦/治療型技能)")]
    [Range(0,1)] public float selfHealthThreshold = 0.5f;
    public float selfHealthBonusScore = 100f;

    [Tooltip("當回合數大於此值時，觸發分數加成 (用於後期決戰)")]
    public int turnThreshold = 5;
    public float lateGameBonusScore = 75f;
    
    public override float CalculateScore(ActionPlan candidateAction, IBattleUnit_ReadOnly actor, AIContext context)
    {
        if (candidateAction.SkillUsed == null || !candidateAction.SkillUsed.isCommanderSkill_OneTimeUse)
        {
            return 0;
        }
        float score = baseScore;
        switch (skillType)
        {
            case CommanderSkillType.Attack:
                if (GetAverageHealth(context.OpponentTeam) < enemyHealthThreshold)
                    score += enemyHealthBonusScore;
                break;
            case CommanderSkillType.Defend:
            case CommanderSkillType.Heal:
                if (GetAverageHealth(context.SelfTeam) < selfHealthThreshold)
                    score += selfHealthBonusScore;
                break;
        }
        
        if (context.TurnCount > turnThreshold)
        {
            score += lateGameBonusScore;
        }

        return score * weight;
    }

    private float GetAverageHealth(System.Collections.Generic.List<BattleUnit> team)
    {
        if (team == null || team.Count == 0) return 0;
        var alive = team.Where(u => !u.IsDead).ToList();
        if (alive.Count == 0) return 0;
        return alive.Sum(u => (float)u.CurrentHP) / alive.Sum(u => (float)u.MaxHP);
    }
}