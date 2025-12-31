using UnityEngine;
using System.Collections.Generic;
using TMPro;

public enum VFXType
{
    GenericAttack,
    GenericHeal,
    IncreaseAttackBuff,
    IncreaseDefenseBuff,
    AddShieldBuff,
    
    Text_Damage,
    Text_Heal,
    Text_Shield,
    Text_Info,
    Text_Miss
}

public class BattleVFXManager : MonoBehaviour
{
    public static BattleVFXManager Instance { get; private set; }

    [System.Serializable]
    public class VFXPrefab
    {
        public VFXType type;
        public GameObject prefab;
    }

    [Header("特效設定")]
    [SerializeField] private List<VFXPrefab> vfxList;
    private Dictionary<VFXType, GameObject> vfxDictionary;

    [Header("飄字設定")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int textPoolSize = 20;
    
    private Queue<FloatingText> textPool = new Queue<FloatingText>();

    void Awake()
    {
        Instance = this;
        vfxDictionary = new Dictionary<VFXType, GameObject>();
        foreach (var vfx in vfxList)
        {
            vfxDictionary[vfx.type] = vfx.prefab;
        }
        
        InitializeTextPool();
    }

    private void InitializeTextPool()
    {
        if (floatingTextPrefab == null) return;
        
        GameObject poolRoot = new GameObject("VFX_TextPool");
        poolRoot.transform.SetParent(this.transform);

        for (int i = 0; i < textPoolSize; i++)
        {
            CreateTextObject(poolRoot.transform);
        }
    }

    private FloatingText CreateTextObject(Transform parent)
    {
        GameObject obj = Instantiate(floatingTextPrefab, parent);
        FloatingText textComp = obj.GetComponent<FloatingText>();
        obj.SetActive(false);
        textPool.Enqueue(textComp);
        return textComp;
    }

    public void PlayVFX(VFXType type, Vector3 position)
    {
        if (vfxDictionary.TryGetValue(type, out GameObject prefab))
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    public void ShowText(Vector3 position, string content, VFXType type)
    {
        if (textPool.Count == 0) CreateTextObject(transform);
        FloatingText txt = textPool.Dequeue();
        Color color = Color.white;
        float fontSize = 4f;

        switch (type)
        {
            case VFXType.Text_Damage: color = new Color(1f, 0.3f, 0.3f); fontSize = 5f; break; // 紅
            case VFXType.Text_Heal:   color = new Color(0.3f, 1f, 0.3f); fontSize = 5f; break; // 綠
            case VFXType.Text_Shield: color = new Color(0.3f, 0.8f, 1f); break; // 藍
            case VFXType.Text_Miss:   color = Color.gray; break;
            default:                  color = Color.white; break;
        }
        txt.transform.position = position;
        txt.Setup(content, color, fontSize);
        txt.gameObject.SetActive(true);
        textPool.Enqueue(txt); 
    }
}