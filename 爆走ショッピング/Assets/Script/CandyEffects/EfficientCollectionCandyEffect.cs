using UnityEngine;

[CreateAssetMenu(menuName = "Game/Candy Effects/Efficient Collection", fileName = "Efficient Collection Candy Effect")]
public sealed class EfficientCollectionCandyEffect : CandyEffect
{
    [SerializeField] private float itemGainMultiplier = 1.1f;

    public override bool CanApply(CandyEffectContext context)
    {
        return base.CanApply(context) && context.ScoreboardManager != null;
    }

    public override void Apply(CandyEffectContext context)
    {
        if (!CanApply(context))
        {
            return;
        }

        context.ScoreboardManager.MultiplyItemGainRange(itemGainMultiplier);
    }

    private void OnValidate()
    {
        itemGainMultiplier = Mathf.Max(0f, itemGainMultiplier);
    }
}
