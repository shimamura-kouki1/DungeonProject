using UnityEngine;

public abstract class PlayerSMBBass<TListener> : StateMachineBehaviour where TListener : class
{
   protected TListener Listener {  get; private set; }

    //MonoBehaviourではないため、外部注入
    public void Init (TListener listener) => Listener = listener;
}
