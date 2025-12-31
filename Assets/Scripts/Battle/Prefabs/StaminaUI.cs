using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private BattleUnit targetUnit;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private TextMeshProUGUI staminaText;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if(targetUnit == null)
        {
            targetUnit = GetComponentInParent<BattleUnit>();
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0;
            staminaSlider.maxValue = 1;
        }
    }

    void Update()
    {
        if (targetUnit != null && !targetUnit.IsDead)
        {
            staminaSlider.value = (float)targetUnit.CurrentStamina / targetUnit.MaxStamina;
            
            if (staminaText != null)
            {
                staminaText.text = $"{targetUnit.CurrentStamina} / {targetUnit.MaxStamina}";
            }            
            transform.rotation = mainCamera.transform.rotation;            
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}