using UnityEngine;
using UnityEngine.UI;

public static class UISoundAutoHook
{
    public static void HookAllButtons(GameObject root, string hoverID = "UI_Hover", string clickID = "UI_Click")
    {
        if (root == null) return;
        Button[] allButtons = root.GetComponentsInChildren<Button>(true);
        int count = 0;
        foreach (var btn in allButtons)
        {
            if (btn.GetComponent<SoundOnButton>() == null)
            {
                var soundScript = btn.gameObject.AddComponent<SoundOnButton>();
                soundScript.Setup(hoverID, clickID);
                count++;
            }
        }
        if (count > 0)
        {
            Debug.Log($"[UISoundAutoHook] 已為 {root.name} 下的 {count} 個按鈕自動掛載音效。");
        }
    }

    public static void HookEntireScene(string hoverID = "UI_Hover", string clickID = "UI_Click")
    {
        Canvas[] allCanvases = GameObject.FindObjectsOfType<Canvas>(true);
        foreach (var canvas in allCanvases)
        {
            HookAllButtons(canvas.gameObject, hoverID, clickID);
        }
    }
}