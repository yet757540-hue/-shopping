using UnityEngine;

[CreateAssetMenu(menuName = "Game/Candy Effects/Running Speed Up", fileName = "Running Speed Up Candy Effect")]
public sealed class RunningSpeedUpCandyEffect : CandyEffect
{
    [SerializeField] private float accelerationSwitchSpeedMultiplier = 1.1f;

    public override bool CanApply(CandyEffectContext context)
    {
        return base.CanApply(context) && context.Player != null;
    }

    public override void Apply(CandyEffectContext context)
    {
        if (!CanApply(context))
        {
            return;
        }

        context.Player.MultiplyAccelerationSwitchSpeed(accelerationSwitchSpeedMultiplier);
    }

    private void OnValidate()
    {
        accelerationSwitchSpeedMultiplier = Mathf.Max(0f, accelerationSwitchSpeedMultiplier);
    }
}
