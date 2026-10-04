using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

/// <summary>
/// 視点操作の入力を読み取り「このフレームで何度回す」に変換する。
/// マウスとコントローラーの入力の違いを吸収する
/// 戻り値の約束： x = 右に回す量(度),y = 上を向く量(度)
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
        if(_playerInput == null)
        {
            Debug.LogError($"{nameof(CameraInput)}: PlayerInput が設定されていません。", this);
            enabled = false;
            return;
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
