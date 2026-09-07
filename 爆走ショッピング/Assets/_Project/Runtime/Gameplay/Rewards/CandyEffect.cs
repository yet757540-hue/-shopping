using UnityEngine;

public abstract class CandyEffect : ScriptableObject
{
    [SerializeField] private string effectName = "Candy Effect";
    [SerializeField] private string description = string.Empty;

    public string EffectName => string.IsNullOrWhiteSpace(effectName) ? name : effectName;
    public string Description => description;

    public virtual bool CanApply(CandyEffectContext context)
    {
        return context != null;
    }

    public abstract void Apply(CandyEffectContext context);
}
