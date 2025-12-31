using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Battle System/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    [Header("基礎資訊")]
    public string enemyID;
    public string enemyName;
    public Sprite enemyIcon;
    public GameObject enemyPrefab;

    [Header("基礎屬性")]
    public int baseHealth;
    public int baseAttack;
    public int baseStamina;
    public int attackRange;

    [Header("技能槽位")]
    [Tooltip("輸入對應 SkillData 的技能ID")]
    public List<string> skillIDs;

    [Header("物品掉落")]
    public int goldDrop;
    // public List<ItemDropInfo> itemDrops; // 未來可擴充為物品掉落
}