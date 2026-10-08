using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 視点操作(Look)の入力を読み、「このフレームで何度回すか」に変換する。
/// マウスとスティックの違い(感度の単位・deltaTime・曲線・Y反転)はここだけで吸収し、
/// CameraRig側はデバイスを一切知らなくて済むようにする。
///
/// 戻り値の約束: x = 右に回す量(度), y = 上を向く量(度)
/// </summary>
public class CameraInput : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private string _lookActionName = "Look";
    [Tooltip("視点切替ボタン(任意。無ければ切替なし)")]
    [SerializeField] private string _switchViewActionName = "SwitchView";

    [Header("マウス")]
    [Tooltip("1ピクセル動かしたときに回る角度(度)")]
    [SerializeField] private float _mouseSensitivity = 0.1f;
    [SerializeField] private bool _invertMouseY = false;

    [Header("ゲームパッド")]
    [Tooltip("スティックを最大まで倒したときの回転速度(度/秒)")]
    [SerializeField] private float _stickSensitivity = 180f;
    [Tooltip("1=線形。大きいほど小さな傾きが鈍くなり、微調整しやすい")]
    [SerializeField, Range(1f, 3f)] private float _stickCurve = 1.5f;
    [SerializeField] private bool _invertStickY = false;

    [Header("カーソル")]
    [SerializeField] private bool _lockCursorOnStart = true;

    private InputAction _look;
    private InputAction _switchView;
    private bool _lastWasGamepad;

    private bool CursorLocked => Cursor.lockState == CursorLockMode.Locked;

    private void Awake()
    {
        if (_playerInput == null)
        {
            Debug.LogError($"{nameof(CameraInput)}: PlayerInput が設定されていません。", this);
            enabled = false;
            return;
        }

        _look = _playerInput.actions.FindAction(_lookActionName, throwIfNotFound: false);
        if (_look == null)
        {
            Debug.LogError($"{nameof(CameraInput)}: アクション '{_lookActionName}' が見つかりません。", this);
            enabled = false;
            return;
        }

        _switchView = _playerInput.actions.FindAction(_switchViewActionName, throwIfNotFound: false);
        if (_switchView == null)
        {
            Debug.LogWarning($"{nameof(CameraInput)}: アクション '{_switchViewActionName}' が無いため視点切替は無効です。", this);
        }
    }

    private void OnEnable()
    {
        if (_lockCursorOnStart) SetCursorLock(true);
    }

    private void OnDisable()
    {
        SetCursorLock(false);
    }

    private void Update()
    {
        // 解除: Esc / 再ロック: 解除中の左クリック（エディタ実行中のEscはUnity側でも解除される）
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && CursorLocked)
        {
            SetCursorLock(false);
        }
        else if (!CursorLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SetCursorLock(true);
        }
    }

    /// <summary>
    /// このフレームの回転量(度)を返す。毎フレーム1回、CameraRigから呼ぶ。
    /// </summary>
    public Vector2 ReadLookDelta()
    {
        if (_look == null || !CursorLocked) return Vector2.zero;

        Vector2 raw = _look.ReadValue<Vector2>();

        // 操作中のデバイスを判別（入力が無いフレームは前回の判定を維持）
        var control = _look.activeControl;
        if (control != null) _lastWasGamepad = control.device is Gamepad;

        return _lastWasGamepad ? ConvertStick(raw) : ConvertMouse(raw);
    }

    /// <summary>このフレームで視点切替ボタンが押されたか。</summary>
    public bool SwitchViewPressed()
    {
        return _switchView != null && _switchView.WasPressedThisFrame();
    }

    private Vector2 ConvertMouse(Vector2 raw)
    {
        // マウスは「このフレームの移動量」なのでdeltaTimeは掛けない
        Vector2 deg = raw * _mouseSensitivity;
        if (_invertMouseY) deg.y = -deg.y;
        return deg;
    }

    private Vector2 ConvertStick(Vector2 raw)
    {
        // スティックは「倒し具合=速度」なのでdeltaTimeを掛ける
        float magnitude = raw.magnitude;
        if (magnitude < 0.0001f) return Vector2.zero;

        Vector2 curved = raw / magnitude * Mathf.Pow(magnitude, _stickCurve);
        Vector2 deg = curved * (_stickSensitivity * Time.deltaTime);
        if (_invertStickY) deg.y = -deg.y;
        return deg;
    }

    private static void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}