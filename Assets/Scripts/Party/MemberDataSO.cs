using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMemberData", menuName = "Party System/Member Data")]
public class MemberDataSO : ScriptableObject
{
    [Header("基礎資訊")]
    public string memberID;
    public string memberName;
    public Sprite memberIcon;
    public GameObject unitPrefab;

    [Header("基礎屬性")]
    public int baseHealth;
    public int baseAttack;
    public int baseStamina;
    public int attackRange;

    [Header("技能槽位")]
    [Tooltip("在此輸入對應 SkillData 的技能ID")]
    public List<string> skillIDs;

    [Header("成長規則 (暫不使用)")]
    public int healthPerLevel;
    public int attackPerLevel;
}