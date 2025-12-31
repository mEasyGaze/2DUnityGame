using UnityEngine;
using System;

[ExecuteInEditMode]
public class UniqueObjectIdentifier : MonoBehaviour, ISceneSaveable
{
    [Header("場景內唯一ID")]
    [Tooltip("此ID在當前場景中必須是唯一的。用於存檔系統識別。")]
    [SerializeField]
    private string id;
    public string ID => id;

    [Header("狀態追蹤")]
    [Tooltip("勾選此項，系統會自動記錄並恢復此物件的 Active (顯示/隱藏) 狀態。")]
    public bool saveActiveState = false;

    [Tooltip("勾選此項，系統會自動記錄並恢復此物件的 Position, Rotation 和 Scale。")]
    public bool saveTransform = false;

    [HideInInspector]
    public bool IsRuntimeInstantiated = false;

    private bool isQuitting = false;

    private void Reset()
    {
        GenerateID();
    }
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
        {
            GenerateID();
        }
    }

    [ContextMenu("Generate New ID")]
    private void GenerateID()
    {
        id = Guid.NewGuid().ToString();
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
        #endif
    }

    public void SetID(string newId)
    {
        if (!string.IsNullOrEmpty(newId))
        {
            id = newId;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (isQuitting) return;
        if (ScenePersistenceManager.Instance != null)
        {
            if (!IsRuntimeInstantiated)
            {
                ScenePersistenceManager.Instance.RecordObjectDestruction(id);
            }
        }
    }

    [System.Serializable]
    public class IdentifierState
    {
        public bool isActive;
        public bool hasTransform;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public object CaptureState()
    {
        if (!saveActiveState && !saveTransform) return null;
        var state = new IdentifierState();
        
        if (saveActiveState)
        {
            state.isActive = gameObject.activeSelf;
        }
        else
        {
            state.isActive = true; 
        }

        if (saveTransform)
        {
            state.hasTransform = true;
            state.position = transform.position;
            state.rotation = transform.rotation;
            state.scale = transform.localScale;
        }
        return state;
    }

    public void RestoreState(object stateData)
    {
        if (!saveActiveState && !saveTransform) return;
        if (stateData is string stateJson)
        {
            var state = JsonUtility.FromJson<IdentifierState>(stateJson);
            
            if (saveActiveState)
            {
                gameObject.SetActive(state.isActive);
            }

            if (saveTransform && state.hasTransform)
            {
                transform.position = state.position;
                transform.rotation = state.rotation;
                transform.localScale = state.scale;
                var rb2d = GetComponent<Rigidbody2D>();
                if (rb2d != null)
                {
                    rb2d.position = state.position;
                    // rb2d.rotation = state.rotation.eulerAngles.z;
                }
            }
        }
    }
}