using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BattlefieldStateSimulator : MonoBehaviour
{
    private List<BattleUnit> allUnits;
    private CharacterStateRule characterStateRule;

    public void Initialize(List<BattleUnit> units, CharacterStateRule rule)
    {
        allUnits = units;
        characterStateRule = rule;
    }

    public void ShowStateFromSnapshot(List<CharacterStateRule.UnitStateSnapshot> snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogError("傳入的快照為空，無法更新視覺！");
            return;
        }

        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
            {
                unit.ResetVisualsToCoreState();
            }
        }

        foreach (var unitSnapshot in snapshot)
        {
            BattleUnit unitToUpdate = allUnits.FirstOrDefault(u => u == unitSnapshot.Unit.GetMonoBehaviour());
            
            if (unitToUpdate != null && !unitToUpdate.IsDead)
            {
                GridPosition previewPos = unitSnapshot.Position;
                int previewStamina = unitSnapshot.Stamina;
                BattleRole previewRole = unitSnapshot.Role;

                unitToUpdate.UpdatePreviewVisuals(previewPos, previewStamina, previewRole);
            }
        }
    }

    public void ShowTemporaryStaminaPreview(IBattleUnit_ReadOnly unitToModify, int staminaCost)
    {
        var latestSnapshot = characterStateRule.GetLatestSnapshot();
        if (latestSnapshot == null) return;

        ShowStateFromSnapshot(latestSnapshot.UnitSnapshots);

        BattleUnit unitMono = unitToModify.GetMonoBehaviour();
        if (unitMono == null) return;

        var unitSnap = latestSnapshot.UnitSnapshots.FirstOrDefault(s => s.Unit == unitToModify);

        if (unitSnap != null)
        {
            int finalPreviewStamina = unitSnap.Stamina - staminaCost;
            unitMono.UpdatePreviewVisuals(unitSnap.Position, finalPreviewStamina, unitSnap.Role);
        }
    }
    
    public void ClearTemporaryPreviews()
    {
        var latestSnapshot = characterStateRule.GetLatestSnapshot();
        if (latestSnapshot != null)
        {
            ShowStateFromSnapshot(latestSnapshot.UnitSnapshots);
        }
    }
}