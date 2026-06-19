using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("参照設定")]
    [SerializeField] private CameraFollowController cameraFollowController; // 画面揺れ用
    [SerializeField] private TimerManager timerManager;                     // タイマー管理用

    [Header("衝突設定")]
    [SerializeField] private float minImpactSpeed = 2f;   // 振動・音が発生する最小衝突速度
    [SerializeField] private float maxImpactSpeed = 20f;  // 最大反応になる衝突速度

    [Header("手柄振動設定")]
    [SerializeField] private float minRumbleStrength = 0.15f; // 最小振動強度
    [SerializeField] private float maxRumbleStrength = 1.0f;  // 最大振動強度
    [SerializeField] private float minRumbleDuration = 0.08f; // 最小振動時間
    [SerializeField] private float maxRumbleDuration = 0.35f; // 最大振動時間
    [SerializeField] private float rumbleCooldown = 0.1f;     // 連続振動防止用クールタイム

    [Header("画面揺れ設定")]
    [SerializeField] private float minShakeStrength = 0.03f;  // 最小画面揺れ強度
    [SerializeField] private float maxShakeStrength = 0.25f;  // 最大画面揺れ強度
    [SerializeField] private float minShakeDuration = 0.08f;  // 最小画面揺れ時間
    [SerializeField] private float maxShakeDuration = 0.35f;  // 最大画面揺れ時間

    [Header("衝突音設定")]
    [SerializeField] private AudioClip[] collisionClips;      // 衝突音リスト
    [SerializeField] private float minCollisionVolume = 0.2f; // 最小音量
    [SerializeField] private float maxCollisionVolume = 1.0f; // 最大音量
    [SerializeField] private float minCollisionPitch = 0.9f;  // 最小ピッチ
    [SerializeField] private float maxCollisionPitch = 1.1f;  // 最大ピッチ
    [SerializeField] private float soundCooldown = 0.08f;     // 連続再生防止用クールタイム

    [Header("タイマー設定")]
    [SerializeField] private string timerStartTag = "TimerStart"; // 離れるとタイマー開始
    [SerializeField] private string timerStopTag = "TimerStop";   // 入るとタイマー停止

    private AudioSource audioSource;

    private Coroutine rumbleCoroutine;
    private float lastRumbleTime = -999f;
    private float lastSoundTime = -999f;

    private void Awake()
    {
        // シングルトンを設定する
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // AudioSourceを取得する
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // GameManagerの位置に関係なく聞こえるように2D音声にする
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        // TimerManagerが未設定の場合はシーン内から自動取得する
        if (timerManager == null)
        {
            timerManager = FindFirstObjectByType<TimerManager>();
        }

        // CameraFollowControllerが未設定の場合はシーン内から自動取得する
        if (cameraFollowController == null)
        {
            cameraFollowController = FindFirstObjectByType<CameraFollowController>();
        }
    }

    public void OnPlayerCollision(Collision collision)
    {
        // 衝突速度を取得する
        float impactSpeed = collision.relativeVelocity.magnitude;

        // 衝突が弱すぎる場合は処理しない
        if (impactSpeed < minImpactSpeed)
        {
            return;
        }

        // 衝突速度を0～1の割合に変換する
        float impactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );

        // 衝突の強さに応じて各演出を発生させる
        TryStartRumble(impactRate);
        TryStartCameraShake(impactRate);
        TryPlayCollisionSound(impactRate);
    }

    private void TryStartRumble(float impactRate)
    {
        // 連続振動を防ぐ
        if (Time.time - lastRumbleTime < rumbleCooldown)
        {
            return;
        }

        lastRumbleTime = Time.time;

        // 衝突の強さに応じて振動強度を決める
        float rumbleStrength = Mathf.Lerp(
            minRumbleStrength,
            maxRumbleStrength,
            impactRate
        );

        // 衝突の強さに応じて振動時間を決める
        float rumbleDuration = Mathf.Lerp(
            minRumbleDuration,
            maxRumbleDuration,
            impactRate
        );

        StartRumble(rumbleStrength, rumbleDuration);
    }

    private void StartRumble(float strength, float duration)
    {
        Gamepad gamepad = Gamepad.current;

        // 手柄がない場合は処理しない
        if (gamepad == null)
        {
            return;
        }

        strength = Mathf.Clamp01(strength);
        duration = Mathf.Max(0f, duration);

        // 前の振動が残っている場合は停止する
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
        }

        rumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, strength, duration));
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float strength, float duration)
    {
        // 左モーターは重め、右モーターは細かめの振動として使う
        gamepad.SetMotorSpeeds(strength * 0.7f, strength);

        yield return new WaitForSeconds(duration);

        // 振動を停止する
        gamepad.SetMotorSpeeds(0f, 0f);

        rumbleCoroutine = null;
    }

    private void TryStartCameraShake(float impactRate)
    {
        if (cameraFollowController == null)
        {
            return;
        }

        // 衝突の強さに応じて画面揺れの強さを決める
        float shakeStrength = Mathf.Lerp(
            minShakeStrength,
            maxShakeStrength,
            impactRate
        );

        // 衝突の強さに応じて画面揺れの時間を決める
        float shakeDuration = Mathf.Lerp(
            minShakeDuration,
            maxShakeDuration,
            impactRate
        );

        cameraFollowController.Shake(shakeStrength, shakeDuration);
    }

    private void TryPlayCollisionSound(float impactRate)
    {
        if (audioSource == null)
        {
            return;
        }

        if (collisionClips == null || collisionClips.Length == 0)
        {
            return;
        }

        // 連続再生を防ぐ
        if (Time.time - lastSoundTime < soundCooldown)
        {
            return;
        }

        lastSoundTime = Time.time;

        // 衝突音をランダムに選ぶ
        AudioClip clip = collisionClips[Random.Range(0, collisionClips.Length)];

        if (clip == null)
        {
            return;
        }

        // 衝突の強さに応じて音量を決める
        float volume = Mathf.Lerp(
            minCollisionVolume,
            maxCollisionVolume,
            impactRate
        );

        // 毎回少しだけピッチを変える
        float pitch = Random.Range(
            minCollisionPitch,
            maxCollisionPitch
        );

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, volume);
    }

    public void OnPlayerTriggerEnter(Collider other)
    {
        // 指定した停止用Triggerに入ったらタイマーを停止する
        if (other.tag != timerStopTag)
        {
            return;
        }

        if (timerManager == null)
        {
            return;
        }

        timerManager.StopTimer();
    }

    public void OnPlayerTriggerExit(Collider other)
    {
        // 指定した開始用Triggerから離れたらタイマーを開始する
        if (other.tag != timerStartTag)
        {
            return;
        }

        if (timerManager == null)
        {
            return;
        }

        timerManager.StartTimer();
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
            return;
        }

        AudioClip clip = collisionClips[0];

        if (clip == null)
        {
            return;
        }

        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, 1f);
    }
}