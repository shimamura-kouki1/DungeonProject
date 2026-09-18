using System;
using UnityEngine;

/// <summary>
/// 実行時に変動するプレイヤーの状態（現在HP/スタミナ/MP、無敵フラグなど）。
/// PlayerStats（設計値・ScriptableObject）と対になるクラス。
/// ScriptableObjectはアセットなので実行時の値を直接書き換えず、
/// このクラスをPlayerControllerが1つ持つ形にする。
/// </summary>
[Serializable]
public class PlayerRuntimeState : IDamageable
{
    private readonly PlayerStats _stats;

    public float CurrentHp { get; private set; }
    public float CurrentStamina { get; private set; }
    public float CurrentMp { get; private set; }

    /// <summary>回避中などの無敵状態か</summary>
    public bool IsInvincible { get; set; }

    public bool IsDead => CurrentHp <= 0f;

    /// <summary>直近でスタミナを消費した時刻（Time.time）。回復ディレイの判定に使用</summary>
    private float _lastStaminaUseTime = -999f;

    public event Action<float, float> OnHpChanged;      // (current, max)
    public event Action<float, float> OnStaminaChanged;  // (current, max)
    public event Action OnDied;

    public PlayerRuntimeState(PlayerStats stats)
    {
        _stats = stats;
        CurrentHp = stats.MaxHp;
        CurrentStamina = stats.MaxStamina;
        CurrentMp = stats.MaxMp;
    }

    /// <summary>
    /// 指定量のスタミナを消費できるか判定し、可能なら消費する。
    /// 攻撃/回避/ダッシュ/ガードは全てこれを通す（仕様書1.2）。
    /// </summary>
    public bool TryConsumeStamina(float amount)
    {
        if (CurrentStamina < amount) return false;

        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, _stats.MaxStamina);
        return true;
    }

    /// <summary>ガード継続中などの連続消費用。足りなければ消費できた分だけ減らしfalseを返す</summary>
    public bool ConsumeStaminaOverTime(float amountPerSecond, float deltaTime)
    {
        float amount = amountPerSecond * deltaTime;
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
        _lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, _stats.MaxStamina);
        return CurrentStamina > _stats.GuardBreakStaminaThreshold;
    }

    /// <summary>毎フレーム呼ぶ。ディレイ経過後、自動でスタミナ回復</summary>
    public void TickStaminaRegen(float deltaTime)
    {
        if (CurrentStamina >= _stats.MaxStamina) return;
        if (Time.time - _lastStaminaUseTime < _stats.StaminaRegenDelay) return;

        CurrentStamina = Mathf.Min(_stats.MaxStamina, CurrentStamina + _stats.StaminaRegenPerSecond * deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, _stats.MaxStamina);
    }

    /// <summary>
    /// 被ダメージ処理。無敵中は無視。ガード中の軽減はPlayerController側で
    /// rawAttackValueに軽減率を掛けてから渡すか、別メソッドを使う想定。
    /// </summary>
    public void TakeDamage(float rawAttackValue)
    {
        if (IsInvincible || IsDead) return;

        float finalDamage = _stats.CalculateIncomingDamage(rawAttackValue);
        CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);
        OnHpChanged?.Invoke(CurrentHp, _stats.MaxHp);

        if (CurrentHp <= 0f)
        {
            OnDied?.Invoke();
        }
    }

    /// <summary>ガード中に受けるダメージ。通常の軽減に加えてガード軽減率も適用</summary>
    public void TakeGuardedDamage(float rawAttackValue)
    {
        float reduced = rawAttackValue * (1f - _stats.GuardDamageReductionRate);
        TakeDamage(reduced);
    }
}