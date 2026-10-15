using System;
using UnityEngine;


[Flags]
public enum CancelTarget
{
    None = 0,
    Attack = 1 << 0,
    Dodge = 1 << 1,
    Guard = 1 << 2,
    Dash = 1 << 3,
}

[Serializable]
public class NormalizedWindow
{
    [SerializeField, Range(0f, 1f)] private float _start = 0f;
    [SerializeField,Range(0f,1f)] private float _end = 1f;

    private bool _open;
    private Action<bool> _onChanged;

    public void Begin(Action<bool> onChanged)
    {
        _open = false;
        _onChanged = onChanged;
    }

    public void Tick(float normalizedTime)
    {
        bool shouldOpen = normalizedTime >= _start && normalizedTime < _end;
        if (shouldOpen == _open) return;
        _open = shouldOpen;
        _onChanged?.Invoke(_open);
    }
    
    /// <summary>
    ///EXIT時にかならず呼ぶこと。開きっぱなしを防ぐため 
    /// </summary>
    public void ForceClose()
    {
        if(_open)
        {
            _open = false;
            _onChanged?.Invoke(false);
        }
        _onChanged = null;
    }

    [Serializable]
    public class CancelWindowSetting
    {
        [SerializeField] private NormalizedWindow _window = new();
        [SerializeField] private CancelTarget _targets = CancelTarget.Dodge;

        public CancelTarget Targets => _targets;
        public void Begin(Action<bool> onChanged) => _window.Begin(onChanged);
        public void Tick(float t) => _window.Tick(t);
        public void ForceClose() => _window.ForceClose();
    }
}
