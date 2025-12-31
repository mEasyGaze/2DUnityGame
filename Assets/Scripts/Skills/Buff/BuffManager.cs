using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void TickAllBuffsOnAllUnits(List<BattleUnit> allUnits)
    {
        foreach (var unit in allUnits)
        {
            if (unit != null && !unit.IsDead)
            {
                var controller = unit.GetComponent<BuffController>();
                if (controller != null)
                {
                    controller.TickAllBuffs();
                }
            }
        }
    }
}