using System;
using UnityEngine;

// 走行操作に使うゲームパッドのボタン配置です。
public enum PlayerMovementControlScheme
{
    Triggers,
    FaceButtons
}

/// <summary>Inspector で選択できる入力方式の名前と値の組です。</summary>
[Serializable]
public sealed class PlayerMovementControlPreset
{
    [SerializeField] private string displayName = "LT/RT";
    [SerializeField] private PlayerMovementControlScheme controlScheme = PlayerMovementControlScheme.FaceButtons;

    // 表示名を返し、未設定ならこのデータで定めた代替名を使用します。
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Control" : displayName;
    // 加速とブレーキに使用するボタン配置を返します。
    public PlayerMovementControlScheme ControlScheme => controlScheme;

    // 引数なしでは既定値を使い、引数ありでは表示名と操作方式を保持します。
    public PlayerMovementControlPreset()
    {
    }

    // 引数なしでは既定値を使い、引数ありでは表示名と操作方式を保持します。
    private PlayerMovementControlPreset(string displayName, PlayerMovementControlScheme controlScheme)
    {
        this.displayName = displayName;
        this.controlScheme = controlScheme;
    }

    // 左右トリガーで加速・ブレーキを操作するプリセットを作ります。
    public static PlayerMovementControlPreset CreateTriggers()
    {
        return new PlayerMovementControlPreset("LT/RT", PlayerMovementControlScheme.Triggers);
    }

    // フェイスボタンで加速・ブレーキを操作するプリセットを作ります。
    public static PlayerMovementControlPreset CreateFaceButtons()
    {
        return new PlayerMovementControlPreset("A/B", PlayerMovementControlScheme.FaceButtons);
    }

    // 両メニューで同じ候補を使います。既存設定を保ち、不足分と null のみ補います。
    public static PlayerMovementControlPreset[] EnsureDefaults(PlayerMovementControlPreset[] presets)
    {
        if (presets == null || presets.Length == 0)
        {
            presets = new[] { CreateTriggers(), CreateFaceButtons() };
        }
        else if (presets.Length == 1)
        {
            presets = new[] { presets[0] ?? CreateTriggers(), CreateFaceButtons() };
        }

        for (int i = 0; i < presets.Length; i++)
        {
            if (presets[i] == null)
            {
                presets[i] = i == 1 ? CreateFaceButtons() : CreateTriggers();
            }

            presets[i].Validate();
        }

        return presets;
    }

    // 表示名が空の場合に、メニュー表示用の既定名を設定します。
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Control";
        }
    }
}
