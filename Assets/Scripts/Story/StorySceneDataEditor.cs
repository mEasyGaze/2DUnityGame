using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(StorySceneData))]
public class StorySceneDataEditor : Editor
{
    private Dictionary<int, bool> phaseFoldouts = new Dictionary<int, bool>();
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private bool stylesInitialized = false;

    private void OnEnable(){}

    private void InitializeStyles()
    {
        if (stylesInitialized) return;
        headerStyle = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };
        boxStyle = new GUIStyle("box") 
        { 
            padding = new RectOffset(10, 10, 10, 10) 
        };
        stylesInitialized = true;
    }

    public override void OnInspectorGUI()
    {
        InitializeStyles();

        serializedObject.Update();

        SerializedProperty sceneIDProp = serializedObject.FindProperty("sceneID");
        SerializedProperty restoreBgmProp = serializedObject.FindProperty("restoreSceneBgmOnEnd");
        SerializedProperty phasesProp = serializedObject.FindProperty("phases");

        EditorGUILayout.LabelField("劇情場景設定", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(sceneIDProp);
        EditorGUILayout.PropertyField(restoreBgmProp, new GUIContent("結束後恢復場景音樂"));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("劇情階段序列", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("將劇情分解為多個階段。每個階段內的動作會同時執行，階段之間按順序執行。", MessageType.Info);

        for (int i = 0; i < phasesProp.arraySize; i++)
        {
            if (DrawPhase(phasesProp, i))
            {
                break; 
            }
        }

        if (GUILayout.Button("添加新階段", GUILayout.Height(30)))
        {
            phasesProp.InsertArrayElementAtIndex(phasesProp.arraySize);
            var newPhase = phasesProp.GetArrayElementAtIndex(phasesProp.arraySize - 1);
            newPhase.FindPropertyRelative("phaseName").stringValue = $"階段 {phasesProp.arraySize}";
        }
        serializedObject.ApplyModifiedProperties();
    }

    private bool DrawPhase(SerializedProperty phasesProp, int phaseIndex)
    {
        SerializedProperty phaseProp = phasesProp.GetArrayElementAtIndex(phaseIndex);
        SerializedProperty phaseNameProp = phaseProp.FindPropertyRelative("phaseName");
        SerializedProperty actionsProp = phaseProp.FindPropertyRelative("actions");
        
        if (!phaseFoldouts.ContainsKey(phaseIndex))
        {
            phaseFoldouts[phaseIndex] = true;
        }
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.BeginHorizontal();
        phaseFoldouts[phaseIndex] = EditorGUILayout.Foldout(phaseFoldouts[phaseIndex], $"階段 {phaseIndex + 1}: {phaseNameProp.stringValue}", true, headerStyle);
        
        GUILayout.FlexibleSpace();
        if (DrawPhaseControls(phasesProp, phaseIndex))
        {
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        EditorGUILayout.EndHorizontal();
        if (phaseFoldouts[phaseIndex])
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(phaseNameProp, new GUIContent("階段名稱"));
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("同時執行的動作:", EditorStyles.label);
            for (int j = 0; j < actionsProp.arraySize; j++)
            {
                if (DrawAction(actionsProp, j))
                {
                    break;
                }
            }

            if (GUILayout.Button("添加新動作"))
            {
                actionsProp.InsertArrayElementAtIndex(actionsProp.arraySize);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
        return false;
    }

    private bool DrawAction(SerializedProperty actionsProp, int actionIndex)
    {
        SerializedProperty actionProp = actionsProp.GetArrayElementAtIndex(actionIndex);
        SerializedProperty actionTypeProp = actionProp.FindPropertyRelative("actionType");
        
        EditorGUILayout.BeginVertical("helpbox");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(actionTypeProp, GUIContent.none, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            actionsProp.DeleteArrayElementAtIndex(actionIndex);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return true;
        }
        EditorGUILayout.EndHorizontal();
        
        StoryActionType selectedType = (StoryActionType)actionTypeProp.enumValueIndex;
        DrawActionParameters(actionProp, selectedType);
        
        EditorGUILayout.EndVertical();
        return false;
    }

    private void DrawActionParameters(SerializedProperty actionProp, StoryActionType type)
    {
        switch (type)
        {
            case StoryActionType.MoveCharacter:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("角色名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetPosition"), new GUIContent("目標位置"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("移動時間 (秒)"));
                break;
            
            case StoryActionType.SetCharacterActive:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("boolValue"), new GUIContent("設為激活"));
                break;
                
            case StoryActionType.SpawnObject:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("objectToSpawn"), new GUIContent("生成 Prefab"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetPosition"), new GUIContent("生成位置"));
                break;

            case StoryActionType.DestroyObject:
                 EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                break;

            case StoryActionType.ChangeSprite:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("newSprite"), new GUIContent("新圖片 Sprite"));
                break;

            case StoryActionType.ParabolicMove:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetPosition"), new GUIContent("落點位置"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("parabolaHeight"), new GUIContent("拋物線高度"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("飛行時間 (秒)"));
                break;

            case StoryActionType.RotateObject:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("rotationAngles"), new GUIContent("旋轉速度 (度/秒)"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("旋轉時間 (秒)"));
                break;

            case StoryActionType.PlayAnimation:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("stringID"), new GUIContent("Trigger Name"));
                break;

            case StoryActionType.PlayMusic:
            case StoryActionType.PlaySoundEffect:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("stringID"), new GUIContent("Audio ID"));
                break;

            case StoryActionType.TriggerGameEvent:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("stringID"), new GUIContent("遊戲事件 ID"));
                break;

            case StoryActionType.StartDialogue:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("dialogueFilePath"), new GUIContent("對話檔案路徑"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("dialogueID"), new GUIContent("對話 ID"));
                break;

            case StoryActionType.Wait:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("等待時間 (秒)"));
                break;

            case StoryActionType.RunSubStory:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("subStoryToRun"), new GUIContent("子劇情檔案"));
                break;
            
            case StoryActionType.MoveCameraToTarget:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("目標物件名"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("移動時間 (秒)"));
                break;

            case StoryActionType.MoveCameraToPosition:
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetPosition"), new GUIContent("目標位置"));
                EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("duration"), new GUIContent("移動時間 (秒)"));
                break;
                
            case StoryActionType.FocusOnCharacter:
                 EditorGUILayout.PropertyField(actionProp.FindPropertyRelative("targetObjectName"), new GUIContent("跟隨目標名"));
                break;

            case StoryActionType.DisableInput:
            case StoryActionType.EnableInput:
            case StoryActionType.ReleaseCameraFocus:
            case StoryActionType.PauseMusic:
            case StoryActionType.ResumeMusic:
            case StoryActionType.StopMusic:
                EditorGUILayout.HelpBox("此動作無需額外參數。", MessageType.None);
                break;
        }
    }

    private bool DrawPhaseControls(SerializedProperty phasesProp, int phaseIndex)
    {
        GUI.enabled = phaseIndex > 0;
        if (GUILayout.Button("↑", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            phasesProp.MoveArrayElement(phaseIndex, phaseIndex - 1);
        }

        GUI.enabled = phaseIndex < phasesProp.arraySize - 1;
        if (GUILayout.Button("↓", EditorStyles.miniButton, GUILayout.Width(20)))
        {
            phasesProp.MoveArrayElement(phaseIndex, phaseIndex + 1);
        }

        GUI.enabled = true;
        if (GUILayout.Button("刪除", EditorStyles.miniButton, GUILayout.Width(40)))
        {
            if (EditorUtility.DisplayDialog("確認刪除", $"確定要刪除階段 {phaseIndex + 1} 嗎？", "確定", "取消"))
            {
                phasesProp.DeleteArrayElementAtIndex(phaseIndex);
                return true;
            }
        }
        return false;
    }
}