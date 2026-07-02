using System;
using UnityEngine;
using UnityEngine.Events;

// 経過時間を計測し、状態変化をイベントで外へ知らせるタイマー本体です。
// 役割:
// - StartTimer で 0 秒から計測開始し、StopTimer で停止、ResetTimer で初期化します。
// - UnityEvent と C# event の両方を用意し、Inspector 接続とコード接続の両方に対応します。
// 接続:
// - TimerZone が StartTimer / StopTimer / ResetTimer を呼びます。
// - TimerDisplayUI が Started / Stopped / ResetCompleted / TimeChanged を購読します。
// - ScoreboardManager は TimerZone 経由で、ゴール時に停止してよいかを判定します。
// 読むときの要点:
// - TimeChanged は毎フレームではなく、センチ秒が変わったときだけ通知します。
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

    // 初期状態の 0 秒を表示側へ通知します。
    private void Awake()
    {
        NotifyTimeChanged(true);
    }

    // 実行中だけ経過時間を進め、必要な間隔で通知します。
    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        NotifyTimeChanged(false);
    }

    // タイマーを 0 秒から開始し、開始イベントを通知します。
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

    // タイマーを停止し、停止時点の時間を通知します。
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

    // タイマーを停止状態の 0 秒へ戻し、リセットイベントを通知します。
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

    // 表示側へ時間変更を通知します。force が true の場合は同じ値でも通知します。
    private void NotifyTimeChanged(bool force)
    {
        int centiseconds = Mathf.FloorToInt(elapsedTime * 100f);

        // 表示側の不要な再描画を減らすため、1/100 秒未満の変化は通知しません。
        if (!force && centiseconds == lastNotifiedCentiseconds)
        {
            return;
        }

        lastNotifiedCentiseconds = centiseconds;
        TimeChanged?.Invoke(elapsedTime);
    }

    // デバッグ表示が有効な場合だけログを出します。
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
    }

    // Inspector のコンテキストメニューから開始テストを行います。
    [ContextMenu("Test Start Timer")]
    private void TestStartTimer()
    {
        StartTimer();
    }

    // Inspector のコンテキストメニューから停止テストを行います。
    [ContextMenu("Test Stop Timer")]
    private void TestStopTimer()
    {
        StopTimer();
    }
}
