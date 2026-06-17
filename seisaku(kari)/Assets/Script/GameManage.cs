using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("参照設定")]
    [SerializeField] private CameraFollowController cameraFollowController; // 画面揺れ用

    [Header("衝突設定")]
    [SerializeField] private float minImpactSpeed = 2f;        // 振動・音が発生する最小衝突速度
    [SerializeField] private float maxImpactSpeed = 20f;       // 最大反応になる衝突速度

    [Header("手柄振動設定")]
    [SerializeField] private float minRumbleStrength = 0.15f;  // 最小振動強度
    [SerializeField] private float maxRumbleStrength = 1.0f;   // 最大振動強度
    [SerializeField] private float minRumbleDuration = 0.08f;  // 最小振動時間
    [SerializeField] private float maxRumbleDuration = 0.35f;  // 最大振動時間
    [SerializeField] private float rumbleCooldown = 0.1f;      // 連続振動防止用クールタイム

    [Header("画面揺れ設定")]
    [SerializeField] private float minShakeStrength = 0.03f;   // 最小画面揺れ強度
    [SerializeField] private float maxShakeStrength = 0.25f;   // 最大画面揺れ強度
    [SerializeField] private float minShakeDuration = 0.08f;   // 最小画面揺れ時間
    [SerializeField] private float maxShakeDuration = 0.35f;   // 最大画面揺れ時間

    [Header("衝突音設定")]
    [SerializeField] private AudioClip[] collisionClips;       // 衝突音リスト
    [SerializeField] private float minCollisionVolume = 0.2f;  // 最小音量
    [SerializeField] private float maxCollisionVolume = 1.0f;  // 最大音量
    [SerializeField] private float minCollisionPitch = 0.9f;   // 最小ピッチ
    [SerializeField] private float maxCollisionPitch = 1.1f;   // 最大ピッチ
    [SerializeField] private float soundCooldown = 0.08f;      // 連続再生防止用クールタイム

    private AudioSource audioSource;

    private Coroutine rumbleCoroutine;
    private float lastRumbleTime = -999f;
    private float lastSoundTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] 既にInstanceが存在するため、このGameManagerを削除します。");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // 衝突音を2D音声として再生する
        // GameManagerがカメラから離れていても聞こえるようにする
        audioSource.spatialBlend = 0f;

        // 音量を最大にしておく
        audioSource.volume = 1f;

        Debug.Log("[GameManager] Instance を設定しました。");
        Debug.Log("[GameManager] AudioSource を取得しました: " + audioSource.name);
    }

    public void OnPlayerCollision(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;

        Debug.Log("[GameManager] 衝突速度: " + impactSpeed);

        // 衝突が弱すぎる場合は何もしない
        if (impactSpeed < minImpactSpeed)
        {
            Debug.Log("[GameManager] 衝突速度が小さいため、反応しません。");
            return;
        }

        // 衝突速度を0～1に変換
        float impactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );

        // 手柄震动
        TryStartRumble(impactRate);

        // 屏幕震动
        TryStartCameraShake(impactRate);

        // 碰撞音效
        TryPlayCollisionSound(impactRate, collision);
    }

    private void TryStartRumble(float impactRate)
    {
        if (Time.time - lastRumbleTime < rumbleCooldown)
        {
            Debug.Log("[GameManager] クールタイム中のため、手柄振動しません。");
            return;
        }

        lastRumbleTime = Time.time;

        float rumbleStrength = Mathf.Lerp(
            minRumbleStrength,
            maxRumbleStrength,
            impactRate
        );

        float rumbleDuration = Mathf.Lerp(
            minRumbleDuration,
            maxRumbleDuration,
            impactRate
        );

        Debug.Log("[GameManager] 手柄振動強度: " + rumbleStrength);
        Debug.Log("[GameManager] 手柄振動時間: " + rumbleDuration);

        StartRumble(rumbleStrength, rumbleDuration);
    }

    private void TryStartCameraShake(float impactRate)
    {
        if (cameraFollowController == null)
        {
            Debug.LogWarning("[GameManager] CameraFollowController が設定されていません。");
            return;
        }

        float shakeStrength = Mathf.Lerp(
            minShakeStrength,
            maxShakeStrength,
            impactRate
        );

        float shakeDuration = Mathf.Lerp(
            minShakeDuration,
            maxShakeDuration,
            impactRate
        );

        Debug.Log("[GameManager] 画面揺れ強度: " + shakeStrength);
        Debug.Log("[GameManager] 画面揺れ時間: " + shakeDuration);

        cameraFollowController.Shake(shakeStrength, shakeDuration);
    }

    private void TryPlayCollisionSound(float impactRate, Collision collision)
    {
        Debug.Log("[GameManager] TryPlayCollisionSound が呼ばれました。");

        if (audioSource == null)
        {
            Debug.LogWarning("[GameManager] audioSource が null です。GameManagerにAudioSourceがあるか確認してください。");
            return;
        }

        if (collisionClips == null)
        {
            Debug.LogWarning("[GameManager] collisionClips が null です。");
            return;
        }

        if (collisionClips.Length == 0)
        {
            Debug.LogWarning("[GameManager] Collision Clips が空です。Inspectorで音声ファイルを設定してください。");
            return;
        }

        if (Time.time - lastSoundTime < soundCooldown)
        {
            Debug.Log("[GameManager] クールタイム中のため、衝突音を再生しません。");
            return;
        }

        lastSoundTime = Time.time;

        AudioClip clip = collisionClips[Random.Range(0, collisionClips.Length)];

        if (clip == null)
        {
            Debug.LogWarning("[GameManager] 選択されたAudioClipがnullです。Collision Clipsに空欄がないか確認してください。");
            return;
        }

        float volume = Mathf.Lerp(
            minCollisionVolume,
            maxCollisionVolume,
            impactRate
        );

        float pitch = Random.Range(
            minCollisionPitch,
            maxCollisionPitch
        );

        audioSource.pitch = pitch;

        Debug.Log(
            "[GameManager] 衝突音再生準備 / Clip: " +
            clip.name +
            " / Volume: " +
            volume +
            " / Pitch: " +
            pitch +
            " / SpatialBlend: " +
            audioSource.spatialBlend
        );

        audioSource.PlayOneShot(clip, volume);

        Debug.Log("[GameManager] PlayOneShot を実行しました。");
    }

    private void StartRumble(float strength, float duration)
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            Debug.LogWarning("[GameManager] Gamepad.current が null です。手柄が認識されていません。");
            return;
        }

        strength = Mathf.Clamp01(strength);
        duration = Mathf.Max(0f, duration);

        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
        }

        rumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, strength, duration));
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float strength, float duration)
    {
        Debug.Log("[GameManager] 手柄振動開始");

        // 左モーター：重い振動
        // 右モーター：細かい振動
        gamepad.SetMotorSpeeds(strength * 0.7f, strength);

        yield return new WaitForSeconds(duration);

        gamepad.SetMotorSpeeds(0f, 0f);

        Debug.Log("[GameManager] 手柄振動停止");

        rumbleCoroutine = null;
    }

    private void OnDisable()
    {
        StopRumble();
    }

    private void OnApplicationQuit()
    {
        StopRumble();
        InputSystem.ResetHaptics();
    }

    private void StopRumble()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad != null)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }
    [ContextMenu("Test Collision Sound")]
    private void TestCollisionSound()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (collisionClips == null || collisionClips.Length == 0)
        {
            Debug.LogWarning("[GameManager] Collision Clips が設定されていません。");
            return;
        }

        AudioClip clip = collisionClips[0];

        if (clip == null)
        {
            Debug.LogWarning("[GameManager] Collision Clips の0番目がnullです。");
            return;
        }

        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.3f;
        audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, 1f);

        Debug.Log("[GameManager] Test Collision Sound を再生しました: " + clip.name);
    }
}