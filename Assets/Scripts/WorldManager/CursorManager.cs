using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("游標元件")]
    [SerializeField] private Image customCursorImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Canvas parentCanvas;

    [Header("提示框元件")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Vector3 tooltipOffset = new Vector3(20, -20, 0);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ResetCursor();
        HideTooltip();
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        if (customCursorImage != null && customCursorImage.gameObject.activeSelf)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePos,
                parentCanvas.worldCamera,
                out pos);
            customCursorImage.transform.position = mousePos;
        }
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            tooltipPanel.transform.position = mousePos + (Vector2)tooltipOffset;
        }
    }

    public void SetCursorIcon(Sprite icon)
    {
        if (customCursorImage != null && icon != null)
        {
            customCursorImage.sprite = icon;
            customCursorImage.gameObject.SetActive(true);
            customCursorImage.raycastTarget = false;
            Cursor.visible = false;
        }
    }

    public void SetQuantityText(string text)
    {
        if (quantityText != null)
        {
            quantityText.text = text;
            quantityText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    public void ShowTooltip(string content)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = content;
            tooltipPanel.SetActive(true);
        }
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void ResetCursor()
    {
        if (customCursorImage != null)
        {
            customCursorImage.gameObject.SetActive(false);
        }
        if (quantityText != null)
        {
            quantityText.gameObject.SetActive(false);
        }
        Cursor.visible = true;
    }
}