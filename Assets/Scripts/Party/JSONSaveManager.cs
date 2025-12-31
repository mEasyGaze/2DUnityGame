using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class PartySaveData
{
    public List<MemberInstance> AllMembers;
    public List<string> BattlePartyInstanceIDs;
}

public static class JSONSaveManager
{
    private static readonly string fileName = "PartyData.json";

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static void SavePartyData(List<MemberInstance> allMembers, List<string> battlePartyInstanceIDs)
    {
        PartySaveData saveData = new PartySaveData
        {
            AllMembers = allMembers,
            BattlePartyInstanceIDs = battlePartyInstanceIDs
        };

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(GetSavePath(), json);
    }

    public static PartySaveData LoadPartyData()
    {
        string path = GetSavePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            PartySaveData saveData = JsonUtility.FromJson<PartySaveData>(json);

            if (saveData != null && saveData.AllMembers != null)
            {
                saveData.AllMembers.RemoveAll(instance => PartyDatabase.GetMemberDataByID(instance.memberDataSO_ID) == null);
            }

            return saveData;
        }
        else
        {
            Debug.Log("找不到存檔檔案，將創建新的隊伍資料。");
            return null;
        }
    }
}