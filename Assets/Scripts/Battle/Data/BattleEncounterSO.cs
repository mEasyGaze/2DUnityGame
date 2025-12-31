using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyPositioning
{
    public EnemyDataSO enemyData;
    public BattleRole role;
}

public enum DefeatActionType
{
    ReturnToTitle,      // 默認：回到標題或讀取存檔 (一般戰鬥)
    TeleportToScene,    // 傳送到指定場景的指定位置 (例如：被打回城鎮、或進入劇情場景)
    TriggerStoryEvent   // 直接觸發一段劇情 (StoryScene) - 如果在同一場景
}

[CreateAssetMenu(fileName = "NewBattleEncounter", menuName = "Battle System/Battle Encounter")]
public class BattleEncounterSO : ScriptableObject
{
    [Header("敵人隊伍配置")]
    public List<EnemyPositioning> enemyTeam;

    [Header("AI 行為配置")]
    [Tooltip("指定此遭遇戰中敵方隊伍使用的人格(軍師)。如果留空，將使用 EnemyBattleAI 中定義的通用後備戰術。")]
    public AIPersonalitySO enemyPersonality;

    [Header("戰鬥獎勵")]
    public int totalGoldReward;
    // public List<ItemReward> itemRewards;

    [Header("戰敗處理 (劇情殺專用)")]
    [Tooltip("玩家戰敗後會發生什麼事？")]
    public DefeatActionType defeatType = DefeatActionType.ReturnToTitle;

    [Tooltip("如果類型是 TeleportToScene，填寫場景名稱。")]
    public string defeatSceneName;
    
    [Tooltip("如果類型是 TeleportToScene，填寫傳送後的坐標。")]
    public Vector3 defeatPosition;

    [Tooltip("如果類型是 TriggerStoryEvent，填寫劇情事件ID (GameEvent)。")]
    public string defeatEventID;

    private void OnValidate()
    {
        int calculatedGold = 0;
        if (enemyTeam != null)
        {
            foreach (var enemyPos in enemyTeam)
            {
                if (enemyPos.enemyData != null)
                {
                    calculatedGold += enemyPos.enemyData.goldDrop;
                }
            }
        }
        totalGoldReward = calculatedGold;
    }
}