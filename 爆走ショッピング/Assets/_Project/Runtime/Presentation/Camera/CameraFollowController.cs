using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>プレイヤー追従と、ボタン押下中の俯瞰カメラを制御します。</summary>
public class CameraFollowController : MonoBehaviour
{
    // 追従するプレイヤーと、使用する子カメラの参照です。
    [Header("追従対象")]
    [SerializeField] private Transform target;
    [SerializeField] private Transform cameraChild;

    // プレイヤーから見たカメラ位置と注視点の相対位置です。
    [Header("オフセット")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 7.8f, -18f);
    [SerializeField] private Vector3 localLookAtOffset = new Vector3(0f, 1.2f, 0f);

    // 位置と向きの追従速度、および水平回転だけを使うかを指定します。
    [Header("追従設定")]
    [SerializeField] private float followSmoothTime = 0.25f;
    [SerializeField] private float rotationSmoothSpeed = 8f;
    [SerializeField] private bool useOnlyTargetYaw = true;

    // 俯瞰表示の有効状態、高さ、移動距離、入力しきい値を設定します。
    [Header("俯瞰カメラ")]
    [SerializeField] private bool enableOverviewCamera = true;
    [SerializeField] private float overviewHeight = 45f;
    [SerializeField] private Vector3 overviewLookAtOffset = Vector3.zero;
    [SerializeField] private float overviewPanSmoothSpeed = 10f;
    [SerializeField] private float overviewPanMaxDistance = 18f;
    [SerializeField] private float overviewPanDeadZone = 0.15f;

    // カメラリグ内で使用する子カメラの Transform を公開します。
    public Transform CameraChild => cameraChild;

    private Vector3 followVelocity;
    private Vector3 overviewPanOffset;

    // 子カメラが未設定ならメインカメラを使用します。
    private void Awake()
    {
        if (cameraChild == null && Camera.main != null)
        {
            cameraChild = Camera.main.transform;
        }
    }

    // プレイヤー更新後に、俯瞰入力・位置追従・向きの順でカメラを更新します。
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        bool isOverviewHeld = IsOverviewHeld();
        UpdateOverviewPan(isOverviewHeld);
        SmoothFollow(isOverviewHeld);
        SmoothRotate(isOverviewHeld);
    }

    // 通常時はプレイヤー基準のオフセットへ追従し、俯瞰中は専用の位置処理へ切り替えます。
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

    // プレイヤー付近の注視点へ滑らかに向けます。俯瞰中は専用の回転処理を使います。
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

    // プレイヤーの上方へ、俯瞰用の移動量を加えた位置まで滑らかに追従します。
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

    // 俯瞰の注視点へ向け、画面の上方向をプレイヤーの前方に合わせます。
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

    // 俯瞰中の右スティックを移動量へ変換し、通常視点に戻ったら移動量を解除します。
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

    // スティック入力をプレイヤーの水平な右・前方向へ変換し、移動距離を制限します。
    private Vector3 GetOverviewPanTargetOffset(Vector2 input)
    {
        Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);
        Quaternion targetYawRotation = GetTargetYawRotation();
        Vector3 right = targetYawRotation * Vector3.right;
        Vector3 forward = targetYawRotation * Vector3.forward;

        right.y = 0f;
        forward.y = 0f;
        right.Normalize();
        forward.Normalize();

        return (right * clampedInput.x + forward * clampedInput.y) * overviewPanMaxDistance;
    }

    // 俯瞰機能が有効で、左ショルダーボタンが押されているか確認します。
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

    // 設定に応じて対象の水平回転だけ、または全回転を取得します。
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

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
