using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Player : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private List<GroundItem> nearbyItems = new List<GroundItem>();
    private List<IInteractable> nearbyInteractables = new List<IInteractable>();

    [Header("隊伍初始設定")]
    [Tooltip("將定義玩家初始持有成員的 PartyDatabase 掛載於此。")]
    [SerializeField] private PartyDatabase initialMemberDatabase;
    private bool hasInitializedParty = false;
    private bool isSubscribed = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        TrySubscribeToInputEvents();
    }

    void OnDisable()
    {
        UnsubscribeFromInputEvents();
    }
    
    void Update()
    {
        if (!isSubscribed)
        {
            TrySubscribeToInputEvents();
        }
    }

    void Start()
    {
        InitializeStartingParty();
    }
    
    private void TrySubscribeToInputEvents()
    {
        if (InputManager.Instance != null && !isSubscribed)
        {
            Debug.Log("[Player.cs] 成功找到 InputManager，正在訂閱事件...");
            InputManager.Instance.OnMove += HandleMoveInput;
            InputManager.Instance.OnInteract += TryInteract;
            InputManager.Instance.OnPickup += TryPickupItem;
            isSubscribed = true;
        }
    }

    private void UnsubscribeFromInputEvents()
    {
        if (InputManager.Instance != null && isSubscribed)
        {
            Debug.Log("[Player.cs] 取消訂閱 InputManager 事件。");
            InputManager.Instance.OnMove -= HandleMoveInput;
            InputManager.Instance.OnInteract -= TryInteract;
            InputManager.Instance.OnPickup -= TryPickupItem;
            isSubscribed = false;
        }
    }

    private void HandleMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    void FixedUpdate()
    {
        if (moveInput != Vector2.zero && ExplorationUIManager.Instance != null && ExplorationUIManager.Instance.IsProgressBarActive)
        {
            ExplorationUIManager.Instance.CancelCurrentProgress();
        }
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        GroundItem item = other.GetComponent<GroundItem>();
        if (item != null && !nearbyItems.Contains(item))
        {
            nearbyItems.Add(item);
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
            Debug.Log($"[Player] 進入可互動物件 '{other.gameObject.name}' 的範圍。");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GroundItem item = other.GetComponent<GroundItem>();
        if (item != null)
        {
            nearbyItems.Remove(item);
        }
        
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
            Debug.Log($"[Player] 離開可互動物件 '{other.gameObject.name}' 的範圍。");
        }
    }

    public void TryPickupItem()
    {
        nearbyItems.RemoveAll(item => item == null);
        if (nearbyItems.Count == 0) return;

        GroundItem closestItem = nearbyItems.OrderBy(item => 
            Vector2.Distance(transform.position, item.transform.position)
        ).FirstOrDefault();

        closestItem?.AttemptPickup();
    }
    
    public void TryInteract()
    {
        nearbyInteractables.RemoveAll(i => i == null || (i as MonoBehaviour) == null);
        
        if (nearbyInteractables.Count == 0)
        {
            Debug.Log("[Player] 附近沒有可互動的物件。");
            return;
        }

        IInteractable closestInteractable = nearbyInteractables.OrderBy(i => 
            Vector2.Distance(transform.position, (i as MonoBehaviour).transform.position)
        ).FirstOrDefault();
        
        if (closestInteractable != null)
        {
            closestInteractable.Interact();
        }
    }

    private void InitializeStartingParty()
    {
        if (hasInitializedParty || PartyManager.Instance == null) return;

        if (initialMemberDatabase == null || initialMemberDatabase.allMembers == null)
        {
            Debug.LogWarning("Player 物件上未指定初始成員資料庫，不新增任何初始成員。");
            return;
        }
        
        if (PartyManager.Instance.AllMembers.Count > 0)
        {
            Debug.Log("偵測到玩家已有成員(可能為讀檔)，跳過初始成員新增程序。");
            hasInitializedParty = true;
            return;
        }

        Debug.Log("正在根據 Player 物件上的設定初始化玩家隊伍...");
        foreach (var memberData in initialMemberDatabase.allMembers)
        {
            if (memberData != null)
            {
                PartyManager.Instance.AddMemberToHolder(memberData.memberID);
            }
        }
        
        var initialBattleParty = PartyManager.Instance.AllMembers.Take(4).ToList();
        foreach (var member in initialBattleParty)
        {
            PartyManager.Instance.SetToBattleParty(member);
        }
        hasInitializedParty = true;
    }

    public void SetPosition(Vector3 position)
    {
        if (rb != null)
        {
            transform.position = position;
            rb.velocity = Vector2.zero;
        }
        else
        {
            transform.position = position;
        }
        Debug.Log($"玩家位置已還原至: {position}");
    }
}

public interface IInteractable
{
    void Interact();
}