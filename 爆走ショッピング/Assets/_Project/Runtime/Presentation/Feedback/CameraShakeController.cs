using UnityEngine;

/// <summary>カメラのローカル座標へ一時的な揺れを加えます。</summary>
[DisallowMultipleComponent]
public class CameraShakeController : MonoBehaviour
{
    // 揺れの細かさと、奥行き方向の揺れの割合です。
    [Header("揺れ設定")]
    [SerializeField] private float shakeFrequency = 35f;
    [SerializeField] private float zMultiplier = 0.2f;

    private Vector3 originalLocalPosition;
    private float shakeStrength;
    private float shakeDuration;
    private float shakeRemainingDuration;
    private Vector3 noiseSeed;

    // 揺れを戻す基準となるカメラのローカル位置を記録します。
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        noiseSeed = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );
    }

    // 揺れの残り時間を進め、基準位置に一時的な揺れを加えます。
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

    // 指定強度と持続時間でカメラ揺れを要求します。
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

    // 揺れの状態をクリアし、カメラを基準のローカル位置へ戻します。
    public void StopShake()
    {
        shakeStrength = 0f;
        shakeDuration = 0f;
        shakeRemainingDuration = 0f;
        transform.localPosition = originalLocalPosition;
    }

    // ノイズから連続的な揺れ方向を作り、指定強度を掛けて返します。
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

    // 無効化時にカメラを揺れのない基準位置へ戻します。
    private void OnDisable()
    {
        StopShake();
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        shakeFrequency = Mathf.Max(1f, shakeFrequency);
        zMultiplier = Mathf.Clamp01(zMultiplier);
    }
}
