using UnityEngine;

[RequireComponent(typeof(Collider))]
// プレイヤーが特定エリアへ入った・出たタイミングでタイマー操作を行うトリガーです。
// 役割:
// - OnTriggerEnter / OnTriggerExit に応じて StartTimer、StopTimer、ResetTimer を実行します。
// - ゴール用 Zone では、スコアボード完了前の StopTimer を拒否できます。
// 接続:
// - timerManager には操作対象の TimerManager を指定します。未設定ならシーンから探します。
// - scoreboardManager は requireScoreboardCompleteToStop が true のときだけ停止前判定に使います。
// - PlayerCollisionReporter の triggerEntered / triggerExited から HandleEnter / HandleExit を呼ぶ接続にも対応しています。
// - lastEnterFrame / lastExitFrame により、同一フレームで二重に呼ばれても 1 回だけ処理します。
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

    // 必要な参照を探し、Collider が Trigger になっているか確認します。
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

    // プレイヤーがエリアに入ったら Enter 側の設定アクションを実行します。
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        HandleEnter(timerManager);
    }

    // プレイヤーがエリアから出たら Exit 側の設定アクションを実行します。
    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        HandleExit(timerManager);
    }

    // UnityEvent など外部接続から Enter 処理を呼ぶための公開入口です。
    public void HandleEnter(TimerManager fallbackTimerManager = null)
    {
        if (lastEnterFrame == Time.frameCount)
        {
            return;
        }

        lastEnterFrame = Time.frameCount;
        ExecuteAction(onEnter, fallbackTimerManager);
    }

    // UnityEvent など外部接続から Exit 処理を呼ぶための公開入口です。
    public void HandleExit(TimerManager fallbackTimerManager = null)
    {
        if (lastExitFrame == Time.frameCount)
        {
            return;
        }

        lastExitFrame = Time.frameCount;
        ExecuteAction(onExit, fallbackTimerManager);
    }

    // playerTag が空なら全 Collider を許可し、設定済みならタグ一致だけを許可します。
    private bool IsPlayer(Collider other)
    {
        if (string.IsNullOrEmpty(playerTag))
        {
            return true;
        }

        return other.CompareTag(playerTag);
    }

    // 設定された TimerZoneAction を実際の TimerManager 操作へ変換します。
    private void ExecuteAction(TimerZoneAction action, TimerManager fallbackTimerManager)
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
                // 買い物リスト未達成なら、タイマー停止せず警告表示だけにします。
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

    // ゴール前に買い物リスト完了が必要かどうかを判定します。
    private bool TryCompleteScoreboardBeforeStop()
    {
        if (!requireScoreboardCompleteToStop || scoreboardManager == null)
        {
            return true;
        }

        return scoreboardManager.TryCompleteScoreboard();
    }
}
