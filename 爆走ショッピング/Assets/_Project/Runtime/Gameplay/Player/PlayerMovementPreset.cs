using System;
using UnityEngine;

/// <summary>移動パラメータ一式に表示名を付けた、選択用プリセットです。</summary>
[Serializable]
public sealed class PlayerMovementPreset
{
    [SerializeField] private string displayName = "Classic";
    [SerializeField] private PlayerMovementSettings settings = PlayerMovementSettings.CreateClassic();

    // 表示名を返し、未設定ならこのデータで定めた代替名を使用します。
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Preset" : displayName;
    // このプリセットが保持している移動設定を公開します。
    public PlayerMovementSettings Settings => settings;

    // 引数なしでは既定値を使い、引数ありでは名前と移動設定を受け取って検証します。
    public PlayerMovementPreset()
    {
    }

    // 引数なしでは既定値を使い、引数ありでは名前と移動設定を受け取って検証します。
    private PlayerMovementPreset(string displayName, PlayerMovementSettings settings)
    {
        this.displayName = displayName;
        this.settings = settings;
        Validate();
    }

    // 標準操作用の既定パラメータを持つ設定、または選択用プリセットを作ります。
    public static PlayerMovementPreset CreateClassic()
    {
        return new PlayerMovementPreset("Classic", PlayerMovementSettings.CreateClassic());
    }

    // 上級操作用のパラメータを持つ設定、または選択用プリセットを作ります。
    public static PlayerMovementPreset CreateHard()
    {
        return new PlayerMovementPreset("Hard", PlayerMovementSettings.CreateHard());
    }

    // 設定を検証し、元のプリセットを変更せずに使える複製を返します。
    public PlayerMovementSettings CreateSettingsCopy()
    {
        Validate();
        return settings.Clone();
    }

    // 表示名・設定参照・数値のうち、このデータが持つ値を有効な状態へ補正します。
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

/// <summary>PlayerManager が受け取る移動・旋回パラメータのデータ本体です。</summary>
[Serializable]
public sealed class PlayerMovementSettings
{
    // 前進・後退の加減速と速度上限、入力・停止しきい値です。
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

    // 低速域の前進加速力を返します。
    public float LowSpeedAcceleration => lowSpeedAcceleration;
    // 高速域の前進加速力を返します。
    public float HighSpeedAcceleration => highSpeedAcceleration;
    // 低速用から高速用の加速力へ切り替える前進速度を返します。
    public float AccelerationSwitchSpeed => accelerationSwitchSpeed;
    // 後退時の加速力を返します。
    public float ReverseAcceleration => reverseAcceleration;
    // 通常減速と横滑り抑制に使用する減速力を返します。
    public float Deceleration => deceleration;
    // ブレーキ入力時の減速力を返します。
    public float BrakeDeceleration => brakeDeceleration;
    // 通常走行時の水平速度上限を返します。
    public float MaxSpeed => maxSpeed;
    // 後退中の水平速度上限を返します。
    public float MaxReverseSpeed => maxReverseSpeed;
    // 加速・ブレーキ入力を有効とするしきい値を返します。
    public float TriggerDeadZone => triggerDeadZone;
    // 速度を停止と見なすしきい値を返します。
    public float StopThreshold => stopThreshold;
    // 旋回入力がないときの回転減衰の強さを返します。
    public float TurnResetSpeed => turnResetSpeed;
    // 旋回入力を無視するスティック範囲を返します。
    public float StickDeadZone => stickDeadZone;
    // 旋回入力に掛ける回転力を返します。
    public float TurnAcceleration => turnAcceleration;
    // 水平旋回の角速度上限を返します。
    public float MaxAngularSpeed => maxAngularSpeed;

    // 標準操作用の既定パラメータを持つ設定、または選択用プリセットを作ります。
    public static PlayerMovementSettings CreateClassic()
    {
        return new PlayerMovementSettings();
    }

    // 上級操作用のパラメータを持つ設定、または選択用プリセットを作ります。
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

    // 全ての移動・旋回パラメータを、新しい設定オブジェクトへ複製します。
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

    // 表示名・設定参照・数値のうち、このデータが持つ値を有効な状態へ補正します。
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
