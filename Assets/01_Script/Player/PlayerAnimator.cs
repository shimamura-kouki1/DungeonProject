using UnityEngine;
using static UnityEngine.CullingGroup;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController _controller;
    private Animator _animator;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int DodgeHash = Animator.StringToHash("Dodge");
    static readonly int GuardHash = Animator.StringToHash("Guard");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (_controller == null)
            _controller = GetComponentInParent<PlayerController>();
    }
    private void OnEnable()
    {
        _controller.StateChanged += OnStateChanged;
        _animator.SetBool(GuardHash, _controller.CurrentState == PlayerController.PlayerActionState.Guard);
    }
    private void OnDisable() => _controller.StateChanged -= OnStateChanged;

    private void Update()
    {
        _animator.SetFloat(SpeedHash, _controller.NormalizedSpeed, 0.1f, Time.deltaTime);
    }

    private void OnStateChanged(PlayerController.PlayerActionState state)
    {
        _animator.SetBool(GuardHash, state == PlayerController.PlayerActionState.Guard);

        if (state == PlayerController.PlayerActionState.Attack) _animator.SetTrigger(AttackHash);
        if (state == PlayerController.PlayerActionState.Dodge) _animator.SetTrigger(DodgeHash);
    }
}
