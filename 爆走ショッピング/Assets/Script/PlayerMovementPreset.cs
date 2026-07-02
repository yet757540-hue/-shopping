using System;
using UnityEngine;

[Serializable]
// スタートメニューの「MOVE PRESET」に表示する移動性能プリセットです。
// 役割:
// - 表示名と PlayerMovementSettings をセットで保持します。
// - Classic / Hard の標準プリセットをコード上で作れます。
// 接続:
// - StartMenuManager が選択し、PlayerMovementPresetApplier 経由で PlayerManager.ApplyMovementSettings に渡します。
// 読むときの要点:
// - CreateSettingsCopy は元プリセットを書き換えないためのコピーを返します。
public sealed class PlayerMovementPreset
{
    [SerializeField] private string displayName = "Classic";
    [SerializeField] private PlayerMovementSettings settings = PlayerMovementSettings.CreateClassic();

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Preset" : displayName;
    public PlayerMovementSettings Settings => settings;

    // Unity のシリアライズ用に必要な空コンストラクタです。
    public PlayerMovementPreset()
    {
    }

    // 表示名と設定値を指定してプリセットを作る内部用コンストラクタです。
    private PlayerMovementPreset(string displayName, PlayerMovementSettings settings)
    {
        this.displayName = displayName;
        this.settings = settings;
        Validate();
    }

    // 標準的な操作感の移動プリセットを作ります。
    public static PlayerMovementPreset CreateClassic()
    {
        return new PlayerMovementPreset("Classic", PlayerMovementSettings.CreateClassic());
    }

    // 速度や旋回が強めの移動プリセットを作ります。
    public static PlayerMovementPreset CreateHard()
    {
        return new PlayerMovementPreset("Hard", PlayerMovementSettings.CreateHard());
    }

    // 元のプリセットを変更しないよう、設定値のコピーを返します。
    public PlayerMovementSettings CreateSettingsCopy()
    {
        Validate();
        return settings.Clone();
    }

    // 表示名と settings の欠落を補い、設定値の範囲も検証します。
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
// PlayerManager に反映される実際の移動パラメータ群です。
// 役割:
// - 加速、減速、最高速、旋回、入力デッドゾーンなどを 1 セットにまとめます。
// - Validate で Inspector から不正な値が入っても最低限の範囲に補正します。
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

    // Classic 用の既定値セットを作ります。
    public static PlayerMovementSettings CreateClassic()
    {
        return new PlayerMovementSettings();
    }

    // Hard 用に数値を調整した設定セットを作ります。
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

    // PlayerManager へ渡しても元データが変わらないよう、全フィールドを複製します。
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

    // 速度、加速度、デッドゾーンなどの不正値を補正します。
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
