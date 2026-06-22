using UnityEngine;

[DisallowMultipleComponent]
public class CameraShakeController : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeFrequency = 35f;
    [SerializeField] private float zMultiplier = 0.2f;

    private Vector3 originalLocalPosition;
    private float shakeStrength;
    private float shakeDuration;
    private float shakeRemainingDuration;
    private Vector3 noiseSeed;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        noiseSeed = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );
    }

    private void LateUpdate()
    {
        if (shakeRemainingDuration <= 0f)
        {
            transform.localPosition = originalLocalPosition;
            return;
        }

        shakeRemainingDuration -= Time.deltaTime;
        float progress = 1f - Mathf.Clamp01(shakeRemainingDuration / Mathf.Max(0.01f, shakeDuration));
        float currentStrength = shakeStrength * (1f - Mathf.SmoothStep(0f, 1f, progress));
        transform.localPosition = originalLocalPosition + GetSmoothShakeOffset(currentStrength);

        if (shakeRemainingDuration <= 0f)
        {
            transform.localPosition = originalLocalPosition;
        }
    }

    public void Shake(float strength, float duration)
    {
        strength = Mathf.Max(0f, strength);
        duration = Mathf.Max(0f, duration);

        if (strength <= 0f || duration <= 0f)
        {
            return;
        }

        if (shakeRemainingDuration <= 0f || strength >= shakeStrength)
        {
            shakeStrength = strength;
            shakeDuration = duration;
            shakeRemainingDuration = duration;
        }
    }

    public void StopShake()
    {
        shakeStrength = 0f;
        shakeDuration = 0f;
        shakeRemainingDuration = 0f;
        transform.localPosition = originalLocalPosition;
    }

    private Vector3 GetSmoothShakeOffset(float strength)
    {
        float time = Time.time * shakeFrequency;
        float x = Mathf.PerlinNoise(noiseSeed.x, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(noiseSeed.y, time) * 2f - 1f;
        float z = Mathf.PerlinNoise(noiseSeed.z, time) * 2f - 1f;
        Vector3 offset = new Vector3(x, y, z * zMultiplier);

        if (offset.sqrMagnitude > 1f)
        {
            offset.Normalize();
        }

        return offset * strength;
    }

    private void OnDisable()
    {
        StopShake();
    }

    private void OnValidate()
    {
        shakeFrequency = Mathf.Max(1f, shakeFrequency);
        zMultiplier = Mathf.Clamp01(zMultiplier);
    }
}
