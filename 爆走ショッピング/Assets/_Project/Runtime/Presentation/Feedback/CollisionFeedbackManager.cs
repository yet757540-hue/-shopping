using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionFeedbackManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private CameraShakeController cameraShakeController;
    [SerializeField] private GamepadRumbleManager rumbleManager;

    [Header("Rumble Settings")]
    [SerializeField] private float minRumbleStrength = 0.15f;
    [SerializeField] private float maxRumbleStrength = 1.0f;
    [SerializeField] private float minRumbleDuration = 0.08f;
    [SerializeField] private float maxRumbleDuration = 0.35f;
    [SerializeField] private float rumbleCooldown = 0.1f;

    [Header("Camera Shake Settings")]
    [SerializeField] private float minShakeStrength = 0.03f;
    [SerializeField] private float maxShakeStrength = 0.25f;
    [SerializeField] private float minShakeDuration = 0.08f;
    [SerializeField] private float maxShakeDuration = 0.35f;
    [SerializeField] private float shakeCooldown = 0.08f;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip[] collisionClips;
    [SerializeField] private float minCollisionVolume = 0.2f;
    [SerializeField] private float maxCollisionVolume = 1.0f;
    [SerializeField] private float minCollisionPitch = 0.9f;
    [SerializeField] private float maxCollisionPitch = 1.1f;
    [SerializeField] private float soundCooldown = 0.08f;

    private AudioSource audioSource;
    private float lastRumbleTime = -999f;
    private float lastShakeTime = -999f;
    private float lastSoundTime = -999f;

    public void Initialize(ImpactSettings configuredImpactSettings)
    {
        impactSettings = configuredImpactSettings;
    }

    private void Awake()
    {
        ResolveReferences();
        ConfigureAudioSource();
    }

    public void PlayFeedback(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        PlayFeedback(collision.relativeVelocity.magnitude);
    }

    public void PlayFeedback(float impactSpeed)
    {
        if (impactSettings == null)
        {
            return;
        }

        float impactRate = impactSettings.GetImpactRateFromRawSpeed(impactSpeed);

        if (!impactSettings.IsStrongEnough(impactSettings.LastAdjustedImpactSpeed))
        {
            return;
        }

        TryStartRumble(impactRate);
        TryStartCameraShake(impactRate);
        TryPlayCollisionSound(impactRate);
    }

    public void StopFeedback()
    {
        if (rumbleManager != null)
        {
            rumbleManager.StopRumble();
        }
    }

    private void ResolveReferences()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
    }

    private void TryStartRumble(float impactRate)
    {
        if (rumbleManager == null || Time.time - lastRumbleTime < rumbleCooldown)
        {
            return;
        }

        lastRumbleTime = Time.time;
        float rumbleStrength = Mathf.Lerp(minRumbleStrength, maxRumbleStrength, impactRate);
        float rumbleDuration = Mathf.Lerp(minRumbleDuration, maxRumbleDuration, impactRate);
        rumbleManager.Rumble(rumbleStrength * 0.7f, rumbleStrength, rumbleDuration);
    }

    private void TryStartCameraShake(float impactRate)
    {
        if (cameraShakeController == null || Time.time - lastShakeTime < shakeCooldown)
        {
            return;
        }

        lastShakeTime = Time.time;
        float shakeStrength = Mathf.Lerp(minShakeStrength, maxShakeStrength, impactRate);
        float shakeDuration = Mathf.Lerp(minShakeDuration, maxShakeDuration, impactRate);
        cameraShakeController.Shake(shakeStrength, shakeDuration);
    }

    private void TryPlayCollisionSound(float impactRate)
    {
        if (audioSource == null || collisionClips == null || collisionClips.Length == 0)
        {
            return;
        }

        if (Time.time - lastSoundTime < soundCooldown)
        {
            return;
        }

        AudioClip clip = collisionClips[Random.Range(0, collisionClips.Length)];

        if (clip == null)
        {
            return;
        }

        lastSoundTime = Time.time;
        audioSource.pitch = Random.Range(minCollisionPitch, maxCollisionPitch);
        audioSource.PlayOneShot(clip, Mathf.Lerp(minCollisionVolume, maxCollisionVolume, impactRate));
    }

    private void OnDisable()
    {
        StopFeedback();
    }

    private void OnApplicationQuit()
    {
        StopFeedback();
    }

    private void OnValidate()
    {
        rumbleCooldown = Mathf.Max(0f, rumbleCooldown);
        shakeCooldown = Mathf.Max(0f, shakeCooldown);
        soundCooldown = Mathf.Max(0f, soundCooldown);
        minRumbleDuration = Mathf.Max(0f, minRumbleDuration);
        maxRumbleDuration = Mathf.Max(minRumbleDuration, maxRumbleDuration);
        minShakeDuration = Mathf.Max(0f, minShakeDuration);
        maxShakeDuration = Mathf.Max(minShakeDuration, maxShakeDuration);
        minCollisionPitch = Mathf.Max(0.01f, minCollisionPitch);
        maxCollisionPitch = Mathf.Max(minCollisionPitch, maxCollisionPitch);
    }
}
