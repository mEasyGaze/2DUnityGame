using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [Header("內容 (Content)")]
    public string title;
    [TextArea(4, 8)]
    public string description;
    public Sprite image;

    [Header("佈局 (Layout)")]
    [Tooltip("用於顯示此步驟內容的 UI 佈局 Prefab。")]
    public GameObject layoutPrefab;
}