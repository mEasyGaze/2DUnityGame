using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    [Header("對話設定")]
    [Tooltip("此對話所在的 XML 檔案名稱")]
    [SerializeField] private string dialogueFileName;

    [Tooltip("要觸發的對話 ID")]
    [SerializeField] private string dialogueID;

    public void Interact()
    {
        if (string.IsNullOrEmpty(dialogueFileName) || string.IsNullOrEmpty(dialogueID))
        {
            Debug.LogWarning($"[DialogueTrigger] {gameObject.name} 未在 Inspector 中設定 dialogueFileName 或 dialogueID。");
            return;
        }
        Debug.Log($"[DialogueTrigger] 觸發對話: 檔案 '{dialogueFileName}', ID '{dialogueID}'");
        DialogueManager.Instance.StartDialogue(dialogueFileName, dialogueID);
    }
}