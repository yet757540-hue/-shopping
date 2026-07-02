using UnityEngine;

[RequireComponent(typeof(Camera))]
// プレイヤーの速度に応じてカメラの視野角を広げる演出コンポーネントです。
// 役割:
// - targetRigidbody の水平速度を見て、minFOV〜maxFOV の間で Camera.fieldOfView を補間します。
// 接続:
// - 実カメラに付け、targetRigidbody に Player の Rigidbody を指定します。
// 読むときの要点:
// - 垂直方向の速度は FOV に使わず、地面上の移動速度だけを参照します。
public class SpeedFOVController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody targetRigidbody;

    [Header("FOV Settings")]
    [SerializeField] private float minFOV = 60f;
    [SerializeField] private float maxFOV = 85f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.25f;

    private Camera cam;
    private float fovVelocity;

    // 同じ GameObject の Camera を取得します。
    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // ターゲットの物理更新後に FOV を滑らかに更新します。
    private void LateUpdate()
    {
        if (targetRigidbody == null || cam == null)
        {
            return;
        }

        UpdateFOVBySpeed();
    }

    // 水平速度を 0〜1 に正規化し、その割合で FOV を補間します。
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

    // FOV と補間時間が不正な値にならないようにします。
    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        smoothTime = Mathf.Max(0.01f, smoothTime);
        maxFOV = Mathf.Max(minFOV, maxFOV);
    }
}
