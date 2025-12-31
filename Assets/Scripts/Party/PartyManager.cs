using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PartyManager : MonoBehaviour, IGameSaveable
{
    public static PartyManager Instance { get; private set; }

    [Header("隊伍設定")]
    [SerializeField] private int maxBattlePartySize = 4;

    [Header("玩家隊伍資料 (運行時狀態)")]
    public List<MemberInstance> AllMembers = new List<MemberInstance>();
    public List<MemberInstance> BattleParty = new List<MemberInstance>();

    public static event System.Action OnPartyUpdated;

    private bool isLoading = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        AllMembers.Clear();
        BattleParty.Clear();
        SaveManager.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
    }

    public void AddMemberToHolder(string memberID)
    {
        if (AllMembers.Any(m => m.memberDataSO_ID == memberID))
        {
            Debug.Log($"玩家已擁有成員模板ID '{memberID}' 的實例，不再重複添加。");
            
            MemberInstance existingMember = AllMembers.FirstOrDefault(m => m.memberDataSO_ID == memberID);
            if (existingMember != null && !BattleParty.Contains(existingMember))
            {
                SetToBattleParty(existingMember);
            }
            return;
        }

        MemberDataSO data = PartyDatabase.GetMemberDataByID(memberID);
        if (data != null)
        {
            MemberInstance newMember = new MemberInstance(memberID);
            AllMembers.Add(newMember);
            Debug.Log($"已將成員 [{data.memberName}] 加入到玩家的倉庫 (AllMembers)。");
            if (!isLoading) NotifyPartyUpdated();
        }
        else
        {
            Debug.LogWarning($"嘗試新增ID為 '{memberID}' 的成員失敗，在資料庫中找不到該成員。");
        }
    }

    public void RemoveMemberCompletely(string memberID)
    {
        MemberInstance memberToRemove = AllMembers.FirstOrDefault(m => m.memberDataSO_ID == memberID);

        if (memberToRemove != null)
        {
            if (BattleParty.Contains(memberToRemove))
            {
                BattleParty.Remove(memberToRemove);
            }
            AllMembers.Remove(memberToRemove);
            Debug.Log($"已從隊伍中徹底移除成員 [{memberToRemove.BaseData.memberName}]。");
            if (!isLoading) NotifyPartyUpdated();
        }
        else
        {
            Debug.LogWarning($"嘗試移除ID為 '{memberID}' 的成員失敗，玩家並不擁有該成員。");
        }
    }

    public bool SetToBattleParty(MemberInstance member)
    {
        if (BattleParty.Count >= maxBattlePartySize)
        {
            Debug.LogWarning("戰鬥隊伍已滿，無法新增！");
            return false;
        }
        if (AllMembers.Contains(member) && !BattleParty.Contains(member))
        {
            BattleParty.Add(member);
            Debug.Log($"成員 [{member.BaseData.memberName}] 已上陣。");
            if (!isLoading) NotifyPartyUpdated();
            return true;
        }
        return false;
    }

    public void RemoveFromBattleParty(MemberInstance member)
    {
        if (BattleParty.Contains(member))
        {
            BattleParty.Remove(member);
            Debug.Log($"成員 [{member.BaseData.memberName}] 已卸下。");
            if (!isLoading) NotifyPartyUpdated();
        }
    }

    public void SwapBattlePartyOrder(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= BattleParty.Count || toIndex < 0 || toIndex >= BattleParty.Count || fromIndex == toIndex)
        {
            return;
        }
        MemberInstance temp = BattleParty[fromIndex];
        BattleParty[fromIndex] = BattleParty[toIndex];
        BattleParty[toIndex] = temp;
        Debug.Log($"交換了戰鬥隊伍位置 {fromIndex} 和 {toIndex}。");
        if (!isLoading) NotifyPartyUpdated();
    }

    public bool HasBattleReadyMembers()
    {
        return BattleParty != null && BattleParty.Any(member => member.currentHP > 0);
    }

    public void NotifyPartyUpdated()
    {
        // Debug.Log("[PartyManager] 觸發 OnPartyUpdated 事件。");
        OnPartyUpdated?.Invoke();
    }

    #region 存檔資料
    public void PopulateSaveData(GameSaveData data)
    {
        data.partyData.allMembers = this.AllMembers;
        data.partyData.battlePartyInstanceIDs = this.BattleParty.Select(m => m.instanceID).ToList();
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        isLoading = true;

        if (data.partyData == null)
        {
            AllMembers = new List<MemberInstance>();
            BattleParty = new List<MemberInstance>();
            isLoading = false;
            return;
        }
        
        AllMembers = data.partyData.allMembers ?? new List<MemberInstance>();
        
        if (AllMembers.Count > 0)
        {
            AllMembers.RemoveAll(instance => 
                string.IsNullOrEmpty(instance.memberDataSO_ID) || 
                PartyDatabase.GetMemberDataByID(instance.memberDataSO_ID) == null
            );
        }
        
        BattleParty.Clear();
        if (data.partyData.battlePartyInstanceIDs != null)
        {
            foreach (string instanceId in data.partyData.battlePartyInstanceIDs)
            {
                MemberInstance member = AllMembers.FirstOrDefault(m => m.instanceID == instanceId);
                if (member != null)
                {
                    BattleParty.Add(member);
                }
            }
        }
        isLoading = false;
    }
    #endregion
}