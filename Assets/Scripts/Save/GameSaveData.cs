using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SceneSaveData
{
    public List<string> objectIDs = new List<string>();
    public List<string> objectStateJsons = new List<string>();
    public List<string> destroyedObjectIDs = new List<string>();
    public List<string> runtimeSpawnedObjectData = new List<string>();

    [System.NonSerialized]
    public Dictionary<string, string> objectStatesJsonMap = new Dictionary<string, string>();

    public void OnBeforeSerialize()
    {
        objectIDs.Clear();
        objectStateJsons.Clear();
        foreach (var pair in objectStatesJsonMap)
        {
            objectIDs.Add(pair.Key);
            objectStateJsons.Add(pair.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        objectStatesJsonMap = new Dictionary<string, string>();
        for (int i = 0; i < objectIDs.Count && i < objectStateJsons.Count; i++)
        {
            objectStatesJsonMap[objectIDs[i]] = objectStateJsons[i];
        }
    }
}

[System.Serializable]
public class GameSaveData
{
    public string gameVersion;
    public string saveTimestamp;
    public float playtimeInSeconds;

    public PlayerStateData playerStateData;
    public InventoryData inventoryData;
    public QuestData questData;
    public PartyData partyData;
    public WorldData worldData;
    public TutorialData tutorialData;
    public ShopManagerData shopManagerData;
    public WorldTimeData worldTimeData;

    public List<string> sceneNames = new List<string>();
    public List<SceneSaveData> sceneSaveDataList = new List<SceneSaveData>();
    
    [System.NonSerialized]
    public Dictionary<string, SceneSaveData> sceneData = new Dictionary<string, SceneSaveData>();

    public GameSaveData()
    {
        playerStateData = new PlayerStateData();
        inventoryData = new InventoryData();
        questData = new QuestData();
        partyData = new PartyData();
        worldData = new WorldData();
        tutorialData = new TutorialData();
        shopManagerData = new ShopManagerData();
        worldTimeData = new WorldTimeData();
        sceneData = new Dictionary<string, SceneSaveData>();
    }
}

#region 子數據結構
[System.Serializable]
public class PlayerStateData
{
    public int money;
    public int level;
    public int currentExperience;
    public int experienceToNextLevel;
}

[System.Serializable]
public class InventoryData
{
    public List<InventorySlotData> slots;
}

[System.Serializable]
public class QuestStatusEntry
{
    public string questID;
    public Quest.QuestStatus status;
}

[System.Serializable]
public class ActiveQuestData
{
    public string questID;
    public List<int> objectiveProgress;
}

[System.Serializable]
public class QuestData
{
    public List<QuestStatusEntry> questStatuses;
    public List<ActiveQuestData> activeQuests;
    public List<string> permanentlyCompletedQuestIDs;
}

[System.Serializable]
public class PartyData
{
    public List<MemberInstance> allMembers;
    public List<string> battlePartyInstanceIDs;
}

[System.Serializable]
public class WorldData
{
    public string sceneName;
    public Vector3 playerPosition;
    public List<string> triggeredEvents;
}

[System.Serializable]
public class TutorialData
{
    public List<string> completedTutorials;
}

[System.Serializable]
public class ShopItemForSave
{
    public string itemID;
    public int quantity;
}

[System.Serializable]
public class ShopRuntimeDataForSave
{
    public string traderNpcID;
    public int currentFund;
    public List<ShopItemForSave> currentStock;
    public int lastRefreshDay;
}

[System.Serializable]
public class ShopManagerData
{
    public List<ShopRuntimeDataForSave> traderData;
    public ShopManagerData() { traderData = new List<ShopRuntimeDataForSave>(); }
}

[System.Serializable]
public class WorldTimeData
{
    public int currentDay;
    public float dayTimer;
}

[System.Serializable]
public class InventorySlotData
{
    public string itemID;
    public int quantity;
}

[System.Serializable]
public class RuntimeSpawnedObjectData
{
    public string prefabType;
    public string instanceID;
    public string stateJson;
}
#endregion