
using UnityEngine;
using TMPro;

/// <summary>TimerManager の残り時間を HUD に反映します。</summary>
public class TimerDisplayUI_TMP : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private RectTransform timerRect;

    [Header("実行時 UI")]
    [SerializeField] private Vector2 size = new Vector2(260f, 64f);
    [SerializeField] private int fontSize = 42;
    [SerializeField] private Color textColor = Color.white;

    [Header("表示位置")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 268f);

    [Header("終了時の状態")]
    [SerializeField] private bool hideAfterStop = false;
    [SerializeField] private bool destroyWhenCompleted = true;

    private int lastDisplayedCentiseconds = -1;

    public void Initialize(TimerManager configuredTimerManager)
    {
        UnsubscribeTimer();
        timerManager = configuredTimerManager;
        SubscribeTimer();
        RefreshFromTimer();
    }

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
        EnsureTimerText();
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
    }

    private void SubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.Completed -= HandleTimerCompleted;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;

        timerManager.Started += HandleTimerStarted;
        timerManager.Stopped += HandleTimerStopped;
        timerManager.Completed += HandleTimerCompleted;
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
        timerManager.Completed -= HandleTimerCompleted;
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

        UpdateTimerText(timerManager.RemainingTime, true);
    }

    private void HandleTimerStarted()
    {
        ResetTimerView();
    }

    private void HandleTimerStopped()
    {
        RefreshFromTimer();

        if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private void HandleTimerCompleted()
    {
        RefreshFromTimer();

        if (destroyWhenCompleted)
        {
            DestroyTimerView();
        }
        else if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    private void HandleTimerReset()
    {
        ResetTimerView();
    }

    private void HandleTimeChanged(float remainingTime)
    {
        UpdateTimerText(remainingTime, false);
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

        RefreshFromTimer();
    }

    private void EnsureTimerText()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<TMP_Text>();
        }

        if (timerRect == null && timerText != null)
        {
            timerRect = timerText.GetComponent<RectTransform>();
        }
    }

    private void ConfigureTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.fontSize = fontSize;
        timerText.color = textColor;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.textWrappingMode = TextWrappingModes.NoWrap;
        timerText.raycastTarget = false;
    }

    private void UpdateTimerText(float remainingTime, bool force)
    {
        if (timerText == null)
        {
            return;
        }

        int centiseconds = Mathf.FloorToInt(remainingTime * 100f);

        if (!force && centiseconds == lastDisplayedCentiseconds)
        {
            return;
        }

        lastDisplayedCentiseconds = centiseconds;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        //int milliseconds = centiseconds % 100;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void DestroyTimerView()
    {
        if (timerText == null)
        {
            return;
        }

        GameObject timerObject = timerText.gameObject;
        timerText = null;
        timerRect = null;

        Destroy(timerObject);
    }

    private void OnValidate()
    {
        size.x = Mathf.Max(80f, size.x);
        size.y = Mathf.Max(24f, size.y);
        fontSize = Mathf.Max(8, fontSize);
    }
}
