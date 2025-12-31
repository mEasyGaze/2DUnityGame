using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("跟隨目標")]
    [Tooltip("如果不指定，會自動尋找 Tag 為 Player 的物件")]
    [SerializeField] private Transform target;

    [Header("移動設定")]
    [Tooltip("數值越小越有慣性，數值越大跟得越緊 (建議 5-10)")]
    [SerializeField] private float smoothing = 5f;

    [Header("地圖邊界限制")]
    [Tooltip("攝影機中心點能到達的左下角極限座標")]
    [SerializeField] private Vector2 minPosition;
    [Tooltip("攝影機中心點能到達的右上角極限座標")]
    [SerializeField] private Vector2 maxPosition;

    private void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                Debug.LogWarning("[CameraController] 場景中找不到 Tag 為 Player 的物件，攝影機無法跟隨。");
            }
        }
        
        if (target != null)
        {
            Vector3 startPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            startPos.x = Mathf.Clamp(startPos.x, minPosition.x, maxPosition.x);
            startPos.y = Mathf.Clamp(startPos.y, minPosition.y, maxPosition.y);
            transform.position = startPos;
        }
    }

    private void LateUpdate()
    {
        if (StoryManager.Instance != null && StoryManager.Instance.IsStorySceneActive)
        {
            return; 
        }
        if (target != null)
        {
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            targetPos.x = Mathf.Clamp(targetPos.x, minPosition.x, maxPosition.x);
            targetPos.y = Mathf.Clamp(targetPos.y, minPosition.y, maxPosition.y);
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minPosition.x + maxPosition.x) / 2, (minPosition.y + maxPosition.y) / 2, 0);
        Vector3 size = new Vector3(Mathf.Abs(maxPosition.x - minPosition.x), Mathf.Abs(maxPosition.y - minPosition.y), 0);
        Gizmos.DrawWireCube(center, size);
    }
}