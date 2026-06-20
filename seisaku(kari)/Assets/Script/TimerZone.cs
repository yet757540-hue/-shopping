using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TimerZone : MonoBehaviour
{
    private enum TimerZoneAction
    {
        None,
        StartTimer,
        StopTimer,
        ResetTimer
    }

    [Header("References")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ScoreboardManager scoreboardManager;

    [Header("Trigger Rules")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private TimerZoneAction onEnter = TimerZoneAction.None;
    [SerializeField] private TimerZoneAction onExit = TimerZoneAction.None;

    [Header("Finish Rules")]
    [SerializeField] private bool requireScoreboardCompleteToStop = true;

    private int lastEnterFrame = -1;
    private int lastExitFrame = -1;

    private void Awake()
    {
        if (timerManager == null)
        {
            timerManager = FindAnyObjectByType<TimerManager>();
        }

        if (scoreboardManager == null)
        {
            scoreboardManager = FindAnyObjectByType<ScoreboardManager>();
        }

        Collider zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            Debug.LogWarning("[TimerZone] Collider should be marked as Trigger.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        HandleEnter(timerManager);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        HandleExit(timerManager);
    }

    public void HandleEnter(TimerManager fallbackTimerManager)
    {
        if (lastEnterFrame == Time.frameCount)
        {
            return;
        }

        lastEnterFrame = Time.frameCount;
        Apply(onEnter, fallbackTimerManager);
    }

    public void HandleExit(TimerManager fallbackTimerManager)
    {
        if (lastExitFrame == Time.frameCount)
        {
            return;
        }

        lastExitFrame = Time.frameCount;
        Apply(onExit, fallbackTimerManager);
    }

    private bool IsPlayer(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private void Apply(TimerZoneAction action, TimerManager fallbackTimerManager)
    {
        TimerManager targetTimer = timerManager != null ? timerManager : fallbackTimerManager;

        if (targetTimer == null)
        {
            return;
        }

        switch (action)
        {
            case TimerZoneAction.StartTimer:
                targetTimer.StartTimer();
                break;
            case TimerZoneAction.StopTimer:
                if (!TryCompleteScoreboardBeforeStop())
                {
                    return;
                }

                targetTimer.StopTimer();
                break;
            case TimerZoneAction.ResetTimer:
                targetTimer.ResetTimer();
                break;
        }
    }

    private bool TryCompleteScoreboardBeforeStop()
    {
        if (!requireScoreboardCompleteToStop || scoreboardManager == null)
        {
            return true;
        }

        return scoreboardManager.TryCompleteScoreboard();
    }
}
