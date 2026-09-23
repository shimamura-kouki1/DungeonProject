using UnityEngine;

/// <summary>
/// プレイヤーの「設計値」を保持するScriptableObject。
/// ここに入るのは全プレイヤー共通の“最大値・調整値”のみ。
/// 現在HPや現在スタミナのような「1プレイ中に変動する値」は
/// PlayerRuntimeState 側に置き、このアセット自体は書き換えない
/// （ScriptableObjectはアセットなので、ここに実行時の値を入れると
/// 　Playを止めても値が残ってしまったり、複数プレイヤーで値を共有して
/// 　しまう事故につながるため）。
/// </summary>
[CreateAssetMenu(fileName = "PlayerStats", menuName = "GameData/Player Stats")]
public class PlayerStats : ScriptableObject
{
    [Header("基礎ステータス（仕様書 1.1）")]
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _attackStats = 10f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _defense = 10f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _maxMp = 50f;

    [Header("ダメージ計算式 軽減率=DEF/(DEF+K)（仕様書 1.3）")]
    [SerializeField] private float _defenseK = 100f;

    [Header("移動")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 720f; // deg/sec

    [Header("ダッシュ")]
    [SerializeField] private float _dashSpeed = 10f;
    [SerializeField] private float _dashStaminaDrainPerSecond = 25f;

    [Header("回避（無敵フレーム付き）")]
    [SerializeField] private float _dodgeSpeed = 12f;
    [SerializeField] private float _dodgeDuration = 0.3f;
    [SerializeField] private float _dodgeInvincibleDuration = 0.2f;
    [SerializeField] private float _dodgeStaminaCost = 20f;

    [Header("ガード")]
    [SerializeField, Range(0f, 1f)] private float _guardDamageReductionRate = 0.8f;
    [SerializeField] private float _guardStaminaDrainPerSecond = 10f;
    [SerializeField] private float _guardBreakStaminaThreshold = 0f;

    [Header("攻撃")]
    [SerializeField] private float _attackStaminaCost = 10f;
    [SerializeField] private float _attackDuration = 0.4f; // 硬直時間（後でAttackSpeedと連動させる想定）

    [Header("スタミナ回復")]
    [SerializeField] private float _staminaRegenPerSecond = 20f;
    [SerializeField] private float _staminaRegenDelay = 0.5f; // 行動後、回復が始まるまでの待ち時間

    // ---- 公開プロパティ ----
    public float MaxHp => _maxHp;
    public float Attack => _attackStats;
    public float AttackSpeed => _attackSpeed;
    public float Defense => _defense;
    public float MaxStamina => _maxStamina;
    public float MaxMp => _maxMp;
    public float DefenseK => _defenseK;

    public float MoveSpeed => _moveSpeed;
    public float RotationSpeed => _rotationSpeed;

    public float DashSpeed => _dashSpeed;
    public float DashStaminaDrainPerSecond => _dashStaminaDrainPerSecond;

    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeDuration => _dodgeDuration;
    public float DodgeInvincibleDuration => _dodgeInvincibleDuration;
    public float DodgeStaminaCost => _dodgeStaminaCost;

    public float GuardDamageReductionRate => _guardDamageReductionRate;
    public float GuardStaminaDrainPerSecond => _guardStaminaDrainPerSecond;
    public float GuardBreakStaminaThreshold => _guardBreakStaminaThreshold;

    public float AttackStaminaCost => _attackStaminaCost;
    public float AttackDuration => _attackDuration;

    public float StaminaRegenPerSecond => _staminaRegenPerSecond;
    public float StaminaRegenDelay => _staminaRegenDelay;

    /// <summary>
    /// 被ダメージ軽減率 = DEF / (DEF + K)（仕様書1.3、確定仕様）
    /// </summary>
    public float GetDamageReductionRate()
    {
        if (_defense + _defenseK <= 0f) return 0f;
        return _defense / (_defense + _defenseK);
    }

    /// <summary>
    /// このステータスの防御力を踏まえた最終被ダメージを返す。
    /// 最終ダメージ = ATK(攻撃側) × (1 − 軽減率(防御側))
    /// </summary>
    public float CalculateIncomingDamage(float attackerAtk)
    {
        return Mathf.Max(0f, attackerAtk * (1f - GetDamageReductionRate()));
    }
}