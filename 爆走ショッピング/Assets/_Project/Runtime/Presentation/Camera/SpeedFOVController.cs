using UnityEngine;

/// <summary>車両速度に応じてカメラの視野角を滑らかに変更します。</summary>
[RequireComponent(typeof(Camera))]
public class SpeedFOVController : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private Rigidbody targetRigidbody;

    // 水平速度を視野角へ対応付ける最小値・最大値・基準速度です。
    [Header("視野角設定")]
    [SerializeField] private float minFOV = 60f;
    [SerializeField] private float maxFOV = 85f;
    [SerializeField] private float maxSpeed = 30f;

    // 視野角の変化が滑らかに追従するまでの時間です。
    [Header("補間")]
    [SerializeField] private float smoothTime = 0.25f;

    private Camera cam;
    private float fovVelocity;

    // 視野角を変更するカメラの参照を取得します。
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // フレーム終盤で速度に対応する視野角へ更新します。
    private void LateUpdate()
    {
        if (targetRigidbody == null || cam == null)
        {
            return;
        }

        UpdateFOVBySpeed();
    }

    // 水平速度を設定範囲の視野角へ変換し、現在の視野角から滑らかに近づけます。
    private void UpdateFOVBySpeed()
    {
        Vector3 velocity = targetRigidbody.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        float speedRate = Mathf.Clamp01(currentSpeed / maxSpeed);
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speedRate);

        cam.fieldOfView = Mathf.SmoothDamp(
            cam.fieldOfView,
            targetFOV,
            ref fovVelocity,
            smoothTime
        );
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        smoothTime = Mathf.Max(0.01f, smoothTime);
        maxFOV = Mathf.Max(minFOV, maxFOV);
    }
}
