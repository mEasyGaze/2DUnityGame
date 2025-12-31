using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNameSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button button;

    private SkillData skillData;
    private System.Action<SkillData> onClickCallback;

    public void Setup(SkillData skill, System.Action<SkillData> callback)
    {
        skillData = skill;
        onClickCallback = callback;

        if (skill != null)
        {
            nameText.text = skill.skillName;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClickCallback?.Invoke(skillData));
    }
}