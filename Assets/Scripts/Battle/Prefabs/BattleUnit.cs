using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections.Generic;

public interface IBattleUnit_ReadOnly
{
    string UnitName { get; }
    int CurrentHP { get; }
    int MaxHP { get; }
    int CurrentStamina { get; }
    int MaxStamina { get; }
    int CurrentAttack { get; }
    int AttackRange { get; }
    bool IsDead { get; }
    bool IsPlayerTeam { get; }

    GridPosition CurrentPosition { get; }
    BattleRole Role { get; }
    
    MemberDataSO MemberData { get; }
    EnemyDataSO EnemyData { get; }

    MemberInstance MemberInstance { get; }

    List<SkillData> Skills { get; }
    
    BattleUnit GetMonoBehaviour();
}

public class BattleUnit : MonoBehaviour, IBattleUnit_ReadOnly
{   
    #region 核心數據與屬性 (Core Data & Properties)
    public MemberDataSO MemberData { get; private set; }
    public EnemyDataSO EnemyData { get; private set; }

    public MemberInstance MemberInstance { get; private set; }

    public BattleRole Role { get; private set; }
    public GridPosition CurrentPosition { get; private set; }
    public int AttackRange { get; private set; }
    public string UnitName { get; private set; }
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int CurrentAttack
    {
        get
        {
            int baseAttack = (MemberInstance != null) ? MemberInstance.CurrentAttack : EnemyData.baseAttack;

            if (buffController == null) return baseAttack;

            // 固定值加成/減成
            float flatBonus = buffController.GetBuffValue(BuffType.IncreaseAttack_Value);
            float flatPenalty = buffController.GetBuffValue(BuffType.DecreaseAttack_Value);
            float finalAttack = baseAttack + flatBonus - flatPenalty;

            // 百分比加成/減成
            float percentBonus = buffController.GetBuffValue(BuffType.IncreaseAttack_Percent);
            float percentPenalty = buffController.GetBuffValue(BuffType.DecreaseAttack_Percent);
            finalAttack *= (1.0f + percentBonus - percentPenalty);

            return Mathf.Max(0, Mathf.RoundToInt(finalAttack));
        }
    }
    public int MaxStamina { get; private set; }
    public int CurrentStamina { get; private set; }
    public List<SkillData> Skills { get; private set; }
    public bool IsPlayerTeam { get; private set; }
    public bool IsDead { get; private set; } = false;
    private bool isDefending = false;

    public static event System.Action<IBattleUnit_ReadOnly> OnUnitDiedGlobal;
    #endregion

