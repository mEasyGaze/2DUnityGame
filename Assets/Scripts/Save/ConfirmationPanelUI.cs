using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ConfirmationPanelUI : MonoBehaviour
{
    [Header("UI 連結")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (cancelButton != null) cancelButton.onClick.AddListener(Hide);
    }

    public void Show(string title, string message, Action onConfirm)
    {
        if (panel == null)
        {
            Debug.LogError("ConfirmationPanelUI 的 panel 物件未指定！");
            return;
        }
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (confirmButton != null) confirmButton.onClick.RemoveAllListeners();
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() => onConfirm?.Invoke());
            confirmButton.onClick.AddListener(Hide);
        }
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}