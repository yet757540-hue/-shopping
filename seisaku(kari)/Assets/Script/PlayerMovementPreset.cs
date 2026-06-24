using System;
using UnityEngine;

[Serializable]
public sealed class PlayerMovementPreset
{
    [SerializeField] private string displayName = "Classic";
    [SerializeField] private PlayerMovementSettings settings = PlayerMovementSettings.CreateClassic();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Preset" : displayName;
    public PlayerMovementSettings Settings => settings;

    public PlayerMovementPreset()
    {
    }

    private PlayerMovementPreset(string displayName, PlayerMovementSettings settings)
    {
        this.displayName = displayName;
        this.settings = settings;
        Validate();
    }

    public static PlayerMovementPreset CreateClassic()
    {
        return new PlayerMovementPreset("Classic", PlayerMovementSettings.CreateClassic());
    }

    public static PlayerMovementPreset CreateHard()
    {
        return new PlayerMovementPreset("Hard", PlayerMovementSettings.CreateHard());
    }

    public PlayerMovementSettings CreateSettingsCopy()
    {
        Validate();
        return settings.Clone();
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Preset";
        }

        if (settings == null)
        {
            settings = PlayerMovementSettings.CreateClassic();
        }

        settings.Validate();
    }
}

[Serializable]
public sealed class PlayerMovementSettings
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

    public float LowSpeedAcceleration => lowSpeedAcceleration;
    public float HighSpeedAcceleration => highSpeedAcceleration;
    public float AccelerationSwitchSpeed => accelerationSwitchSpeed;
    public float ReverseAcceleration => reverseAcceleration;
    public float Deceleration => deceleration;
    public float BrakeDeceleration => brakeDeceleration;
    public float MaxSpeed => maxSpeed;
    public float MaxReverseSpeed => maxReverseSpeed;
    public float TriggerDeadZone => triggerDeadZone;
    public float StopThreshold => stopThreshold;
    public float TurnResetSpeed => turnResetSpeed;
    public float StickDeadZone => stickDeadZone;
    public float TurnAcceleration => turnAcceleration;
    public float MaxAngularSpeed => maxAngularSpeed;

    public static PlayerMovementSettings CreateClassic()
    {
        return new PlayerMovementSettings();
    }

    public static PlayerMovementSettings CreateHard()
    {
        return new PlayerMovementSettings
        {
            lowSpeedAcceleration = 32f,
            highSpeedAcceleration = 14f,
            accelerationSwitchSpeed = 18f,
            reverseAcceleration = 10f,
            deceleration = 18f,
            brakeDeceleration = 32f,
            maxSpeed = 38f,
            maxReverseSpeed = 5f,
            triggerDeadZone = 0.14f,
            stopThreshold = 0.08f,
            turnResetSpeed = 6f,
            stickDeadZone = 0.08f,
            turnAcceleration = 10f,
            maxAngularSpeed = 4f
        };
    }

    public PlayerMovementSettings Clone()
    {
        return new PlayerMovementSettings
        {
            lowSpeedAcceleration = lowSpeedAcceleration,
            highSpeedAcceleration = highSpeedAcceleration,
            accelerationSwitchSpeed = accelerationSwitchSpeed,
            reverseAcceleration = reverseAcceleration,
            deceleration = deceleration,
            brakeDeceleration = brakeDeceleration,
            maxSpeed = maxSpeed,
            maxReverseSpeed = maxReverseSpeed,
            triggerDeadZone = triggerDeadZone,
            stopThreshold = stopThreshold,
            turnResetSpeed = turnResetSpeed,
            stickDeadZone = stickDeadZone,
            turnAcceleration = turnAcceleration,
            maxAngularSpeed = maxAngularSpeed
        };
    }

    public void Validate()
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
