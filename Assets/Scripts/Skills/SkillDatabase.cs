using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Party System/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills;

    private Dictionary<string, SkillData> skillDictionary;
    private bool isInitialized = false;

    private void OnEnable()
    {
        isInitialized = false;
    }

    public void Initialize()
    {
        if (isInitialized) return;

        if (allSkills == null)
        {
            allSkills = new List<SkillData>();
        }

        skillDictionary = allSkills.ToDictionary(skill => skill.skillID, skill => skill);
        foreach (var skill in allSkills)
        {
            if (skill != null && !skillDictionary.ContainsKey(skill.skillID))
            {
                skillDictionary.Add(skill.skillID, skill);
            }
            else
            {
                Debug.LogWarning($"技能資料庫中發現重複或空的技能ID: {skill?.skillID}");
            }
        }
        isInitialized = true;
    }

    public SkillData GetSkillDataByID(string id)
    {
        if (!isInitialized)
        {
            Debug.LogError("SkillDatabase 尚未初始化！請確保 SkillManager 已運行。");
            return null;
        }

        if (skillDictionary.TryGetValue(id, out SkillData data))
        {
            return data;
        }
        else
        {
            return null;
        }
    }
}