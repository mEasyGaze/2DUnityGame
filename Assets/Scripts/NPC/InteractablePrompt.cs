using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class InteractionPrompt : MonoBehaviour
{
    [Header("提示設定")]
    [Tooltip("要顯示的提示UI物件 (例如 'E' 鍵圖標的 Prefab)。")]
    [SerializeField] private GameObject promptVisual;

    [Tooltip("提示UI相對於此物件中心點的偏移量。")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0);

    private GameObject promptInstance;
    private Transform playerTransform;

    void Awake()
    {
        if (promptVisual == null)
        {
            Debug.LogError($"在 '{gameObject.name}' 上的 InteractionPrompt 未指定 promptVisual！");
            this.enabled = false;
            return;
        }
        promptInstance = Instantiate(promptVisual, transform.position + offset, Quaternion.identity, this.transform);
        promptInstance.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (promptInstance != null)
            {
                promptInstance.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (promptInstance != null)
            {
                promptInstance.SetActive(false);
            }
        }
    }

    // 可選：如果你希望提示圖標永遠朝向攝影機或玩家
    // void Update()
    // {
    //     if (promptInstance != null && promptInstance.activeSelf)
    //     {
    //         // 例如，使其不隨父物件旋轉
    //         promptInstance.transform.rotation = Quaternion.identity;
    //     }
    // }
}