using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TimerDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform timerRect;

    [Header("Runtime UI")]
    [SerializeField] private bool createTextIfMissing = true;
    [SerializeField] private Vector2 size = new Vector2(260f, 64f);
    [SerializeField] private int fontSize = 42;
    [SerializeField] private Color textColor = Color.white;

    [Header("Display Positions")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 268f);
    [SerializeField] private Vector2 centerPosition = new Vector2(0f, 0f);

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float centerHoldTime = 1.0f;
    [SerializeField] private bool hideAfterStop = false;

    private int lastDisplayedCentiseconds = -1;
    private Coroutine moveCoroutine;

    private void Awake()
    {
        ResolveReferences();
        EnsureTimerText();
        ConfigureTimerText();
        ResetTimerView();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeTimer();
        RefreshFromTimer();
    }

    private void OnDisable()
    {
        UnsubscribeTimer();
    }

    private void ResolveReferences()
    {
        if (timerManager == null)
        {
            timerManager = GetComponent<TimerManager>();
        }

        if (timerManager == null)
        {
            timerManager = FindAnyObjectByType<TimerManager>();
        }
    }

    private void SubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;

        timerManager.Started += HandleTimerStarted;
        timerManager.Stopped += HandleTimerStopped;
        timerManager.ResetCompleted += HandleTimerReset;
        timerManager.TimeChanged += HandleTimeChanged;
    }

    private void UnsubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;
    }

    private void RefreshFromTimer()
    {
        if (timerManager == null)
        {
            UpdateTimerText(0f, true);
            return;
        }

        UpdateTimerText(timerManager.ElapsedTime, true);
    }

    private void HandleTimerStarted()
    {
        ResetTimerView();
    }

    private void HandleTimerStopped()
    {
        RefreshFromTimer();
        StartMoveTimerToCenter();
    }

    private void HandleTimerReset()
    {
        ResetTimerView();
    }

    private void HandleTimeChanged(float elapsedTime)
    {
        UpdateTimerText(elapsedTime, false);
    }

    private void ResetTimerView()
    {
        StopMoveCoroutine();

        if (timerRect != null)
        {
            timerRect.anchoredPosition = topPosition;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        RefreshFromTimer();
    }

    private void EnsureTimerText()
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

    private void UpdateTimerText(float elapsedTime, bool force)
    {
        if (timerText == null)
        {
            return;
        }

        int centiseconds = Mathf.FloorToInt(elapsedTime * 100f);

        if (!force && centiseconds == lastDisplayedCentiseconds)
        {
            return;
        }

        lastDisplayedCentiseconds = centiseconds;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = centiseconds % 100;
        timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    private void StartMoveTimerToCenter()
    {
        StopMoveCoroutine();
        moveCoroutine = StartCoroutine(MoveTimerToCenter());
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

    private void StopMoveCoroutine()
    {
        if (moveCoroutine == null)
        {
            return;
        }

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
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
