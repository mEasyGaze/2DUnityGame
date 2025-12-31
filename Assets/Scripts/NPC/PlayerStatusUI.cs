using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusUI : MonoBehaviour
{
    [Header("UI 元件連結")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider experienceSlider;
    [SerializeField] private TextMeshProUGUI experienceText;

    void OnEnable()
    {
        PlayerState.OnMoneyChanged += UpdateMoneyText;
        PlayerState.OnLevelChanged += UpdateLevelText;
        PlayerState.OnExperienceChanged += UpdateExperienceUI;

        SaveManager.OnGameLoadComplete += RefreshAllStats;
        RefreshAllStats();
    }

    void OnDisable()
    {
        PlayerState.OnMoneyChanged -= UpdateMoneyText;
        PlayerState.OnLevelChanged -= UpdateLevelText;
        PlayerState.OnExperienceChanged -= UpdateExperienceUI;
        
        SaveManager.OnGameLoadComplete -= RefreshAllStats;
    }

    private void RefreshAllStats()
    {
        if (PlayerState.Instance != null)
        {
            UpdateMoneyText(PlayerState.Instance.GetCurrentMoney());
            UpdateLevelText(PlayerState.Instance.GetCurrentLevel());
            UpdateExperienceUI(PlayerState.Instance.GetCurrentExperience(), PlayerState.Instance.GetExperienceToNextLevel());
        }
    }

    private void UpdateMoneyText(int newMoneyValue)
    {
        if (moneyText != null)
        {
            moneyText.text = $"金錢：{newMoneyValue.ToString("N0")}";
        }
    }

    private void UpdateLevelText(int newLevelValue)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {newLevelValue}";
        }
    }

    private void UpdateExperienceUI(int currentExp, int maxExp)
    {
        if (experienceSlider != null)
        {
            experienceSlider.value = (maxExp > 0) ? (float)currentExp / maxExp : 0;
        }

        if (experienceText != null)
        {
            experienceText.text = $"{currentExp} / {maxExp}";
        }
    }
}