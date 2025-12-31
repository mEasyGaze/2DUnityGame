using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CharacterStateRule", menuName = "Battle System/Character State Rule")]
public class CharacterStateRule : ScriptableObject
{
    #region 背包模擬
    public class SimulatedInventory
    {
        private Dictionary<string, int> consumableCounts;

        public SimulatedInventory()
        {
            consumableCounts = new Dictionary<string, int>();
            if (InventoryManager.Instance != null && InventoryManager.Instance.playerInventoryData != null)
            {
                foreach (var slot in InventoryManager.Instance.playerInventoryData.slots)
                {
                    if (!slot.IsEmpty() && slot.item.itemType == ItemType.Consumable)
                    {
                        if (consumableCounts.ContainsKey(slot.item.uniqueItemID))
                        {
                            consumableCounts[slot.item.uniqueItemID] += slot.quantity;
                        }
                        else
                        {
                            consumableCounts[slot.item.uniqueItemID] = slot.quantity;
                        }
                    }
                }
            }
        }

        public SimulatedInventory(SimulatedInventory source)
        {
            this.consumableCounts = new Dictionary<string, int>(source.consumableCounts);
        }

        public void ConsumeItem(string itemID)
        {
            if (consumableCounts.ContainsKey(itemID) && consumableCounts[itemID] > 0)
            {
                consumableCounts[itemID]--;
            }
        }

        public bool HasAnyConsumables()
        {
            return consumableCounts.Any(pair => pair.Value > 0);
        }

        public bool HasItem(string itemID)
        {
            return consumableCounts.ContainsKey(itemID) && consumableCounts[itemID] > 0;
        }
    }

    public class BattleStateSnapshot
    {
        public List<UnitStateSnapshot> UnitSnapshots { get; }
        public SimulatedInventory InventorySnapshot { get; }

        public BattleStateSnapshot(List<UnitStateSnapshot> unitSnaps, SimulatedInventory inventorySnap)
        {
            UnitSnapshots = unitSnaps;
            InventorySnapshot = inventorySnap;
        }
    }
    #endregion

    #region 快照資料 & 運行
    public class UnitStateSnapshot
    {
        public IBattleUnit_ReadOnly Unit { get; }
        public int Stamina { get; }
        public GridPosition Position { get; }
        public BattleRole Role { get; }
        public UnitStateSnapshot(IBattleUnit_ReadOnly unit) { Unit = unit; Stamina = unit.CurrentStamina; Position = unit.CurrentPosition; Role = unit.Role; }
        public UnitStateSnapshot(UnitStateSnapshot source) { Unit = source.Unit; Stamina = source.Stamina; Position = source.Position; Role = source.Role; }
        public UnitStateSnapshot(UnitStateSnapshot source, int newStamina, GridPosition newPosition, BattleRole newRole) { Unit = source.Unit; Stamina = newStamina; Position = newPosition; Role = newRole; }
    }
    private List<BattleStateSnapshot> planningStepSnapshots = new List<BattleStateSnapshot>();

    public void InitializeSnapshots(List<BattleUnit> allUnits)
    {
        planningStepSnapshots.Clear();
        
        List<UnitStateSnapshot> initialUnitSnapshot = new List<UnitStateSnapshot>();
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
            {
                initialUnitSnapshot.Add(new UnitStateSnapshot(unit));
            }
        }
        SimulatedInventory initialInventory = new SimulatedInventory();
        BattleStateSnapshot initialBattleState = new BattleStateSnapshot(initialUnitSnapshot, initialInventory);

        planningStepSnapshots.Add(initialBattleState);
        Debug.Assert(planningStepSnapshots.Count == 1, "初始化後，快照列表長度不為1！");
    }

    public BattleStateSnapshot GetLatestSnapshot()
    {
        if (planningStepSnapshots.Count == 0)
        {
            Debug.LogError("嚴重錯誤：快照列表為空！無法獲取最新快照。");
            return null;
        }
        return planningStepSnapshots.Last();
    }
    
    public void PruneSnapshotsToCount(int targetCount)
    {
        if (targetCount < 1) targetCount = 1;

        if (planningStepSnapshots.Count > targetCount)
        {
            int removeCount = planningStepSnapshots.Count - targetCount;
            planningStepSnapshots.RemoveRange(targetCount, removeCount);
        }
    }

    public void GenerateAndStoreNextSnapshot(ActionPlan plan, BattleActions actionCosts)
    {
        BattleStateSnapshot previousBattleState = GetLatestSnapshot();
        if (previousBattleState == null) return;

        List<UnitStateSnapshot> nextUnitState = previousBattleState.UnitSnapshots.Select(s => new UnitStateSnapshot(s)).ToList();
        
        SimulatedInventory nextInventoryState = new SimulatedInventory(previousBattleState.InventorySnapshot);

        if (plan.Type != ActionType.Skip && plan.Source != null)
        {
            var sourceSnap = nextUnitState.FirstOrDefault(s => s.Unit == plan.Source);
            if (sourceSnap != null)
            {
                int sourceIndex = nextUnitState.IndexOf(sourceSnap);
                int newStamina = sourceSnap.Stamina;
                GridPosition newSourcePos = sourceSnap.Position;
                BattleRole newSourceRole = sourceSnap.Role;

                switch (plan.Type)
                {
                    case ActionType.Attack:
                        newStamina -= actionCosts.GetAttackStaminaCost();
                        break;

                    case ActionType.Rest:
                        newStamina += actionCosts.GetRestStaminaRecovery();
                        newStamina = Mathf.Min(newStamina, plan.Source.MaxStamina);
                        break;

                    case ActionType.Item:
                        if (plan.ItemUsed != null)
                        {
                            nextInventoryState.ConsumeItem(plan.ItemUsed.uniqueItemID);
                        }
                        break;
                    
                    case ActionType.Exchange:
                        newStamina -= actionCosts.GetExchangeStaminaCost();
                        var targetSnap = nextUnitState.FirstOrDefault(s => s.Unit == plan.Target);
                        if (targetSnap != null)
                        {
                            int targetIndex = nextUnitState.IndexOf(targetSnap);
                            newSourcePos = targetSnap.Position;
                            newSourcePos = targetSnap.Position;
                            GridPosition newTargetPos = sourceSnap.Position;
                            newSourceRole = targetSnap.Role;
                            BattleRole newTargetRole = sourceSnap.Role;
                            nextUnitState[targetIndex] = new UnitStateSnapshot(targetSnap, targetSnap.Stamina, newTargetPos, newTargetRole);
                        }
                        break;
                    case ActionType.Skill:
                        if (plan.SkillUsed != null)
                        {
                            newStamina -= plan.SkillUsed.staminaCost;
                        }
                        break;
                }
                nextUnitState[sourceIndex] = new UnitStateSnapshot(sourceSnap, newStamina, newSourcePos, newSourceRole);
            }
        }
        BattleStateSnapshot nextBattleState = new BattleStateSnapshot(nextUnitState, nextInventoryState);
        planningStepSnapshots.Add(nextBattleState);
    }
    
    public void RestoreAllUnitStamina(List<BattleUnit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
            {
                int recoveryAmount = Mathf.CeilToInt(unit.MaxStamina / 2f);
                unit.RestoreStamina(recoveryAmount);
            }
        }
    }
    #endregion
}