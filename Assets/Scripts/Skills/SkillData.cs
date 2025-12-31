using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill System/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("基礎資訊")]
    public string skillID;
    public string skillName;
    public Sprite skillIcon;
    [TextArea(3, 5)]
    public string skillDescription;

    [Header("核心規則")]
    public SkillType skillType;
    public SkillTargetType targetType;

    [Header("消耗與限制 (主動/指揮官技能)")]
    [Tooltip("施放此技能需要消耗的體力值。")]
    public int staminaCost;
    [Tooltip("如果是指揮官技能，將此設為 True。")]
    public bool isCommanderSkill_OneTimeUse;
    [Tooltip("施放此技能的最大距離。0 代表無距離限制。")]
    public int range;

    [Header("技能效果列表")]
    [Tooltip("將此技能會觸發的所有效果 (SkillEffect) 模板拖曳至此。")]
    public List<SkillEffect> effects;
}