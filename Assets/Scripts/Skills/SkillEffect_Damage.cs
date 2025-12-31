using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Skill System/Effects/Damage")]
public class SkillEffect_Damage : SkillEffect
{
    [Header("傷害設定")]
    [Tooltip("技能造成的基礎傷害值。")]
    public int baseDamage;
    [Tooltip("勾選後，傷害會額外加上施法者攻擊力的一定比例。")]
    public bool scalesWithAttack;
    [Tooltip("攻擊力的加成比例。1.0 代表 100% 的攻擊力。")]
    public float attackScalingFactor = 1.0f;

    public override void Execute(IBattleUnit_ReadOnly source, List<IBattleUnit_ReadOnly> targets, BattleManager battleManager)
    {
        foreach (var target in targets)
        {
            if (target == null || target.IsDead) continue;

            int totalDamage = baseDamage;
            if (scalesWithAttack && source != null)
            {
                totalDamage += Mathf.RoundToInt(source.CurrentAttack * attackScalingFactor);
            }
            target.GetMonoBehaviour().TakeDamage(totalDamage);
            string logMessage = $"{LogFormatter.Unit(source)} 的技能對 {LogFormatter.Unit(target)} 造成了 {LogFormatter.Damage(totalDamage)}！";
        BattleLog.Instance.AddLog(logMessage);
        }
    }
}