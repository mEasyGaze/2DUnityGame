using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MemberSkillUI : MonoBehaviour
{
    public static MemberSkillUI Instance { get; private set; }

    [Header("列表面板")]
    [SerializeField] private GameObject listPanel;
    [SerializeField] private TextMeshProUGUI listTitleText;
    [SerializeField] private Transform listContainer;
    [SerializeField] private SkillNameSlotUI slotPrefab;
    [SerializeField] private Button listCloseButton;

    [Header("詳情面板")]
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI staminaCostText;
    [SerializeField] private TextMeshProUGUI targetTypeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button detailsCloseButton;

    private List<SkillNameSlotUI> spawnedSlots = new List<SkillNameSlotUI>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        listCloseButton.onClick.AddListener(CloseList);
        detailsCloseButton.onClick.AddListener(CloseDetails);
        CloseAll();
    }

    public void ShowSkillList(MemberInstance member)
    {
        if (member == null) return;
        CloseDetails();
        listPanel.SetActive(true);
        
        if (listTitleText != null) listTitleText.text = $"{member.BaseData.memberName} 的技能";
        foreach (var slot in spawnedSlots) Destroy(slot.gameObject);
        spawnedSlots.Clear();
        if (member.Skills != null)
        {
            foreach (var skill in member.Skills)
            {
                var slot = Instantiate(slotPrefab, listContainer);
                slot.Setup(skill, ShowSkillDetails);
                spawnedSlots.Add(slot);
            }
        }
    }

    private void ShowSkillDetails(SkillData skill)
    {
        detailsPanel.SetActive(true);
        // listPanel.SetActive(false); 

        skillNameText.text = skill.skillName;
        staminaCostText.text = $"消耗體力: {skill.staminaCost}";
        targetTypeText.text = $"目標: {GetTargetTypeString(skill.targetType)}";
        descriptionText.text = skill.skillDescription;
    }

    public void CloseList()
    {
        listPanel.SetActive(false);
        CloseDetails();
    }

    public void CloseDetails()
    {
        detailsPanel.SetActive(false);
    }

    public void CloseAll()
    {
        listPanel.SetActive(false);
        detailsPanel.SetActive(false);
    }

    private string GetTargetTypeString(SkillTargetType type)
    {
        switch (type)
        {
            case SkillTargetType.Enemy_Single: return "單體敵人";
            case SkillTargetType.Enemy_All: return "全體敵人";
            case SkillTargetType.Ally_Single: return "單體隊友";
            case SkillTargetType.Ally_All: return "全體隊友";
            case SkillTargetType.Self: return "自身";
            default: return type.ToString();
        }
    }
}