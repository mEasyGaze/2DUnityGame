using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffController : MonoBehaviour
{
    private BattleUnit owner;
    private readonly List<BuffInstance> activeBuffs = new List<BuffInstance>();
    private readonly List<BuffInstance> activeAuras = new List<BuffInstance>();
    public float CurrentShield { get; private set; }

    void Awake()
    {
        owner = GetComponent<BattleUnit>();
    }

    public void ApplyBuff(BuffDefinition definition, IBattleUnit_ReadOnly source)
    {
        if (definition.Type == BuffType.AddShield)
        {
            CurrentShield += definition.Value;
            BattleLog.Instance.AddLog($"{owner.UnitName} 獲得了 {Mathf.RoundToInt(definition.Value)} 點護盾！");
            return;
        }
        BuffInstance newBuff = new BuffInstance(definition, source);
        activeBuffs.Add(newBuff);
        BattleLog.Instance.AddLog($"{owner.UnitName} 獲得了 [{definition.Type}] 效果！");
    }

    public void ClearShield()
    {
        CurrentShield = 0;
    }

    public int AbsorbDamage(int incomingDamage)
    {
        if (CurrentShield <= 0) return 0;

        float damageToAbsorb = Mathf.Min(incomingDamage, CurrentShield);

        CurrentShield -= damageToAbsorb;
        
        int absorbedAmount = Mathf.RoundToInt(damageToAbsorb);

        if (absorbedAmount > 0)
        {
            BattleLog.Instance.AddLog($"{owner.UnitName} 的護盾吸收了 {absorbedAmount} 點傷害。");
        }
        return absorbedAmount;
    }

    public void ApplyAura(BuffDefinition definition, IBattleUnit_ReadOnly source)
    {
        BuffInstance newAura = new BuffInstance(definition, source);
        activeAuras.Add(newAura);
    }

    public void ClearAllAuras()
    {
        activeAuras.Clear();
    }

    public void TickAllBuffs()
    {
        foreach (var buff in activeBuffs)
        {
            if (buff.IsExpired()) continue;
            if (buff.Definition.Type == BuffType.HealOverTime)
            {
                int healAmount = Mathf.RoundToInt(buff.Definition.Value);
                owner.Heal(healAmount); 
                BattleLog.Instance.AddLog($"{owner.UnitName} 因 [持續治療] 效果恢復了 {healAmount} 點生命。");
            }
            else if (buff.Definition.Type == BuffType.DamageOverTime)
            {
                int dotDamage = Mathf.RoundToInt(buff.Definition.Value);
                owner.TakeDamage(dotDamage);
                BattleLog.Instance.AddLog($"{owner.UnitName} 因 [持續傷害] 效果受到了 {dotDamage} 點傷害。");
            }
        }

        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            activeBuffs[i].Tick();
            if (activeBuffs[i].IsExpired())
            {
                BattleLog.Instance.AddLog($"{owner.UnitName} 身上的 [{activeBuffs[i].Definition.Type}] 效果消失了。");
                activeBuffs.RemoveAt(i);
            }
        }
    }

    public float GetBuffValue(BuffType type)
    {
        float timedBuffValue = activeBuffs
            .Where(b => b.Definition.Type == type && !b.IsExpired())
            .Sum(b => b.Definition.Value);
        float auraBuffValue = activeAuras
            .Where(a => a.Definition.Type == type)
            .Sum(a => a.Definition.Value);
        return timedBuffValue + auraBuffValue;
    }
    
    public bool HasBuff(BuffType type)
    {
        return activeBuffs.Any(b => b.Definition.Type == type && !b.IsExpired()) || activeAuras.Any(a => a.Definition.Type == type);
    }
}