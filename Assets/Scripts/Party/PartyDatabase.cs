using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "PartyDatabase", menuName = "Party System/Party Database")]
public class PartyDatabase : ScriptableObject
{
    public List<MemberDataSO> allMembers;

    private static Dictionary<string, MemberDataSO> memberDictionary;
    private static bool isLoaded = false;

    private static void LoadDatabase()
    {
        if (isLoaded) return;
        
        var databaseInstance = Resources.Load<PartyDatabase>("PartyDatabase");
        if (databaseInstance == null)
        {
            Debug.LogError("嚴重錯誤：在 'Resources' 文件夾中找不到名為 'PartyDatabase' 的 ScriptableObject！");
            memberDictionary = new Dictionary<string, MemberDataSO>();
            isLoaded = true;
            return;
        }

        if (databaseInstance.allMembers == null)
        {
            databaseInstance.allMembers = new List<MemberDataSO>();
        }
        
        try
        {
            memberDictionary = databaseInstance.allMembers.ToDictionary(member => member.memberID, member => member);
            Debug.Log("[PartyDatabase] 隊伍資料庫已載入，共 " + memberDictionary.Count + " 名成員模板。");
        }
        catch (System.ArgumentException e)
        {
            Debug.LogError($"[PartyDatabase] 初始化失敗，發現重複的 memberID！請檢查您的 MemberDataSO 檔案。錯誤: {e.Message}");
            memberDictionary = new Dictionary<string, MemberDataSO>();
        }
        isLoaded = true;
    }

    public static MemberDataSO GetMemberDataByID(string id)
    {
        if (!isLoaded)
        {
            LoadDatabase();
        }

        if (memberDictionary == null)
        {
            Debug.LogError("[PartyDatabase] 字典尚未初始化！無法查詢成員。");
            return null;
        }

        memberDictionary.TryGetValue(id, out MemberDataSO data);
        if (data == null)
        {
            Debug.LogWarning($"[PartyDatabase] 在資料庫中找不到ID為 '{id}' 的成員資料。");
        }
        return data;
    }
}