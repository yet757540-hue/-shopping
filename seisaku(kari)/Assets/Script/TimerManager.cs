using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private TMP_Text timerText;          // タイマー表示用テキスト
    [SerializeField] private RectTransform timerRect;     // タイマーUIのRectTransform

    [Header("表示位置設定")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 420f);     // 画面上部の位置
    [SerializeField] private Vector2 centerPosition = new Vector2(0f, 0f);   // 画面中央の位置

    [Header("演出設定")]
    [SerializeField] private float moveDuration = 0.35f;  // 中央へ移動する時間
    [SerializeField] private float centerHoldTime = 1.0f; // 中央に表示しておく時間
    [SerializeField] private bool hideAfterStop = false;  // 停止演出後に非表示にするか
    

    private float elapsedTime = 0f;
    private bool isRunning = false;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TMP_Text>();
        }

        if (timerRect == null && timerText != null)
        {
            timerRect = timerText.GetComponent<RectTransform>();
        }

        ResetTimerView();
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    public void StartTimer()
    {
        // 前回の演出が残っている場合は止める
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        // タイマーをリセット
        elapsedTime = 0f;
        isRunning = true;

        // 画面上部に戻す
        if (timerRect != null)
        {
            timerRect.anchoredPosition = topPosition;
        }

        // 表示する
        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        UpdateTimerText();

        Debug.Log("[TimerManager] タイマー開始");
    }

    public void StopTimer()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        UpdateTimerText();

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(MoveTimerToCenter());

        Debug.Log("[TimerManager] タイマー停止: " + elapsedTime);
    }

    public void ResetTimer()
    {
        isRunning = false;
        elapsedTime = 0f;
        ResetTimerView();

        Debug.Log("[TimerManager] タイマーリセット");
    }

    private void ResetTimerView()
    {
        if (timerRect != null)
        {
            timerRect.anchoredPosition = topPosition;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);

        timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    private IEnumerator MoveTimerToCenter()
    {
        if (timerRect == null)
        {
            yield break;
        }

        Vector2 startPosition = timerRect.anchoredPosition;
        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;

            float t = timer / moveDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            timerRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                centerPosition,
                t
            );

            yield return null;
        }

        timerRect.anchoredPosition = centerPosition;

        yield return new WaitForSeconds(centerHoldTime);

        if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        moveCoroutine = null;
    }
    [ContextMenu("Test Start Timer")]
    private void TestStartTimer()
    {
        StartTimer();
    }
    [ContextMenu("Test Stop Timer")]
    private void TestStopTimer()
    {
        StopTimer();
    }
}