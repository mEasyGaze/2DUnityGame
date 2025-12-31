using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI 連結")]
    [SerializeField] private TextMeshProUGUI slotIndexText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI sceneNameText;
    [SerializeField] private GameObject emptySlotInfo;
    [SerializeField] private GameObject filledSlotInfo;
    [SerializeField] private Button slotButton;
    [SerializeField] private Button deleteButton;

    private int slotIndex;
    private Action<int> onClickCallback;
    private Action<int> onDeleteCallback;

    public void Setup(int index, GameSaveData data, Action<int> clickCallback, Action<int> deleteCallback)
    {
        this.slotIndex = index;
        this.onClickCallback = clickCallback;
        this.onDeleteCallback = deleteCallback;

        slotIndexText.text = $"槽位 {index + 1}";

        bool hasData = (data != null);
        bool isCorrupted = SaveManager.Instance.DoesSaveFileExist(index) && !hasData;

        filledSlotInfo.SetActive(hasData || isCorrupted);
        emptySlotInfo.SetActive(!hasData && !isCorrupted);

        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(hasData || isCorrupted);
            if (hasData || isCorrupted)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            }
        }

        if (isCorrupted)
        {
            timestampText.text = "<color=red>存檔已損壞</color>";
            sceneNameText.text = "請刪除此存檔";
            slotButton.interactable = false;
        }
        else if (hasData)
        {
            timestampText.text = data.saveTimestamp;
            sceneNameText.text = $"場景: {data.worldData.sceneName}";
            slotButton.interactable = true;
        }

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotButtonClicked);
    }

    private void OnSlotButtonClicked()
    {
        onClickCallback?.Invoke(slotIndex);
    }

    private void OnDeleteButtonClicked()
    {
        onDeleteCallback?.Invoke(slotIndex);
    }

    public void SetInteractable(bool interactable)
    {
        if (slotButton != null)
        {
            slotButton.interactable = interactable;
        }
    }
}