using UnityEngine;
using UnityEngine.SceneManagement;

public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("資料庫引用")]
    [SerializeField] private SkillDatabase skillDatabase;

    public SkillDatabase Database => skillDatabase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeManager();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        if (skillDatabase == null)
        {
            skillDatabase = Resources.Load<SkillDatabase>("SkillDatabase");
            
            if (skillDatabase == null)
            {
                Debug.LogError("SkillManager 無法找到 SkillDatabase！請在 Inspector 中指定它，或將其放置在 'Resources' 文件夾下。");
                return;
            }
        }
        skillDatabase.Initialize(); 
        Debug.Log("SkillManager 已成功初始化 SkillDatabase。");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}