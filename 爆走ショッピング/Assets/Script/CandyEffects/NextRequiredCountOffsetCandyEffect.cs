using UnityEngine;

[CreateAssetMenu(menuName = "Game/Candy Effects/Next Required Count Offset", fileName = "Next Required Count Offset Candy Effect")]
public sealed class NextRequiredCountOffsetCandyEffect : CandyEffect
{
    [SerializeField] private int requiredCountOffset = -1;

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

        context.ScoreboardManager.AddNextRequiredItemCountOffset(requiredCountOffset);
    }
}
