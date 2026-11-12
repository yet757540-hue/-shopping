using UnityEngine;

public class ScoreCarryManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private ScoreboardManager scoreboardManager;

    public static ScoreCarryManager Instance { get; private set; }

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
    
    public void SaveScore()
    {
        //score保存場所
        PlayerPrefs.SetInt("score", scoreboardManager.TotalScore);
    }

}
