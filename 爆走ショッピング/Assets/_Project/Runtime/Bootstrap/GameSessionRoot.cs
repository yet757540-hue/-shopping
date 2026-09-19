using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームシーンの構成ルートです。機能間の参照、初期化、イベント接続をここだけに集めます。
/// 各機能は必要な依存先だけを受け取り、シーン全体を検索しません。
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameSessionRoot : MonoBehaviour
{
    // ゲーム進行を担当するサービスです。すべて同じゲームシーンに配置します。
    [Header("ゲーム進行サービス")]
    [SerializeField] private PlayerManager player;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCollisionReporter collisionReporter;
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private SettlementArea settlementArea;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ScoreboardManager scoreboardManager;
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private ScoreTarget[] scoreTargets = System.Array.Empty<ScoreTarget>();

    // 表示・入力を担当するコンポーネントです。
    [Header("表示と入力")]
    [SerializeField] private CollisionFeedbackManager collisionFeedback;
    [SerializeField] private InventoryInfluenceSettings inventoryInfluence;
    [SerializeField] private InventoryStatusUI inventoryHud;
    [SerializeField] private TimerDisplayUI timerHud;
    [SerializeField] private ScoreboardView scoreboardView;
    [SerializeField] private InGameOptionMenu optionMenu;
    [SerializeField] private GameResultScreenManager resultScreen;

    private bool subscribed;

    // 必須参照を検証し、正常な場合だけ各機能を初期化してイベントを接続します。
    private void Awake()
    {
        if (!ValidateReferences(true))
        {
            enabled = false;
            return;
        }

        InitializeSystems();
        Subscribe();
    }

    // シーン破棄時に、登録したゲーム進行イベントを解除します。
    private void OnDestroy()
    {
        Unsubscribe();
    }

    // 不足している必須参照を集め、必要に応じてエラーを表示します。
    public bool ValidateReferences(bool logErrors)
    {
        List<string> missing = new List<string>();
        Require(player, nameof(player), missing);
        Require(inventory, nameof(inventory), missing);
        Require(collisionReporter, nameof(collisionReporter), missing);
        Require(impactSettings, nameof(impactSettings), missing);
        Require(settlementArea, nameof(settlementArea), missing);
        Require(timerManager, nameof(timerManager), missing);
        Require(scoreboardManager, nameof(scoreboardManager), missing);
        Require(pauseManager, nameof(pauseManager), missing);
        Require(restartManager, nameof(restartManager), missing);
        Require(collisionFeedback, nameof(collisionFeedback), missing);
        Require(inventoryInfluence, nameof(inventoryInfluence), missing);
        Require(inventoryHud, nameof(inventoryHud), missing);
        Require(timerHud, nameof(timerHud), missing);
        Require(scoreboardView, nameof(scoreboardView), missing);
        Require(optionMenu, nameof(optionMenu), missing);
        Require(resultScreen, nameof(resultScreen), missing);

        if (scoreTargets == null || scoreTargets.Length == 0)
        {
            missing.Add(nameof(scoreTargets));
        }

        if (missing.Count == 0)
        {
            return true;
        }

        if (logErrors)
        {
            Debug.LogError("[GameSessionRoot] Missing required scene references: " + string.Join(", ", missing), this);
        }

        return false;
    }

    // 初期化順をここに並べることで、ゲーム開始時の接続順を追いやすくします。
    private void InitializeSystems()
    {
        scoreboardManager.Initialize(impactSettings, inventory, settlementArea, scoreTargets, scoreboardView);
        inventoryInfluence.Initialize(inventory, player, impactSettings);
        collisionFeedback.Initialize(impactSettings);
        restartManager.Initialize(pauseManager);
        optionMenu.Initialize(pauseManager, restartManager, player);
        resultScreen.Initialize(timerManager, scoreboardManager, restartManager, pauseManager);
        inventoryHud.Initialize(inventory, inventoryInfluence);
        timerHud.Initialize(timerManager);
    }

    // UnityEvent ではなく C# イベントを使う接続を、開始時にまとめて登録します。
    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        collisionReporter.CollisionEntered += scoreboardManager.RegisterCollision;
        collisionReporter.CollisionEntered += collisionFeedback.PlayFeedback;
        settlementArea.PlayerExited += timerManager.StartTimer;
        timerManager.Started += scoreboardManager.StartScoreboard;
        timerManager.Stopped += scoreboardManager.StartScoreboard;
        timerManager.ResetCompleted += scoreboardManager.ClearScoreboard;
        timerManager.Completed += resultScreen.ShowResultScreen;
        subscribed = true;
    }

    // シーン破棄時には同じ組み合わせを解除し、破棄済み参照の呼び出しを防ぎます。
    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        collisionReporter.CollisionEntered -= scoreboardManager.RegisterCollision;
        collisionReporter.CollisionEntered -= collisionFeedback.PlayFeedback;
        settlementArea.PlayerExited -= timerManager.StartTimer;
        timerManager.Started -= scoreboardManager.StartScoreboard;
        timerManager.Stopped -= scoreboardManager.StartScoreboard;
        timerManager.ResetCompleted -= scoreboardManager.ClearScoreboard;
        timerManager.Completed -= resultScreen.ShowResultScreen;
        subscribed = false;
    }

    // 参照が未設定なら、そのフィールド名を不足一覧に追加します。
    private static void Require(Object value, string name, ICollection<string> missing)
    {
        if (value == null)
        {
            missing.Add(name);
        }
    }
}
