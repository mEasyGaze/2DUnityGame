using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System;

public class KeybindingEntryUI : MonoBehaviour
{
    [Header("UI 連結")]
    [SerializeField] private TextMeshProUGUI actionNameText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private TextMeshProUGUI keyText;

    private InputActionReference actionReference;
    
    public int BindingIndex { get; private set; }

    public void Setup(InputActionReference actionRef, int index, Action<KeybindingEntryUI> onRebindClicked)
    {
        this.actionReference = actionRef;
        this.BindingIndex = index;
        
        if (actionNameText != null)
        {
            actionNameText.text = actionRef.action.name;
        }
        rebindButton.onClick.RemoveAllListeners();
        rebindButton.onClick.AddListener(() => onRebindClicked(this));
        
        UpdateKeyTextToDefault();
    }
    
    public string GetActionName()
    {
        return actionReference != null ? actionReference.action.name : "未知動作";
    }

    public string GetActionId()
    {
        if (actionReference == null || actionReference.action == null) return null;
        return actionReference.action.bindings[BindingIndex].id.ToString();
    }

    public void UpdateKeyTextToDefault()
    {
        if (keyText != null && actionReference != null && actionReference.action != null)
        {
            keyText.text = InputControlPath.ToHumanReadableString(
                actionReference.action.bindings[BindingIndex].path,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }
    }

    public void UpdateKeyTextWithOverride(string overridePath)
    {
        if (keyText != null)
        {
             keyText.text = InputControlPath.ToHumanReadableString(
                overridePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }
    }

    public void StartRebinding(Action<InputActionRebindingExtensions.RebindingOperation> onStarted, Action<InputActionRebindingExtensions.RebindingOperation> onComplete)
    {
        if (actionReference == null || actionReference.action == null) return;
        keyText.text = "...";
        rebindButton.interactable = false;
        
        var action = actionReference.action;
        action.Disable();

        var rebindOp = action.PerformInteractiveRebinding(BindingIndex)
            .WithControlsExcluding("Mouse")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => {
                FinishRebinding(operation, onComplete);
            })
            .OnCancel(operation => {
                FinishRebinding(operation, onComplete);
            });
        onStarted?.Invoke(rebindOp);
        rebindOp.Start();
    }
    
    private void FinishRebinding(InputActionRebindingExtensions.RebindingOperation operation, Action<InputActionRebindingExtensions.RebindingOperation> onComplete)
    {
        rebindButton.interactable = true;
        actionReference.action.Enable();
        if (keyText != null)
        {
            keyText.text = InputControlPath.ToHumanReadableString(
                actionReference.action.bindings[BindingIndex].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }
        onComplete?.Invoke(operation);
    }
}