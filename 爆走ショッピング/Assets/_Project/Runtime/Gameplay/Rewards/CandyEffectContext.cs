public sealed class CandyEffectContext
{
    public TimerManager TimerManager { get; set; }
    public ScoreboardManager ScoreboardManager { get; set; }
    public PlayerInventory Inventory { get; set; }
    public PlayerManager Player { get; set; }
    public InventoryInfluenceSettings InventoryInfluenceSettings { get; set; }
}
