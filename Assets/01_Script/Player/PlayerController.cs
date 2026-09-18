using UnityEngine;

/// <summary>
/// プレイヤーの入力受付・移動・状態遷移を担当するMonoBehaviour。
///
/// MVP方針:
///  - 状態ごとにクラスを分けるState Patternまではまだ導入しない。
///    行動の種類(攻撃・回避・ガード・ダッシュ)は少なく、遷移条件も単純なので
///    enum + switch の軽量なステートマシンで十分。ジャストガードやコンボなど
///    状態ごとのロジックが複雑化してきたら、このファイルを崩さずに
///    IPlayerState方式へリファクタリングする（各状態はこのクラスのメソッド
///    にほぼ対応しているので移行コストは低い想定）。
///  - 攻撃の当たり判定・ダメージ付与はまだ実装しない（敵側のIDamageableが
///    無いため）。Attack状態は硬直とスタミナ消費のみ実装し、実装計画ステップ2
///    で武器判定/敵ヒット処理を追加する。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IDamageable
{
    public enum PlayerActionState
    {
        Idle,
        Move,
        Dash,
        Dodge,
        Guard,
        Attack
    }

    [Header("参照")]
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private Camera _referenceCamera; // 未指定ならCamera.mainを使用

    [Header("接地判定")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private LayerMask _groundMask = ~0;

    private CharacterController _controller;
    private PlayerRuntimeState _runtimeState;

    public PlayerActionState CurrentState { get; private set; } = PlayerActionState.Idle;
    public PlayerRuntimeState RuntimeState => _runtimeState;

    // IDamageable委譲
    public bool IsDead => _runtimeState.IsDead;

    // 入力・移動用
    private Vector2 _moveInput;
    private Vector3 _moveDirWorld;
    private Vector3 _velocity; // 重力込みの縦方向速度など

    // 状態タイマー（Dash/Dodge/Attack/Guardの経過・残り時間管理に使用）
    private float _stateTimer;
    private Vector3 _stateMoveDirection; // Dash/Dodge中に使う固定移動方向

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (_stats == null)
        {
            Debug.LogError($"{nameof(PlayerController)}: PlayerStats が設定されていません。", this);
            enabled = false;
            return;
        }

        _runtimeState = new PlayerRuntimeState(_stats);

        if (_referenceCamera == null)
        {
            _referenceCamera = Camera.main;
        }
    }

    private void Update()
    {
        ReadInput();
        _runtimeState.TickStaminaRegen(Time.deltaTime);

        switch (CurrentState)
        {
            case PlayerActionState.Idle:
            case PlayerActionState.Move:
                UpdateIdleOrMove();
                break;
            case PlayerActionState.Dash:
                UpdateDash();
                break;
            case PlayerActionState.Dodge:
                UpdateDodge();
                break;
            case PlayerActionState.Guard:
                UpdateGuard();
                break;
            case PlayerActionState.Attack:
                UpdateAttack();
                break;
        }

        ApplyGravity();
    }

    // ---------------- 入力 ----------------

    private void ReadInput()
    {
        _moveInput.x = Input.GetAxisRaw("Horizontal");
        _moveInput.y = Input.GetAxisRaw("Vertical");
        _moveDirWorld = CameraRelativeDirection(_moveInput);

        // 行動不能な状態（Dash/Dodge/Guard/Attack）では新規入力を受け付けない（MVPではシンプルに排他制御）
        bool canStartNewAction = CurrentState == PlayerActionState.Idle || CurrentState == PlayerActionState.Move;
        if (!canStartNewAction) return;

        if (Input.GetButtonDown("Fire1")) // 攻撃
        {
            TryStartAttack();
        }
        else if (Input.GetButtonDown("Dodge")) // 回避（Input Managerに要追加。無ければLeftControl等で代用）
        {
            TryStartDodge();
        }
        else if (Input.GetButtonDown("Dash")) // ダッシュ（Input Managerに要追加）
        {
            TryStartDash();
        }
        else if (Input.GetButton("Guard")) // 防御（長押し。Input Managerに要追加）
        {
            TryStartGuard();
        }
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

        Vector3 forward = _referenceCamera != null ? _referenceCamera.transform.forward : Vector3.forward;
        Vector3 right = _referenceCamera != null ? _referenceCamera.transform.right : Vector3.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 dir = forward * input.y + right * input.x;
        return dir.sqrMagnitude > 1f ? dir.normalized : dir;
    }

    // ---------------- Idle / Move ----------------

    private void UpdateIdleOrMove()
    {
        if (_moveDirWorld.sqrMagnitude > 0.0001f)
        {
            CurrentState = PlayerActionState.Move;
            Move(_moveDirWorld, _stats.MoveSpeed);
            RotateTowards(_moveDirWorld);
        }
        else
        {
            CurrentState = PlayerActionState.Idle;
        }
    }

    // ---------------- Dash ----------------

    private void TryStartDash()
    {
        if (!_runtimeState.TryConsumeStamina(_stats.DashStaminaCost)) return;

        // 入力方向があればその方向、無ければ現在の向きへダッシュ
        _stateMoveDirection = _moveDirWorld.sqrMagnitude > 0.0001f ? _moveDirWorld : transform.forward;
        _stateTimer = _stats.DashDuration;
        CurrentState = PlayerActionState.Dash;
    }

    private void UpdateDash()
    {
        Move(_stateMoveDirection, _stats.DashSpeed);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            CurrentState = PlayerActionState.Idle;
        }
    }

    // ---------------- Dodge（無敵フレーム付き） ----------------

    private void TryStartDodge()
    {
        if (!_runtimeState.TryConsumeStamina(_stats.DodgeStaminaCost)) return;

        _stateMoveDirection = _moveDirWorld.sqrMagnitude > 0.0001f ? _moveDirWorld : transform.forward;
        _stateTimer = _stats.DodgeDuration;
        _runtimeState.IsInvincible = true;
        CurrentState = PlayerActionState.Dodge;

        // 無敵時間はDodgeDuration以下で個別に切れる想定（DodgeInvincibleDuration）
        Invoke(nameof(EndInvincibility), _stats.DodgeInvincibleDuration);
    }

    private void EndInvincibility()
    {
        _runtimeState.IsInvincible = false;
    }

    private void UpdateDodge()
    {
        Move(_stateMoveDirection, _stats.DodgeSpeed);

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            CurrentState = PlayerActionState.Idle;
        }
    }

    // ---------------- Guard ----------------

    private void TryStartGuard()
    {
        // ガード開始の最低コストチェックはせず、継続的に減らしていく
        CurrentState = PlayerActionState.Guard;
    }

    private void UpdateGuard()
    {
        // 長押しが離されたらガード解除
        if (!Input.GetButton("Guard"))
        {
            CurrentState = PlayerActionState.Idle;
            return;
        }

        bool stillHasStamina = _runtimeState.ConsumeStaminaOverTime(_stats.GuardStaminaDrainPerSecond, Time.deltaTime);
        if (!stillHasStamina)
        {
            // ガードブレイク
            CurrentState = PlayerActionState.Idle;
        }

        // ガード中は移動しない（MVP方針。移動しながらのガードは後で検討）
    }

    // ---------------- Attack ----------------

    private void TryStartAttack()
    {
        if (!_runtimeState.TryConsumeStamina(_stats.AttackStaminaCost)) return;

        _stateTimer = _stats.AttackDuration;
        CurrentState = PlayerActionState.Attack;

        // TODO(実装計画ステップ2以降): ここで武器の当たり判定を発生させ、
        // 範囲内のIDamageableに _stats.Attack を渡してダメージを与える。
    }

    private void UpdateAttack()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            CurrentState = PlayerActionState.Idle;
        }
    }

    // ---------------- 共通処理 ----------------

    private void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        _controller.Move(direction.normalized * speed * Time.deltaTime);
        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _stats.RotationSpeed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
        {
            _velocity.y = -2f; // 接地を維持するための小さい下向き速度
        }
        else
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        _controller.Move(_velocity * Time.deltaTime);
    }

    // ---------------- IDamageable ----------------

    public void TakeDamage(float rawAttackValue)
    {
        if (CurrentState == PlayerActionState.Guard)
        {
            _runtimeState.TakeGuardedDamage(rawAttackValue);
        }
        else
        {
            _runtimeState.TakeDamage(rawAttackValue);
        }
    }
}
