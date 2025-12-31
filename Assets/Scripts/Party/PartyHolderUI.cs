using UnityEngine;
using System.Collections.Generic;

public class PartyHolderUI : MonoBehaviour
{
    [SerializeField] private GameObject holderPanel;
    [SerializeField] private Transform contentParent;
    [SerializeField] private MemberCardUI cardPrefab;

    private List<MemberCardUI> spawnedCards = new List<MemberCardUI>();

    private void Awake()
    {
        if (holderPanel == null) holderPanel = this.gameObject;
        holderPanel.SetActive(false);
        SaveManager.OnGameLoadComplete += HandleGameLoadComplete;
    }

    private void OnEnable()
    {
        PartyManager.OnPartyUpdated += UpdateUI;
        if (holderPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    private void OnDisable()
    {
        PartyManager.OnPartyUpdated -= UpdateUI;
        if (PartyDetailUI.Instance != null)
        {
            PartyDetailUI.Instance.Hide();
        }
    }

    private void OnDestroy()
    {
        SaveManager.OnGameLoadComplete -= HandleGameLoadComplete;
    }

    private void HandleGameLoadComplete()
    {
        if (holderPanel.activeSelf)
        {
            Debug.Log("[PartyHolderUI] 監聽到遊戲加載完成，正在刷新...");
            UpdateUI();
        }
    }

    public void TogglePanel()
    {
        bool isActive = !holderPanel.activeSelf;
        holderPanel.SetActive(isActive);

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
            Destroy(card.gameObject);
        }
        spawnedCards.Clear();

        foreach (var member in PartyManager.Instance.AllMembers)
        {
            MemberCardUI newCard = Instantiate(cardPrefab, contentParent);
            newCard.Setup(member, OnMemberCardClicked);
            
            bool isSelected = PartyDetailUI.Instance != null && PartyDetailUI.Instance.IsShowingDetailsFor(member);
            bool isInBattle = PartyManager.Instance.BattleParty.Contains(member);
            newCard.UpdateVisualState(isSelected, isInBattle);
            
            spawnedCards.Add(newCard);
        }
    }

    private void OnMemberCardClicked(MemberInstance member)
    {
        PartyDetailUI.Instance.ShowMemberDetails(member);
        UpdateUI();
    }
}