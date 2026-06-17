using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("振動設定")]
    [SerializeField] private float minImpactSpeed = 2f;        // 振動が発生する最小衝突速度
    [SerializeField] private float maxImpactSpeed = 20f;       // 最大振動になる衝突速度

    [SerializeField] private float minRumbleStrength = 0.15f;  // 最小振動強度
    [SerializeField] private float maxRumbleStrength = 1.0f;   // 最大振動強度

    [SerializeField] private float minRumbleDuration = 0.08f;  // 最小振動時間
    [SerializeField] private float maxRumbleDuration = 0.35f;  // 最大振動時間

    [SerializeField] private float rumbleCooldown = 0.1f;      // 連続振動防止用クールタイム

    private Coroutine rumbleCoroutine;
    private float lastRumbleTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] 既にInstanceが存在するため、このGameManagerを削除します。");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        Debug.Log("[GameManager] Instance を設定しました。");
    }

    public void OnPlayerCollision(Collision collision)
    {
        Debug.Log("[GameManager] OnPlayerCollision が呼ばれました。");

        // 衝突速度を取得
        float impactSpeed = collision.relativeVelocity.magnitude;

        Debug.Log("[GameManager] 衝突速度: " + impactSpeed);

        // 衝突が弱すぎる場合は振動しない
        if (impactSpeed < minImpactSpeed)
        {
            Debug.Log("[GameManager] 衝突速度が小さいため、振動しません。");
            return;
        }

        // 短時間に何度も振動しないようにする
        if (Time.time - lastRumbleTime < rumbleCooldown)
        {
            Debug.Log("[GameManager] クールタイム中のため、振動しません。");
            return;
        }

        lastRumbleTime = Time.time;

        // 衝突速度を0～1に変換
        float impactRate = Mathf.InverseLerp(
            minImpactSpeed,
            maxImpactSpeed,
            impactSpeed
        );

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

        Debug.Log("[GameManager] 衝突割合: " + impactRate);
        Debug.Log("[GameManager] 振動強度: " + rumbleStrength);
        Debug.Log("[GameManager] 振動時間: " + rumbleDuration);

        StartRumble(rumbleStrength, rumbleDuration);
    }

    private void StartRumble(float strength, float duration)
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            Debug.LogWarning("[GameManager] Gamepad.current が null です。手柄が認識されていません。");
            return;
        }

        Debug.Log("[GameManager] Gamepad を検出しました: " + gamepad.displayName);

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
        Debug.Log("[GameManager] 振動開始");

        // 左モーター：重い振動
        // 右モーター：細かい振動
        gamepad.SetMotorSpeeds(strength * 0.7f, strength);

        yield return new WaitForSeconds(duration);

        gamepad.SetMotorSpeeds(0f, 0f);

        Debug.Log("[GameManager] 振動停止");

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
}