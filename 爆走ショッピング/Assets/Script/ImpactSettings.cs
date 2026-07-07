using UnityEngine;

[DisallowMultipleComponent]
public class ImpactSettings : MonoBehaviour
{
    [Header("Impact Speed Range")]
    [SerializeField] private float minImpactSpeed = 2f;
    [SerializeField] private float maxImpactSpeed = 20f;

    [Header("Runtime Influence")]
    [SerializeField] private float loadImpactMultiplier = 1f;
    [SerializeField] private float lastRawImpactSpeed = 0f;
    [SerializeField] private float lastAdjustedImpactSpeed = 0f;
    [SerializeField] private float lastImpactRate = 0f;

    public float LoadImpactMultiplier => loadImpactMultiplier;
    public float LastRawImpactSpeed => lastRawImpactSpeed;
    public float LastAdjustedImpactSpeed => lastAdjustedImpactSpeed;
    public float LastImpactRate => lastImpactRate;

    public void SetLoadImpactMultiplier(float multiplier)
    {
        loadImpactMultiplier = Mathf.Max(0f, multiplier);
    }

    public float GetAdjustedImpactSpeed(float rawImpactSpeed)
    {
        lastRawImpactSpeed = Mathf.Max(0f, rawImpactSpeed);
        lastAdjustedImpactSpeed = lastRawImpactSpeed * loadImpactMultiplier;
        return lastAdjustedImpactSpeed;
    }

    public bool IsStrongEnough(float impactSpeed)
    {
        return impactSpeed >= minImpactSpeed;
    }

    public float GetImpactRate(float impactSpeed)
    {
        lastImpactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );
        return lastImpactRate;
    }

    public float GetImpactRateFromRawSpeed(float rawImpactSpeed)
    {
        float adjustedImpactSpeed = GetAdjustedImpactSpeed(rawImpactSpeed);
        return GetImpactRate(adjustedImpactSpeed);
    }

    private void OnValidate()
    {
        minImpactSpeed = Mathf.Max(0f, minImpactSpeed);
        maxImpactSpeed = Mathf.Max(minImpactSpeed + 0.01f, maxImpactSpeed);
        loadImpactMultiplier = Mathf.Max(0f, loadImpactMultiplier);
    }
}
