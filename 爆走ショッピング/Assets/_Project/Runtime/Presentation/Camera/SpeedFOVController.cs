using UnityEngine;

[RequireComponent(typeof(Camera))]
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

    private void Awake()
    {
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

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        smoothTime = Mathf.Max(0.01f, smoothTime);
        maxFOV = Mathf.Max(minFOV, maxFOV);
    }
}
