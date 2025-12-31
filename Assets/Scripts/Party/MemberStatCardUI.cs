using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemberStatCardUI : MonoBehaviour
{
    [Header("基礎連結")]
    [SerializeField] private Image memberIconImage;
    [SerializeField] private TextMeshProUGUI memberNameText;
    [SerializeField] private Button cardButton;
    [SerializeField] private GameObject selectedBorder;

    [Header("互動連結")]
    [SerializeField] private Button removeButton;
    [SerializeField] private Button skillButton;

    [Header("屬性連結")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI attackRangeText;

    private MemberInstance currentMember;

    public void Setup(MemberInstance memberInstance, System.Action<MemberInstance> onClickCallback)
    {
        currentMember = memberInstance;
        if (currentMember.BaseData == null) return;

        MemberDataSO baseData = currentMember.BaseData;
        memberNameText.text = baseData.memberName;
        memberIconImage.sprite = baseData.memberIcon;

        hpText.text = $"{currentMember.currentHP}/{currentMember.MaxHP}";
        attackText.text = $"{currentMember.CurrentAttack}";
        staminaText.text = $"{currentMember.currentStamina}/{currentMember.MaxStamina}";
        attackRangeText.text = $"{baseData.attackRange}";

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => 
        {
            if (InventoryManager.Instance.IsSelectingTarget)
            {
                InventoryManager.Instance.ConfirmItemUsageOnMember(currentMember);
            }
            else
            {
                onClickCallback?.Invoke(currentMember);
            }
        });
        if (removeButton != null)
        {
            removeButton.onClick.RemoveAllListeners();
            removeButton.onClick.AddListener(() => 
            {
                PartyManager.Instance.RemoveFromBattleParty(currentMember);
            });
        }
        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(() => 
            {
                if (MemberSkillUI.Instance != null)
                {
                    MemberSkillUI.Instance.ShowSkillList(currentMember);
                }
            });
        }
    }

    public void UpdateVisualState(bool isSelected)
    {
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(isSelected);
        }
    }

    public void SetRemoveButtonVisible(bool isVisible)
    {
        if (removeButton != null)
        {
            removeButton.gameObject.SetActive(isVisible);
        }
    }
}