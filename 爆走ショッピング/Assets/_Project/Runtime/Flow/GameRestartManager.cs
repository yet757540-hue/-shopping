using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>シーン再開・リスタート・タイトル復帰を一か所にまとめます。</summary>
[DisallowMultipleComponent]
public class GameRestartManager : MonoBehaviour
{
    // Inspector で選べる遷移先です。実際の存在確認は遷移直前に行います。
    [Header("シーン")]
    [SerializeField] private bool restartActiveScene = true;
    [SerializeField] private string gameSceneName = "idou";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private GameTimePauseManager pauseManager;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
    [SerializeField] private UnityEvent beforeRestart = new UnityEvent();
    [SerializeField] private UnityEvent beforeReturnToStartMenu = new UnityEvent();
    [SerializeField] private UnityEvent restartFailed = new UnityEvent();

    // シーン遷移前に時間を復帰させるための停止管理を受け取ります。
    public void Initialize(GameTimePauseManager configuredPauseManager)
    {
        pauseManager = configuredPauseManager;
    }

    // 設定に応じて、現在のシーンまたは指定されたゲームシーンを再読み込みします。
    public void RestartGame()
    {
        string sceneName = restartActiveScene ? SceneManager.GetActiveScene().name : gameSceneName;
        RestartScene(sceneName);
    }

    // 現在表示中のシーンを再読み込みします。
    public void RestartCurrentScene()
    {
        RestartScene(SceneManager.GetActiveScene().name);
    }

    // Inspector で指定したゲームシーンを再読み込みします。
    public void RestartConfiguredGameScene()
    {
        RestartScene(gameSceneName);
    }

    // 次回の再開で使用するゲームシーン名を変更します。
    public void SetGameSceneName(string sceneName)
    {
        gameSceneName = sceneName;
    }

    // タイトル復帰イベントを通知してから、指定したタイトルシーンへ移動します。
    public void ReturnToStartMenu()
    {
        LoadScene(startMenuSceneName, "Start menu", beforeReturnToStartMenu);
    }

    // 再開用イベントを、共通のシーン読み込み処理へ渡します。
    private void RestartScene(string sceneName)
    {
        LoadScene(sceneName, "Restart", beforeRestart);
    }

    // すべてのシーン遷移で行う検証と timeScale の復帰を共通化します。
    private void LoadScene(string sceneName, string actionName, UnityEvent beforeLoad)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[GameRestartManager] " + actionName + " scene name is empty.", this);
            restartFailed.Invoke();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[GameRestartManager] Scene is not available in Build Settings: " + sceneName, this);
            restartFailed.Invoke();
            return;
        }

        ResetTimeBeforeSceneLoad();
        beforeLoad.Invoke();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // 停止管理があれば依頼も解除し、なければ時間倍率だけを通常に戻します。
    private void ResetTimeBeforeSceneLoad()
    {
        if (pauseManager != null)
        {
            pauseManager.ForceResumeGame(1f);
            return;
        }

        Time.timeScale = 1f;
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            gameSceneName = "idou";
        }

        if (string.IsNullOrWhiteSpace(startMenuSceneName))
        {
            startMenuSceneName = "StartMenu";
        }
    }
}
