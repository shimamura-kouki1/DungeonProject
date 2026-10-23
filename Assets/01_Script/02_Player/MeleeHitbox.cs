using System.Collections.Generic;
using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private float _damage = 10f;
    [SerializeField] private LayerMask _enemyMask = ~0;
    //攻撃中の命中した相手の記録
    private readonly HashSet<IDamageable> _hitTargets = new HashSet<IDamageable>();
    [SerializeField] private Collider _hitBoxCollider;
    private Transform _ownerRoot;
    private bool _isAttacking;

    private void Awake()
    {
        if (_hitBoxCollider == null)
        {
            _hitBoxCollider = GetComponent<Collider>();
        }
        if (_hitBoxCollider != null)
        {
            _hitBoxCollider.isTrigger = true;
        }
        //プレイヤー側のTransformを取得する。
        _ownerRoot = transform.root;
    }

    public void BeginAttack(float damage)
    {
        _damage = damage;
        _hitTargets.Clear();
        _isAttacking = true;
        SetHitBoxActive(true);
    }

    public void EndAttack()
    {
        _isAttacking = false;
        SetHitBoxActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isAttacking) return;

        //敵のレイヤー以外を無視する
        if ((_enemyMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (other.transform.root == _ownerRoot) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null || target.IsDead) return;

        if (!_hitTargets.Add(target)) return;

        target.TakeDamage(_damage);
    }

    private void SetHitBoxActive(bool active)
    {
        if(_hitBoxCollider != null)
        {
            _hitBoxCollider.enabled = active;
        }
    }
}
