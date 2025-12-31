using UnityEngine;
using System.Collections.Generic;

public class PartyBattleUI : MonoBehaviour
{
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private Transform battleSlotsParent;
    [SerializeField] private MemberStatCardUI cardPrefab; 

    private List<MemberStatCardUI> spawnedCards = new List<MemberStatCardUI>();

    private void Awake()
    {
        if (battlePanel == null) battlePanel = this.gameObject;
        battlePanel.SetActive(false);
    }

    private void OnEnable()
    {
        PartyManager.OnPartyUpdated += UpdateUI;
        InventoryManager.OnItemSelectionModeChanged += HandleItemSelectionModeChanged;
        HandleItemSelectionModeChanged(InventoryManager.Instance.IsSelectingTarget);
    }

    private void OnDisable()
    {
        PartyManager.OnPartyUpdated -= UpdateUI;
        InventoryManager.OnItemSelectionModeChanged -= HandleItemSelectionModeChanged;
        if (PartyDetailUI.Instance != null)
        {
            PartyDetailUI.Instance.Hide();
        }
    }

    public void TogglePanel()
    {
        bool isActive = !battlePanel.activeSelf;
        battlePanel.SetActive(isActive);
        if (isActive)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (PartyManager.Instance == null) return;
        foreach (var card in spawnedCards)
        {
            if (card != null) Destroy(card.gameObject);
        }
        spawnedCards.Clear();
        foreach (var member in PartyManager.Instance.BattleParty)
        {
            MemberStatCardUI newCard = Instantiate(cardPrefab, battleSlotsParent);
            
            newCard.Setup(member, null); 
            newCard.SetRemoveButtonVisible(!InventoryManager.Instance.IsSelectingTarget);
            newCard.UpdateVisualState(false); 
            
            spawnedCards.Add(newCard);
        }
    }

    private void HandleItemSelectionModeChanged(bool isSelecting)
    {
        foreach (var card in spawnedCards)
        {
            if (card != null)
            {
                card.SetRemoveButtonVisible(!isSelecting);
            }
        }
    }
}