using System;
using UnityEngine;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool showDebugLog = false;

    [Header("Events")]
    [SerializeField] private UnityEvent timerStarted = new UnityEvent();
    [SerializeField] private UnityEvent timerStopped = new UnityEvent();
    [SerializeField] private UnityEvent timerReset = new UnityEvent();

    private float elapsedTime = 0f;
    private bool isRunning = false;
    private int lastNotifiedCentiseconds = -1;

    public event Action Started;
    public event Action Stopped;
    public event Action ResetCompleted;
    public event Action<float> TimeChanged;

    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        NotifyTimeChanged(true);
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        NotifyTimeChanged(false);
    }

    public void StartTimer()
    {
        if (isRunning)
        {
            return;
        }

        elapsedTime = 0f;
        lastNotifiedCentiseconds = -1;
        isRunning = true;

        NotifyTimeChanged(true);
        timerStarted.Invoke();
        Started?.Invoke();
        Log("[TimerManager] Timer started");
    }

    public void StopTimer()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        NotifyTimeChanged(true);
        timerStopped.Invoke();
        Stopped?.Invoke();
        Log("[TimerManager] Timer stopped: " + elapsedTime);
    }

    public void ResetTimer()
    {
        isRunning = false;
        elapsedTime = 0f;
        lastNotifiedCentiseconds = -1;

        NotifyTimeChanged(true);
        timerReset.Invoke();
        ResetCompleted?.Invoke();
        Log("[TimerManager] Timer reset");
    }

    private void NotifyTimeChanged(bool force)
    {
        int centiseconds = Mathf.FloorToInt(elapsedTime * 100f);

        if (!force && centiseconds == lastNotifiedCentiseconds)
        {
            return;
        }

        lastNotifiedCentiseconds = centiseconds;
        TimeChanged?.Invoke(elapsedTime);
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
