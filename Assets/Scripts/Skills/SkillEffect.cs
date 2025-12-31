using UnityEngine;
using System.Collections.Generic;

public abstract class SkillEffect : ScriptableObject
{
    [Header("效果描述")]
    [TextArea] 
    [Tooltip("這個效果的內部描述，方便開發者理解其作用。")]
    public string effectDescription;
    
    // 這是所有子類都必須實現的核心方法。
    // source: 技能的施放者。
    // targets: 所有被技能影響的目標單位列表。
    // battleManager: 用於訪問戰鬥場景的全局狀態和物件。
    public abstract void Execute(IBattleUnit_ReadOnly source, List<IBattleUnit_ReadOnly> targets, BattleManager battleManager);
}