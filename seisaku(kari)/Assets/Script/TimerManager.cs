using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TimerManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform timerRect;

    [Header("Runtime UI")]
    [SerializeField] private bool createTextIfMissing = true;
    [SerializeField] private Vector2 size = new Vector2(260f, 64f);
    [SerializeField] private int fontSize = 42;
    [SerializeField] private Color textColor = Color.white;

    [Header("Display Positions")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 260f);
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
            timerText = GetComponentInChildren<Text>();
        }

        if (timerText == null && createTextIfMissing)
        {
            CreateRuntimeTimerText();
        }

        if (timerRect == null && timerText != null)
        {
            timerRect = timerText.GetComponent<RectTransform>();
        }

        ConfigureTimerText();
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

    private void CreateRuntimeTimerText()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Timer Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject textObject = new GameObject("Timer Text");
        textObject.transform.SetParent(canvas.transform, false);

        timerRect = textObject.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = topPosition;
        timerRect.sizeDelta = size;

        timerText = textObject.AddComponent<Text>();
    }

    private void ConfigureTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.font = JapaneseUIFont.Get(fontSize);
        timerText.fontSize = fontSize;
        timerText.color = textColor;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        timerText.verticalOverflow = VerticalWrapMode.Overflow;
        timerText.raycastTarget = false;
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
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            timerRect.anchoredPosition = Vector2.Lerp(startPosition, centerPosition, t);
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

    private void OnValidate()
    {
        size.x = Mathf.Max(80f, size.x);
        size.y = Mathf.Max(24f, size.y);
        fontSize = Mathf.Max(8, fontSize);
        moveDuration = Mathf.Max(0.01f, moveDuration);
        centerHoldTime = Mathf.Max(0f, centerHoldTime);
    }
}
