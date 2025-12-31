using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class KeybindingManager : MonoBehaviour
{
    private InventoryUI inventoryUI; 
    private QuestUI questUI;
    private PartyHolderUI partyHolderUI;
    private PartyBattleUI partyBattleUI;
    private GameSystemUI gameSystemUI;

    public static KeybindingManager Instance { get; private set; }
    private bool areEventsSubscribed = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToInputEvents();
    }
    
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (InputManager.Instance != null)
        {
            UnsubscribeFromInputEvents();
        }
    }
    
    void Start()
    {
        FindUIReferences();
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIReferences();
    }
    
    private void SubscribeToInputEvents()
    {
        if (InputManager.Instance == null || areEventsSubscribed) return;

        InputManager.Instance.OnToggleInventory += HandleToggleInventory;
        InputManager.Instance.OnToggleQuestLog += HandleToggleQuestLog;
        InputManager.Instance.OnTogglePartyHolder += HandleTogglePartyHolder;
        InputManager.Instance.OnToggleBattleParty += HandleToggleBattleParty;
        InputManager.Instance.OnEscape += HandleEscapeKey;
        
        areEventsSubscribed = true;
        Debug.Log("KeybindingManager: Events subscribed.");
    }
    
    private void UnsubscribeFromInputEvents()
    {
        if (InputManager.Instance == null || !areEventsSubscribed) return;

        InputManager.Instance.OnToggleInventory -= HandleToggleInventory;
        InputManager.Instance.OnToggleQuestLog -= HandleToggleQuestLog;
        InputManager.Instance.OnTogglePartyHolder -= HandleTogglePartyHolder;
        InputManager.Instance.OnToggleBattleParty -= HandleToggleBattleParty;
        InputManager.Instance.OnEscape -= HandleEscapeKey;
        
        areEventsSubscribed = false;
        Debug.Log("KeybindingManager: Events unsubscribed.");
    }

    private void FindUIReferences()
    {
        inventoryUI = FindObjectOfType<InventoryUI>(true);
        questUI = FindObjectOfType<QuestUI>(true);
        partyHolderUI = FindObjectOfType<PartyHolderUI>(true);
        partyBattleUI = FindObjectOfType<PartyBattleUI>(true);
        gameSystemUI = FindObjectOfType<GameSystemUI>(true);
    }
    
    #region 按鍵處理邏輯
    private void HandleToggleInventory() => inventoryUI?.TogglePanel();
    private void HandleToggleQuestLog() => questUI?.TogglePanel();
    private void HandleTogglePartyHolder() => partyHolderUI?.TogglePanel();
    private void HandleToggleBattleParty() => partyBattleUI?.TogglePanel();
    
    private void HandleEscapeKey()
    {
        if (GameManager.Instance != null)
        {
            GameState state = GameManager.Instance.CurrentGameState;
            if (state != GameState.Exploration && state != GameState.Paused)
            {
                return;
            }
        }
        
        if (gameSystemUI != null && gameSystemUI.IsAnyPanelActive())
        {
            gameSystemUI.TryCloseSubPanels();
            return;
        }
        
        if (inventoryUI != null && inventoryUI.IsPanelActive()) 
        {
            inventoryUI.HidePanel();
        }
        else if (questUI != null && questUI.gameObject.activeInHierarchy)
        {
            questUI.TogglePanel();
        }
        else if (partyHolderUI != null && partyHolderUI.gameObject.activeInHierarchy)
        {
            partyHolderUI.TogglePanel();
        }
        else if (partyBattleUI != null && partyBattleUI.gameObject.activeInHierarchy)
        {
            partyBattleUI.TogglePanel();
        }
        else
        {
            if (gameSystemUI != null)
            {
                gameSystemUI.ToggleMainPanel();
            }
        }
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsSelectingTarget)
        {
            return; 
        }
    }
    #endregion
    
    #region 按鍵綁定
    public void SaveKeybindings()
    {
        if (InputManager.Instance == null) return;
        var playerControls = InputManager.Instance.GetPlayerControls();
        if (playerControls == null) return;
        string rebinds = playerControls.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("KeyRebinds", rebinds);
        PlayerPrefs.Save();
        Debug.Log("按鍵綁定已儲存。");
    }

    public void LoadKeybindings()
    {
        if (InputManager.Instance == null) return;
        var playerControls = InputManager.Instance.GetPlayerControls();
        if (playerControls == null) return;
        if (PlayerPrefs.HasKey("KeyRebinds"))
        {
            string rebinds = PlayerPrefs.GetString("KeyRebinds");
            playerControls.LoadBindingOverridesFromJson(rebinds);
            Debug.Log("按鍵綁定已載入。");
        }
    }
    #endregion
}