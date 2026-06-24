using System;
using UnityEngine;

public enum PlayerMovementControlScheme
{
    Triggers,
    FaceButtons
}

[Serializable]
public sealed class PlayerMovementControlPreset
{
    [SerializeField] private string displayName = "LT/RT";
    [SerializeField] private PlayerMovementControlScheme controlScheme = PlayerMovementControlScheme.Triggers;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Control" : displayName;
    public PlayerMovementControlScheme ControlScheme => controlScheme;

    public PlayerMovementControlPreset()
    {
    }

    private PlayerMovementControlPreset(string displayName, PlayerMovementControlScheme controlScheme)
    {
        this.displayName = displayName;
        this.controlScheme = controlScheme;
    }

    public static PlayerMovementControlPreset CreateTriggers()
    {
        return new PlayerMovementControlPreset("LT/RT", PlayerMovementControlScheme.Triggers);
    }

    public static PlayerMovementControlPreset CreateFaceButtons()
    {
        return new PlayerMovementControlPreset("A/B", PlayerMovementControlScheme.FaceButtons);
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Control";
        }
    }
}
