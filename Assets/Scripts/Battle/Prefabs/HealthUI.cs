using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthUI : MonoBehaviour
{
    [Header("核心單位")]
    [SerializeField] private BattleUnit targetUnit;
    private BuffController buffController;

    [Header("UI 連結")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider shieldSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (targetUnit == null)
        {
            targetUnit = GetComponentInParent<BattleUnit>();
        }

        if (targetUnit != null)
        {
            buffController = targetUnit.GetComponent<BuffController>();
        }
        else
        {
            Debug.LogError("HealthUI 找不到目標 BattleUnit！", gameObject);
            this.enabled = false;
            return;
        }
        
        if (shieldSlider == null)
        {
            Debug.LogError("HealthUI 未指定 Shield Slider！護盾將無法正確顯示。", gameObject);
        }
    }

    void LateUpdate()
    {
        if (targetUnit == null || targetUnit.IsDead)
        {
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
            return;
        }
        
        float currentHP = targetUnit.CurrentHP;
        float maxHP = targetUnit.MaxHP;
        float currentShield = (buffController != null) ? buffController.CurrentShield : 0;
        
        if (shieldSlider != null && shieldSlider.gameObject.activeSelf != (currentShield > 0))
        {
            shieldSlider.gameObject.SetActive(currentShield > 0);
        }

        float totalCombined = currentHP + currentShield;
        float displayMaxValue;

        if (totalCombined > maxHP)
        {
            displayMaxValue = totalCombined;
        }
        else
        {
            displayMaxValue = maxHP;
        }

        if (healthSlider != null) healthSlider.maxValue = displayMaxValue;
        if (shieldSlider != null) shieldSlider.maxValue = displayMaxValue;
        
        if (shieldSlider != null) shieldSlider.value = totalCombined;
        if (healthSlider != null) healthSlider.value = currentHP;
        if (healthText != null)
        {
            int displayHP = Mathf.RoundToInt(currentHP);
            int displayMaxHP = Mathf.RoundToInt(maxHP);
            int displayShield = Mathf.RoundToInt(currentShield);

            if (currentShield > 0)
            {
                healthText.text = $"{displayHP}+({displayShield}) / {displayMaxHP}";
            }
            else
            {
                healthText.text = $"{displayHP} / {displayMaxHP}";
            }
        }
        
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}