using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public enum GameState
{
    Exploration,  // 探索模式
    InDialogue,   // 對話中
    InShop,       // 商店中
    InTutorial,   // 教學中
    InStoryScene, // 劇情演出中
    InBattle,     // 戰鬥中
    Paused        // 遊戲暫停 (打開系統選單)
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentGameState { get; private set; }
    public BattleEncounterSO CurrentEncounter { get; private set; }
    
    [Header("場景名稱設定")]
    [SerializeField] private string mainSceneName = "GameScene";
    [SerializeField] private string battleSceneName = "BattleScene";

    [Header("場景管理UI")]
    [Tooltip("請將您場景中的讀取畫面 UI 物件拖曳至此")]
    public GameObject loadingScreen;

    private string currentBattleTriggerID;
    private bool lastBattleWon = false;
    
    private string sceneBeforeBattle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
        else
        {
            Debug.LogError("[GameManager] 未在 Inspector 中指定 LoadingScreen 物件！非同步載入功能可能無法正確顯示讀取畫面。");
        }
        CurrentGameState = GameState.Exploration;
    }

    public void SetGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;
        CurrentGameState = newState;
        Debug.Log($"[GameManager] 遊戲狀態已切換至: {newState}");
        switch (newState)
        {
            case GameState.Exploration:
                InputManager.Instance?.EnableGameplayInput(true);
                Time.timeScale = 1f;
                break;

            case GameState.InDialogue:
            case GameState.InShop:
            case GameState.InTutorial:
            case GameState.InStoryScene:
            case GameState.Paused:
                InputManager.Instance?.EnableGameplayInput(false);
                Time.timeScale = 0f;
                break;
                
            case GameState.InBattle:
                InputManager.Instance?.EnableGameplayInput(false);
                Time.timeScale = 1f;
                break;
        }
    }

    public void StartBattle(BattleEncounterSO encounter, string enemyID = null)
    {
        if (encounter == null)
        {
            Debug.LogError("嘗試開始戰鬥，但傳入的 BattleEncounterSO 為空！");
            return;
        }
        currentBattleTriggerID = enemyID;
        lastBattleWon = false;
        sceneBeforeBattle = SceneManager.GetActiveScene().name;
        SetGameState(GameState.InBattle);
        StartCoroutine(LoadBattleSceneCoroutine(encounter));
    }

    public void EndBattle()
    {
        StartCoroutine(LoadAfterBattleCoroutine());
    }

    private IEnumerator LoadBattleSceneCoroutine(BattleEncounterSO encounter)
    {
        CurrentEncounter = encounter;
        InputManager.Instance?.EnableGameplayInput(false);
        SaveManager.Instance.SaveSceneStateToMemory();
        
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(battleSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }
    }
    
    private IEnumerator LoadAfterBattleCoroutine()
    {
        ProcessBattleResults();
        UpdatePartyStatusAfterBattle();
        
        string targetScene = sceneBeforeBattle;
        if (string.IsNullOrEmpty(targetScene)) targetScene = mainSceneName;
        Vector3? targetPosition = null;
        string eventToTrigger = null;

        if (!lastBattleWon && CurrentEncounter != null)
        {
            Debug.Log($"[GameManager] 戰鬥失敗處理: {CurrentEncounter.defeatType}");
            switch (CurrentEncounter.defeatType)
            {
                case DefeatActionType.ReturnToTitle:
                    // targetScene = "TitleScene";
                    break;

                case DefeatActionType.TeleportToScene:
                    if (!string.IsNullOrEmpty(CurrentEncounter.defeatSceneName))
                    {
                        targetScene = CurrentEncounter.defeatSceneName;
                        targetPosition = CurrentEncounter.defeatPosition;
                    }
                    break;
                
                case DefeatActionType.TriggerStoryEvent:
                    eventToTrigger = CurrentEncounter.defeatEventID;
                    break;
            }
        }
        else if (lastBattleWon)
        {
            // 戰勝：通常留在原場景，但要銷毀敵人
            // 如果有戰勝後觸發劇情的設計，也可以在這裡擴充
        }
        CurrentEncounter = null;

        if (loadingScreen != null) loadingScreen.SetActive(true);
        InputManager.Instance?.EnableGameplayInput(false);
        Debug.Log($"[GameManager] 正在載入場景: {targetScene}");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);

        while (!asyncLoad.isDone) yield return null;
        yield return new WaitForEndOfFrame(); 
        SaveManager.Instance.LoadSceneStateFromMemory();
        Debug.Log("[GameManager] 場景狀態已恢復。");
        
        if (targetScene == sceneBeforeBattle && lastBattleWon && !string.IsNullOrEmpty(currentBattleTriggerID))
        {
            HandleVictoryDestruction(currentBattleTriggerID);
        }
        var player = FindObjectOfType<Player>();
        if (player != null)
        {
            if (targetPosition.HasValue)
            {
                player.SetPosition(targetPosition.Value);
            }
            else if (!lastBattleWon && targetScene == sceneBeforeBattle)
            {
                if (!string.IsNullOrEmpty(currentBattleTriggerID))
                {
                    var triggerObj = FindObjectsOfType<UniqueObjectIdentifier>()
                        .FirstOrDefault(id => id.ID == currentBattleTriggerID);
                        
                    if (triggerObj != null)
                    {
                        Vector3 direction = (player.transform.position - triggerObj.transform.position).normalized;
                        if (direction == Vector3.zero) direction = Vector2.down;
                        Vector3 safePos = triggerObj.transform.position + direction * 1.0f;
                        player.SetPosition(safePos);
                    }
                }
            }
        }
        currentBattleTriggerID = null;
        if (loadingScreen != null) loadingScreen.SetActive(false);
        if (!string.IsNullOrEmpty(eventToTrigger))
        {
            Debug.Log($"[GameManager] 觸發戰後劇情事件: {eventToTrigger}");
            GameEventManager.Instance.TriggerEvent(eventToTrigger);
        }
        else
        {
            SetGameState(GameState.Exploration);
        }
    }

    private void HandleVictoryDestruction(string targetID)
    {
        ScenePersistenceManager.Instance.RecordObjectDestruction(targetID);
        var allIdentifiers = FindObjectsOfType<UniqueObjectIdentifier>();
        foreach (var idObj in allIdentifiers)
        {
            if (idObj.ID == targetID)
            {
                Debug.Log($"[GameManager] 戰鬥勝利，銷毀觸發戰鬥的物件: {idObj.name}");
                Destroy(idObj.gameObject);
                break;
            }
        }
    }

    private void UpdatePartyStatusAfterBattle()
    {
        if (BattleManager.Instance == null || PartyManager.Instance == null) return;
        foreach (var unit in BattleManager.Instance.PlayerUnits)
        {
            MemberInstance instanceToUpdate = PartyManager.Instance.AllMembers.FirstOrDefault(
                m => m.instanceID == unit.MemberInstance.instanceID
            );
            if (instanceToUpdate != null)
            {
                instanceToUpdate.currentHP = Mathf.Max(1, unit.CurrentHP);
                Debug.Log($"已更新成員 [{instanceToUpdate.BaseData.memberName}] 的血量為: {instanceToUpdate.currentHP}");
            }
        }
    }

    private void ProcessBattleResults()
    {
        if (BattleManager.Instance == null) return;
        if (BattleManager.Instance.IsBattleWon())
        {
            lastBattleWon = true;

            var encounterData = BattleManager.Instance.EncounterData;
            int totalGold = 0;
            // int totalExp = 0;
            foreach(var enemyUnit in BattleManager.Instance.EnemyUnits)
            {
                if(enemyUnit.EnemyData != null)
                {
                    totalGold += enemyUnit.EnemyData.goldDrop;
                    // totalExp += enemyUnit.EnemyData.expDrop;
                }
            }
            if(PlayerState.Instance != null)
            {
                PlayerState.Instance.AddMoney(totalGold);
                // PlayerState.Instance.GainExperience(totalExp);
                Debug.Log($"戰鬥勝利！獲得金幣: {totalGold}");
            }
            if (QuestManager.Instance != null)
            {
                foreach (var enemyUnit in BattleManager.Instance.EnemyUnits)
                {
                    if (enemyUnit.IsDead && enemyUnit.EnemyData != null)
                    {
                        string enemyID = enemyUnit.EnemyData.enemyID;
                        QuestManager.Instance.AdvanceObjective(enemyID, QuestObjectiveType.Kill, 1);
                        Debug.Log($"向任務系統報告擊殺: {enemyID}");
                    }
                }
            }
        }
        else
        {
            lastBattleWon = false;
        }
    }
}