using UnityEngine;

[DisallowMultipleComponent]
// カメラ本体またはカメラ子オブジェクトに付ける揺れ演出です。
// 役割:
// - 衝突などの外部イベントから Shake を呼ばれたとき、一定時間だけ localPosition を揺らします。
// - 揺れは PerlinNoise で作るため、完全なランダムよりも滑らかに見えます。
// 接続:
// - CollisionFeedbackManager から呼ばれる想定です。
// - CameraFollowController.CameraChild から自動取得または自動追加される場合があります。
// 読むときの要点:
// - originalLocalPosition を基準に毎フレーム offset を足し、終了時に必ず元の位置へ戻します。
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

    // 初期位置とノイズの種を保存し、揺れ終了時に正しい位置へ戻せるようにします。
    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
        noiseSeed = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );
    }

    // 揺れ時間が残っている間だけ、元の localPosition に揺れオフセットを足します。
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

    // 外部から揺れを開始します。より強い揺れだけが現在の揺れを上書きします。
    public void Shake(float strength, float duration)
    {
        strength = Mathf.Max(0f, strength);
        duration = Mathf.Max(0f, duration);

        if (strength <= 0f || duration <= 0f)
        {
            return;
        }

        // 既存の揺れより弱い揺れは上書きしないことで、強い衝突の余韻を優先します。
        if (shakeRemainingDuration <= 0f || strength >= shakeStrength)
        {
            shakeStrength = strength;
            shakeDuration = duration;
            shakeRemainingDuration = duration;
        }
    }

    // 揺れ状態を完全に消し、カメラを元のローカル位置へ戻します。
    public void StopShake()
    {
        shakeStrength = 0f;
        shakeDuration = 0f;
        shakeRemainingDuration = 0f;
        transform.localPosition = originalLocalPosition;
    }

    // PerlinNoise から滑らかな 3D 揺れオフセットを作ります。
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

    // 無効化時に位置ずれが残らないようにします。
    private void OnDisable()
    {
        StopShake();
    }

    // 揺れ周波数と Z 方向倍率を安全な範囲に保ちます。
    private void OnValidate()
    {
        shakeFrequency = Mathf.Max(1f, shakeFrequency);
        zMultiplier = Mathf.Clamp01(zMultiplier);
    }
}
