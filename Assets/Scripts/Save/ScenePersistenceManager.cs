using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public class ScenePersistenceManager : MonoBehaviour, IGameSaveable
{
    public static ScenePersistenceManager Instance { get; private set; }
    private HashSet<string> destroyedObjectIDs = new HashSet<string>();

    [Header("動態生成設定")]
    [SerializeField] private GameObject groundItemPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Register(this);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Unregister(this);
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        destroyedObjectIDs.Clear();
    }

    public void RecordObjectDestruction(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            if (!destroyedObjectIDs.Contains(id))
            {
                destroyedObjectIDs.Add(id);
                Debug.Log($"[ScenePersistenceManager] 記錄物件銷毀: {id}");
            }
        }
    }
    
    public void PopulateSaveData(GameSaveData data)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (!data.sceneData.ContainsKey(currentSceneName))
        {
            data.sceneData[currentSceneName] = new SceneSaveData();
        }
        SceneSaveData sceneSaveData = data.sceneData[currentSceneName];
        
        sceneSaveData.objectStatesJsonMap.Clear();
        sceneSaveData.destroyedObjectIDs = new List<string>(destroyedObjectIDs);
        sceneSaveData.runtimeSpawnedObjectData.Clear();
        
        var saveableEntities = FindObjectsOfType<MonoBehaviour>(true).OfType<ISceneSaveable>();
        foreach (var entity in saveableEntities)
        {
            var component = entity as Component;
            if (component.gameObject.scene.name != currentSceneName) continue;
            if (component != null && component.TryGetComponent<UniqueObjectIdentifier>(out var identifier))
            {
                if (identifier.IsRuntimeInstantiated)
                {
                    object state = entity.CaptureState();
                    string jsonState = JsonUtility.ToJson(state);
                    
                    string prefabType = "";
                    if (component is GroundItem) prefabType = "GroundItem";
                    if (!string.IsNullOrEmpty(prefabType))
                    {
                        var runtimeData = new RuntimeSpawnedObjectData
                        {
                            prefabType = prefabType,
                            instanceID = identifier.ID,
                            stateJson = jsonState
                        };
                        sceneSaveData.runtimeSpawnedObjectData.Add(JsonUtility.ToJson(runtimeData));
                    }
                }
                else
                {
                    if (!destroyedObjectIDs.Contains(identifier.ID))
                    {
                        object state = entity.CaptureState();
                        if (state != null) 
                        {
                            string jsonState = JsonUtility.ToJson(state);
                            sceneSaveData.objectStatesJsonMap[identifier.ID] = jsonState;
                        }
                    }
                }
            }
        }
        Debug.Log($"[ScenePersistenceManager] 場景 '{currentSceneName}' 狀態已保存...");
    }

    public void LoadFromSaveData(GameSaveData data)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        if (!data.sceneData.ContainsKey(currentSceneName))
        {
            destroyedObjectIDs.Clear();
            return;
        }
        SceneSaveData sceneSaveData = data.sceneData[currentSceneName];
        
        destroyedObjectIDs = new HashSet<string>(sceneSaveData.destroyedObjectIDs);
        
        var allIdentifiers = FindObjectsOfType<UniqueObjectIdentifier>(true);
        foreach (var identifier in allIdentifiers)
        {
            if (!identifier.IsRuntimeInstantiated && destroyedObjectIDs.Contains(identifier.ID))
            {
                Debug.Log($"[ScenePersistenceManager] 銷毀已記錄死亡的物件: {identifier.name} ({identifier.ID})");
                Destroy(identifier.gameObject);
            }
        }
        
        var existingRuntimeObjects = FindObjectsOfType<UniqueObjectIdentifier>().Where(i => i.IsRuntimeInstantiated).ToList();
        foreach(var obj in existingRuntimeObjects)
        {
            DestroyImmediate(obj.gameObject);
        }
        
        if (sceneSaveData.runtimeSpawnedObjectData != null)
        {
            foreach (var json in sceneSaveData.runtimeSpawnedObjectData)
            {
                var runtimeData = JsonUtility.FromJson<RuntimeSpawnedObjectData>(json);
                if (runtimeData.prefabType == "GroundItem" && groundItemPrefab != null)
                {
                    GameObject newObj = Instantiate(groundItemPrefab);
                    var identifier = newObj.GetComponent<UniqueObjectIdentifier>();
                    if (identifier == null) identifier = newObj.AddComponent<UniqueObjectIdentifier>();
                    
                    identifier.SetID(runtimeData.instanceID);
                    identifier.IsRuntimeInstantiated = true;
                    
                    var saveable = newObj.GetComponent<ISceneSaveable>();
                    if (saveable != null)
                    {
                        saveable.RestoreState(runtimeData.stateJson);
                    }
                }
            }
        }
        
        var entityMap = new Dictionary<string, ISceneSaveable>();
        var allSaveables = FindObjectsOfType<MonoBehaviour>(true)
            .OfType<ISceneSaveable>()
            .Where(e => (e as Component).GetComponent<UniqueObjectIdentifier>() != null);
        foreach (var entity in allSaveables)
        {
            var component = entity as Component;
            if (component.gameObject.scene.name != currentSceneName) continue;
            var idComponent = component.GetComponent<UniqueObjectIdentifier>();
            if (entityMap.ContainsKey(idComponent.ID))
            {
                Debug.LogWarning($"[ScenePersistenceManager] 發現重複 ID 的物件: {idComponent.name} (ID: {idComponent.ID})。這可能會導致狀態恢復錯誤，請檢查場景設置。忽略此重複項。");
            }
            else
            {
                entityMap.Add(idComponent.ID, entity);
            }
        }
        
        foreach (var savedState in sceneSaveData.objectStatesJsonMap)
        {
            string objectID = savedState.Key;
            string stateJson = savedState.Value;
            
            if (entityMap.TryGetValue(objectID, out ISceneSaveable entity))
            {
                entity.RestoreState(stateJson);
            }
        }
        Debug.Log($"[ScenePersistenceManager] 場景 '{currentSceneName}' 狀態已從內存恢復。");
    }
}