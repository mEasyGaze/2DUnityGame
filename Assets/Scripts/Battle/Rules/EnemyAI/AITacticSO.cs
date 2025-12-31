using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAITactic", menuName = "Battle System/AI/AI Tactic")]
public class AITacticSO : ScriptableObject
{
    [Header("評分策略 (參謀列表)")]
    [Tooltip("此戰術文件包含的所有評分參謀。AI 會綜合所有參謀的意見。")]
    public List<SM_BaseSO> scoringModifiers;
}