    #region 元件與事件連結
    [Header("元件連結")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private StaminaUI staminaUI;
    
    [Header("狀態反饋")]
    [SerializeField] private GameObject selectionHighlight;
    [SerializeField] private GameObject planningHighlight; 

    [Header("戰術預演UI")]
    [SerializeField] private TextMeshProUGUI previewStaminaText;

    private BuffController buffController;
    public UnityAction<BattleUnit> OnUnitClicked;
    #endregion
    
    #region 初始化
    public void Setup(MemberInstance instance, BattleRole role, GridPosition initialPosition)
    {
        MemberInstance = instance;
        MemberData = instance.BaseData;
        UnitName = MemberData.memberName;
        Role = role;
        CurrentPosition = initialPosition;
        AttackRange = MemberData.attackRange;
        IsPlayerTeam = true;
        
        MaxHP = instance.MaxHP;
        CurrentHP = instance.currentHP;
        MaxStamina = instance.MaxStamina;
        CurrentStamina = instance.MaxStamina;
        if (spriteRenderer != null) spriteRenderer.sprite = MemberData.memberIcon;

        Skills = new List<SkillData>();
        if (MemberData.skillIDs != null)
        {
            foreach (var id in MemberData.skillIDs)
            {
                SkillData skill = SkillManager.Instance.Database.GetSkillDataByID(id);
                if (skill != null)
                {
                    Skills.Add(skill);
                }
            }
        }
        buffController = GetComponent<BuffController>();
        ResetVisualsToCoreState();
    }

    public void Setup(EnemyDataSO data, BattleRole role, GridPosition initialPosition)
    {
        EnemyData = data;
        UnitName = EnemyData.enemyName;
        Role = role;
        CurrentPosition = initialPosition;
        AttackRange = data.attackRange;
        IsPlayerTeam = false;
        
        MaxHP = data.baseHealth;
        CurrentHP = data.baseHealth;
        MaxStamina = data.baseStamina;
        CurrentStamina = data.baseStamina;
        if (spriteRenderer != null) spriteRenderer.sprite = EnemyData.enemyIcon;

        Skills = new List<SkillData>();
        if (EnemyData.skillIDs != null)
        {
            foreach (var id in EnemyData.skillIDs)
            {
                SkillData skill = SkillManager.Instance.Database.GetSkillDataByID(id);
                if (skill != null)
                {
                    Skills.Add(skill);
                }
            }
        }
        buffController = GetComponent<BuffController>();
        ResetVisualsToCoreState();
    }
    #endregion

    #region 狀態變更方法
    public void TakeDamage(int damage)
    {
        Debug.Log($"[TakeDamage] {UnitName} 嘗試受到 {damage} 點傷害。當前HP: {CurrentHP}");
        if (IsDead) return;

        int absorbedByShield = buffController.AbsorbDamage(damage);
        int remainingDamage = damage - absorbedByShield;

        if (absorbedByShield > 0 && BattleVFXManager.Instance != null)
        {
            BattleVFXManager.Instance.ShowText(transform.position + Vector3.up * 0.5f, $"-{absorbedByShield}", VFXType.Text_Shield);
        }

        if (remainingDamage <= 0) return;
        float finalDamage = remainingDamage;

        float defensePercent = buffController.GetBuffValue(BuffType.IncreaseDefense_Percent);
        finalDamage *= (1.0f - defensePercent);
        float defenseFlat = buffController.GetBuffValue(BuffType.IncreaseDefense_Value);
        finalDamage -= defenseFlat;
        if (isDefending)
        {
            finalDamage /= 2;
            ConsumeStamina(1);
            BattleLog.Instance.AddLog($"{UnitName} 處於防禦狀態，消耗 1 點體力。");
        }
        finalDamage = Mathf.Max(0, finalDamage);

        int damageToApply = Mathf.RoundToInt(finalDamage);
        CurrentHP -= damageToApply;
        if (BattleVFXManager.Instance != null)
        {
            BattleVFXManager.Instance.ShowText(transform.position + Vector3.up, damageToApply.ToString(), VFXType.Text_Damage);
        }
        if (CurrentHP <= 0)
        {
            CurrentHP = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        CurrentHP += amount;
        CurrentHP = Mathf.Min(CurrentHP, MaxHP);
        BattleLog.Instance.AddLog($"{UnitName} 恢復了 {amount} 點生命值。");
        if (BattleVFXManager.Instance != null)
        {
            BattleVFXManager.Instance.ShowText(transform.position + Vector3.up, $"+{amount}", VFXType.Text_Heal);
        }
    }

    private void Die()
    {
        IsDead = true;
        gameObject.SetActive(false);
        BattleLog.Instance.AddLog($"{UnitName} 已陣亡！");

        OnUnitDiedGlobal?.Invoke(this); 
    }

    public void ConsumeStamina(int amount)
    {
        CurrentStamina = Mathf.Max(0, CurrentStamina - amount);
    }

    public void RestoreStamina(int amount)
    {
        CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
    }

    public void SetStamina(int value)
    {
        CurrentStamina = Mathf.Clamp(value, 0, MaxStamina);
    }

    public void SetNewPosition(GridPosition newPosition)
    {
        this.CurrentPosition = newPosition;
    }

    public void SetRole(BattleRole newRole)
    {
        this.Role = newRole;
    }

    public void SetDefenseState(bool defending)
    {
        isDefending = defending;
    }
    #endregion

    #region 視覺與互動 (Visual & Interaction)
    public void UpdatePreviewVisuals(GridPosition previewPosition, int previewStamina, BattleRole previewRole)
    {
        if (BattleManager.Instance != null && BattleManager.Instance.GridSpawns[(int)previewPosition] != null)
        {
            transform.position = BattleManager.Instance.GridSpawns[(int)previewPosition].position;
        }

        if (previewStaminaText != null)
        {
            int staminaChange = previewStamina - this.CurrentStamina;
            if (staminaChange != 0)
            {
                previewStaminaText.text = staminaChange > 0 ? $"+{staminaChange}" : staminaChange.ToString();
                previewStaminaText.color = staminaChange < 0 ? Color.red : Color.green;
                previewStaminaText.gameObject.SetActive(true);
            }
            else
            {
                previewStaminaText.gameObject.SetActive(false);
            }
        }
    }

    public void ResetVisualsToCoreState()
    {
        if (BattleManager.Instance != null && BattleManager.Instance.GridSpawns[(int)this.CurrentPosition] != null)
        {
            transform.position = BattleManager.Instance.GridSpawns[(int)this.CurrentPosition].position;
        }

        if (previewStaminaText != null)
        {
            previewStaminaText.gameObject.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        OnUnitClicked?.Invoke(this);
    }
    
    public void SetHighlight(bool state)
    {
        if (selectionHighlight != null) selectionHighlight.SetActive(state);
    }
    
    public void SetPlanningHighlight(bool state)
    {
        if (planningHighlight != null) planningHighlight.SetActive(state);
    }
    #endregion
    public BattleUnit GetMonoBehaviour() => this;
}