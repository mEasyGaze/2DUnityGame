using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Linq;

public static class QuestDatabase
{
    private static Dictionary<string, Quest> quests;
    private static bool isLoaded = false;

    private static void LoadDatabase()
    {
        if (isLoaded) return;
        quests = new Dictionary<string, Quest>();
        
        TextAsset[] questFiles = Resources.LoadAll<TextAsset>("GameData/Quests");
        if (questFiles.Length == 0)
        {
            Debug.LogWarning("[QuestDatabase] 在 'Resources/GameData/Quests' 資料夾中找不到任何任務檔案。");
            isLoaded = true;
            return;
        }

        foreach (var questFile in questFiles)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Quest));
            using (StringReader reader = new StringReader(questFile.text))
            {
                try
                {
                    Quest quest = (Quest)serializer.Deserialize(reader);
                    if (quest != null && !string.IsNullOrEmpty(quest.questID))
                    {
                        if (!quests.ContainsKey(quest.questID))
                        {
                            quests.Add(quest.questID, quest);
                        }
                        else
                        {
                            Debug.LogWarning($"[QuestDatabase] 發現重複的任務 ID: {quest.questID} (在檔案 {questFile.name} 中)。");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[QuestDatabase] 解析任務檔案 '{questFile.name}' 失敗: {e.Message}");
                }
            }
        }
        Debug.Log($"[QuestDatabase] 成功載入 {quests.Count} 個獨立任務。");
        isLoaded = true;
    }

    public static Quest GetQuest(string questID)
    {
        LoadDatabase();
        if (quests.TryGetValue(questID, out Quest questTemplate))
        {
            return new Quest
            {
                questID = questTemplate.questID,
                questName = questTemplate.questName,
                description = questTemplate.description,
                giverNPCID = questTemplate.giverNPCID,
                handInNPCID = questTemplate.handInNPCID,
                prerequisiteQuestIDs = new List<string>(questTemplate.prerequisiteQuestIDs),
                isRepeatable = questTemplate.isRepeatable,
                
                objectives = questTemplate.objectives.Select(o => new QuestObjective {
                    type = o.type,
                    targetID = o.targetID,
                    requiredAmount = o.requiredAmount,
                    description = o.description,
                    currentAmount = 0,
                    startingAmount = 0,
                    prerequisiteIndex = o.prerequisiteIndex
                }).ToList(),
                
                itemRewards = new List<ItemReward>(questTemplate.itemRewards),
                moneyReward = questTemplate.moneyReward
            };
        }
        Debug.LogWarning($"[QuestDatabase] 找不到 ID 為 '{questID}' 的任務。");
        return null;
    }
}