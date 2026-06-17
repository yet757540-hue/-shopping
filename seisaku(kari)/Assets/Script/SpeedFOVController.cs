using UnityEngine;

public class SpeedFOVController : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Rigidbody targetRigidbody; // プレイヤーのRigidbody

    [Header("FOV設定")]
    [SerializeField] private float minFOV = 60f;        // 最低速度時のFOV
    [SerializeField] private float maxFOV = 85f;        // 最高速度時のFOV
    [SerializeField] private float maxSpeed = 30f;      // FOVが最大になる速度

    [Header("補間設定")]
    [SerializeField] private float smoothTime = 0.25f;  // FOV変化の滑らかさ

    private Camera cam;
    private float fovVelocity;

    private void Awake()
    {
        // このオブジェクトについているCameraを取得
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (targetRigidbody == null || cam == null)
        {
            return;
        }

        UpdateFOVBySpeed();
    }

    private void UpdateFOVBySpeed()
    {
        // Rigidbodyの現在速度を取得
        Vector3 velocity = targetRigidbody.linearVelocity;

        // Y方向を除外して、水平方向の速度だけ使う
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);

        // 現在の速度
        float currentSpeed = horizontalVelocity.magnitude;

        // 速度を0～1の範囲に変換
        float speedRate = Mathf.Clamp01(currentSpeed / maxSpeed);

        // 速度に応じた目標FOVを計算
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speedRate);

        // FOVを滑らかに変更する
        cam.fieldOfView = Mathf.SmoothDamp(
            cam.fieldOfView,
            targetFOV,
            ref fovVelocity,
            smoothTime
        );
    }
}