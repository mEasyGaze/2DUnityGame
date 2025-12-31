// 技能的總體類型
public enum SkillType
{
    Active,     // 主動技能
    Passive,    // 被動技能
    CommanderActive,    // 指揮官主動技能
    CommanderPassive    // 指揮官被動技能
}

// 技能的目標選擇邏輯
public enum SkillTargetType
{
    // --- 無目標或自動目標 ---
    None,               // 無需目標 (例如，一個只對自己生效的Buff)
    Self,               // 僅自己

    // --- 攻擊型目標 ---
    Enemy_Single,       // 敵方單體
    Enemy_Penetrate,    // 敵方穿透 (前衛 & 遠1)
    Enemy_All,          // 敵方全體 (除後勤)

    // --- 友方目標 ---
    Ally_Single,        // 友方單體
    Ally_All,           // 友方全體 (除後勤)

    // --- 被動觸發目標 ---
    Passive_Owner,      // 被動技能的擁有者
    Passive_AllAllies   // 被動技能的所有隊友
}

// 狀態效果（Buff/Debuff）的具體類型
public enum BuffType
{
    // --- 增益效果 (Buffs) ---
    IncreaseAttack_Value,   // 增加固定數值的攻擊力
    IncreaseAttack_Percent, // 增加百分比的攻擊力
    IncreaseDefense_Value,  // 增加固定數值的防禦力（減傷）
    IncreaseDefense_Percent,// 增加百分比的防禦力（減傷）
    AddShield,              // 添加一個臨时的護盾值
    HealOverTime,           // 持續回血 (HOT)

    // --- 減益效果 (Debuffs) ---
    DecreaseAttack_Value,   // 降低固定數值的攻擊力
    DecreaseAttack_Percent, // 降低百分比的攻擊力
    DecreaseDefense_Value,  // 降低固定數值的防禦力（破甲）
    DecreaseDefense_Percent,// 降低百分比的防禦力（破甲）
    DamageOverTime,         // 持續傷害 (DOT)
    Stun,                   // 暈眩 (無法行動)
}