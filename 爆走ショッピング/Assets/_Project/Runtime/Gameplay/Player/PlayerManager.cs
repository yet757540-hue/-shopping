using UnityEngine;
using UnityEngine.InputSystem;

///<summary>車両の入力値を Rigidbody への加速・減速・旋回に変換します。</summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerManager : MonoBehaviour
{
    // 前後移動の加減速です。速度域で加速力を切り替えます。
    [Header("移動設定")]
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

    // 旋回入力への反応、回転力、減衰、角速度上限の設定です。
    [Header("旋回設定")]
    [SerializeField] private float turnResetSpeed = 8f;
    [SerializeField] private float stickDeadZone = 0.1f;
    [SerializeField] private float turnAcceleration = 8f;
    [SerializeField] private float maxAngularSpeed = 3f;

    // ゲームパッドを直接読むかどうかと、加速・ブレーキのボタン配置です。
    [Header("入力設定")]
    [SerializeField] private bool readGamepadDirectly = true;
    [SerializeField] private PlayerMovementControlScheme controlScheme = PlayerMovementControlScheme.FaceButtons;

    private Rigidbody rb;
    private float steerInput;
    private float accelerateInput;
    private float brakeInput;
    private bool isReversing = false;
    private float loadAccelerationMultiplier = 1f;
    private float loadDecelerationMultiplier = 1f;
    private float loadTurnAccelerationMultiplier = 1f;
    private float loadTurnDecelerationMultiplier = 1f;

    // 現在保持している加速入力の強さを返します。
    public float AccelerateInput => accelerateInput;
    // 加速とブレーキに使用するボタン配置を返します。
    public PlayerMovementControlScheme ControlScheme => controlScheme;
    // 低速用から高速用の加速力へ切り替える前進速度を返します。
    public float AccelerationSwitchSpeed => accelerationSwitchSpeed;

    // Rigidbody を取得し、補間・連続衝突判定・角速度上限を設定します。
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = maxAngularSpeed;
    }

    // 直接入力が有効な場合に、現在のゲームパッド入力を読み取ります。
    private void Update()
    {
        if (readGamepadDirectly)
        {
            ReadGamepadInput();
        }
    }

    // 物理処理は順序を固定し、加速→旋回→上限適用の流れを保ちます。
    private void FixedUpdate()
    {
        ApplyDriveInput();
        ApplyTurnTorque();
        ApplyTurnDeceleration();
        LimitHorizontalSpeed();
        LimitAngularSpeed();
    }

    // 旋回をマイナス一から一、加速とブレーキをゼロから一に制限して保持します。
    public void SetMoveInput(float steer, float accelerate, float brake)
    {
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        accelerateInput = Mathf.Clamp01(accelerate);
        brakeInput = Mathf.Clamp01(brake);
    }

    // 所持重量による加速・減速・旋回の倍率を受け取り、負の値を除きます。
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

    // プリセットを検証して走行設定へ反映し、Rigidbody の角速度上限も更新します。
    public void ApplyMovementSettings(PlayerMovementSettings settings)
    {
        if (settings == null)
        {
            return;
        }

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

    // 操作方式を切り替え、直前の加速・ブレーキ入力と後退状態を解除します。
    public void ApplyControlScheme(PlayerMovementControlScheme scheme)
    {
        controlScheme = scheme;
        SetMoveInput(steerInput, 0f, 0f);
        isReversing = false;
    }

    // 操作方式に対応するボタンを読み取り、未接続なら入力をゼロにします。
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
                    gamepad.buttonEast.ReadValue(),
                    gamepad.buttonSouth.ReadValue()
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

    // アクセル、ブレーキ、後退の三状態をここで分岐します。
    private void ApplyDriveInput()
    {
        bool isPressingAccelerate = accelerateInput > triggerDeadZone;
        bool isPressingBrake = brakeInput > triggerDeadZone;

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float horizontalSpeed = horizontalVelocity.magnitude;

        // アクセルとブレーキが同時なら、アクセル側を優先します。
        if (isPressingAccelerate)
        {
            isReversing = false;

            float forwardSpeed = GetForwardSpeed(horizontalVelocity);
            float currentAcceleration = GetForwardAcceleration(forwardSpeed);
            Vector3 force = transform.forward * currentAcceleration * accelerateInput;

            rb.AddForce(force, ForceMode.Acceleration);
            ApplyLateralBrake(deceleration * loadDecelerationMultiplier);
            return;
        }

        if (isPressingBrake)
        {
            // 後退開始後はブレーキ入力を後退入力として扱います。
            if (isReversing)
            {
                ApplyReverseForce();
                return;
            }

            // 走行中はまず停止させ、十分に遅くなった時点で後退へ移ります。
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

        // どちらも押されていない場合は、自然減速だけを適用します。
        isReversing = false;
        ApplyBrake(deceleration * loadDecelerationMultiplier);
    }

    // 前進速度の切り替え値に応じて加速力を選び、重量補正を掛けます。
    private float GetForwardAcceleration(float forwardSpeed)
    {
        if (forwardSpeed < accelerationSwitchSpeed)
        {
            return lowSpeedAcceleration * loadAccelerationMultiplier;
        }

        return highSpeedAcceleration * loadAccelerationMultiplier;
    }

    // 水平速度を車両の前方へ投影し、前進方向の速度だけを求めます。
    private float GetForwardSpeed(Vector3 horizontalVelocity)
    {
        return Vector3.Dot(horizontalVelocity, GetHorizontalForward());
    }

    // 水平速度から前後方向の成分を引き、横滑りの速度を取り出します。
    private Vector3 GetLateralVelocity(Vector3 horizontalVelocity)
    {
        Vector3 forward = GetHorizontalForward();
        Vector3 forwardVelocity = forward * Vector3.Dot(horizontalVelocity, forward);
        return horizontalVelocity - forwardVelocity;
    }

    // 車両の前方から上下成分を除き、水平面上の単位方向を返します。
    private Vector3 GetHorizontalForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        return forward;
    }

    // ブレーキ入力の強さと重量補正に応じて、後退方向へ力を加えます。
    private void ApplyReverseForce()
    {
        Vector3 reverseForce = -transform.forward * reverseAcceleration * loadAccelerationMultiplier * brakeInput;
        rb.AddForce(reverseForce, ForceMode.Acceleration);
    }

    // 水平移動を減速します。一回の減速で止まる場合は逆向きに加速させず停止します。
    private void ApplyBrake(float brakePower)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizontalVelocity.magnitude;

        float speedDrop = brakePower * Time.fixedDeltaTime;

        // 停止判定内、または今回の減速で止まる場合は速度をゼロにします。
        if (speed <= stopThreshold || speed <= speedDrop)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 brakeForce = -horizontalVelocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    // 前後・上下速度を保ったまま、横滑り成分だけを減速させます。
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

        float speedDrop = brakePower * Time.fixedDeltaTime;

        // 横滑りだけを止め、前後方向と上下方向の速度を保ちます。
        if (lateralSpeed <= stopThreshold || lateralSpeed <= speedDrop)
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

    // デッドゾーンを超えた旋回入力に、重量補正付きの回転力を与えます。
    private void ApplyTurnTorque()
    {
        if (Mathf.Abs(steerInput) <= stickDeadZone)
        {
            return;
        }

        Vector3 torque = Vector3.up * steerInput * turnAcceleration * loadTurnAccelerationMultiplier;
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    // 旋回入力がないときに回転を減衰させ、十分小さければ回転を止めます。
    private void ApplyTurnDeceleration()
    {
        if (Mathf.Abs(steerInput) > stickDeadZone)
        {
            return;
        }

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

    // 後退状態に応じた速度上限を適用し、上下方向の速度は維持します。
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

    // 水平旋回の角速度を制限し、他の軸の角速度をゼロにします。
    private void LimitAngularSpeed()
    {
        Vector3 angularVelocity = rb.angularVelocity;
        float limitedY = Mathf.Clamp(
            angularVelocity.y,
            -maxAngularSpeed,
            maxAngularSpeed
        );

        rb.angularVelocity = new Vector3(0f, limitedY, 0f);
    }

    // Rigidbody の速度から上下成分を除きます。
    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    // 上下方向の速度だけを残し、水平移動を止めます。
    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
