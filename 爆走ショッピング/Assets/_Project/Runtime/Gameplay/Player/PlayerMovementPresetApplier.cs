using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerMovementPresetApplier
{
    private static PlayerMovementSettings pendingSettings;
    private static bool hasPendingSettings;
    private static PlayerMovementSettings retainedSettings;
    private static bool hasRetainedSettings;
    private static int retainedSettingsIndex;
    private static bool hasRetainedSettingsIndex;
    private static PlayerMovementControlScheme pendingControlScheme;
    private static bool hasPendingControlScheme;
    private static PlayerMovementControlScheme retainedControlScheme;
    private static bool hasRetainedControlScheme;
    private static int retainedControlSchemeIndex;
    private static bool hasRetainedControlSchemeIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    public static void SetPendingSettings(PlayerMovementSettings settings)
    {
        pendingSettings = settings?.Clone();
        hasPendingSettings = pendingSettings != null;

        if (pendingSettings != null)
        {
            retainedSettings = pendingSettings.Clone();
            hasRetainedSettings = true;
        }
    }

    public static void SetPendingSettings(PlayerMovementSettings settings, int selectedPresetIndex)
    {
        SetPendingSettings(settings);

        if (!hasPendingSettings)
        {
            return;
        }

        retainedSettingsIndex = Mathf.Max(0, selectedPresetIndex);
        hasRetainedSettingsIndex = true;
    }

    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme)
    {
        pendingControlScheme = controlScheme;
        hasPendingControlScheme = true;
        retainedControlScheme = controlScheme;
        hasRetainedControlScheme = true;
    }

    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme, int selectedControlSchemeIndex)
    {
        SetPendingControlScheme(controlScheme);
        retainedControlSchemeIndex = Mathf.Max(0, selectedControlSchemeIndex);
        hasRetainedControlSchemeIndex = true;
    }

    public static bool TryGetRetainedSettingsIndex(out int selectedPresetIndex)
    {
        selectedPresetIndex = retainedSettingsIndex;
        return hasRetainedSettingsIndex;
    }

    public static bool TryGetRetainedControlSchemeIndex(out int selectedControlSchemeIndex)
    {
        selectedControlSchemeIndex = retainedControlSchemeIndex;
        return hasRetainedControlSchemeIndex;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerMovementSettings settingsToApply = hasPendingSettings && pendingSettings != null
            ? pendingSettings
            : hasRetainedSettings
                ? retainedSettings
                : null;
        bool shouldApplyControlScheme = hasPendingControlScheme || hasRetainedControlScheme;
        PlayerMovementControlScheme controlSchemeToApply = hasPendingControlScheme
            ? pendingControlScheme
            : retainedControlScheme;

        if (settingsToApply == null && !shouldApplyControlScheme)
        {
            return;
        }

        PlayerManager[] players = Object.FindObjectsByType<PlayerManager>();

        if (players.Length == 0)
        {
            return;
        }

        foreach (PlayerManager player in players)
        {
            if (settingsToApply != null)
            {
                player.ApplyMovementSettings(settingsToApply);
            }

            if (shouldApplyControlScheme)
            {
                player.ApplyControlScheme(controlSchemeToApply);
            }
        }

        pendingSettings = null;
        hasPendingSettings = false;
        hasPendingControlScheme = false;
    }
}
