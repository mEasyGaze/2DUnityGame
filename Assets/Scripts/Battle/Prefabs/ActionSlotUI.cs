using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActionSlotUI : MonoBehaviour
{
    [Header("內容UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI actionText;

    [Header("狀態物件")]
    [SerializeField] private GameObject emptyStateObject;
    [SerializeField] private GameObject filledStateObject;

    [Header("互動元件")]
    [SerializeField] private Button cancelButton;

    private ActionPlan currentPlan;

    public void Setup(System.Action<ActionPlan> onCancelClicked)
    {
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(() =>
        {
            if (currentPlan != null)
            {
                onCancelClicked(currentPlan);
            }
        });
    }

    public void UpdateView(ActionPlan plan)
    {
        currentPlan = plan;
        if (plan == null)
        {
            emptyStateObject.SetActive(true);
            filledStateObject.SetActive(false);
        }
        else
        {
            emptyStateObject.SetActive(false);
            filledStateObject.SetActive(true);
            
            if (plan.Source != null)
            {
                if (plan.Source.MemberData != null) icon.sprite = plan.Source.MemberData.memberIcon;
                else if (plan.Source.EnemyData != null) icon.sprite = plan.Source.EnemyData.enemyIcon;
                
                actionText.text = $"{plan.Source.UnitName[0]} > {plan.Type.ToActionName()}";
            }
            else
            {
                if (icon != null) icon.sprite = null;
                actionText.text = "跳過";
            }
        }
    }

    public void SetCancelButtonInteractable(bool isInteractable)
    {
        cancelButton.interactable = isInteractable;
    }
}