using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
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
    public float AccelerationSwitchSpeed => accelerationSwitchSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.maxAngularVelocity = maxAngularSpeed;
    }

    private void Update()
    {
        if (readGamepadDirectly)
        {
            ReadGamepadInput();
        }
    }

    private void FixedUpdate()
    {
        ApplyDriveInput();
        ApplyTurnTorque();
        ApplyTurnDeceleration();
        LimitHorizontalSpeed();
        LimitAngularSpeed();
    }

    public void SetMoveInput(float steer, float accelerate, float brake)
    {
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        accelerateInput = Mathf.Clamp01(accelerate);
        brakeInput = Mathf.Clamp01(brake);
    }

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

    public void MultiplyAccelerationSwitchSpeed(float multiplier)
    {
        accelerationSwitchSpeed = Mathf.Max(0f, accelerationSwitchSpeed * Mathf.Max(0f, multiplier));
    }

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

    public void ApplyControlScheme(PlayerMovementControlScheme scheme)
    {
        controlScheme = scheme;
        SetMoveInput(steerInput, 0f, 0f);
        isReversing = false;
    }

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
            ApplyLateralBrake(deceleration * loadDecelerationMultiplier);
            return;
        }

        if (isPressingBrake)
        {
            if (isReversing)
            {
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

    private float GetForwardAcceleration(float forwardSpeed)
    {
        if (forwardSpeed < accelerationSwitchSpeed)
        {
            return lowSpeedAcceleration * loadAccelerationMultiplier;
        }

        return highSpeedAcceleration * loadAccelerationMultiplier;
    }

    private float GetForwardSpeed(Vector3 horizontalVelocity)
    {
        return Vector3.Dot(horizontalVelocity, GetHorizontalForward());
    }

    private Vector3 GetLateralVelocity(Vector3 horizontalVelocity)
    {
        Vector3 forward = GetHorizontalForward();
        Vector3 forwardVelocity = forward * Vector3.Dot(horizontalVelocity, forward);
        return horizontalVelocity - forwardVelocity;
    }

    private Vector3 GetHorizontalForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();
        return forward;
    }

    private void ApplyReverseForce()
    {
        Vector3 reverseForce = -transform.forward * reverseAcceleration * loadAccelerationMultiplier * brakeInput;
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

        float speedDrop = brakePower * Time.fixedDeltaTime;

        if (speed <= speedDrop)
        {
            StopHorizontalMovement();
            return;
        }

        Vector3 brakeForce = -horizontalVelocity.normalized * brakePower;
        rb.AddForce(brakeForce, ForceMode.Acceleration);
    }

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

    private void ApplyTurnTorque()
    {
        if (Mathf.Abs(steerInput) <= stickDeadZone)
        {
            return;
        }

        Vector3 torque = Vector3.up * steerInput * turnAcceleration * loadTurnAccelerationMultiplier;
        rb.AddTorque(torque, ForceMode.Acceleration);
    }

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

    private Vector3 GetHorizontalVelocity()
    {
        Vector3 velocity = rb.linearVelocity;
        return new Vector3(velocity.x, 0f, velocity.z);
    }

    private void StopHorizontalMovement()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }

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
