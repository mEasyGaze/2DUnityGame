using UnityEngine;
using System.Collections.Generic;

public class SkillSelectionPanelUI : MonoBehaviour
{
    [Header("UI 參考")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private SkillButtonUI buttonPrefab;

    private List<SkillButtonUI> spawnedButtons = new List<SkillButtonUI>();

    void Awake()
    {
        if (panel == null) panel = this.gameObject;
        panel.SetActive(false);
    }

    public void ShowPanel(Dictionary<SkillData, bool> skillFeasibility, System.Action<SkillData> onSkillSelectedCallback)
    {
        ClearButtons();

        if (skillFeasibility == null || skillFeasibility.Count == 0)
        {
            BattleLog.Instance.AddLog("此單位沒有可用的技能。");
            return;
        }

        foreach (var pair in skillFeasibility)
        {
            SkillData skill = pair.Key;
            bool isUsable = pair.Value;

            SkillButtonUI newButton = Instantiate(buttonPrefab, buttonContainer);
            
            System.Action<SkillData> wrappedCallback = (selectedSkill) => {
                onSkillSelectedCallback(selectedSkill);
                HidePanel();
            };
            
            newButton.Setup(skill, isUsable, wrappedCallback);
            spawnedButtons.Add(newButton);
        }

        panel.SetActive(true);
    }

    public void HidePanel()
    {
        panel.SetActive(false);
        ClearButtons();
    }

    private void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn.gameObject);
        }
        spawnedButtons.Clear();
    }
}