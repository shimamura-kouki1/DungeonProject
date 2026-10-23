using UnityEngine;

/// <summary>
/// Enemyに共通する体力・ダメージ・死亡処理を管理する基盤
/// 探索や、移動、攻撃の判断は別制御
/// </summary>
public abstract class EnemyBase : MonoBehaviour,IDamageable
{
    [SerializeField] private EnemyStats _enemyStats;

    private float _currentHp;
    private bool _isDead;

    public EnemyStats EnemyStats => _enemyStats;
    public float CurrentHp => _currentHp;
    public float MaxHp => _enemyStats != null ? _enemyStats.MaxHp:0f;
    public bool IsDead => _isDead;

    public virtual void Awake()
    {
        if(_enemyStats == null)
        {
            enabled = false;
            return;
        }
        _currentHp = _enemyStats.MaxHp;
    }

    /// <summary>
    /// ダメージの受け取り
    /// </summary>
    /// <param name="AttackValue"></param>
    public virtual void TakeDamage(float AttackValue)
    {
        if (_isDead || _enemyStats == null) return;

        float damage = _enemyStats.CalculateIncomingDamage(AttackValue);
        _currentHp = Mathf.Max(0f, _currentHp - damage);

        OnDamage(damage);

        if(_currentHp <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// ダメージを受けるとき、敵ごとに必要なら実装する
    /// </summary>
    /// <param name="damage"></param>
    protected virtual void OnDamage(float damage) { }

    /// <summary>
    /// 死亡処理　演出やドロップなどの実装をここにまとめる。
    /// </summary>
    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
