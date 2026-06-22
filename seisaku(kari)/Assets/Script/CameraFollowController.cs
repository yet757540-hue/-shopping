using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        if (cameraChild == null && Camera.main != null)
        {
            cameraChild = Camera.main.transform;
        }
    }

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
