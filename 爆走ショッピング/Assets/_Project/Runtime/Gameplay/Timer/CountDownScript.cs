using System.Collections;
using System.Xml.Serialization;
using TMPro;
using UnityEditor.Search;
using UnityEngine;

public class CountDownScript : MonoBehaviour
{
    //ゲームシーン最初のカウントダウンのスクリプトです
    [Header("カウントの速度")]
    [SerializeField]private int CountSpeed = 1;
    [Header("文字を入れるテキスト")]
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject countGameObject;

    private double CountTime = 3;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        Time.timeScale = 0f;
    }
    void Start()
    {
        StartCoroutine(StartCount(CountSpeed));
    }
    public IEnumerator StartCount(float duration)
    {
        //3
        countText.text = CountTime.ToString();
        yield return new WaitForSecondsRealtime(duration);
        //2
        CountTime--;
        countText.text = CountTime.ToString();
        yield return new WaitForSecondsRealtime(duration);
        //1
        CountTime--;
        countText.text = CountTime.ToString();
        yield return new WaitForSecondsRealtime(duration);
        //GO
        countText.text = "GO!";
        yield return new WaitForSecondsRealtime(duration/2);
        Time.timeScale = 1f;
        countGameObject.SetActive(false);
    }
}
