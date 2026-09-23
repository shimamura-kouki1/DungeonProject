using UnityEngine;
[CreateAssetMenu(fileName = "EnemyStats", menuName = "GameData/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("基礎ステータス")]
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _attackStats = 10f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField] private float _defense = 10f;

    [Header("ダメージ計算式　軽減率=DEF/(DEF+K)")]
    [SerializeField] private float _defenseK = 100f;

    [Header("移動")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 720f;

    public float MaxHp => _maxHp;
    public float Attack => _attackStats;
    public float AttackSpeed => _attackSpeed;
    public float Defense => _defense;
    public float DefenseK => _defenseK;

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
