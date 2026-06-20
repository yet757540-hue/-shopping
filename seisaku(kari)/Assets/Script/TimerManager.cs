using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerRect;

    [Header("Display Positions")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 420f);
    [SerializeField] private Vector2 centerPosition = new Vector2(0f, 0f);

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float centerHoldTime = 1.0f;
    [SerializeField] private bool hideAfterStop = false;
    [SerializeField] private bool showDebugLog = false;

    [Header("Events")]
    [SerializeField] private UnityEvent timerStarted = new UnityEvent();
    [SerializeField] private UnityEvent timerStopped = new UnityEvent();
    [SerializeField] private UnityEvent timerReset = new UnityEvent();

    private float elapsedTime = 0f;
    private bool isRunning = false;
    private int lastDisplayedCentiseconds = -1;

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
        if (isRunning)
        {
            return;
        }

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        elapsedTime = 0f;
        lastDisplayedCentiseconds = -1;
        isRunning = true;

        if (timerRect != null)
        {
            timerRect.anchoredPosition = topPosition;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        UpdateTimerText();
        timerStarted.Invoke();
        Log("[TimerManager] Timer started");
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
        timerStopped.Invoke();
        Log("[TimerManager] Timer stopped: " + elapsedTime);
    }

    public void ResetTimer()
    {
        isRunning = false;
        elapsedTime = 0f;
        lastDisplayedCentiseconds = -1;
        ResetTimerView();
        timerReset.Invoke();

        Log("[TimerManager] Timer reset");
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

        int centiseconds = Mathf.FloorToInt(elapsedTime * 100f);
        if (centiseconds == lastDisplayedCentiseconds)
        {
            return;
        }

        lastDisplayedCentiseconds = centiseconds;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = centiseconds % 100;

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
        float duration = Mathf.Max(0.01f, moveDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = timer / duration;
            t = Mathf.SmoothStep(0f, 1f, t);

            timerRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                centerPosition,
                t
            );

            yield return null;
        }

        timerRect.anchoredPosition = centerPosition;

        yield return new WaitForSeconds(Mathf.Max(0f, centerHoldTime));

        if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        moveCoroutine = null;
    }

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
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
