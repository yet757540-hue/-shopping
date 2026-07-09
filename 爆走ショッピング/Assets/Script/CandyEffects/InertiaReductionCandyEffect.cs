using UnityEngine;

[CreateAssetMenu(menuName = "Game/Candy Effects/Inertia Reduction", fileName = "Inertia Reduction Candy Effect")]
public sealed class InertiaReductionCandyEffect : CandyEffect
{
    [SerializeField] private float influenceFactorMultiplier = 0.5f;

    public override bool CanApply(CandyEffectContext context)
    {
        return base.CanApply(context) && context.InventoryInfluenceSettings != null;
    }

    public override void Apply(CandyEffectContext context)
    {
        if (!CanApply(context))
        {
            return;
        }

        context.InventoryInfluenceSettings.MultiplyAllInfluenceFactors(influenceFactorMultiplier);
    }

    private void OnValidate()
    {
        influenceFactorMultiplier = Mathf.Max(0f, influenceFactorMultiplier);
    }
}
