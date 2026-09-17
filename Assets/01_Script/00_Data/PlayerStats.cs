using UnityEngine;

public class PlayerStats : ScriptableObject
{
    public float MaxHp => _maxHp;
    public float Attack => _attackStats;
    public float AttackSpeed => _attackSpeed;
    public float Defense => _defense;
    public float MaxStamina => _maxStamina;
    public float DefenseK => _defenseK;

    [SerializeField] private float _maxHp;
    [SerializeField] private float _attackStats;
    [SerializeField] private float _attackSpeed;
    [SerializeField] private float _defense;
    [SerializeField] private float _maxStamina;
    [SerializeField] private float _defenseK;


}
