using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public enum QuestObjectiveType { Collect, Kill, Talk, GoTo }

[System.Serializable]
public class QuestObjective
{
    [XmlAttribute("type")]
    public QuestObjectiveType type;

    [XmlAttribute("targetID")]
    public string targetID;

    [XmlAttribute("amount")]
    public int requiredAmount;

    [XmlAttribute("description")]
    public string description;

    [XmlAttribute("prereqIndex")]
    [System.ComponentModel.DefaultValue(-1)]
    public int prerequisiteIndex = -1;

    [XmlIgnore]
    public int currentAmount = 0;

    [XmlIgnore]
    public int startingAmount = 0;

    [XmlIgnore]
    public bool IsComplete => currentAmount >= requiredAmount;
}

[System.Serializable]
public class ItemReward
{
    [XmlAttribute("id")]
    public string itemID;

    [XmlAttribute("amount")]
    public int amount;
}

[System.Serializable]
public class Quest
{
    public enum QuestStatus { NotStarted, InProgress, Completed, Failed }
    
    [XmlAttribute("id")]
    public string questID;
    
    [XmlElement("Name")]
    public string questName;

    [XmlElement("Description")]
    public string description;
    
    [XmlElement("GiverNPC")]
    public string giverNPCID;

    [XmlElement("HandInNPC")]
    public string handInNPCID;
    
    [XmlArray("Prerequisites")]
    [XmlArrayItem("QuestID")]
    public List<string> prerequisiteQuestIDs = new List<string>();

    [XmlElement("IsRepeatable")]
    public bool isRepeatable = false;
    
    [XmlArray("Objectives")]
    [XmlArrayItem("Objective")]
    public List<QuestObjective> objectives = new List<QuestObjective>();
    
    [XmlArray("Rewards")]
    [XmlArrayItem("Item")]
    public List<ItemReward> itemRewards = new List<ItemReward>();
    
    [XmlElement("Money")]
    public int moneyReward;
    
    public bool AreAllObjectivesComplete()
    {
        foreach (var obj in objectives)
        {
            if (obj.currentAmount < obj.requiredAmount)
            {
                return false;
            }
        }
        return true;
    }
    
    public void ResetProgress()
    {
        foreach (var obj in objectives)
        {
            obj.currentAmount = 0;
        }
    }
}