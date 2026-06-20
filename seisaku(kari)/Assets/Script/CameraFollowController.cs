using UnityEngine;

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

    public Transform CameraChild => cameraChild;

    private Vector3 followVelocity;

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

        SmoothFollow();
        SmoothRotate();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void SmoothFollow()
    {
        Quaternion targetYawRotation = GetTargetYawRotation();
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
    }
}
