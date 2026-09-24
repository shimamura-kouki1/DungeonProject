using System;
using UnityEngine;

/// <summary>
/// 簡易エネミー（MVP）
/// 行動: 待機 → 接近 → 予備動作(telegraph) → 攻撃判定 → 硬直 → 再接近
/// ステータスは EnemyStats(ScriptableObject) から取得。
/// 前提: プレイヤーに "Player" タグ / プレイヤーは IDamageable を実装 /
///       プレイヤーの攻撃判定が敵の Collider(CharacterController) に当たる。
[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour,IDamageable
{
    [Header("Data")]
    [SerializeField] private EnemyStats _enemyStats;

    [Header("AI")]
    [SerializeField] float detectRange = 12f;
    [SerializeField] float attackRange = 1.8f;

    [Header("Attack")]
    [SerializeField] float windUpTime = 0.6f;       // 予備動作（回避/ガードの猶予）
    [SerializeField] float hitRadius = 1.0f;
    [SerializeField] float hitForwardOffset = 1.2f;
    [SerializeField] LayerMask targetLayers = ~0;   // プレイヤーのレイヤーに絞ると軽い
                                                    // 攻撃後の硬直 = 1 / AttackSpeed 秒（AttackSpeed=1 なら1秒）

    [Header("Feedback (任意)")]
    [SerializeField] Renderer bodyRenderer;
    [SerializeField] Color windUpColor = Color.red;
    [SerializeField] float hitFlashTime = 0.1f;
    [SerializeField] float destroyDelay = 0.5f;

    public float CurrentHp { get; private set; }
    public bool IsDead { get; private set; }
    /// <summary>死亡時に1回通知。ステップ3のコインドロップはここに購読する。</summary>
    public event Action<EnemyController> OnDied;

    enum State { Idle, Chase, WindUp, Recover, Dead }
    State state = State.Idle;
    float stateTimer;

    CharacterController cc;
    Transform target;
    Color baseColor = Color.white;
    float flashTimer;
    float verticalVelocity;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        if(_enemyStats == null)
        {
            Debug.Log($"{name}EnemyStatsが未設定です", this);
            enabled = false;
            return;
        }
        CurrentHp = _enemyStats.MaxHp;
        if (bodyRenderer != null) baseColor = bodyRenderer.material.color;
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
        else Debug.LogWarning($"{name}: Player タグのオブジェクトが見つかりません");
    }
    void Update()
    {
        if (IsDead) return;

        UpdateFlash();
        ApplyGravity();
        if (target == null) return;

        float dist = FlatDistance(target.position);

        switch (state)
        {
            case State.Idle:
                if (dist <= detectRange) ChangeState(State.Chase);
                break;

            case State.Chase:
                if (dist > detectRange) { ChangeState(State.Idle); break; }
                FaceTarget();
                if (dist <= attackRange) { ChangeState(State.WindUp); break; }
                cc.Move(transform.forward * (_enemyStats.MoveSpeed * Time.deltaTime));
                break;

            case State.WindUp:
                // 予備動作中は向き・移動を固定 → 回避しやすい
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f)
                {
                    DoAttackHit();
                    ChangeState(State.Recover);
                }
                break;

            case State.Recover:
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0f) ChangeState(State.Chase);
                break;
        }
    }

    void ChangeState(State next)
    {
        state = next;
        switch (next)
        {
            case State.WindUp:
                stateTimer = windUpTime;
                SetColor(windUpColor);
                break;
            case State.Recover:
                stateTimer = 1f / Mathf.Max(0.01f, _enemyStats.AttackSpeed);
                SetColor(baseColor);
                break;
            default:
                SetColor(baseColor);
                break;
        }
    }

    void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, _enemyStats.RotationSpeed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        cc.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
    }

    float FlatDistance(Vector3 p)
    {
        Vector3 d = p - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void DoAttackHit()
    {
        Vector3 center = transform.position + transform.forward * hitForwardOffset + Vector3.up * 0.5f;
        var hits = Physics.OverlapSphere(center, hitRadius, targetLayers, QueryTriggerInteraction.Ignore);

        foreach (var h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (!h.TryGetComponent(out IDamageable dmg)) continue;
            if (dmg is EnemyController) continue;   // 敵同士は攻撃しない
            dmg.TakeDamage(_enemyStats.Attack);           // 生のATKを渡す（軽減は受け手側）
            break;                                  // 1スイング1ヒット
        }
    }

    // ---------- IDamageable ----------
    public void TakeDamage(float rawAttackValue)
    {
        if (IsDead) return;

        float damage = _enemyStats.CalculateIncomingDamage(rawAttackValue);
        CurrentHp = Mathf.Max(0f, CurrentHp - damage);
        Debug.Log($"[{name}] ATK {rawAttackValue} / DEF {_enemyStats.Defense} → {damage:F1} ダメージ (HP {CurrentHp:F1}/{_enemyStats.MaxHp})");

        flashTimer = hitFlashTime;
        if (bodyRenderer != null) bodyRenderer.material.color = Color.white;

        if (state == State.Idle) ChangeState(State.Chase); // 攻撃されたら反応

        if (CurrentHp <= 0f) Die();
    }

    void Die()
    {
        IsDead = true;
        state = State.Dead;
        cc.enabled = false;
        OnDied?.Invoke(this);
        Destroy(gameObject, destroyDelay);
    }

    // ---------- 見た目 ----------
    void SetColor(Color c)
    {
        if (bodyRenderer != null && flashTimer <= 0f) bodyRenderer.material.color = c;
    }

    void UpdateFlash()
    {
        if (flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f && bodyRenderer != null)
            bodyRenderer.material.color = state == State.WindUp ? windUpColor : baseColor;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * hitForwardOffset + Vector3.up * 0.5f, hitRadius);
    }
}
