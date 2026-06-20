using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraShakeController : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float shakeFrequency = 35f;
    [SerializeField] private float zMultiplier = 0.2f;

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    public void Shake(float strength, float duration)
    {
        strength = Mathf.Max(0f, strength);
        duration = Mathf.Max(0f, duration);

        StopShake();

        if (strength <= 0f || duration <= 0f)
        {
            return;
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(strength, duration));
    }

    public void StopShake()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition = originalLocalPosition;
    }

    private IEnumerator ShakeCoroutine(float strength, float duration)
    {
        float timer = 0f;
        float interval = 1f / Mathf.Max(1f, shakeFrequency);

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress = Mathf.Clamp01(timer / duration);
            float currentStrength = Mathf.Lerp(strength, 0f, progress);
            Vector3 shakeOffset = Random.insideUnitSphere * currentStrength;
            shakeOffset.z *= zMultiplier;

            transform.localPosition = originalLocalPosition + shakeOffset;

            yield return new WaitForSeconds(interval);
        }

        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
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
