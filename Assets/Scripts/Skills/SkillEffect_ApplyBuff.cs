using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BuffDefinition
{
    [Tooltip("要施加的狀態效果類型。")]
    public BuffType Type;

    [Tooltip("效果的數值。對於百分比類型，0.2 代表 20%。對於固定值，代表具體數值。")]
    public float Value;

    [Tooltip("效果的持續回合數。0 代表立即生效，1 代表持續到下回合開始前。")]
    public int Duration;
}

[CreateAssetMenu(fileName = "ApplyBuffEffect", menuName = "Skill System/Effects/Apply Buff")]
public class SkillEffect_ApplyBuff : SkillEffect
{
    [Header("Buff 設定")]
    [Tooltip("此技能效果會施加的所有 Buff/Debuff 列表。")]
    public List<BuffDefinition> BuffsToApply;

    [Header("指揮官被動光環")]
    [Tooltip("如果這是一個指揮官被動技能提供的、持續生效的光環效果，請勾選此項。")]
    public bool isCommanderPassiveAura = false;

    public override void Execute(IBattleUnit_ReadOnly source, List<IBattleUnit_ReadOnly> targets, BattleManager battleManager)
    {
        foreach (var target in targets)
        {
            if (target == null || target.IsDead) continue;

            var buffController = target.GetMonoBehaviour().GetComponent<BuffController>();
            if (buffController == null) continue;

            foreach (var buffDef in BuffsToApply)
            {
                buffController.ApplyBuff(buffDef, source);
            }
        }
    }
}