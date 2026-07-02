using System;
using UnityEngine;

// PlayerManager が入力をどう読むかを表します。
// Triggers は RT/LT、FaceButtons は A/B 相当のボタンを加速・ブレーキに使います。
public enum PlayerMovementControlScheme
{
    Triggers,
    FaceButtons
}

[Serializable]
// スタートメニューの「MOVE CONTROL」に表示する入力方式プリセットです。
// 役割:
// - 表示名と PlayerMovementControlScheme をセットで保持します。
// 接続:
// - StartMenuManager が選択値を PlayerMovementPresetApplier に渡し、シーン読み込み後に PlayerManager.ApplyControlScheme へ反映します。
public sealed class PlayerMovementControlPreset
{
    [SerializeField] private string displayName = "LT/RT";
    [SerializeField] private PlayerMovementControlScheme controlScheme = PlayerMovementControlScheme.Triggers;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Control" : displayName;
    public PlayerMovementControlScheme ControlScheme => controlScheme;

    // Unity のシリアライズ用に必要な空コンストラクタです。
    public PlayerMovementControlPreset()
    {
    }

    // 表示名と入力方式を指定してプリセットを作る内部用コンストラクタです。
    private PlayerMovementControlPreset(string displayName, PlayerMovementControlScheme controlScheme)
    {
        this.displayName = displayName;
        this.controlScheme = controlScheme;
    }

    // RT/LT トリガー操作用の標準プリセットを作ります。
    public static PlayerMovementControlPreset CreateTriggers()
    {
        return new PlayerMovementControlPreset("LT/RT", PlayerMovementControlScheme.Triggers);
    }

    // A/B ボタン操作用の標準プリセットを作ります。
    public static PlayerMovementControlPreset CreateFaceButtons()
    {
        return new PlayerMovementControlPreset("A/B", PlayerMovementControlScheme.FaceButtons);
    }

    // 表示名が空のとき、メニューで壊れない既定名へ補正します。
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Control";
        }
    }
}
