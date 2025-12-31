using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    
    private PlayerControls playerControls;

    public bool IsInitialized { get; private set; } = false;

    public event Action<Vector2> OnMove;
    public event Action OnInteract;
    public event Action OnPickup;
    public event Action OnToggleInventory;
    public event Action OnToggleQuestLog;
    public event Action OnTogglePartyHolder;
    public event Action OnToggleBattleParty;
    public event Action OnToggleMap;
    public event Action OnEscape;

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("發現重複的 InputManager 實例，已將新的銷毀。");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Initialize();
    }

    private void Initialize()
    {
        if (IsInitialized) return;
        playerControls = new PlayerControls();
        RegisterActions();
        IsInitialized = true;
        Debug.Log("InputManager Initialized and is now persistent.");
    }

    private void OnEnable()
    {
        playerControls?.UI.Enable();
        playerControls?.Gameplay.Enable();
    }

    private void OnDisable()
    {
        playerControls?.UI.Disable();
        playerControls?.Gameplay.Disable();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            if (playerControls != null)
            {
                UnregisterGameplayActions();
                playerControls.Dispose();
                playerControls = null;
            }
        }
    }

    private void RegisterActions()
    {
        playerControls.Gameplay.Move.performed += HandleMove;
        playerControls.Gameplay.Move.canceled += HandleMove;
        playerControls.Gameplay.Interact.performed += HandleInteract;
        playerControls.Gameplay.Pickup.performed += HandlePickup;
        playerControls.Gameplay.ToggleInventory.performed += HandleToggleInventory;
        playerControls.Gameplay.ToggleQuestLog.performed += HandleToggleQuestLog;
        playerControls.Gameplay.TogglePartyHolder.performed += HandleTogglePartyHolder;
        playerControls.Gameplay.ToggleBattleParty.performed += HandleToggleBattleParty;
        playerControls.Gameplay.ToggleMap.performed += HandleToggleMap;
        playerControls.UI.Escape.performed += HandleEscape;
    }

    private void UnregisterGameplayActions()
    {
        if (playerControls == null) return;
        playerControls.Gameplay.Move.performed -= HandleMove;
        playerControls.Gameplay.Move.canceled -= HandleMove;
        playerControls.Gameplay.Interact.performed -= HandleInteract;
        playerControls.Gameplay.Pickup.performed -= HandlePickup;
        playerControls.Gameplay.ToggleInventory.performed -= HandleToggleInventory;
        playerControls.Gameplay.ToggleQuestLog.performed -= HandleToggleQuestLog;
        playerControls.Gameplay.TogglePartyHolder.performed -= HandleTogglePartyHolder;
        playerControls.Gameplay.ToggleBattleParty.performed -= HandleToggleBattleParty;
        playerControls.Gameplay.ToggleMap.performed -= HandleToggleMap;
        playerControls.UI.Escape.performed -= HandleEscape;
    }

    #region 輸入操作處理
    private void HandleMove(InputAction.CallbackContext context) => OnMove?.Invoke(context.ReadValue<Vector2>());
    private void HandleInteract(InputAction.CallbackContext context) => OnInteract?.Invoke();
    private void HandlePickup(InputAction.CallbackContext context) => OnPickup?.Invoke();
    private void HandleToggleInventory(InputAction.CallbackContext context) => OnToggleInventory?.Invoke();
    private void HandleToggleQuestLog(InputAction.CallbackContext context) => OnToggleQuestLog?.Invoke();
    private void HandleTogglePartyHolder(InputAction.CallbackContext context) => OnTogglePartyHolder?.Invoke();
    private void HandleToggleBattleParty(InputAction.CallbackContext context) => OnToggleBattleParty?.Invoke();
    private void HandleToggleMap(InputAction.CallbackContext context) => OnToggleMap?.Invoke();
    private void HandleEscape(InputAction.CallbackContext context) => OnEscape?.Invoke();
    #endregion

    public PlayerControls GetPlayerControls()
    {
        return playerControls;
    }

    public void EnableGameplayInput(bool enable)
    {
        if (playerControls == null) return;
        if (enable)
        {
            playerControls.Gameplay.Enable();
            Debug.Log("[InputManager] 遊戲玩法輸入已啟用。");
        }
        else
        {
            playerControls.Gameplay.Disable();
            Debug.Log("[InputManager] 遊戲玩法輸入已禁用。");
        }
    }
}