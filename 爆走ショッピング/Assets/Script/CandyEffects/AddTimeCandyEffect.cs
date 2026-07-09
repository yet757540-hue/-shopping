using UnityEngine;

[CreateAssetMenu(menuName = "Game/Candy Effects/Add Time", fileName = "Add Time Candy Effect")]
public sealed class AddTimeCandyEffect : CandyEffect
{
    [SerializeField] private float seconds = 15f;

    public override bool CanApply(CandyEffectContext context)
    {
        return base.CanApply(context) && context.TimerManager != null;
    }

    public override void Apply(CandyEffectContext context)
    {
        if (!CanApply(context))
        {
            return;
        }

        context.TimerManager.AddTime(seconds);
    }

    private void OnValidate()
    {
        seconds = Mathf.Max(0f, seconds);
    }
}
