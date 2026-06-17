using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerManager : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float lowSpeedAcceleration = 25f;    // 低速時の前進加速度
    [SerializeField] private float highSpeedAcceleration = 10f;   // 高速時の前進加速度
    [SerializeField] private float accelerationSwitchSpeed = 15f; // 加速度を切り替える速度

    [SerializeField] private float reverseAcceleration = 12f;     // 後退加速度
    [SerializeField] private float deceleration = 25f;            // 通常減速度
    [SerializeField] private float brakeDeceleration = 40f;       // ブレーキ減速度

    [SerializeField] private float maxSpeed = 30f;                // 最大前進速度
    [SerializeField] private float maxReverseSpeed = 6f;          // 最大後退速度
    [SerializeField] private float triggerDeadZone = 0.1f;        // L2 / R2 のデッドゾーン
    [SerializeField] private float stopThreshold = 0.1f;          // 停止判定用の速度

    [Header("回転設定")]
    [SerializeField] private float turnResetSpeed = 20f;     // 無入力時に回転速度を0へ戻す速さ
    [SerializeField] private float stickDeadZone = 0.1f;     // 左スティックのデッドゾーン
    [SerializeField] private float turnAcceleration = 8f;         // 回転加速度
    [SerializeField] private float turnDeceleration = 12f;        // 回転減速度
    [SerializeField] private float maxAngularSpeed = 3f;          // 最大回転速度

    private Rigidbody rb;

    // 入力値
    private float stickX;
    private float r2Value;
    private float l2Value;

    // 後退中かどうか
    private bool isReversing = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // X軸・Z軸の回転を固定し、Y軸回転だけ許可する
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        // 見た目のカクつきを減らす
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 高速移動時のすり抜けを減らす
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Rigidbodyの最大角速度を設定する
        rb.maxAngularVelocity = maxAngularSpeed;
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            stickX = 0f;
            r2Value = 0f;
            l2Value = 0f;
            return;
        }

        // 左スティックの左右入力を取得
        stickX = gamepad.leftStick.x.ReadValue();

        // R2の押し込み具合を取得
        r2Value = gamepad.rightTrigger.ReadValue();

        // L2の押し込み具合を取得
        l2Value = gamepad.leftTrigger.ReadValue();
    }

    private void FixedUpdate()
    {
        ApplyDriveInput();

        ApplyTurnTorque();
        ApplyTurnDeceleration();

        LimitHorizontalSpeed();
        LimitAngularSpeed();
    }

    private void ApplyDriveInput()
    {
        bool isPressingR2 = r2Value > triggerDeadZone;
        bool isPressingL2 = l2Value > triggerDeadZone;

        Vector3 horizontalVelocity = GetHorizontalVelocity();
        float horizontalSpeed = horizontalVelocity.magnitude;

        // R2を押している場合は前進
        if (isPressingR2)
        {
            isReversing = false;

            // 現在の速度に応じて加速度を切り替える
            float currentAcceleration = GetForwardAcceleration(horizontalSpeed);

            Vector3 force = transform.forward * currentAcceleration * r2Value;
            rb.AddForce(force, ForceMode.Acceleration);

            return;
        }

        // L2を押している場合
        if (isPressingL2)
        {
            // すでに後退状態なら、後退を継続する
            if (isReversing)
            {
                ApplyReverseForce();
                return;
            }

            // 速度がほぼ0なら、後退状態に入る
            if (horizontalSpeed <= stopThreshold)
            {
                StopHorizontalMovement();
                isReversing = true;
                ApplyReverseForce();
                return;
            }

            // 速度がある場合、L2はブレーキとして使う
            ApplyBrake(brakeDeceleration);
            return;
        }

        // R2もL2も押していない場合は自然減速
        isReversing = false;
        ApplyBrake(deceleration);
    }

    private float GetForwardAcceleration(float horizontalSpeed)
    {
        // 速度が指定値未満なら低速用加速度を使う
        if (GetForwardSpeed() < accelerationSwitchSpeed)
        {
            return lowSpeedAcceleration;
        }

        // 速度が指定値以上なら高速用加速度を使う
        return highSpeedAcceleration;
    }
    private float GetForwardSpeed()
    {
        // Rigidbodyの現在速度を取得
        Vector3 velocity = rb.linearVelocity;

        // Y方向を無視して、水平方向の速度だけ使う
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        // プレイヤーの正面方向
        Vector3 forward = transform.forward;

        // Y方向を無視して、水平な前方向にする
        forward.y = 0f;
        forward.Normalize();

        // プレイヤーの正面方向に対する速度を取得
        float forwardSpeed = Vector3.Dot(horizontalVelocity, forward);

        return forwardSpeed;
    }

    private void ApplyReverseForce()
    {
        // 後ろ方向へ加速する
        Vector3 reverseForce = -transform.forward * reverseAcceleration * l2Value;

        rb.AddForce(reverseForce, ForceMode.Acceleration);
    }

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

        // ブレーキで速度が反転しないように、止まりそうなら直接0にする
        float speedDrop = brakePower * Time.fixedDeltaTime;

        if (speed <= speedDrop)
        {
            StopHorizontalMovement();
            return;
        }

        // 現在の移動方向と逆向きに減速力を加える
        Vector3 brakeForce = -horizontalVelocity.normalized * brakePower;

        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

    private void ApplyTurnTorque()
    {
        if(Mathf.Abs(stickX) <= stickDeadZone)
        {
            return;
        }

        // 左スティックの左右入力に応じてY軸方向へ回転加速度を加える
        Vector3 torque = Vector3.up * stickX * turnAcceleration;

        rb.AddTorque(torque, ForceMode.Acceleration);
    }

    private void ApplyTurnDeceleration()
    {
        // 左スティックに入力がある場合は復位しない
        if (Mathf.Abs(stickX) > stickDeadZone)
        {
            return;
        }

        Vector3 angularVelocity = rb.angularVelocity;

        // Y軸の回転速度を少しずつ0に近づける
        float newY = Mathf.MoveTowards(
            angularVelocity.y,
            0f,
            turnResetSpeed * Time.fixedDeltaTime
        );

        // X/Zは固定されている前提だが、念のため0にする
        rb.angularVelocity = new Vector3(0f, newY, 0f);
    }
    private void LimitHorizontalSpeed()
    {
        Vector3 velocity = rb.linearVelocity;

        // 水平方向の速度だけを取得
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        if (horizontalVelocity.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // 後退状態のときだけ後退速度上限を使う
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

    private void LimitAngularSpeed()
    {
        Vector3 angularVelocity = rb.angularVelocity;

        // Y軸回転速度を最大値以内に制限する
        float limitedY = Mathf.Clamp(
            angularVelocity.y,
            -maxAngularSpeed,
            maxAngularSpeed
        );

        rb.angularVelocity = new Vector3(
            angularVelocity.x,
            limitedY,
            angularVelocity.z
        );
    }

    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.linearVelocity;

        // Y方向の速度は残して、水平方向だけ止める
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}