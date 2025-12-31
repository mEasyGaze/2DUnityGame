using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

#region Enums
public enum DialogueActionType { Unknown, GoToSegment, EndDialogue, StartDialogue, AcceptQuest, CompleteQuest, AddItem, RemoveItem, AddMoney, AdvanceQuestObjective, AddPartyMember, RemovePartyMember, OpenShop, TriggerGameEvent, StartStoryScene, ContinueDialogue, CloseDialogue, StartBattle }
public enum DialogueConditionType { Unknown, QuestStatus, HasItem, QuestAvailable, QuestCheck, HasPartyMember, IsTrader, GameEventTriggered }
public enum DialogueCheckType { Unknown, QuestCheck, HasItem, QuestStatus, GameEventTriggered }
public enum DynamicOptionSource { Unknown, QuestManager }
public enum DynamicOptionType { Unknown, AvailableQuests, InProgressOrCompletableQuests }
#endregion

#region 連續對話
[System.Serializable]
public class DialogueLine
{
    [XmlElement("Speaker")]
    public string speaker;
    [XmlElement("Text")]
    public string text;
}
#endregion

#region 對話片段
[System.Serializable]
public class DialogueSegment
{
    [XmlAttribute("id")]
    public string id;

    [XmlElement("Speaker")]
    public string speakerName;
    [XmlElement("Text")]
    public string dialogueText;
    [XmlArray("DialogueChain")]
    [XmlArrayItem("Line")]
    public List<DialogueLine> dialogueChain = new List<DialogueLine>();
    [XmlArray("Actions")]
    [XmlArrayItem("Action")]
    public List<DialogueAction> actions = new List<DialogueAction>();
    [XmlElement("IsEnd")]
    public bool isEnd = false;
    [XmlElement("IsContinue")]
    public bool isContinue = false;
    [XmlElement("IsClose")]
    public bool isClose = false;

    [XmlArray("Options")]
    [XmlArrayItem("Option")]
    public List<DialogueOption> options = new List<DialogueOption>();
    [XmlElement("DynamicOptions")]
    public List<DynamicOptions> dynamicOptions = new List<DynamicOptions>();
    [XmlElement("Branch")]
    public DialogueBranch branch;
}
#endregion

#region 玩家選項
[System.Serializable]
public class DialogueOption
{
    [XmlElement("Text")]
    public string text;
    [XmlElement("Condition")]
    public List<DialogueCondition> displayConditions = new List<DialogueCondition>();
    [XmlElement("Action")]
    public List<DialogueAction> actions = new List<DialogueAction>();
    [XmlElement("Branch")]
    public DialogueBranch branch;
}
#endregion

#region 動態選項範本
[System.Serializable]
public class DialogueOptionTemplate
{
    [XmlAttribute("text")]
    public string text;
    [XmlElement("Action")]
    public List<DialogueAction> actions = new List<DialogueAction>();
    [XmlElement("Branch")]
    public DialogueBranch branch;
}
#endregion

#region 動態生成請求
[System.Serializable]
public class DynamicOptions
{
    [XmlAttribute("source")]
    public string source;
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("npcID")]
    public string npcID;
    [XmlAttribute("exclude")]
    public string excludeIDs;
    [XmlElement("OptionTemplate")]
    public DialogueOptionTemplate optionTemplate;

    [XmlIgnore]
    public DynamicOptionSource SourceType
    {
        get
        {
            if (System.Enum.TryParse(source, true, out DynamicOptionSource result))
            {
                return result;
            }
            if (!string.IsNullOrEmpty(source))
            {
                Debug.LogWarning($"[DialogueData] 未知的 DynamicOptions source: '{source}'");
            }
            return DynamicOptionSource.Unknown;
        }
    }
    
    [XmlIgnore]
    public DynamicOptionType RequestType
    {
        get
        {
            if (System.Enum.TryParse(type, true, out DynamicOptionType result))
            {
                return result;
            }
            if (!string.IsNullOrEmpty(type))
            {
                Debug.LogWarning($"[DialogueData] 未知的 DynamicOptions type: '{type}'");
            }
            return DynamicOptionType.Unknown;
        }
    }
}
#endregion

#region 條件/動作基底類別
[System.Serializable]
public class BaseDialogueAction
{
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("value")]
    public string value;
}
#endregion

#region 具體條件與動作
[System.Serializable]
public class DialogueCondition : BaseDialogueAction
{
    [XmlIgnore]
    public DialogueConditionType ConditionType
    {
        get
        {
            if (System.Enum.TryParse(type, true, out DialogueConditionType result))
            {
                return result;
            }
            if (!string.IsNullOrEmpty(type))
            {
                Debug.LogWarning($"[DialogueData] 未知的 DialogueCondition type: '{type}'");
            }
            return DialogueConditionType.Unknown;
        }
    }    
}

[System.Serializable]
public class DialogueAction : BaseDialogueAction
{
    [XmlIgnore]
    public DialogueActionType ActionType
    {
        get
        {
            if (System.Enum.TryParse(type, true, out DialogueActionType result))
            {
                return result;
            }
            if (!string.IsNullOrEmpty(type)) 
            {
                Debug.LogWarning($"[DialogueData] 未知的 DialogueAction type: '{type}'");
            }
            return DialogueActionType.Unknown;
        }
    }
}
#endregion

#region 完整對話
[System.Serializable]
public class Dialogue
{
    [XmlAttribute("id")]
    public string dialogueID;
    [XmlElement("Segment")]
    public List<DialogueSegment> segments = new List<DialogueSegment>();
}
#endregion

#region 對話資料庫
[XmlRoot("DialogueDatabase")]
public class DialogueDatabase
{
    [XmlElement("Dialogue")]
    public List<Dialogue> dialogues = new List<Dialogue>();
}
#endregion

#region 分支邏輯
[System.Serializable]
public class DialogueCheck
{
    [XmlAttribute("type")]
    public string type;
    [XmlAttribute("value")]
    public string value;

    [XmlIgnore]
    public DialogueCheckType CheckType
    {
        get
        {
            if (System.Enum.TryParse(type, true, out DialogueCheckType result))
            {
                return result;
            }
            if (!string.IsNullOrEmpty(type))
            {
                Debug.LogWarning($"[DialogueData] 未知的 DialogueCheck type: '{type}'");
            }
            return DialogueCheckType.Unknown;
        }
    }
}

[System.Serializable]
public class DialogueBranch
{
    [XmlElement("Check")]
    public DialogueCheck check;
    [XmlArray("OnTrue")]
    [XmlArrayItem("Action")]
    public List<DialogueAction> trueActions = new List<DialogueAction>();
    [XmlArray("OnFalse")]
    [XmlArrayItem("Action")]
    public List<DialogueAction> falseActions = new List<DialogueAction>();
}
#endregion