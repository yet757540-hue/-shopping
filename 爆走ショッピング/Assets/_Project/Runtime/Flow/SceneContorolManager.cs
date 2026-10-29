
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class SceneController : MonoBehaviour
{
    // Singletonのインスタンスを保存
    public static SceneController Instance { get; private set; }

    private void Awake()
    {
        // インスタンスが存在しない場合
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        // すでにインスタンスが存在する場合
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 指定したシーンへの移動
    public void MoveScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            throw new System.Exception(
                "シーンが読み込みできません: " + sceneName
            );
        }

        SceneManager.LoadScene(sceneName);
    }

    // 現在のシーンを再読み込み
    public void resetScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }
}
