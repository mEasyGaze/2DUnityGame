using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))] 
public class DialogueUI : MonoBehaviour
{
    [Header("UI 元件")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform optionsContainer;
    [SerializeField] private GameObject optionButtonPrefab;
    [SerializeField] private ScrollRect optionsScrollRect;

    [Header("劇情對話元件")]
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private Button dialoguePanelButton;

    private CanvasGroup dialogueCanvasGroup;
    private List<GameObject> currentOptionButtons = new List<GameObject>();
    private List<Selectable> previouslyDisabledSelectables = new List<Selectable>();

    private RectTransform optionsContainerRect;
    private RectTransform scrollRectViewport;

    void Start()
    {
        UISoundAutoHook.HookEntireScene();
    }
    
    void Awake()
    {
        dialogueCanvasGroup = GetComponent<CanvasGroup>(); // 獲取 CanvasGroup
        if (optionsContainer != null) optionsContainerRect = optionsContainer.GetComponent<RectTransform>();
        if (optionsScrollRect != null) scrollRectViewport = optionsScrollRect.viewport;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.RegisterDialogueUI(this);
            if (dialoguePanelButton != null)
            {
                dialoguePanelButton.onClick.AddListener(() => DialogueManager.Instance.AdvanceDialogueChain());
            }
        }
        else
        {
            Debug.LogError("[DialogueUI] 找不到 DialogueManager 的實例來進行註冊！");
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        HandleAutoScrolling();
    }

    void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.UnregisterDialogueUI(this);
        }
    }

    public void ShowDialogue(string speakerName, string text)
    {
        dialoguePanel.SetActive(true);
        speakerNameText.text = speakerName;
        dialogueText.text = text;
        ClearOptions();
        
        SetChainAdvanceActive(true);
    }

    public void ShowOptions(List<DialogueOption> options, Action<DialogueOption> onOptionSelectedCallback)
    {
        ClearOptions();
        SetChainAdvanceActive(false);
        DisableOtherSelectables();

        for (int i = 0; i < options.Count; i++)
        {
            GameObject buttonGO = Instantiate(optionButtonPrefab, optionsContainer);
            buttonGO.GetComponentInChildren<TextMeshProUGUI>().text = options[i].text;
            
            Button button = buttonGO.GetComponent<Button>();
            DialogueOption currentOption = options[i];
            button.onClick.AddListener(() => {
                onOptionSelectedCallback?.Invoke(currentOption);
            });
            
            currentOptionButtons.Add(buttonGO);
        }
        
        SetupButtonNavigation();

        if (currentOptionButtons.Count > 0)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(currentOptionButtons[0]);
            }
        }
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
        ClearOptions();
        HideContinuePrompt();
    }

    private void ClearOptions()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        foreach (var button in currentOptionButtons)
        {
            Destroy(button);
        }
        currentOptionButtons.Clear();
        EnableOtherSelectables();
    }

    private void HandleAutoScrolling()
    {
        if (!dialoguePanel.activeSelf || currentOptionButtons.Count == 0 || EventSystem.current == null) return;
        
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null || selectedObject.transform.parent != optionsContainer)
        {
            return;
        }
        if (optionsScrollRect == null) return;

        RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
        
        Vector3 selectedBottomInViewport = scrollRectViewport.InverseTransformPoint(selectedRect.TransformPoint(new Vector3(0, selectedRect.rect.yMin, 0)));
        Vector3 selectedTopInViewport = scrollRectViewport.InverseTransformPoint(selectedRect.TransformPoint(new Vector3(0, selectedRect.rect.yMax, 0)));
        
        float viewportHeight = scrollRectViewport.rect.height;

        if (selectedTopInViewport.y > 0)
        {
            optionsContainerRect.anchoredPosition -= new Vector2(0, selectedTopInViewport.y);
        }
        else if (selectedBottomInViewport.y < -viewportHeight)
        {
            optionsContainerRect.anchoredPosition -= new Vector2(0, selectedBottomInViewport.y + viewportHeight);
        }
    }
    
    private void SetupButtonNavigation()
    {
        for (int i = 0; i < currentOptionButtons.Count; i++)
        {
            Button button = currentOptionButtons[i].GetComponent<Button>();
            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = currentOptionButtons[(i - 1 + currentOptionButtons.Count) % currentOptionButtons.Count].GetComponent<Selectable>();
            nav.selectOnDown = currentOptionButtons[(i + 1) % currentOptionButtons.Count].GetComponent<Selectable>();
            
            button.navigation = nav;
        }
    }

    private void DisableOtherSelectables()
    {
        previouslyDisabledSelectables.Clear();
        foreach (var selectable in Selectable.allSelectablesArray)
        {
            bool isOutsideDialoguePanel = selectable.transform.IsChildOf(this.transform) == false;
            
            if (isOutsideDialoguePanel && selectable.interactable)
            {
                selectable.interactable = false;
                previouslyDisabledSelectables.Add(selectable);
            }
        }
    }

    private void EnableOtherSelectables()
    {
        foreach (var selectable in previouslyDisabledSelectables)
        {
            if (selectable != null)
            {
                selectable.interactable = true;
            }
        }
        previouslyDisabledSelectables.Clear();
    }

    public void ShowContinuePrompt()
    {
        if (continuePrompt != null) continuePrompt.SetActive(true);
    }
    
    public void HideContinuePrompt()
    {
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    public void SetChainAdvanceActive(bool isActive)
    {
        if (dialogueCanvasGroup != null)
        {
            dialoguePanelButton.enabled = isActive;
            optionsScrollRect.enabled = !isActive;
        }
    }
}