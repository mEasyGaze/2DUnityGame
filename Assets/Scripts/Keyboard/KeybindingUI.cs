using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class KeybindingUI : MonoBehaviour
{
    [Header("要綁定的動作列表")]
    [Tooltip("將所有想讓玩家自訂的動作 (InputActionReference) 拖到這裡。")]
    [SerializeField] private List<InputActionReference> actionsToBind;

    [Header("UI 預製件與容器")]
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Transform container;
    
    [Header("控制按鈕")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;

    [Header("重新綁定提示")]
    [SerializeField] private GameObject rebindingOverlay;
    [SerializeField] private TextMeshProUGUI rebindingText;
    
    private List<KeybindingEntryUI> spawnedEntries = new List<KeybindingEntryUI>();
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    
    private Dictionary<string, string> bindingOverrides = new Dictionary<string, string>();
    
    private bool isPopulated = false;

    void Start()
    {
        if (applyButton != null) applyButton.onClick.AddListener(ApplyAndSaveChanges);
        if (resetButton != null) resetButton.onClick.AddListener(ResetBindingsToDefault);
        if (backButton != null) backButton.onClick.AddListener(ClosePanelWithoutSaving);
        if (rebindingOverlay != null) rebindingOverlay.SetActive(false);
    }
    
    private void OnEnable()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsInitialized)
        {
            if (!isPopulated)
            {
                PopulateUI();
            }
            
            LoadSavedOverridesToDictionary();
            RefreshAllEntryTexts();
        }
        else
        {
            Debug.LogWarning("KeybindingUI enabled, but InputManager is not ready yet. UI will not populate.");
        }
    }
    
    private void OnDisable()
    {
        rebindingOperation?.Cancel();
        rebindingOperation = null;
    }
    
    private void PopulateUI()
    {
        if (isPopulated || container == null || entryPrefab == null) return;
        foreach (Transform child in container) { Destroy(child.gameObject); }
        spawnedEntries.Clear();

        if (actionsToBind == null || actionsToBind.Count == 0) return;
        foreach (var actionRef in actionsToBind)
        {
            if (actionRef == null || actionRef.action == null) continue;
            if (actionRef.action.bindings.Count > 0 && actionRef.action.bindings[0].isComposite) continue;
            
            int bindingIndex = 0;

            GameObject entryGO = Instantiate(entryPrefab, container);
            KeybindingEntryUI entryUI = entryGO.GetComponent<KeybindingEntryUI>();
            
            if (entryUI != null)
            {
                entryUI.Setup(actionRef, bindingIndex, HandleRebindRequest);
                spawnedEntries.Add(entryUI);
            }
        }
        isPopulated = true;
        Debug.Log("KeybindingUI Populated with " + spawnedEntries.Count + " entries.");
    }
    
    private void HandleRebindRequest(KeybindingEntryUI entryToRebind)
    {
        rebindingOperation?.Cancel();
        
        if (rebindingOverlay != null)
        {
            rebindingOverlay.SetActive(true);
            if (rebindingText != null)
            {
                rebindingText.text = $"正在綁定: {entryToRebind.GetActionName()}...\n請按下任意鍵 (ESC 取消)";
            }
        }
        
        entryToRebind.StartRebinding(
            (op) => {
                rebindingOperation = op;
            },
            (operation) => {
                if (!operation.canceled)
                {
                    string newPath = operation.action.bindings[entryToRebind.BindingIndex].overridePath;
                    bindingOverrides[entryToRebind.GetActionId()] = newPath;
                    entryToRebind.UpdateKeyTextWithOverride(newPath);
                }
                
                if (rebindingOverlay != null) rebindingOverlay.SetActive(false);
                
                rebindingOperation?.Dispose();
                rebindingOperation = null;
            }
        );
    }
    
    private void RefreshAllEntryTexts()
    {
        if (!isPopulated) return;
        
        foreach(var entry in spawnedEntries)
        {
            if (entry == null) continue;
            
            string actionId = entry.GetActionId();
            if (bindingOverrides.TryGetValue(actionId, out string overridePath))
            {
                entry.UpdateKeyTextWithOverride(overridePath);
            }
            else
            {
                entry.UpdateKeyTextToDefault();
            }
        }
    }
    
    public void ApplyAndSaveChanges()
    {
        if (!isPopulated) return;
        
        string overridesJson = DictionaryToJson(bindingOverrides);
        
        if (InputManager.Instance != null && InputManager.Instance.GetPlayerControls() != null)
        {
            var playerControls = InputManager.Instance.GetPlayerControls();
            playerControls.LoadBindingOverridesFromJson(overridesJson, true);
        }
        
        PlayerPrefs.SetString("KeyRebinds", overridesJson);
        PlayerPrefs.Save();
        
        Debug.Log("按鍵綁定已套用並儲存。");
        ClosePanelWithoutSaving();
    }
    
    public void ResetBindingsToDefault()
    {
        if (!isPopulated) return;
        bindingOverrides.Clear();
        RefreshAllEntryTexts();
        Debug.Log("所有按鍵已在 UI 上恢復預設。按下 '套用' 以確認。");
    }

    public void ClosePanelWithoutSaving()
    {
        if (isPopulated)
        {
            LoadSavedBindingsAndApplyToRuntime();
        }
        gameObject.SetActive(false);
    }

    private void LoadSavedBindingsAndApplyToRuntime()
    {
        if (InputManager.Instance == null) return;
        var playerControls = InputManager.Instance.GetPlayerControls();
        if (playerControls == null) return;
        if (PlayerPrefs.HasKey("KeyRebinds"))
        {
            string rebinds = PlayerPrefs.GetString("KeyRebinds");
            playerControls.LoadBindingOverridesFromJson(rebinds);
        }
        else
        {
            playerControls.RemoveAllBindingOverrides();
        }
    }

    private void LoadSavedOverridesToDictionary()
    {
        bindingOverrides.Clear();
        if (PlayerPrefs.HasKey("KeyRebinds"))
        {
            string rebindsJson = PlayerPrefs.GetString("KeyRebinds");
            if (string.IsNullOrEmpty(rebindsJson)) return;

            var rebindsWrapper = JsonUtility.FromJson<RebindsWrapper>(rebindsJson);
            if (rebindsWrapper != null && rebindsWrapper.bindings != null)
            {
                foreach(var binding in rebindsWrapper.bindings)
                {
                    bindingOverrides[binding.id] = binding.path;
                }
            }
        }
    }
    
    private string DictionaryToJson(Dictionary<string, string> dict)
    {
        var bindingsList = dict.Select(kvp => 
            new RebindData { id = kvp.Key, path = kvp.Value }
        ).ToList();
        var wrapper = new RebindsWrapper { bindings = bindingsList };
        return JsonUtility.ToJson(wrapper);
    }
    
    [System.Serializable]
    private class RebindsWrapper { public List<RebindData> bindings; }

    [System.Serializable]
    private class RebindData { public string id; public string path; }
}