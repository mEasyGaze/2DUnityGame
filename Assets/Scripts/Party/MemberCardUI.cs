using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class MemberCardUI : MonoBehaviour
{
    [SerializeField] private Image memberIconImage;
    [SerializeField] private TextMeshProUGUI memberNameText;
    [SerializeField] private Button cardButton;
    [SerializeField] private GameObject selectedBorder;
    [SerializeField] private GameObject inBattleIndicator;

    private MemberInstance currentMember;

    public void Setup(MemberInstance memberInstance, System.Action<MemberInstance> onClickCallback)
    {
        currentMember = memberInstance;
        if (currentMember.BaseData != null)
        {
            memberNameText.text = currentMember.BaseData.memberName;
            memberIconImage.sprite = currentMember.BaseData.memberIcon;
            cardButton.interactable = true;
        }
        else
        {
            memberNameText.text = "數據錯誤";
            memberIconImage.sprite = null; 
            cardButton.interactable = false;
        }
        
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => 
        {
            if (InventoryManager.Instance.IsSelectingTarget)
            {
                InventoryManager.Instance.ConfirmItemUsageOnMember(currentMember);
            }
            else
            {
                onClickCallback(currentMember);
            }
        });
    }

    public void UpdateVisualState(bool isSelected, bool isInBattleParty)
    {
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(isSelected);
        }
        if (inBattleIndicator != null)
        {
            inBattleIndicator.SetActive(isInBattleParty);
        }
    }
}