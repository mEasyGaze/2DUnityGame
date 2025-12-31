using UnityEngine;

public static class LogFormatter
{
    // 玩家和我方單位的顏色
    private const string PlayerColor = "#00FFFF"; // 青色 (Cyan)
    // 敵方單位的顏色
    private const string EnemyColor = "#FFA500"; // 橙色 (Orange)
    // 傷害數值的顏色
    private const string DamageColor = "#FF4500"; // 橙紅色 (OrangeRed)
    // 治療數值的顏色
    private const string HealColor = "#32CD32"; // 酸橙綠 (LimeGreen)
    // 增益效果的顏色
    private const string BuffColor = "#FFFF00"; // 黃色 (Yellow)
    // 減益效果的顏色
    private const string DebuffColor = "#DA70D6"; // 蘭花紫 (Orchid)
    // 護盾的顏色
    private const string ShieldColor = "#87CEEB"; // 天藍色 (SkyBlue)
    // 系統或通用消息顏色
    private const string SystemColor = "#C0C0C0"; // 銀色 (Silver)

    public static string Unit(IBattleUnit_ReadOnly unit)
    {
        string color = unit.IsPlayerTeam ? PlayerColor : EnemyColor;
        return $"<color={color}>[{unit.UnitName}]</color>";
    }
    public static string Damage(int amount)
    {
        return $"<color={DamageColor}>{amount}點傷害</color>";
    }
    public static string Heal(int amount)
    {
        return $"<color={HealColor}>{amount}點治療</color>";
    }
    public static string Shield(float amount)
    {
        return $"<color={ShieldColor}>{Mathf.RoundToInt(amount)}點護盾</color>";
    }
    public static string Buff(string buffName)
    {
         return $"<color={BuffColor}>[{buffName}]</color>";
    }
    public static string Debuff(string debuffName)
    {
        return $"<color={DebuffColor}>[{debuffName}]</color>";
    }
    public static string System(string message)
    {
        return $"<color={SystemColor}>{message}</color>";
    }
}