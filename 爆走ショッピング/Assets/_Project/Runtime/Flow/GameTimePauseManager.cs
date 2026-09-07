using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class GameTimePauseManager : MonoBehaviour
{
    [Serializable]
    private sealed class PauseStateEvent : UnityEvent<bool>
    {
    }

    [Header("Settings")]
    [SerializeField] private bool resumeOnDisable = true;
    [SerializeField] private bool stopGamepadRumbleOnPause = true;

    [Header("Events")]
    [SerializeField] private UnityEvent pauseStarted = new UnityEvent();
    [SerializeField] private UnityEvent pauseEnded = new UnityEvent();
    [SerializeField] private PauseStateEvent pauseStateChanged = new PauseStateEvent();

    private readonly HashSet<string> pauseSources = new HashSet<string>();
    private float resumeTimeScale = 1f;
    private bool isPaused;

    public bool IsPaused => isPaused;
    public int PauseSourceCount => pauseSources.Count;

    private void Awake()
    {
        if (Time.timeScale > 0f)
        {
            resumeTimeScale = Time.timeScale;
        }
    }

    private void OnDisable()
    {
        if (resumeOnDisable)
        {
            ForceResumeGame();
        }
    }

    public void PauseGame()
    {
        RequestPause("Manual");
    }

    public void ResumeGame()
    {
        ReleasePause("Manual");
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void SetPaused(bool paused)
    {
        if (paused)
        {
            PauseGame();
            return;
        }

        ResumeGame();
    }

    public void RequestPause(string source)
    {
        string key = NormalizeSource(source);

        if (!pauseSources.Add(key))
        {
            return;
        }

        RefreshPauseState();
    }

    public void ReleasePause(string source)
    {
        string key = NormalizeSource(source);

        if (!pauseSources.Remove(key))
        {
            return;
        }

        RefreshPauseState();
    }

    public void ClearPauseRequests()
    {
        if (pauseSources.Count == 0)
        {
            return;
        }

        pauseSources.Clear();
        RefreshPauseState();
    }

    public void ForceResumeGame()
    {
        pauseSources.Clear();
        ApplyResumed(1f);
    }

    public void ForceResumeGame(float timeScale)
    {
        pauseSources.Clear();
        ApplyResumed(Mathf.Max(0.0001f, timeScale));
    }

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

    private string NormalizeSource(string source)
    {
        return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
    }
}
