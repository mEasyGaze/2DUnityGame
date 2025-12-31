using System.Collections.Generic;

[System.Serializable]
public class KillCounterEntry
{
    public string enemyID;
    public int count;
}

[System.Serializable]
public class PlayerStatsData
{
    public List<KillCounterEntry> killCounters = new List<KillCounterEntry>();
}