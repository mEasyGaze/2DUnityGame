using UnityEngine;

public enum StoryActionType
{
    // --- 角色與物件控制 ---
    MoveCharacter,
    SetCharacterActive,
    SpawnObject,
    DestroyObject,
    ChangeSprite,
    RotateObject,
    ParabolicMove,
    PlayAnimation,

    // --- 流程與交互控制 ---
    StartDialogue,
    Wait,
    DisableInput,
    EnableInput,
    RunSubStory,

    // --- 鏡頭控制 ---
    MoveCameraToTarget,
    MoveCameraToPosition,
    FocusOnCharacter,
    ReleaseCameraFocus,

    // --- 遊戲事件與音效 ---
    TriggerGameEvent,
    PlayMusic,
    StopMusic,
    PauseMusic,
    ResumeMusic,
    PlaySoundEffect
}

[System.Serializable]
public class StoryAction
{
    [Tooltip("此動作的類型")]
    public StoryActionType actionType;

    [Header("通用參數")]
    public string targetObjectName;
    public Vector2 targetPosition;
    public float duration;
    public bool boolValue;

    [Header("對話專用參數")]
    public string dialogueFilePath;
    public string dialogueID;

    [Header("生成物件專用參數")]
    public GameObject objectToSpawn;

    [Header("事件/音效/動畫參數")]
    [Tooltip("GameEvent ID / Audio ID / Animation Trigger Name")]
    public string stringID; 

    [Header("視覺效果參數")]
    public Sprite newSprite;
    public Vector3 rotationAngles;
    public float parabolaHeight;
    
    [Header("子劇情專用參數")]
    public StorySceneData subStoryToRun;
}