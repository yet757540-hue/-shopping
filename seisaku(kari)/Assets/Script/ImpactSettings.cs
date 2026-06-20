using UnityEngine;

[DisallowMultipleComponent]
public class ImpactSettings : MonoBehaviour
{
    [Header("Impact Speed Range")]
    [SerializeField] private float minImpactSpeed = 2f;
    [SerializeField] private float maxImpactSpeed = 20f;

    public bool IsStrongEnough(float impactSpeed)
    {
        return impactSpeed >= minImpactSpeed;
    }

    public float GetImpactRate(float impactSpeed)
    {
        return Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );
    }

    private void OnValidate()
    {
        minImpactSpeed = Mathf.Max(0f, minImpactSpeed);
        maxImpactSpeed = Mathf.Max(minImpactSpeed + 0.01f, maxImpactSpeed);
    }
}
