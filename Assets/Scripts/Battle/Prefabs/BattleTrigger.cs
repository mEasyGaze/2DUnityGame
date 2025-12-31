using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(UniqueObjectIdentifier))]
public class BattleTrigger : MonoBehaviour
{
    [SerializeField] private BattleEncounterSO battleEncounter;
    private UniqueObjectIdentifier uniqueID;

    private void Awake()
    {
        uniqueID = GetComponent<UniqueObjectIdentifier>();
        
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"物件 '{gameObject.name}' 上的 BattleTrigger2D 腳本需要其 Collider2D 設置為 'Is Trigger'，已自動為您設定。");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[BattleTrigger2D] 偵測到 Player '{other.name}' 進入範圍，準備觸發戰鬥！");
            GetComponent<Collider2D>().enabled = false;
            if (GameManager.Instance != null && battleEncounter != null)
            {
                string myID = uniqueID != null ? uniqueID.ID : null;
                GameManager.Instance.StartBattle(battleEncounter, myID);
            }
            else
            {
                Debug.LogError("[BattleTrigger2D] 無法觸發戰鬥！GameManager 或 BattleEncounterSO 未設定！");
            }
        }
    }
    
    public void TriggerBattle()
    {
        Debug.Log($"[BattleTrigger] 在 '{gameObject.name}' 上觸發戰鬥！");
        
        var collider = GetComponent<Collider2D>();
        if(collider != null) collider.enabled = false;
        if (GameManager.Instance != null && battleEncounter != null)
        {
            string myID = uniqueID != null ? uniqueID.ID : null;
            GameManager.Instance.StartBattle(battleEncounter, myID);
        }
        else
        {
            Debug.LogError($"[BattleTrigger] 在 '{gameObject.name}' 上無法觸發戰鬥！GameManager 或 BattleEncounterSO 未設定！");
        }
    }
}