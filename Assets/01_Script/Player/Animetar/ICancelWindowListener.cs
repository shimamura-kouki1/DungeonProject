public interface ICancelWindowListener
{
    // owner：どの行動のSMBからの通知か
    void OnCancelWindowChanged(PlayerController.PlayerActionState owner, bool open, CancelTarget targets);
}