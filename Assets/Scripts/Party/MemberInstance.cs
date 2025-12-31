using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class MemberInstance
{
    public string memberDataSO_ID;
    public string instanceID;
    public int level;
    public int experience;
    public int currentHP;
    public int currentStamina;

    [NonSerialized]
    private MemberDataSO _baseData;
    public MemberDataSO BaseData
    {
        get
        {
            if (_baseData == null)
            {
                _baseData = PartyDatabase.GetMemberDataByID(memberDataSO_ID);
                if (_baseData == null)
                {
                    Debug.LogError($"無法為 instanceID '{instanceID}' 找到 memberDataSO_ID '{memberDataSO_ID}' 對應的模板數據！這可能是因為存檔數據過時或模板被刪除。");
                }
            }
            return _baseData;
        }
    }

    public int MaxHP => BaseData != null ? BaseData.baseHealth + (level - 1) * BaseData.healthPerLevel : 1;
    public int CurrentAttack => BaseData != null ? BaseData.baseAttack + (level - 1) * BaseData.attackPerLevel : 0;
    public int MaxStamina => BaseData != null ? BaseData.baseStamina : 0;

    [NonSerialized]
    private List<SkillData> _skills;
    public List<SkillData> Skills
    {
        get
        {
            if (BaseData == null) return new List<SkillData>();
            if (_skills == null)
            {
                if (SkillManager.Instance != null)
                {
                    _skills = BaseData.skillIDs
                        .Select(id => SkillManager.Instance.Database.GetSkillDataByID(id))
                        .Where(skill => skill != null)
                        .ToList();
                }
                else
                {
                    _skills = new List<SkillData>();
                    Debug.LogError("無法獲取技能列表，因為 SkillManager 不存在！");
                }
            }
            return _skills;
        }
    }

    public MemberInstance(string so_id, int startLevel = 1)
    {
        memberDataSO_ID = so_id;
        instanceID = Guid.NewGuid().ToString();
        level = startLevel;
        experience = 0;
        
        if (BaseData != null)
        {
            currentHP = MaxHP;
            currentStamina = MaxStamina;
        }
        else
        {
            currentHP = 1;
            currentStamina = 0;
        }
    }

    public MemberInstance() { }
}