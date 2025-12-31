using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillListSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedHighlight;

    private SkillData skillData;
    private System.Action<SkillData> onClickCallback;

    public void Setup(SkillData skill, System.Action<SkillData> callback)
    {
        skillData = skill;
        onClickCallback = callback;

        if (skill != null)
        {
            nameText.text = skill.skillName;
            iconImage.sprite = skill.skillIcon;
            // iconImage.enabled = skill.icon != null;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => 
        {
            onClickCallback?.Invoke(skillData);
            SetSelected(true);
        });
        
        SetSelected(false);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight != null) selectedHighlight.SetActive(isSelected);
    }
}