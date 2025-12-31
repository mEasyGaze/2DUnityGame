using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "HealEffect", menuName = "Skill System/Effects/Heal")]
public class SkillEffect_Heal : SkillEffect
{
    [Header("治療設定")]
    [Tooltip("技能提供的基礎治療量。")]
    public int healAmount;

    public override void Execute(IBattleUnit_ReadOnly source, List<IBattleUnit_ReadOnly> targets, BattleManager battleManager)
    {
        foreach (var target in targets)
        {
            if (target == null || target.IsDead) continue;

            target.GetMonoBehaviour().Heal(healAmount);
            BattleLog.Instance.AddLog($"{source.UnitName} 的技能使 {target.UnitName} 恢復了 {healAmount} 點生命！");
        }
    }
}