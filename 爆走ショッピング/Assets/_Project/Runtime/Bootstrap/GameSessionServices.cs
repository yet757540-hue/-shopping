using UnityEngine;

/// <summary>
/// One explicit set of gameplay-scene dependencies.  It is created by
/// <see cref="GameSessionRoot"/> and passed to systems that need to collaborate.
/// It deliberately does not perform scene-wide searches or create components.
/// </summary>
public sealed class GameSessionServices
{
    public PlayerManager Player { get; }
    public PlayerInventory Inventory { get; }
    public PlayerCollisionReporter CollisionReporter { get; }
    public ImpactSettings ImpactSettings { get; }
    public SettlementArea SettlementArea { get; }
    public TimerManager Timer { get; }
    public ScoreboardManager Scoreboard { get; }
    public GameTimePauseManager Pause { get; }
    public GameRestartManager Restart { get; }
    public CandyEffectLibrary CandyEffectLibrary { get; }

    public GameSessionServices(
        PlayerManager player,
        PlayerInventory inventory,
        PlayerCollisionReporter collisionReporter,
        ImpactSettings impactSettings,
        SettlementArea settlementArea,
        TimerManager timer,
        ScoreboardManager scoreboard,
        GameTimePauseManager pause,
        GameRestartManager restart,
        CandyEffectLibrary candyEffectLibrary)
    {
        Player = player;
        Inventory = inventory;
        CollisionReporter = collisionReporter;
        ImpactSettings = impactSettings;
        SettlementArea = settlementArea;
        Timer = timer;
        Scoreboard = scoreboard;
        Pause = pause;
        Restart = restart;
        CandyEffectLibrary = candyEffectLibrary;
    }
}
