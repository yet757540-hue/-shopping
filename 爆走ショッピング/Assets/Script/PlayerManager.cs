using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
// Rigidbody を使ってプレイヤーを車のように動かす中心コンポーネントです。
// 役割:
// - ゲームパッド入力または外部入力から、加速、ブレーキ、後退、旋回を物理力として適用します。
// - 最高速度、横滑り抑制、旋回速度制限、停止判定をまとめて扱います。
// 接続:
// - InventoryInfluenceSettings から SetLoadInfluence を呼ばれ、荷物重量に応じて移動性能が変化します。
// - StartMenuManager で選んだ設定は PlayerMovementPresetApplier 経由で ApplyMovementSettings / ApplyControlScheme に届きます。
// - GameManager は同じ GameObject に PlayerInventory を付ける前提でこのコンポーネントを探します。
// 読むときの要点:
// - Update は入力取得、FixedUpdate は物理更新です。
// - ApplyDriveInput が前後移動、ApplyTurnTorque と ApplyTurnDeceleration が旋回を担当します。
public class PlayerManager : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float lowSpeedAcceleration = 25f;
    [SerializeField] private float highSpeedAcceleration = 10f;
    [SerializeField] private float accelerationSwitchSpeed = 15f;
    [SerializeField] private float reverseAcceleration = 12f;
    [SerializeField] private float deceleration = 25f;
    [SerializeField] private float brakeDeceleration = 40f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float maxReverseSpeed = 6f;
    [SerializeField] private float triggerDeadZone = 0.1f;
    [SerializeField] private float stopThreshold = 0.1f;

    [Header("Turn Settings")]
    [SerializeField] private float turnResetSpeed = 8f;
    [SerializeField] private float stickDeadZone = 0.1f;
    [SerializeField] private float turnAcceleration = 8f;
    [SerializeField] private float maxAngularSpeed = 3f;

    [Header("Input Settings")]
    [SerializeField] private bool readGamepadDirectly = true;
    [SerializeField] private PlayerMovementControlScheme controlScheme = PlayerMovementControlScheme.Triggers;

    private Rigidbody rb;
    private float steerInput;
    private float accelerateInput;
    private float brakeInput;
    private bool isReversing = false;
    private float loadAccelerationMultiplier = 1f;
    private float loadDecelerationMultiplier = 1f;
    private float loadTurnAccelerationMultiplier = 1f;
    private float loadTurnDecelerationMultiplier = 1f;

    public float AccelerateInput => accelerateInput;
    public PlayerMovementControlScheme ControlScheme => controlScheme;

    // Rigidbody の基本設定を、この移動制御向けに初期化します。
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = maxAngularSpeed;
    }

    // 直接入力モードの場合、毎フレーム現在のゲームパッド入力を読み取ります。
    private void Update()
    {
        if (readGamepadDirectly)
        {
            ReadGamepadInput();
        }
    }

    // Rigidbody へ力やトルクを加える処理は FixedUpdate に集約します。
    private void FixedUpdate()
    {
        // 物理更新の順番は、前後移動 -> 旋回入力 -> 旋回減衰 -> 速度制限です。
        ApplyDriveInput();
        ApplyTurnTorque();
        ApplyTurnDeceleration();
        LimitHorizontalSpeed();
        LimitAngularSpeed();
    }

    // 外部入力や直接入力から受け取った値を安全な範囲に丸めて保存します。
    public void SetMoveInput(float steer, float accelerate, float brake)
    {
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        accelerateInput = Mathf.Clamp01(accelerate);
        brakeInput = Mathf.Clamp01(brake);
    }

    // 所持品重量による移動性能倍率を受け取ります。
    public void SetLoadInfluence(
        float accelerationMultiplier,
        float decelerationMultiplier,
        float turnAccelerationMultiplier,
        float turnDecelerationMultiplier
    )
    {
        loadAccelerationMultiplier = Mathf.Max(0f, accelerationMultiplier);
        loadDecelerationMultiplier = Mathf.Max(0f, decelerationMultiplier);
        loadTurnAccelerationMultiplier = Mathf.Max(0f, turnAccelerationMultiplier);
        loadTurnDecelerationMultiplier = Mathf.Max(0f, turnDecelerationMultiplier);
    }

    // プリセットから渡された移動設定を現在の PlayerManager に反映します。
    public void ApplyMovementSettings(PlayerMovementSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        // StartMenu で選んだプリセット値を実際の移動パラメータへ反映します。
        settings.Validate();

        lowSpeedAcceleration = settings.LowSpeedAcceleration;
        highSpeedAcceleration = settings.HighSpeedAcceleration;
        accelerationSwitchSpeed = settings.AccelerationSwitchSpeed;
        reverseAcceleration = settings.ReverseAcceleration;
        deceleration = settings.Deceleration;
        brakeDeceleration = settings.BrakeDeceleration;
        maxSpeed = settings.MaxSpeed;
        maxReverseSpeed = settings.MaxReverseSpeed;
        triggerDeadZone = settings.TriggerDeadZone;
        stopThreshold = settings.StopThreshold;
        turnResetSpeed = settings.TurnResetSpeed;
        stickDeadZone = settings.StickDeadZone;
        turnAcceleration = settings.TurnAcceleration;
        maxAngularSpeed = settings.MaxAngularSpeed;

        if (rb != null)
        {
            rb.maxAngularVelocity = maxAngularSpeed;
        }
    }

    // 入力方式を切り替え、加速・ブレーキ状態と後退状態をリセットします。
    public void ApplyControlScheme(PlayerMovementControlScheme scheme)
    {
        controlScheme = scheme;
        SetMoveInput(steerInput, 0f, 0f);
        isReversing = false;
    }

    // 現在の controlScheme に応じてゲームパッド入力を steer / accelerate / brake へ割り当てます。
    private void ReadGamepadInput()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            SetMoveInput(0f, 0f, 0f);
            return;
        }

        float steer = gamepad.leftStick.x.ReadValue();

        switch (controlScheme)
        {
            case PlayerMovementControlScheme.FaceButtons:
                SetMoveInput(
                    steer,
                    gamepad.buttonSouth.ReadValue(),
                    gamepad.buttonEast.ReadValue()
                );
                break;
            case PlayerMovementControlScheme.Triggers:
            default:
                SetMoveInput(
                    steer,
                    gamepad.rightTrigger.ReadValue(),
                    gamepad.leftTrigger.ReadValue()
                );
                break;
        }
    }

    // 加速、ブレーキ、後退、自然減速のどれを行うかを入力状態から決めます。
    private void ApplyDriveInput()
    {
        bool isPressingAccelerate = accelerateInput > triggerDeadZone;
        bool isPressingBrake = brakeInput > triggerDeadZone;

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float horizontalSpeed = horizontalVelocity.magnitude;

        if (isPressingAccelerate)
        {
            isReversing = false;

            float forwardSpeed = GetForwardSpeed(horizontalVelocity);
            float currentAcceleration = GetForwardAcceleration(forwardSpeed);
            Vector3 force = transform.forward * currentAcceleration * accelerateInput;

            rb.AddForce(force, ForceMode.Acceleration);
            // 加速中に通常ブレーキをかけると前進まで削るため、横滑り成分だけを減衰させます。
            ApplyLateralBrake(deceleration * loadDecelerationMultiplier);
            return;
        }

        if (isPressingBrake)
        {
            if (isReversing)
            {
                // 一度停止判定を越えた後は、ブレーキ入力を後退入力として扱います。
                ApplyReverseForce();
                return;
            }

            if (horizontalSpeed <= stopThreshold)
            {
                StopHorizontalMovement();
                isReversing = true;
                ApplyReverseForce();
                return;
            }

            ApplyBrake(brakeDeceleration * loadDecelerationMultiplier);
            return;
        }

        isReversing = false;
        ApplyBrake(deceleration * loadDecelerationMultiplier);
    }

    // 現在の前進速度に応じて低速用・高速用の加速度を切り替えます。
    private float GetForwardAcceleration(float forwardSpeed)
    {
        if (forwardSpeed < accelerationSwitchSpeed)
        {
            return lowSpeedAcceleration * loadAccelerationMultiplier;
        }

        return highSpeedAcceleration * loadAccelerationMultiplier;
    }

    // 水平速度のうち、プレイヤー前方成分だけを速度として取り出します。
    private float GetForwardSpeed(Vector3 horizontalVelocity)
    {
        return Vector3.Dot(horizontalVelocity, GetHorizontalForward());
    }

    // 水平速度から前方成分を引き、横滑り成分だけを返します。
    private Vector3 GetLateralVelocity(Vector3 horizontalVelocity)
    {
        Vector3 forward = GetHorizontalForward();
        Vector3 forwardVelocity = forward * Vector3.Dot(horizontalVelocity, forward);
        return horizontalVelocity - forwardVelocity;
    }

    // プレイヤーの forward を水平面へ投影して正規化します。
    private Vector3 GetHorizontalForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        return forward;
    }

    // ブレーキ入力を後退用の加速度として Rigidbody に加えます。
    private void ApplyReverseForce()
    {
        Vector3 reverseForce = -transform.forward * reverseAcceleration * loadAccelerationMultiplier * brakeInput;
        rb.AddForce(reverseForce, ForceMode.Acceleration);
    }

    // 水平速度全体を減速させます。入力がない時の自然減速や通常ブレーキに使います。
    private void ApplyBrake(float brakePower)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed <= stopThreshold)
        {
            StopHorizontalMovement();
            return;
        }

        float speedDrop = brakePower * Time.fixedDeltaTime;

        if (speed <= speedDrop)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 brakeForce = -horizontalVelocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    // 加速中に横滑りだけを減速し、前進加速を邪魔しないようにします。
    private void ApplyLateralBrake(float brakePower)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 lateralVelocity = GetLateralVelocity(horizontalVelocity);
        float lateralSpeed = lateralVelocity.magnitude;

        if (lateralSpeed <= stopThreshold)
        {
            rb.linearVelocity = new Vector3(
                velocity.x - lateralVelocity.x,
                velocity.y,
                velocity.z - lateralVelocity.z
            );
            return;
        }

        float speedDrop = brakePower * Time.fixedDeltaTime;

        if (lateralSpeed <= speedDrop)
        {
            rb.linearVelocity = new Vector3(
                velocity.x - lateralVelocity.x,
                velocity.y,
                velocity.z - lateralVelocity.z
            );
            return;
        }

        Vector3 brakeForce = -lateralVelocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    // スティック入力がある間、Y 軸回転トルクを加えます。
    private void ApplyTurnTorque()
    {
        if (Mathf.Abs(steerInput) <= stickDeadZone)
        {
            return;
        }

        Vector3 torque = Vector3.up * steerInput * turnAcceleration * loadTurnAccelerationMultiplier;
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    // スティックを離した後、Y 軸の角速度をゆっくり減らします。
    private void ApplyTurnDeceleration()
    {
        if (Mathf.Abs(steerInput) > stickDeadZone)
        {
            return;
        }

        // スティックを離したときだけ逆向きトルクで旋回を戻し、入力中の曲がりを邪魔しません。
        Vector3 angularVelocity = rb.angularVelocity;

        if (Mathf.Abs(angularVelocity.y) <= 0.001f)
        {
            rb.angularVelocity = new Vector3(0f, 0f, 0f);
            return;
        }

        float resetPower = turnResetSpeed * loadTurnDecelerationMultiplier;
        Vector3 resetTorque = Vector3.up * -angularVelocity.y * resetPower;
        rb.AddTorque(resetTorque, ForceMode.Acceleration);
    }

    // 前進中と後退中で別々の最高速度を適用します。
    private void LimitHorizontalSpeed()
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        float speedLimit = isReversing ? maxReverseSpeed : maxSpeed;

        if (horizontalVelocity.magnitude <= speedLimit)
        {
            return;
        }

        Vector3 limitedHorizontalVelocity = horizontalVelocity.normalized * speedLimit;

        rb.linearVelocity = new Vector3(
            limitedHorizontalVelocity.x,
            velocity.y,
            limitedHorizontalVelocity.z
        );
    }

    // 旋回速度を上限内に収め、不要な X/Z 回転を消します。
    private void LimitAngularSpeed()
    {
        Vector3 angularVelocity = rb.angularVelocity;
        float limitedY = Mathf.Clamp(
            angularVelocity.y,
            -maxAngularSpeed,
            maxAngularSpeed
        );

        // 横転方向の回転を使わないゲームなので、X/Z 回転はここで消します。
        rb.angularVelocity = new Vector3(0f, limitedY, 0f);
    }

    // Rigidbody の速度から水平成分だけを取り出します。
    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    // Y 方向速度は残したまま、地面上の移動だけを止めます。
    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    // Inspector 値を物理計算で安全に使える範囲へ補正します。
    private void OnValidate()
    {
        lowSpeedAcceleration = Mathf.Max(0f, lowSpeedAcceleration);
        highSpeedAcceleration = Mathf.Max(0f, highSpeedAcceleration);
        accelerationSwitchSpeed = Mathf.Max(0f, accelerationSwitchSpeed);
        reverseAcceleration = Mathf.Max(0f, reverseAcceleration);
        deceleration = Mathf.Max(0f, deceleration);
        brakeDeceleration = Mathf.Max(0f, brakeDeceleration);
        maxSpeed = Mathf.Max(0f, maxSpeed);
        maxReverseSpeed = Mathf.Max(0f, maxReverseSpeed);
        triggerDeadZone = Mathf.Clamp01(triggerDeadZone);
        stopThreshold = Mathf.Max(0f, stopThreshold);
        turnResetSpeed = Mathf.Max(0f, turnResetSpeed);
        stickDeadZone = Mathf.Clamp01(stickDeadZone);
        turnAcceleration = Mathf.Max(0f, turnAcceleration);
        maxAngularSpeed = Mathf.Max(0.01f, maxAngularSpeed);
    }
}
