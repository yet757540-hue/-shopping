using System;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float countdownDuration = 180f;
    [SerializeField] private bool showDebugLog = false;

    [Header("Events")]
    [SerializeField] private UnityEvent timerStarted = new UnityEvent();
    [SerializeField] private UnityEvent timerPaused = new UnityEvent();
    [SerializeField] private UnityEvent timerResumed = new UnityEvent();
    [SerializeField] private UnityEvent timerStopped = new UnityEvent();
    [SerializeField] private UnityEvent timerCompleted = new UnityEvent();
    [SerializeField] private UnityEvent timerReset = new UnityEvent();

    private float remainingTime;
    private bool isRunning = false;
    private bool isPaused = false;
    private bool hasStarted = false;
    private bool isComplete = false;
    private int lastNotifiedCentiseconds = -1;

    public event Action Started;
    public event Action Paused;
    public event Action Resumed;
    public event Action Stopped;
    public event Action Completed;
    public event Action ResetCompleted;
    public event Action<float> TimeChanged;

    public float Duration => countdownDuration;
    public float RemainingTime => remainingTime;
    public float ElapsedTime => remainingTime;
    public bool IsRunning => isRunning;
    public bool IsPaused => isPaused;
    public bool HasStarted => hasStarted;
    public bool IsComplete => isComplete;

    private void Awake()
    {
        countdownDuration = Mathf.Max(0.01f, countdownDuration);
        remainingTime = countdownDuration;
        NotifyTimeChanged(true);
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
        NotifyTimeChanged(false);
        TimeOverCheck();
    }

    public void TimeOverCheck()
    {
        if (remainingTime <= 0f && (isRunning || isPaused || hasStarted))
        {
            CompleteTimer();
        }
    }

    public void StartTimer()
    {
        if (isRunning)
        {
            return;
        }

        if (isPaused)
        {
            ResumeTimer();
            return;
        }

        StartNewCountdown();
    }

    public void RestartTimer()
    {
        StartNewCountdown();
    }

    public void PauseTimer()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        isPaused = true;

        NotifyTimeChanged(true);
        timerPaused.Invoke();
        Paused?.Invoke();
        Log("[TimerManager] Timer paused: " + remainingTime);
    }

    public void ResumeTimer()
    {
        if (!isPaused || remainingTime <= 0f)
        {
            return;
        }

        isRunning = true;
        isPaused = false;

        NotifyTimeChanged(true);
        timerResumed.Invoke();
        Resumed?.Invoke();
        Log("[TimerManager] Timer resumed: " + remainingTime);
    }

    public void TogglePause()
    {
        if (isRunning)
        {
            PauseTimer();
            return;
        }

        if (isPaused)
        {
            ResumeTimer();
        }
    }

    private void StartNewCountdown()
    {
        countdownDuration = Mathf.Max(0.01f, countdownDuration);
        remainingTime = countdownDuration;
        lastNotifiedCentiseconds = -1;
        isRunning = true;
        isPaused = false;
        hasStarted = true;
        isComplete = false;

        NotifyTimeChanged(true);
        timerStarted.Invoke();
        Started?.Invoke();
        Log("[TimerManager] Timer started: " + remainingTime);
    }

    public void StopTimer()
    {
        if (!isRunning && !isPaused && !hasStarted)
        {
            return;
        }

        isRunning = false;
        isPaused = false;
        hasStarted = false;
        isComplete = false;

        NotifyTimeChanged(true);
        timerStopped.Invoke();
        Stopped?.Invoke();
        Log("[TimerManager] Timer stopped: " + remainingTime);
    }

    public void EndTimer()
    {
        CompleteTimer();
    }

    public void ResetTimer()
    {
        isRunning = false;
        isPaused = false;
        hasStarted = false;
        isComplete = false;
        remainingTime = countdownDuration;
        lastNotifiedCentiseconds = -1;

        NotifyTimeChanged(true);
        timerReset.Invoke();
        ResetCompleted?.Invoke();
        Log("[TimerManager] Timer reset");
    }

    public void SetDuration(float seconds)
    {
        countdownDuration = Mathf.Max(0.01f, seconds);
        ResetTimer();
    }

    public void SetRemainingTime(float seconds)
    {
        remainingTime = Mathf.Clamp(seconds, 0f, countdownDuration);
        isComplete = remainingTime <= 0f && isComplete;
        lastNotifiedCentiseconds = -1;
        NotifyTimeChanged(true);
        TimeOverCheck();
    }

    public void AddTime(float seconds)
    {
        SetRemainingTime(remainingTime + seconds);
    }

    private void CompleteTimer()
    {
        if (isComplete)
        {
            return;
        }

        isRunning = false;
        isPaused = false;
        hasStarted = false;
        isComplete = true;
        remainingTime = 0f;
        lastNotifiedCentiseconds = -1;

        NotifyTimeChanged(true);
        timerCompleted.Invoke();
        Completed?.Invoke();
        Log("[TimerManager] Timer completed");
    }

    private void NotifyTimeChanged(bool force)
    {
        int centiseconds = Mathf.FloorToInt(remainingTime * 100f);

        if (!force && centiseconds == lastNotifiedCentiseconds)
        {
            return;
        }

        lastNotifiedCentiseconds = centiseconds;
        TimeChanged?.Invoke(remainingTime);
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
        countdownDuration = Mathf.Max(0.01f, countdownDuration);
    }
}
