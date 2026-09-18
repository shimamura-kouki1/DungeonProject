/// <summary>
/// ダメージを受けられるオブジェクトの共通インターフェース。
/// 実装計画ステップ2で敵にも実装する想定。プレイヤー側にも
/// 先に実装しておくことで、攻撃側のロジックを敵/プレイヤーで
/// 使い回せるようにする。
/// </summary>
public interface IDamageable
{
    /// <summary>生のダメージ値（軽減前のATK）を受け取り、内部で軽減計算して適用する</summary>
    void TakeDamage(float rawAttackValue);

    bool IsDead { get; }
}