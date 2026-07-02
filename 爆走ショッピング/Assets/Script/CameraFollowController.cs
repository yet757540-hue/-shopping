using UnityEngine;
using UnityEngine.InputSystem;

// プレイヤーを追いかけるカメラ親オブジェクト用の制御です。
// 役割:
// - 通常時は target の向きに合わせた後方・上方位置へ滑らかに追従します。
// - L1 押下中は俯瞰カメラへ切り替え、右スティックで見下ろし位置を少し移動できます。
// 接続:
// - target には Player など追従対象の Transform を指定します。
// - cameraChild は実カメラの Transform で、CollisionFeedbackManager が揺れ用 CameraShakeController を探す入口にもなります。
// 読むときの要点:
// - LateUpdate で入力確認、位置追従、回転補間を順番に実行します。
// - useOnlyTargetYaw が true の場合、坂や傾きの影響を避けて Y 軸回転だけを追従に使います。
public class CameraFollowController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraChild;

    [Header("Offsets")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 7.8f, -18f);
    [SerializeField] private Vector3 localLookAtOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Follow Settings")]
    [SerializeField] private float followSmoothTime = 0.25f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private bool useOnlyTargetYaw = true;

    [Header("Overview Camera")]
    [SerializeField] private bool enableOverviewCamera = true;
    [SerializeField] private float overviewHeight = 45f;
    [SerializeField] private Vector3 overviewLookAtOffset = Vector3.zero;
    [SerializeField] private float overviewPanSmoothSpeed = 10f;
    [SerializeField] private float overviewPanMaxDistance = 18f;
    [SerializeField] private float overviewPanDeadZone = 0.15f;

    public Transform CameraChild => cameraChild;

    private Vector3 followVelocity;
    private Vector3 overviewPanOffset;

    // 実カメラ参照が未設定なら MainCamera を自動で拾います。
    private void Awake()
    {
        if (cameraChild == null && Camera.main != null)
        {
            cameraChild = Camera.main.transform;
        }
    }

    // プレイヤー移動後の位置を元にカメラを追従させるため、LateUpdate で更新します。
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // L1 の状態で通常カメラと俯瞰カメラの計算ルートを切り替えます。
        bool isOverviewHeld = IsOverviewHeld();
        UpdateOverviewPan(isOverviewHeld);
        SmoothFollow(isOverviewHeld);
        SmoothRotate(isOverviewHeld);
    }

    // 通常追従と俯瞰追従を切り替え、カメラ親の位置だけを更新します。
    private void SmoothFollow(bool isOverviewHeld)
    {
        if (isOverviewHeld)
        {
            SmoothFollowOverview();
            return;
        }

        Quaternion targetYawRotation = GetTargetYawRotation();
        Vector3 targetPosition = target.position + targetYawRotation * localOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );
    }

    // 通常視点と俯瞰視点で注視点を切り替え、カメラ親の回転だけを更新します。
    private void SmoothRotate(bool isOverviewHeld)
    {
        if (isOverviewHeld)
        {
            SmoothRotateOverview();
            return;
        }

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

    // 俯瞰時はプレイヤー頭上に固定高さで移動し、右スティックのパン分を足します。
    private void SmoothFollowOverview()
    {
        Vector3 targetPosition = target.position + overviewPanOffset + Vector3.up * overviewHeight;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref followVelocity,
            followSmoothTime
        );
    }

    // 俯瞰時はプレイヤー周辺を見下ろす向きへ補間します。
    private void SmoothRotateOverview()
    {
        Vector3 lookAtPoint = target.position + overviewPanOffset + overviewLookAtOffset;
        Vector3 direction = lookAtPoint - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, target.forward);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    // 俯瞰中だけ右スティック入力をパン移動量へ変換します。
    private void UpdateOverviewPan(bool isOverviewHeld)
    {
        if (!isOverviewHeld)
        {
            overviewPanOffset = Vector3.zero;
            return;
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        Vector2 input = gamepad.rightStick.ReadValue();
        Vector3 targetPanOffset = Vector3.zero;

        if (input.sqrMagnitude > overviewPanDeadZone * overviewPanDeadZone)
        {
            targetPanOffset = GetOverviewPanTargetOffset(input);
        }

        overviewPanOffset = Vector3.Lerp(
            overviewPanOffset,
            targetPanOffset,
            overviewPanSmoothSpeed * Time.deltaTime
        );
    }

    // 右スティック入力を、プレイヤーの向き基準の水平オフセットへ変換します。
    private Vector3 GetOverviewPanTargetOffset(Vector2 input)
    {
        Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
        Quaternion targetYawRotation = GetTargetYawRotation();
        Vector3 right = targetYawRotation * Vector3.right;
        Vector3 forward = targetYawRotation * Vector3.forward;

        // 俯瞰中のパン移動は水平面だけで行い、高さは overviewHeight に任せます。
        right.y = 0f;
        forward.y = 0f;
        right.Normalize();
        forward.Normalize();

        return (right * clampedInput.x + forward * clampedInput.y) * overviewPanMaxDistance;
    }

    // L1 が押されている間だけ俯瞰モードを有効にします。
    private bool IsOverviewHeld()
    {
        if (!enableOverviewCamera)
        {
            return false;
        }

        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return false;
        }

        return gamepad.leftShoulder.isPressed;
    }

    // 追従計算で使う対象の回転を返します。通常は水平回転だけを使います。
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

    // Inspector で極端な値が入っても実行時に破綻しない範囲へ補正します。
    private void OnValidate()
    {
        followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
        overviewHeight = Mathf.Max(1f, overviewHeight);
        overviewPanSmoothSpeed = Mathf.Max(0f, overviewPanSmoothSpeed);
        overviewPanMaxDistance = Mathf.Max(0f, overviewPanMaxDistance);
        overviewPanDeadZone = Mathf.Clamp01(overviewPanDeadZone);
    }
}
