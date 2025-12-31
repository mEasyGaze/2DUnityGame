using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SceneActor
{
    [Tooltip("在 StoryAction 中使用的邏輯名稱，例如 '主角', '市長NPC', '城門'")]
    public string logicalName;
    [Tooltip("將場景中對應的 GameObject 拖拽到這裡")]
    public GameObject sceneObject;
}

public class StorySceneRunner : MonoBehaviour
{
    [Header("劇情數據")]
    [Tooltip("將要在此場景中播放的 StorySceneData 腳本資源拖拽於此")]
    [SerializeField] private StorySceneData sceneData;

    [Header("場景角色名單 (Cast List)")]
    [Tooltip("將此劇情中需要用到的所有場景物件在此處進行綁定")]
    [SerializeField] private List<SceneActor> sceneActors = new List<SceneActor>();

    private Dictionary<string, GameObject> actorDictionary;

    void Awake()
    {
        actorDictionary = new Dictionary<string, GameObject>();
        foreach (var actor in sceneActors)
        {
            if (actor.sceneObject == null)
            {
                Debug.LogWarning($"在 '{gameObject.name}' 的角色名單中, 邏輯名稱 '{actor.logicalName}' 沒有綁定任何場景物件！", this);
                continue;
            }
            if (!actorDictionary.ContainsKey(actor.logicalName))
            {
                actorDictionary.Add(actor.logicalName, actor.sceneObject);
            }
            else
            {
                Debug.LogWarning($"在 '{gameObject.name}' 的角色名單中發現重複的邏輯名稱: '{actor.logicalName}'", this);
            }
        }
    }

    public GameObject GetActor(string logicalName)
    {
        if (actorDictionary.TryGetValue(logicalName, out GameObject actorObject))
        {
            return actorObject;
        }

        Debug.LogError($"在 '{gameObject.name}' 的角色名單中找不到邏輯名稱為 '{logicalName}' 的物件！請檢查綁定。", this);
        return null;
    }
    
    public void StartScene()
    {
        if (sceneData == null)
        {
            Debug.LogError($"'{gameObject.name}' 沒有掛載任何 StorySceneData，無法啟動劇情。", this);
            return;
        }
        StoryManager.Instance.StartStoryScene(sceneData, this);
    }
}