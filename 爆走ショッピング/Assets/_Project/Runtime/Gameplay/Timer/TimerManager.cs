using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>ラウンドのカウントダウンと、状態変更イベントを一か所で管理します。</summary>
public class TimerManager : MonoBehaviour
{
    // 動作方針を切り替える Inspector 設定です。
    [Header("設定")]
    [SerializeField] private float countdownDuration = 180f;
    [SerializeField] private bool showDebugLog = false;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
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

    // タイマーに設定された制限時間を秒単位で返します。
    public float Duration => countdownDuration;
    // 現在の残り時間を秒単位で返します。
    public float RemainingTime => remainingTime;
    // タイマーが減算処理を実行する状態かを返します。
    public bool IsRunning => isRunning;
    // 現在、一時停止状態かを返します。
    public bool IsPaused => isPaused;
    // 計測開始済みで、停止・終了・リセットされていないかを返します。
    public bool HasStarted => hasStarted;
    // 時間切れ処理が完了済みかを返します。
    public bool IsComplete => isComplete;

    // 制限時間を補正し、初期の残り時間を表示側へ通知します。
    private void Awake()
    {
        countdownDuration = Mathf.Max(0.01f, countdownDuration);
        remainingTime = countdownDuration;
        NotifyTimeChanged(true);
    }

    // 実行中だけ残り時間を減算します。タイマー単体の停止に加え、Time.timeScale がゼロの間も減算は進みません。
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

    // 開始済みのタイマーがゼロに達したら、時間切れ処理を呼びます。
    public void TimeOverCheck()
    {
        if (remainingTime <= 0f && (isRunning || isPaused || hasStarted))
        {
            CompleteTimer();
        }
    }

    // 実行中なら何もせず、一時停止中なら再開し、それ以外なら新しく開始します。
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

    // 現在の状態に関係なく、設定された制限時間から計測をやり直します。
    public void RestartTimer()
    {
        StartNewCountdown();
    }

    // 残り時間を保ってタイマー単体の減算を止め、C# イベントを通知します。
    public void PauseTimer()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        isPaused = true;

        NotifyTimeChanged(true);
        Paused?.Invoke();
        Log("[TimerManager] Timer paused: " + remainingTime);
    }

    // 一時停止中で時間が残っていれば、減算を再開して C# イベントを通知します。
    public void ResumeTimer()
    {
        if (!isPaused || remainingTime <= 0f)
        {
            return;
        }

        isRunning = true;
        isPaused = false;

        NotifyTimeChanged(true);
        Resumed?.Invoke();
        Log("[TimerManager] Timer resumed: " + remainingTime);
    }

    // タイマー単体の実行と一時停止を切り替えます。
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

    // 残り時間と状態フラグを初期化し、新しい計測の開始を通知します。
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
        Started?.Invoke();
        Log("[TimerManager] Timer started: " + remainingTime);
    }

    // 残り時間を保って開始状態を解除し、停止を通知します。
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
        Stopped?.Invoke();
        Log("[TimerManager] Timer stopped: " + remainingTime);
    }

    // 残り時間によらず、時間切れと同じ終了処理を実行します。
    public void EndTimer()
    {
        CompleteTimer();
    }

    // 減算を止めて残り時間を制限時間に戻し、リセット完了を通知します。
    public void ResetTimer()
    {
        isRunning = false;
        isPaused = false;
        hasStarted = false;
        isComplete = false;
        remainingTime = countdownDuration;
        lastNotifiedCentiseconds = -1;

        NotifyTimeChanged(true);
        ResetCompleted?.Invoke();
        Log("[TimerManager] Timer reset");
    }

    // 制限時間を変更し、タイマー全体をリセットします。
    public void SetDuration(float seconds)
    {
        countdownDuration = Mathf.Max(0.01f, seconds);
        ResetTimer();
    }

    // 残り時間を制限時間内に補正して通知し、必要なら時間切れを処理します。
    public void SetRemainingTime(float seconds)
    {
        remainingTime = Mathf.Clamp(seconds, 0f, countdownDuration);
        isComplete = remainingTime <= 0f && isComplete;
        lastNotifiedCentiseconds = -1;
        NotifyTimeChanged(true);
        TimeOverCheck();
    }

    // 現在の残り時間に指定秒数を加えます。結果はゼロから制限時間までに制限されます。
    public void AddTime(float seconds)
    {
        SetRemainingTime(remainingTime + seconds);
    }

    // 終了を一度だけ処理し、残り時間をゼロにして完了イベントを通知します。
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
        Completed?.Invoke();
        Log("[TimerManager] Timer completed");
    }

    // 百分の一秒の表示値が変わった場合、または強制更新時に残り時間を通知します。
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

    // デバッグ表示が有効な場合だけ、受け取ったメッセージを出力します。
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
    }

    // Inspector のコンテキストメニューから、開始動作を確認します。
    [ContextMenu("Test Start Timer")]
    private void TestStartTimer()
    {
        StartTimer();
    }

    // Inspector のコンテキストメニューから、停止動作を確認します。
    [ContextMenu("Test Stop Timer")]
    private void TestStopTimer()
    {
        StopTimer();
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        countdownDuration = Mathf.Max(0.01f, countdownDuration);
    }
}
