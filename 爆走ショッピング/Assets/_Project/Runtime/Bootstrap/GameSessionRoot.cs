using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Composition root for a gameplay scene. All cross-feature links are owned
/// here so gameplay systems keep a narrow, inspectable dependency surface.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class GameSessionRoot : MonoBehaviour
{
    [Header("Gameplay services")]
    [SerializeField] private PlayerManager player;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerCollisionReporter collisionReporter;
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private SettlementArea settlementArea;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ScoreboardManager scoreboardManager;
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private CandyEffectLibrary candyEffectLibrary;
    [SerializeField] private ScoreTarget[] scoreTargets = System.Array.Empty<ScoreTarget>();

    [Header("Presentation")]
    [SerializeField] private CollisionFeedbackManager collisionFeedback;
    [SerializeField] private InventoryInfluenceSettings inventoryInfluence;
    [SerializeField] private InventoryStatusUI inventoryHud;
    [SerializeField] private TimerDisplayUI timerHud;
    [SerializeField] private ScoreboardView scoreboardView;
    [SerializeField] private InGameWindowManager windowManager;
    [SerializeField] private InGameOptionMenu optionMenu;
    [SerializeField] private CandyRewardWindowManager rewardWindow;
    [SerializeField] private GameResultScreenManager resultScreen;

    private GameSessionServices services;
    private bool subscribed;

    public GameSessionServices Services => services;

    private void Awake()
    {
        if (!ValidateReferences(true))
        {
            enabled = false;
            return;
        }

        services = new GameSessionServices(
            player, inventory, collisionReporter, impactSettings, settlementArea,
            timerManager, scoreboardManager, pauseManager, restartManager, candyEffectLibrary);

        scoreboardManager.Initialize(impactSettings, inventory, settlementArea, scoreTargets, scoreboardView);
        inventoryInfluence.Initialize(inventory, player, impactSettings);
        collisionFeedback.Initialize(impactSettings);
        restartManager.Initialize(pauseManager);
        windowManager.Initialize(pauseManager);
        optionMenu.Initialize(pauseManager, restartManager, player);
        rewardWindow.Initialize(services, inventoryInfluence);
        resultScreen.Initialize(timerManager, scoreboardManager, restartManager, pauseManager);
        inventoryHud.Initialize(inventory, inventoryInfluence);
        timerHud.Initialize(timerManager);

        Subscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

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
        Require(candyEffectLibrary, nameof(candyEffectLibrary), missing);
        Require(collisionFeedback, nameof(collisionFeedback), missing);
        Require(inventoryInfluence, nameof(inventoryInfluence), missing);
        Require(inventoryHud, nameof(inventoryHud), missing);
        Require(timerHud, nameof(timerHud), missing);
        Require(scoreboardView, nameof(scoreboardView), missing);
        Require(windowManager, nameof(windowManager), missing);
        Require(optionMenu, nameof(optionMenu), missing);
        Require(rewardWindow, nameof(rewardWindow), missing);
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
        scoreboardManager.SettlementCompleted += rewardWindow.ShowRewardWindow;
        timerManager.Completed += resultScreen.ShowResultScreen;
        subscribed = true;
    }

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
        scoreboardManager.SettlementCompleted -= rewardWindow.ShowRewardWindow;
        timerManager.Completed -= resultScreen.ShowResultScreen;
        subscribed = false;
    }

    private static void Require(Object value, string name, ICollection<string> missing)
    {
        if (value == null)
        {
            missing.Add(name);
        }
    }
}
