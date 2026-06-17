using System.Collections;
using UnityEngine;

public class CameraShakeController : MonoBehaviour
{
    [Header("画面揺れ設定")]
    [SerializeField] private float shakeFrequency = 35f; // 揺れの細かさ

    private Vector3 originalLocalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        // 初期ローカル位置を保存
        originalLocalPosition = transform.localPosition;
    }

    public void Shake(float strength, float duration)
    {
        strength = Mathf.Max(0f, strength);
        duration = Mathf.Max(0f, duration);

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.localPosition = originalLocalPosition;
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(strength, duration));
    }

    private IEnumerator ShakeCoroutine(float strength, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            // 時間経過に応じて揺れを弱くする
            float t = timer / duration;
            float currentStrength = Mathf.Lerp(strength, 0f, t);

            // ランダムな揺れ方向を作る
            Vector3 shakeOffset = Random.insideUnitSphere * currentStrength;

            // Z方向に大きく動くと見た目が不自然になりやすいので抑える
            shakeOffset.z *= 0.2f;

            transform.localPosition = originalLocalPosition + shakeOffset;

            yield return new WaitForSeconds(1f / shakeFrequency);
        }

        transform.localPosition = originalLocalPosition;
        shakeCoroutine = null;
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        transform.localPosition = originalLocalPosition;
    }
}