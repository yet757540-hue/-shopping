using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GameRestartManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private bool restartActiveScene = true;
    [SerializeField] private string gameSceneName = "idou";
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private GameTimePauseManager pauseManager;

    [Header("Events")]
    [SerializeField] private UnityEvent beforeRestart = new UnityEvent();
    [SerializeField] private UnityEvent beforeReturnToStartMenu = new UnityEvent();
    [SerializeField] private UnityEvent restartFailed = new UnityEvent();

    public void RestartGame()
    {
        string sceneName = restartActiveScene ? SceneManager.GetActiveScene().name : gameSceneName;
        RestartScene(sceneName);
    }

    public void RestartCurrentScene()
    {
        RestartScene(SceneManager.GetActiveScene().name);
    }

    public void RestartConfiguredGameScene()
    {
        RestartScene(gameSceneName);
    }

    public void SetGameSceneName(string sceneName)
    {
        gameSceneName = sceneName;
    }

    public void ReturnToStartMenu()
    {
        if (string.IsNullOrWhiteSpace(startMenuSceneName))
        {
            Debug.LogError("[GameRestartManager] Start menu scene name is empty.", this);
            restartFailed.Invoke();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(startMenuSceneName))
        {
            Debug.LogError("[GameRestartManager] Scene is not available in Build Settings: " + startMenuSceneName, this);
            restartFailed.Invoke();
            return;
        }

        ResetTimeBeforeSceneLoad();
        beforeReturnToStartMenu.Invoke();
        SceneManager.LoadScene(startMenuSceneName, LoadSceneMode.Single);
    }

    private void RestartScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[GameRestartManager] Restart scene name is empty.", this);
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
        beforeRestart.Invoke();
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void ResetTimeBeforeSceneLoad()
    {
        if (pauseManager == null)
        {
            pauseManager = FindAnyObjectByType<GameTimePauseManager>();
        }

        if (pauseManager != null)
        {
            pauseManager.ForceResumeGame(1f);
            return;
        }

        Time.timeScale = 1f;
    }

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
