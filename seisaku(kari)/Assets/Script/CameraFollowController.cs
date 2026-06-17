using UnityEngine;

public class CameraFollowController : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] private Transform target;       // プレイヤー
    [SerializeField] private Transform cameraChild;  // Main Camera

    [Header("相対位置設定")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 7.8f, -18f);
    // プレイヤーから見たカメラRigの相対位置

    [Header("注視点設定")]
    [SerializeField] private Vector3 localLookAtOffset = new Vector3(0f, 1.2f, 0f);
    // プレイヤーから見た注視点

    [Header("追従設定")]
    [SerializeField] private float followSmoothTime = 0.25f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private bool useOnlyTargetYaw = true;

    [Header("画面揺れ設定")]
    [SerializeField] private bool enableShake = true;
    [SerializeField] private float shakeFrequency = 45f;      // 揺れの細かさ
    [SerializeField] private float shakeZMultiplier = 0.2f;   // Z方向の揺れの弱さ
    [SerializeField] private bool showDebugLog = true;

    private Vector3 followVelocity;

    private Vector3 cameraOriginalLocalPosition;

    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeStrength = 0f;
    private float shakeSeedX;
    private float shakeSeedY;
    private float shakeSeedZ;

    private void Awake()
    {
        if (cameraChild == null && Camera.main != null)
        {
            cameraChild = Camera.main.transform;
        }

        if (cameraChild != null)
        {
            cameraOriginalLocalPosition = cameraChild.localPosition;
        }

        shakeSeedX = Random.Range(0f, 100f);
        shakeSeedY = Random.Range(0f, 100f);
        shakeSeedZ = Random.Range(0f, 100f);

        if (showDebugLog)
        {
            Debug.Log("[CameraFollowController] Awake");
            Debug.Log("[CameraFollowController] Camera Child: " +
                      (cameraChild != null ? cameraChild.name : "null"));
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        SmoothFollow();
        SmoothRotate();
        UpdateCameraShake();
    }

    private void SmoothFollow()
    {
        Quaternion targetYawRotation = GetTargetYawRotation();

        // プレイヤーの向きに応じて相対位置を回転させる
        Vector3 targetPosition = target.position + targetYawRotation * localOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );
    }

    private void SmoothRotate()
    {
        Quaternion targetYawRotation = GetTargetYawRotation();

        Vector3 lookAtPoint = target.position + targetYawRotation * localLookAtOffset;
        Vector3 direction = lookAtPoint - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    private void UpdateCameraShake()
    {
        if (!enableShake || cameraChild == null)
        {
            return;
        }

        if (shakeTimer <= 0f)
        {
            cameraChild.localPosition = cameraOriginalLocalPosition;
            return;
        }

        shakeTimer -= Time.deltaTime;

        float progress = 1f - Mathf.Clamp01(shakeTimer / shakeDuration);

        // 時間経過で揺れを弱くする
        float currentStrength = Mathf.Lerp(shakeStrength, 0f, progress);

        float time = Time.time * shakeFrequency;

        // PerlinNoiseでランダムだが滑らかな揺れを作る
        float x = Mathf.PerlinNoise(shakeSeedX, time) * 2f - 1f;
        float y = Mathf.PerlinNoise(shakeSeedY, time) * 2f - 1f;
        float z = Mathf.PerlinNoise(shakeSeedZ, time) * 2f - 1f;

        Vector3 shakeOffset = new Vector3(
            x * currentStrength,
            y * currentStrength,
            z * currentStrength * shakeZMultiplier
        );

        cameraChild.localPosition = cameraOriginalLocalPosition + shakeOffset;

        if (shakeTimer <= 0f)
        {
            cameraChild.localPosition = cameraOriginalLocalPosition;

            if (showDebugLog)
            {
                Debug.Log("[CameraFollowController] Shake終了");
            }
        }
    }

    public void Shake(float strength, float duration)
    {
        if (!enableShake)
        {
            return;
        }

        if (cameraChild == null)
        {
            Debug.LogWarning("[CameraFollowController] cameraChild が設定されていません。");
            return;
        }

        shakeStrength = Mathf.Max(0f, strength);
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;

        if (showDebugLog)
        {
            Debug.Log("[CameraFollowController] Shake開始 / 強度: " + shakeStrength + " / 時間: " + shakeDuration);
        }
    }

    [ContextMenu("Test Shake")]
    private void TestShake()
    {
        Shake(0.5f, 0.5f);
    }

    private Quaternion GetTargetYawRotation()
    {
        if (target == null)
        {
            return Quaternion.identity;
        }

        if (useOnlyTargetYaw)
        {
            return Quaternion.Euler(0f, target.eulerAngles.y, 0f);
        }

        return target.rotation;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}