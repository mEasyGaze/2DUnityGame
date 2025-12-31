using UnityEngine;
using System.Collections;

[System.Serializable]
public class ResourceNodeState
{
    public bool isDepleted;
    public float respawnTimer;
}

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueObjectIdentifier))]
public class ResourceNode : MonoBehaviour, IInteractable, ISceneSaveable
{
    [Header("資源配置")]
    [SerializeField] private LootTableSO lootTable;
    [SerializeField] private string requiredToolID;
    [SerializeField] private float gatheringTime = 3f;

    [Header("重生設定")]
    [SerializeField] private float respawnTimeInSeconds = 60f;

    [Header("視覺狀態")]
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject depletedVisual;
    [SerializeField] private string actionText = "採集中...";

    [Header("掉落設定")]
    [SerializeField] private float spawnRadius = 1.0f;
    
    [Header("事件廣播")]
    [Tooltip("當此類型的資源【首次】被成功採集時，廣播此事件ID。留空則不廣播。")]
    [SerializeField] private string onFirstGatherCompleteEventID;

    private bool isDepleted = false;
    private float currentRespawnTimer = 0f;
    private static bool hasAnyNodeOfTypeBroadcasted = false;

    void Start()
    {
        if (!isDepleted)
        {
            SetState(false);
        }
    }

    public void Interact()
    {
        if (isDepleted)
        {
            Debug.Log("這個資源點已經枯竭了。");
            return;
        }

        if (!string.IsNullOrEmpty(requiredToolID))
        {
            Item tool = ItemDatabase.Instance.GetItemByID(requiredToolID);
            if (tool == null || !InventoryManager.Instance.HasItem(tool))
            {
                Debug.Log($"缺少必要的工具: {tool?.itemName ?? requiredToolID}");
                return;
            }
        }
        
        Transform targetTransform = (activeVisual != null && activeVisual.activeInHierarchy) ? activeVisual.transform : this.transform;
        ExplorationUIManager.Instance.StartProgressBar(targetTransform, gatheringTime, OnGatheringComplete, actionText);
    }

    private void OnGatheringComplete()
    {
        if (isDepleted) return;
        if (!string.IsNullOrEmpty(onFirstGatherCompleteEventID) && !hasAnyNodeOfTypeBroadcasted)
        {
            GameEventManager.Instance.TriggerEvent(onFirstGatherCompleteEventID);
            hasAnyNodeOfTypeBroadcasted = true;
        }

        LootResult loot = lootTable.GetLoot();
        
        Vector3 spawnCenter = (activeVisual != null && activeVisual.activeInHierarchy) 
                            ? activeVisual.transform.position 
                            : this.transform.position;

        foreach(var item in loot.items)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = spawnCenter + (Vector3)offset;
            InventoryManager.Instance.SpawnGroundItem(item, 1, spawnPosition);
        }
        if (loot.money > 0) PlayerState.Instance.AddMoney(loot.money);
        
        Debug.Log($"採集完成！獲得 {loot.items.Count} 件物品和 {loot.money} 金錢。");
        
        SetState(true);
        StartCoroutine(RespawnCoroutine());
    }

    private void SetState(bool depleted)
    {
        isDepleted = depleted;
        if (activeVisual != null) activeVisual.SetActive(!depleted);
        if (depletedVisual != null) depletedVisual.SetActive(depleted);
    }

    private IEnumerator RespawnCoroutine()
    {
        currentRespawnTimer = respawnTimeInSeconds;
        while (currentRespawnTimer > 0)
        {
            currentRespawnTimer -= Time.deltaTime;
            yield return null;
        }
        currentRespawnTimer = 0;
        SetState(false);
    }
    
    private IEnumerator RespawnFromSave()
    {
        while (currentRespawnTimer > 0)
        {
            currentRespawnTimer -= Time.deltaTime;
            yield return null;
        }
        currentRespawnTimer = 0;
        SetState(false);
    }

    #region 存檔資料
    public object CaptureState()
    {
        return new ResourceNodeState
        {
            isDepleted = this.isDepleted,
            respawnTimer = this.currentRespawnTimer
        };
    }

    public void RestoreState(object stateData)
    {
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<ResourceNodeState>(stateJson);
            
            this.isDepleted = state.isDepleted;
            this.currentRespawnTimer = state.respawnTimer;
            
            SetState(this.isDepleted);
            
            if (this.isDepleted && this.currentRespawnTimer > 0)
            {
                StartCoroutine(RespawnFromSave());
            }
        }
        else
        {
            Debug.LogError($"[ResourceNode] RestoreState 接收到的數據類型錯誤，期望為 string，實際為 {stateData.GetType()}");
        }
    }
    #endregion
}