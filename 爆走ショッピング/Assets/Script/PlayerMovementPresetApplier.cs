using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerMovementPresetApplier
{
    private static PlayerMovementSettings pendingSettings;
    private static bool hasPendingSettings;
    private static PlayerMovementControlScheme pendingControlScheme;
    private static bool hasPendingControlScheme;

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
    }

    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme)
    {
        pendingControlScheme = controlScheme;
        hasPendingControlScheme = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((!hasPendingSettings || pendingSettings == null) && !hasPendingControlScheme)
        {
            return;
        }

        PlayerManager[] players = Object.FindObjectsByType<PlayerManager>(FindObjectsSortMode.None);

        if (players.Length == 0)
        {
            return;
        }

        foreach (PlayerManager player in players)
        {
            if (hasPendingSettings && pendingSettings != null)
            {
                player.ApplyMovementSettings(pendingSettings);
            }

            if (hasPendingControlScheme)
            {
                player.ApplyControlScheme(pendingControlScheme);
            }
        }

        pendingSettings = null;
        hasPendingSettings = false;
        hasPendingControlScheme = false;
    }
}
