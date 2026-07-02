using UnityEngine;
using UnityEngine.SceneManagement;

// スタートメニューで選んだ移動設定を、ゲームシーン読み込み後の PlayerManager へ反映する静的中継役です。
// 役割:
// - シーン切り替え前に選択された PlayerMovementSettings と ControlScheme を一時保存します。
// - sceneLoaded イベントで新しいシーン内の PlayerManager を探して設定を適用します。
// 接続:
// - StartMenuManager.QueueSelectedMovementPreset が SetPendingSettings / SetPendingControlScheme を呼びます。
// - PlayerManager.ApplyMovementSettings と ApplyControlScheme が最終的な反映先です。
// 読むときの要点:
// - RuntimeInitializeOnLoadMethod により、シーン読み込みイベントの購読は自動で設定されます。
public static class PlayerMovementPresetApplier
{
    private static PlayerMovementSettings pendingSettings;
    private static bool hasPendingSettings;
    private static PlayerMovementControlScheme pendingControlScheme;
    private static bool hasPendingControlScheme;

    // シーン読み込み完了イベントを登録し、二重登録を避けるため一度解除してから追加します。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // 次に読み込まれるシーンへ適用する移動設定を保存します。
    public static void SetPendingSettings(PlayerMovementSettings settings)
    {
        pendingSettings = settings?.Clone();
        hasPendingSettings = pendingSettings != null;
    }

    // 次に読み込まれるシーンへ適用する操作方式を保存します。
    public static void SetPendingControlScheme(PlayerMovementControlScheme controlScheme)
    {
        pendingControlScheme = controlScheme;
        hasPendingControlScheme = true;
    }

    // シーン読み込み後、保存済み設定があればすべての PlayerManager へ反映します。
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if ((!hasPendingSettings || pendingSettings == null) && !hasPendingControlScheme)
        {
            return;
        }

        // シーン内に複数 PlayerManager があっても、選択された設定を全員へ反映します。
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
