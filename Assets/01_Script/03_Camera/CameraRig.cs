using System;
using UnityEngine;

/// <summary>
/// プレイヤーの周りを回る軌道カメラ。
/// TPS/FPSの違いは CameraProfile(設定値)だけで表現し、このクラスに視点ごとの分岐は無い。
///   TPS: Distance > 0
///   FPS: Distance = 0 (距離が近づくと自キャラのRendererを自動で非表示にする)
/// 入力デバイスは知らない(CameraInputが度数に変換して渡す)。
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform _target;
    [SerializeField] private CameraInput _input;

    [Header("プロファイル")]
    [Tooltip("切替順。先頭が初期の視点")]
    [SerializeField] private CameraProfile[] _profiles;
    [Tooltip("視点切替時に設定値が目標へ近づく速さ")]
    [SerializeField] private float _blendSpeed = 10f;

    [Header("共通設定")]
    [SerializeField] private float _initialPitch = 15f;
    [Tooltip("カメラが衝突する層。Playerの層は外すこと")]
    [SerializeField] private LayerMask _collisionMask = ~0;
    [Tooltip("壁から離れたとき、元の距離に戻る速さ。壁に近づく側は即座に寄る")]
    [SerializeField] private float _recoverSpeed = 8f;
    [Tooltip("カメラがこの距離より近いと自キャラを非表示にする")]
    [SerializeField] private float _hideModelDistance = 0.3f;

    private Camera _camera;
    private Renderer[] _targetRenderers;
    private bool _modelHidden;

    private int _profileIndex;
    private float _yaw;
    private float _pitch;
    private float _distanceFraction = 1f; // 0〜1。壁に押されて縮んだ割合

    // 補間中の「現在の」値(目標はCurrentProfile)
    private Vector3 _pivotOffset;
    private Vector3 _shoulderOffset;
    private float _distance;
    private float _fov;

    public CameraProfile CurrentProfile => _profiles[_profileIndex];

    /// <summary>現在の視点の種類</summary>
    public CameraViewMode ViewMode => CurrentProfile.ViewMode;

    /// <summary>視点の種類が変わったときに通知する(同じ種類のProfile間の切替では通知しない)</summary>
    public event Action<CameraViewMode> ViewModeChanged;

    /// <summary>水平方向の向きのみ</summary>
    public Quaternion YawRotation => Quaternion.Euler(0f, _yaw, 0f);

    private void Awake()
    {
        _camera = GetComponent<Camera>();

        if (_target == null || _profiles == null || _profiles.Length == 0)
        {
            Debug.LogError($"{nameof(CameraRig)}: target または profiles が設定されていません。", this);
            enabled = false;
            return;
        }

        _targetRenderers = _target.GetComponentsInChildren<Renderer>();

        _yaw = _target.eulerAngles.y;
        SnapToProfile(CurrentProfile);
        _pitch = Mathf.Clamp(_initialPitch, CurrentProfile.MinPitch, CurrentProfile.MaxPitch);
    }

    private void LateUpdate()
    {
        // プレイヤーはUpdateで移動済み。その後に追従するのでLateUpdate。
        if (_input != null)
        {
            if (_input.SwitchViewPressed()) NextProfile();
            Rotate(_input.ReadLookDelta());
        }

        BlendTowardProfile();
        UpdateTransform();
        UpdateModelVisibility();
    }

    // ---------------- 公開API ----------------

    /// <summary>回転を加える。x=右に回す度数, y=上を向く度数。ロックオン等で外から回す口にもなる。</summary>
    public void Rotate(Vector2 deltaDegrees)
    {
        _yaw += deltaDegrees.x;
        _pitch = Mathf.Clamp(_pitch - deltaDegrees.y, CurrentProfile.MinPitch, CurrentProfile.MaxPitch);
    }

    public void NextProfile() => SetProfile((_profileIndex + 1) % _profiles.Length);

    public void SetProfile(int index)
    {
        if (index < 0 || index >= _profiles.Length || index == _profileIndex) return;

        CameraViewMode previous = ViewMode;
        _profileIndex = index;
        // 新しいプロファイルの角度範囲に収める
        _pitch = Mathf.Clamp(_pitch, CurrentProfile.MinPitch, CurrentProfile.MaxPitch);

        if (ViewMode != previous) ViewModeChanged?.Invoke(ViewMode);
    }

    // ---------------- 内部処理 ----------------

    private void SnapToProfile(CameraProfile p)
    {
        _pivotOffset = p.PivotOffset;
        _shoulderOffset = p.ShoulderOffset;
        _distance = p.Distance;
        _fov = p.Fov;
    }

    private void BlendTowardProfile()
    {
        CameraProfile p = CurrentProfile;
        float k = 1f - Mathf.Exp(-_blendSpeed * Time.deltaTime);
        _pivotOffset = Vector3.Lerp(_pivotOffset, p.PivotOffset, k);
        _shoulderOffset = Vector3.Lerp(_shoulderOffset, p.ShoulderOffset, k);
        _distance = Mathf.Lerp(_distance, p.Distance, k);
        _fov = Mathf.Lerp(_fov, p.Fov, k);
    }

    private void UpdateTransform()
    {
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = _target.position + _pivotOffset;

        // 注視点から「理想のカメラ位置」へのオフセット
        Vector3 offset = rotation * (_shoulderOffset + Vector3.back * _distance);
        float length = offset.magnitude;

        // 壁チェック: 注視点→理想位置へSphereCastし、当たったらその手前まで縮める
        float wantFraction = 1f;
        if (length > 0.01f &&
            Physics.SphereCast(pivot, CurrentProfile.CollisionRadius, offset / length, out RaycastHit hit,
                length, _collisionMask, QueryTriggerInteraction.Ignore))
        {
            wantFraction = Mathf.Clamp01(hit.distance / length);
        }

        // 寄るのは即座に(めり込み防止)、戻るのは滑らかに(ガタつき防止)
        _distanceFraction = wantFraction < _distanceFraction
            ? wantFraction
            : Mathf.Lerp(_distanceFraction, wantFraction, 1f - Mathf.Exp(-_recoverSpeed * Time.deltaTime));

        transform.SetPositionAndRotation(pivot + offset * _distanceFraction, rotation);
        _camera.fieldOfView = _fov;
    }

    private void UpdateModelVisibility()
    {
        // 壁に押されて寄った場合も隠れる(カメラが体にめり込む対策を兼ねる)
        bool hide = _distance * _distanceFraction < _hideModelDistance;
        if (hide == _modelHidden) return;

        _modelHidden = hide;
        foreach (var r in _targetRenderers)
        {
            if (r != null) r.enabled = !hide;
        }
    }
}