using UnityEngine;
using static NormalizedWindow;

public class AttackBehaviour : PlayerSMBBass<IAttackAnimationListener>
{
    [SerializeField] private CancelWindowSetting _cancel = new();

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _cancel.Begin(open => Listener?.OnCancelWindowChanged(PlayerController.PlayerActionState.Attack, open, _cancel.Targets));
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _cancel.Tick(stateInfo.normalizedTime);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _cancel.ForceClose();
        Listener?.OnAttackAnimationFinished();
    }
}
