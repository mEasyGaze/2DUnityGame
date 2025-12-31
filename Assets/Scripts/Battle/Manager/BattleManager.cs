using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BattleManager : MonoBehaviour
{
    #region 核心數據&模組引用
    public static BattleManager Instance { get; private set; }
    public BattleEncounterSO EncounterData { get; private set; }
    public List<BattleUnit> PlayerUnits { get; private set; }
    public List<BattleUnit> EnemyUnits { get; private set; }
    
    public Transform[] GridSpawns => gridSpawns;
    [SerializeField] private Transform[] gridSpawns = new Transform[8];

    [Header("系統模組")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private BattleRules battleRules;
    [SerializeField] private BattleActions battleActions;
    [SerializeField] private EnemyBattleAI enemyAI;
    [SerializeField] private CharacterStateRule characterStateRule;
    [SerializeField] private BattlefieldStateSimulator stateSimulator;
    
    private TurnActionPlanner actionPlanner;
    #endregion

    #region Unity生命週期
    private void Awake() 
    { 
        Instance = this; 
        if (stateSimulator == null)
        {
            stateSimulator = FindObjectOfType<BattlefieldStateSimulator>();
        }
    }
    
    void Start()
    {
        EncounterData = GameManager.Instance.CurrentEncounter;
        if (EncounterData == null) { Debug.LogError("沒有傳入戰鬥遭遇數據！"); return; }

        SpawnUnits();
        InitializeSystems();
        
        turnManager.StartBattle();
    }
    #endregion

    #region 初始化與單位生成
    private void InitializeSystems()
    {
        actionPlanner = new TurnActionPlanner();
        turnManager.Initialize(this, uiManager, actionPlanner, battleRules, battleActions, enemyAI, characterStateRule, stateSimulator, EncounterData.enemyPersonality);
        uiManager.Initialize(turnManager);
        stateSimulator.Initialize(GetAllUnits(), characterStateRule);
    }
    
    private void SpawnUnits()
    {
        PlayerUnits = new List<BattleUnit>();
        EnemyUnits = new List<BattleUnit>();
    
        var battleParty = PartyManager.Instance.BattleParty;
        for (int i = 0; i < battleParty.Count; i++)
        {
            var role = (BattleRole)i;
            var position = GetInitialPositionForRole(role, true);
            var spawnTransform = gridSpawns[(int)position];
            
            GameObject unitGO = Instantiate(battleParty[i].BaseData.unitPrefab, spawnTransform.position, spawnTransform.rotation);
            BattleUnit unit = unitGO.GetComponent<BattleUnit>();
            unit.Setup(battleParty[i], role, position);
            unit.OnUnitClicked += turnManager.OnUnitClicked;
            PlayerUnits.Add(unit);
        }
        foreach (var enemyPos in EncounterData.enemyTeam)
        {
            var role = enemyPos.role;
            var position = GetInitialPositionForRole(role, false);
            var spawnTransform = gridSpawns[(int)position];
            GameObject unitGO = Instantiate(enemyPos.enemyData.enemyPrefab, spawnTransform.position, spawnTransform.rotation);
            BattleUnit unit = unitGO.GetComponent<BattleUnit>();
            unit.Setup(enemyPos.enemyData, role, position);
            unit.OnUnitClicked += turnManager.OnUnitClicked;
            EnemyUnits.Add(unit);
        }
    }
    #endregion

    #region 戰鬥邏輯輔助
    public void HandleTeamPromotions()
    {
        CheckAndPromoteForTeam(PlayerUnits);
        CheckAndPromoteForTeam(EnemyUnits);
    }

    private void CheckAndPromoteForTeam(List<BattleUnit> team)
    {
        var promotionOrder = new List<GridPosition> { GridPosition.PlayerVanguard, GridPosition.PlayerRanged1, GridPosition.PlayerRanged2, GridPosition.PlayerSupport };
        if (team.Any() && !team.First().IsPlayerTeam)
        {
            promotionOrder = new List<GridPosition> { GridPosition.EnemyVanguard, GridPosition.EnemyRanged1, GridPosition.EnemyRanged2, GridPosition.EnemySupport };
        }
        
        for (int i = 0; i < promotionOrder.Count - 1; i++)
        {
            GridPosition currentPos = promotionOrder[i];
            BattleUnit unitAtCurrentPos = GetUnitAtPosition(currentPos, team);
            if (unitAtCurrentPos == null || unitAtCurrentPos.IsDead)
            {
                for (int j = i + 1; j < promotionOrder.Count; j++)
                {
                    GridPosition substitutePos = promotionOrder[j];
                    BattleUnit substituteUnit = GetUnitAtPosition(substitutePos, team);
                    if (substituteUnit != null && !substituteUnit.IsDead)
                    {
                        BattleRole oldRole = substituteUnit.Role;
                        BattleRole newRole = GetRoleForPosition(currentPos);
                        BattleLog.Instance.AddLog($"{substituteUnit.UnitName} (原 {oldRole}) 自動替補到 {currentPos}，新身份為 {newRole}！");
                        
                        substituteUnit.SetNewPosition(currentPos);
                        substituteUnit.SetRole(newRole);
                        substituteUnit.transform.position = gridSpawns[(int)currentPos].position;
                        break;
                    }
                }
            }
        }
    }

    private GridPosition GetInitialPositionForRole(BattleRole role, bool isPlayer)
    {
        if (isPlayer)
        {
            switch (role)
            {
                case BattleRole.Vanguard: return GridPosition.PlayerVanguard;
                case BattleRole.Ranged1:  return GridPosition.PlayerRanged1;
                case BattleRole.Ranged2:  return GridPosition.PlayerRanged2;
                case BattleRole.Support:  return GridPosition.PlayerSupport;
            }
        }
        else
        {
            switch (role)
            {
                case BattleRole.Vanguard: return GridPosition.EnemyVanguard;
                case BattleRole.Ranged1:  return GridPosition.EnemyRanged1;
                case BattleRole.Ranged2:  return GridPosition.EnemyRanged2;
                case BattleRole.Support:  return GridPosition.EnemySupport;
            }
        }
        throw new System.Exception("無效的角色或隊伍");
    }

    private BattleRole GetRoleForPosition(GridPosition position)
    {
        switch (position)
        {
            case GridPosition.PlayerVanguard:
            case GridPosition.EnemyVanguard:
                return BattleRole.Vanguard;
            
            case GridPosition.PlayerRanged1:
            case GridPosition.EnemyRanged1:
                return BattleRole.Ranged1;

            case GridPosition.PlayerRanged2:
            case GridPosition.EnemyRanged2:
                return BattleRole.Ranged2;

            case GridPosition.PlayerSupport:
            case GridPosition.EnemySupport:
                return BattleRole.Support;
            
            default:
                Debug.LogError($"無法為位置 {position} 找到對應的身份！");
                return BattleRole.Vanguard;
        }
    }
    #endregion

    #region 動畫協程
    public IEnumerator AnimateExchangeCoroutine(BattleUnit unitA, BattleUnit unitB, float duration)
    {
        Vector3 startPosA = unitA.transform.position;
        Vector3 startPosB = unitB.transform.position;
        
        Transform endTransformA = GridSpawns[(int)unitA.CurrentPosition];
        Transform endTransformB = GridSpawns[(int)unitB.CurrentPosition];

        Vector3 endPosA = endTransformA.position;
        Vector3 endPosB = endTransformB.position;

        float elapsedTime = 0f;
        float swayAmplitude = 20f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);

            unitA.transform.position = Vector3.Lerp(startPosA, endPosA, progress);
            unitB.transform.position = Vector3.Lerp(startPosB, endPosB, progress);

            float sway = Mathf.Sin(progress * Mathf.PI * 2 * (duration / 0.5f)) * swayAmplitude * (1 - progress);
            unitA.transform.rotation = Quaternion.Euler(0, 0, sway);
            unitB.transform.rotation = Quaternion.Euler(0, 0, -sway);

            yield return null;
        }
        unitA.transform.position = endPosA;
        unitB.transform.position = endPosB;
        unitA.transform.rotation = Quaternion.identity;
        unitB.transform.rotation = Quaternion.identity;
    }
    #endregion

    #region 公共查詢方法
    public BattleUnit GetUnitAtPosition(GridPosition pos, List<BattleUnit> team) => team.FirstOrDefault(u => u.CurrentPosition == pos && !u.IsDead);
    public List<BattleUnit> GetAllUnits() => PlayerUnits.Concat(EnemyUnits).ToList();
    public List<BattleUnit> GetOpposingTeam(BattleUnit unit) => unit.IsPlayerTeam ? EnemyUnits : PlayerUnits;
    public List<BattleUnit> GetSameTeam(BattleUnit unit) => unit.IsPlayerTeam ? PlayerUnits : EnemyUnits;
    public BattleUnit GetUnitAtPosition(GridPosition pos)
    {
        if (pos == GridPosition.None) return null;
        return GetAllUnits().FirstOrDefault(u => !u.IsDead && u.CurrentPosition == pos);
    }
    public bool IsBattleWon()
    {
        return turnManager != null && turnManager.CurrentState == BattleState.Won;
    }
    #endregion
}