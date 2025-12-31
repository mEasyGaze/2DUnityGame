using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private Image skillIcon;

    public void Setup(SkillData skill, bool isUsable, System.Action<SkillData> onClickCallback)
    {
        if (skillNameText != null) skillNameText.text = skill.skillName;
        if (skillIcon != null)
        {
            skillIcon.sprite = skill.skillIcon;
            skillIcon.enabled = (skill.skillIcon != null);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => {
            onClickCallback(skill);
        });
        button.interactable = isUsable;
    }
}