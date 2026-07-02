using UnityEngine;

[RequireComponent(typeof(AudioSource))]
// 衝突の強さを、音・振動・カメラ揺れへ変換するまとめ役です。
// 役割:
// - PlayerCollisionReporter などから衝突情報を受け取り、ImpactSettings で衝突強度を正規化します。
// - 強度に応じて GamepadRumbleManager、CameraShakeController、AudioSource を動かします。
// 接続:
// - ImpactSettings は衝突しきい値と荷物重量による倍率を持ちます。
// - GamepadRumbleManager と CameraShakeController は未設定ならシーンから探し、必要に応じて作成します。
// - AudioSource は同じ GameObject に必要です。
// 読むときの要点:
// - PlayFeedback(float) が入口で、impactRate 0〜1 を各演出の強さと長さへ変換します。
// - 各演出には cooldown があり、連続衝突で音や振動が過密にならないようにしています。
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

    // 起動時に必要な参照をそろえ、AudioSource を衝突音用に初期化します。
    private void Awake()
    {
        ResolveReferences();
        ConfigureAudioSource();
    }

    // Collision から相対速度を取り出して、速度ベースの演出処理へ渡します。
    public void PlayFeedback(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        PlayFeedback(collision.relativeVelocity.magnitude);
    }

    // 衝突速度を正規化し、振動・カメラ揺れ・音へ分配します。
    public void PlayFeedback(float impactSpeed)
    {
        if (impactSettings == null)
        {
            return;
        }

        float impactRate = impactSettings.GetImpactRateFromRawSpeed(impactSpeed);

        // 荷物重量の補正後でもしきい値未満なら、演出は出しません。
        if (!impactSettings.IsStrongEnough(impactSettings.LastAdjustedImpactSpeed))
        {
            return;
        }

        TryStartRumble(impactRate);
        TryStartCameraShake(impactRate);
        TryPlayCollisionSound(impactRate);
    }

    // 現在進行中のフィードバックを停止します。主に振動の残りを消す用途です。
    public void StopFeedback()
    {
        if (rumbleManager != null)
        {
            rumbleManager.StopRumble();
        }
    }

    // 未設定の参照をシーンから探し、必要なら同じ GameObject に補助コンポーネントを追加します。
    private void ResolveReferences()
    {
        audioSource = GetComponent<AudioSource>();

        if (impactSettings == null)
        {
            impactSettings = FindAnyObjectByType<ImpactSettings>();
        }

        if (impactSettings == null)
        {
            impactSettings = gameObject.AddComponent<ImpactSettings>();
        }

        if (rumbleManager == null)
        {
            rumbleManager = FindAnyObjectByType<GamepadRumbleManager>();
        }

        if (rumbleManager == null)
        {
            rumbleManager = gameObject.AddComponent<GamepadRumbleManager>();
        }

        if (cameraShakeController == null)
        {
            cameraShakeController = FindAnyObjectByType<CameraShakeController>();
        }

        if (cameraShakeController == null)
        {
            CameraFollowController cameraFollowController = FindAnyObjectByType<CameraFollowController>();
            Transform cameraChild = cameraFollowController != null ? cameraFollowController.CameraChild : null;

            // カメラ追従親ではなく実カメラ側を揺らすため、CameraChild を優先します。
            if (cameraChild != null)
            {
                cameraShakeController = cameraChild.GetComponent<CameraShakeController>();

                if (cameraShakeController == null)
                {
                    cameraShakeController = cameraChild.gameObject.AddComponent<CameraShakeController>();
                }
            }
        }
    }

    // AudioSource を UI 的な 2D 衝突音として鳴る設定にします。
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

    // impactRate に応じてゲームパッド振動の強さと長さを決めます。
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

    // impactRate に応じてカメラ揺れの強さと長さを決めます。
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

    // impactRate に応じて衝突音量を変え、ピッチを少しランダム化します。
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

    // 無効化時に振動が残らないようにします。
    private void OnDisable()
    {
        StopFeedback();
    }

    // アプリ終了時にも振動停止を保証します。
    private void OnApplicationQuit()
    {
        StopFeedback();
    }

    // Inspector の値を、長さやピッチとして成立する範囲へ補正します。
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
