using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StoryPhase
{
    [Tooltip("此階段的描述性名稱（僅供編輯器內識別）")]
    public string phaseName = "New Phase";

    [Tooltip("此階段內將會【同時】執行的所有動作")]
    public List<StoryAction> actions = new List<StoryAction>();
}

[CreateAssetMenu(fileName = "NewStoryScene", menuName = "Story/Story Scene Data")]
public class StorySceneData : ScriptableObject
{
    [Header("劇情場景設定")]
    [Tooltip("此劇情場景的唯一ID，用於被其他系統觸發")]
    public string sceneID;

    [Header("音樂控制")]
    [Tooltip("勾選此項，劇情結束後將自動切換回該場景原本的背景音樂 (AudioBackground)。\n如果不勾選，則繼續播放劇情最後一段音樂。")]
    public bool restoreSceneBgmOnEnd = true;

    [Header("劇情階段序列")]
    [Tooltip("按照執行順序排列的劇情階段列表。每個階段內的動作會同時執行。")]
    public List<StoryPhase> phases = new List<StoryPhase>();
}