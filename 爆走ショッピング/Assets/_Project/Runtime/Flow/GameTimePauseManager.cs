using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一時停止の依頼元を集合で管理します。最後の依頼が解除されるまでゲーム時間は再開しません。
/// </summary>
[DisallowMultipleComponent]
public class GameTimePauseManager : MonoBehaviour
{
    // 停止中かどうかを Inspector のイベントから通知するための型です。
    [Serializable]
    private sealed class PauseStateEvent : UnityEvent<bool>
    {
    }

    // 動作方針を切り替える Inspector 設定です。
    [Header("設定")]
    [SerializeField] private bool resumeOnDisable = true;
    [SerializeField] private bool stopGamepadRumbleOnPause = true;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
    [SerializeField] private UnityEvent pauseStarted = new UnityEvent();
    [SerializeField] private UnityEvent pauseEnded = new UnityEvent();
    [SerializeField] private PauseStateEvent pauseStateChanged = new PauseStateEvent();

    private readonly HashSet<string> pauseSources = new HashSet<string>();
    private float resumeTimeScale = 1f;
    private bool isPaused;

    // 現在、一時停止状態かを返します。
    public bool IsPaused => isPaused;
    // 現在残っている停止依頼元の数を返します。
    public int PauseSourceCount => pauseSources.Count;

    // 現在の時間倍率を、一時停止から復帰するときの値として記録します。
    private void Awake()
    {
        if (Time.timeScale > 0f)
        {
            resumeTimeScale = Time.timeScale;
        }
    }

    // 設定が有効なら、無効化時に全ての停止依頼を解除して通常速度へ戻します。
    private void OnDisable()
    {
        if (resumeOnDisable)
        {
            ForceResumeGame();
        }
    }

    // 手動操作を依頼元として、一時停止を要求します。
    public void PauseGame()
    {
        RequestPause("Manual");
    }

    // 手動操作の停止依頼を解除します。他の依頼が残っていれば停止を続けます。
    public void ResumeGame()
    {
        ReleasePause("Manual");
    }

    // 現在の停止状態に応じて、手動の停止依頼を追加または解除します。
    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    // 指定された状態に応じて、手動の停止・再開処理を呼びます。
    public void SetPaused(bool paused)
    {
        if (paused)
        {
            PauseGame();
            return;
        }

        ResumeGame();
    }

    // 同じ依頼元は一度だけ数えるため、重複した Open 呼び出しでも解除漏れになりません。
    public void RequestPause(string source)
    {
        string key = NormalizeSource(source);

        if (!pauseSources.Add(key))
        {
            return;
        }

        RefreshPauseState();
    }

    // 指定した依頼元を取り除き、残った依頼数から停止状態を更新します。
    public void ReleasePause(string source)
    {
        string key = NormalizeSource(source);

        if (!pauseSources.Remove(key))
        {
            return;
        }

        RefreshPauseState();
    }

    // 全依頼を解除し、停止前に記録した時間倍率へ戻します。
    public void ClearPauseRequests()
    {
        if (pauseSources.Count == 0)
        {
            return;
        }

        pauseSources.Clear();
        RefreshPauseState();
    }

    // 全依頼を解除し、引数なしなら通常速度、引数ありなら指定倍率へ強制復帰します。
    public void ForceResumeGame()
    {
        pauseSources.Clear();
        ApplyResumed(1f);
    }

    // 全依頼を解除し、引数なしなら通常速度、引数ありなら指定倍率へ強制復帰します。
    public void ForceResumeGame(float timeScale)
    {
        pauseSources.Clear();
        ApplyResumed(Mathf.Max(0.0001f, timeScale));
    }

    // 依頼元の有無と現在状態が異なる時だけ Time.timeScale を変更します。
    private void RefreshPauseState()
    {
        bool shouldPause = pauseSources.Count > 0;

        if (shouldPause == isPaused)
        {
            return;
        }

        if (shouldPause)
        {
            ApplyPaused();
            return;
        }

        ApplyResumed(resumeTimeScale);
    }

    // 復帰用の時間倍率を保存して時間を止め、必要なら振動も停止してイベントを通知します。
    private void ApplyPaused()
    {
        if (Time.timeScale > 0f)
        {
            resumeTimeScale = Time.timeScale;
        }

        Time.timeScale = 0f;
        isPaused = true;

        if (stopGamepadRumbleOnPause)
        {
            StopGamepadRumble();
        }

        pauseStarted.Invoke();
        pauseStateChanged.Invoke(true);
    }

    // 振動コルーチンと入力システムの振動状態をリセットします。
    private void StopGamepadRumble()
    {
        foreach (GamepadRumbleManager rumbleManager in FindObjectsByType<GamepadRumbleManager>())
        {
            if (rumbleManager != null)
            {
                rumbleManager.StopRumble();
            }
        }

        GamepadRumbleManager.ResetAllHaptics();
    }

    // 時間倍率を復帰させ、停止状態から変化した場合だけ再開イベントを通知します。
    private void ApplyResumed(float nextTimeScale)
    {
        bool wasPaused = isPaused || Time.timeScale == 0f;

        Time.timeScale = Mathf.Max(0.0001f, nextTimeScale);
        resumeTimeScale = Time.timeScale;
        isPaused = false;

        if (!wasPaused)
        {
            return;
        }

        pauseEnded.Invoke();
        pauseStateChanged.Invoke(false);
    }

    // 依頼元の前後の空白を除き、未指定なら共通の識別名を返します。
    private string NormalizeSource(string source)
    {
        return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
    }
}
