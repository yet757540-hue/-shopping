using TMPro;
using UnityEngine;

public class ResultScoreManager : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    

    private void Start()
    {
        int ResultScore = PlayerPrefs.GetInt("score", 0);

        Debug.Log(ResultScore);
        scoreText.text ="スコア　"+ ResultScore.ToString();
    }
}