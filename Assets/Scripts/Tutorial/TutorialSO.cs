using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Tutorial", menuName = "Tutorial/Tutorial Data")]
public class TutorialSO : ScriptableObject
{
    [Header("教學識別")]
    [Tooltip("此教學的唯一ID，用於觸發和完成狀態追蹤。")]
    public string tutorialID;

    [Tooltip("此教學在『教學回顧』列表中顯示的標題。")]
    public string tutorialTitle;

    [Header("教學步驟序列")]
    [Tooltip("整個教學將按照這個列表的順序進行。")]
    public List<TutorialStep> steps = new List<TutorialStep>();
}