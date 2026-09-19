using UnityEngine;

/// <summary>衝突速度を振動・カメラ揺れ・効果音の強さへ変換します。</summary>
[RequireComponent(typeof(AudioSource))]
public class CollisionFeedbackManager : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private CameraShakeController cameraShakeController;
    [SerializeField] private GamepadRumbleManager rumbleManager;

    // 衝突強度に対応する振動の強さ・長さ・再生間隔です。
    [Header("振動設定")]
    [SerializeField] private float minRumbleStrength = 0.15f;
    [SerializeField] private float maxRumbleStrength = 1.0f;
    [SerializeField] private float minRumbleDuration = 0.08f;
    [SerializeField] private float maxRumbleDuration = 0.35f;
    [SerializeField] private float rumbleCooldown = 0.1f;

    // 衝突強度に対応する揺れの大きさ・長さ・再生間隔です。
    [Header("カメラ揺れ設定")]
    [SerializeField] private float minShakeStrength = 0.03f;
    [SerializeField] private float maxShakeStrength = 0.25f;
    [SerializeField] private float minShakeDuration = 0.08f;
    [SerializeField] private float maxShakeDuration = 0.35f;
    [SerializeField] private float shakeCooldown = 0.08f;

    // ランダム再生する音と、音量・ピッチ・再生間隔の設定です。
    [Header("効果音設定")]
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

    // 重量補正と衝突強度の計算を担当する設定を受け取ります。
    public void Initialize(ImpactSettings configuredImpactSettings)
    {
        impactSettings = configuredImpactSettings;
    }

    // 同じオブジェクトの音源を取得し、衝突音の再生設定を整えます。
    private void Awake()
    {
        ResolveReferences();
        ConfigureAudioSource();
    }

    // 衝突の相対速度または直接渡された速度を使い、しきい値以上なら振動・揺れ・音を再生します。
    public void PlayFeedback(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        PlayFeedback(collision.relativeVelocity.magnitude);
    }

    // 衝突の相対速度または直接渡された速度を使い、しきい値以上なら振動・揺れ・音を再生します。
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

    // このコンポーネントが使用するゲームパッドの振動を止めます。
    public void StopFeedback()
    {
        if (rumbleManager != null)
        {
            rumbleManager.StopRumble();
        }
    }

    // 同じオブジェクトにある AudioSource を取得します。
    private void ResolveReferences()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // 衝突音を起動時に鳴らさず、ループしない 2D 音源として設定します。
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

    // クールダウンを確認し、衝突強度に応じた強さと長さで振動させます。
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

    // クールダウンを確認し、衝突強度に応じたカメラ揺れを開始します。
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

    // 有効な音源とクールダウンを確認し、ランダムな衝突音を強度に応じた音量で再生します。
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

    // 無効化時に振動を停止します。
    private void OnDisable()
    {
        StopFeedback();
    }

    // アプリ終了時に振動を停止します。
    private void OnApplicationQuit()
    {
        StopFeedback();
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
