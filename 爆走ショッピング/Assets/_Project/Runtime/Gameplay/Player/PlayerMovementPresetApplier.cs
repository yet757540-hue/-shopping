using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン遷移をまたいで選択済みの移動設定を保持し、次の PlayerManager へ適用します。
/// </summary>
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

    // シーン読み込みイベントは一度だけ登録し、重複適用を防ぎます。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // 移動設定を複製して次シーン用に保持します。選択番号の引数があれば番号も記録します。
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

    // 移動設定を複製して次シーン用に保持します。選択番号の引数があれば番号も記録します。
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

    // 操作方式を次シーン用と再利用用に保持し、引数があれば選択番号も記録します。
    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme)
    {
        pendingControlScheme = controlScheme;
        hasPendingControlScheme = true;
        retainedControlScheme = controlScheme;
        hasRetainedControlScheme = true;
    }

    // 操作方式を次シーン用と再利用用に保持し、引数があれば選択番号も記録します。
    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme, int selectedControlSchemeIndex)
    {
        SetPendingControlScheme(controlScheme);
        retainedControlSchemeIndex = Mathf.Max(0, selectedControlSchemeIndex);
        hasRetainedControlSchemeIndex = true;
    }

    // 保存された移動プリセット番号と、保存済みかどうかを返します。
    public static bool TryGetRetainedSettingsIndex(out int selectedPresetIndex)
    {
        selectedPresetIndex = retainedSettingsIndex;
        return hasRetainedSettingsIndex;
    }

    // 保存された操作プリセット番号と、保存済みかどうかを返します。
    public static bool TryGetRetainedControlSchemeIndex(out int selectedControlSchemeIndex)
    {
        selectedControlSchemeIndex = retainedControlSchemeIndex;
        return hasRetainedControlSchemeIndex;
    }

    // 保留中の設定を優先してプレイヤーへ適用し、適用できた場合に保留状態だけを解除します。
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 新しい選択があれば優先し、なければ直前のシーンで保持した設定を使います。
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

        // プレイヤーに適用できた後だけ保留状態を解除し、再開用の保持設定は残します。
        pendingSettings = null;
        hasPendingSettings = false;
        hasPendingControlScheme = false;
    }
}
