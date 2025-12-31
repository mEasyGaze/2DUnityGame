using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyDetailUI : MonoBehaviour
{
    public static PartyDetailUI Instance { get; private set; }
    
    [Header("UI 連結")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private MemberSkillUI memberSkillPanel;
    [SerializeField] private Image memberIcon;
    [SerializeField] private TextMeshProUGUI memberNameText;
    // *** 移除: [SerializeField] private TextMeshProUGUI levelText; ***
    // *** 移除: [SerializeField] private TextMeshProUGUI expText; ***
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private TextMeshProUGUI attackRangeText;

    [Header("互動元件")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button skillsButton;
    
    private MemberInstance currentMember;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        detailPanel.SetActive(false);
        closeButton.onClick.AddListener(Hide);
    }
    
    public void ShowMemberDetails(MemberInstance member)
    {
        currentMember = member;
        detailPanel.SetActive(true);
        MemberDataSO baseData = member.BaseData;

        memberIcon.sprite = baseData.memberIcon;
        memberNameText.text = baseData.memberName;
        hpText.text = $"{member.currentHP} / {member.MaxHP}";
        attackText.text = $"{member.CurrentAttack}";
        staminaText.text = $"{member.currentStamina} / {member.MaxStamina}";
        attackRangeText.text = $"{baseData.attackRange}";
        // levelText.text = $"等級: {member.level}";
        // expText.text = $"經驗值: {member.experience} / 100"; 

        bool isInBattleParty = PartyManager.Instance.BattleParty.Contains(member);
        if (isInBattleParty)
        {
            actionButtonText.text = "移除";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(RemoveFromBattleParty);
        }
        else
        {
            actionButtonText.text = "上陣";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(AddToBattleParty);
        }

        if (skillsButton != null)
        {
            skillsButton.onClick.RemoveAllListeners();
            skillsButton.onClick.AddListener(() => 
            {
                if (MemberSkillUI.Instance != null)
                {
                    MemberSkillUI.Instance.ShowSkillList(member);
                }
            });
        }
    }

    public void Hide()
    {
        currentMember = null;
        detailPanel.SetActive(false);
        
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.NotifyPartyUpdated();
        }
    }
    
    public bool IsShowingDetailsFor(MemberInstance member)
    {
        return detailPanel.activeSelf && currentMember == member;
    }

    private void AddToBattleParty()
    {
        if (currentMember != null)
        {
            PartyManager.Instance.SetToBattleParty(currentMember);
            ShowMemberDetails(currentMember);
        }
    }

    private void RemoveFromBattleParty()
    {
        if (currentMember != null)
        {
            PartyManager.Instance.RemoveFromBattleParty(currentMember);
            ShowMemberDetails(currentMember);
        }
    }
}