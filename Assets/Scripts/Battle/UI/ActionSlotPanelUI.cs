using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class ActionSlotPanelUI : MonoBehaviour
{
    [Header("UI 連結")]
    [SerializeField] private GameObject panel;
    [SerializeField] private List<ActionSlotUI> vanguardSlots;
    [SerializeField] private List<ActionSlotUI> rangedSlots;
    [SerializeField] private List<GameObject> highlightObjects;

    private Dictionary<int, ActionSlotUI> allSlots = new Dictionary<int, ActionSlotUI>();

    public void Initialize(System.Action<UICommandType, ActionPlan> onCommand)
    {
        for (int i = 0; i < vanguardSlots.Count; i++)
        {
            int stepIndex = i * 2;
            allSlots[stepIndex] = vanguardSlots[i];
            vanguardSlots[i].Setup(plan => onCommand(UICommandType.CancelSingleAction, plan));
        }
        for (int i = 0; i < rangedSlots.Count; i++)
        {
            int stepIndex = i * 2 + 1;
            allSlots[stepIndex] = rangedSlots[i];
            rangedSlots[i].Setup(plan => onCommand(UICommandType.CancelSingleAction, plan));
        }
    }
    
    public void UpdatePanel(TurnActionPlanner planner)
    {
        int lastPlannedStep = planner.GetLastPlanStepIndex();
        
        for(int i = 0; i < 8; i++)
        {
            if (allSlots.TryGetValue(i, out ActionSlotUI slot))
            {
                ActionPlan plan = planner.GetPlanAtStep(i);
                
                slot.UpdateView(plan);
                
                bool isCancellable = (plan != null && i == lastPlannedStep);
                
                if (plan != null && plan.TransactionID != Guid.Empty)
                {
                    var lastPlan = planner.GetPlanAtStep(lastPlannedStep);
                    if (lastPlan != null && lastPlan.TransactionID == plan.TransactionID)
                    {
                        isCancellable = true;
                    }
                }
                slot.SetCancelButtonInteractable(isCancellable);
            }
        }
        
        panel.SetActive(true);
    }

    public void SetPlanningHighlight(int stepIndex)
    {
        for (int i = 0; i < highlightObjects.Count; i++)
        {
            if (highlightObjects[i] != null)
            {
                highlightObjects[i].SetActive(i == stepIndex);
            }
        }
    }
    
    public void HidePanel()
    {
        panel.SetActive(false);
    }
}