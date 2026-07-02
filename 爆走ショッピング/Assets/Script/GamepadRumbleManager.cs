using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ゲームパッドの振動を一定時間だけ再生・停止します。
// 役割:
// - Rumble で左右モーターの強さと時間を受け取り、コルーチンで自動停止します。
// - Disable、Pause、Quit のタイミングでハプティクスが残らないようにリセットします。
// 接続:
// - CollisionFeedbackManager から衝突演出の一部として呼ばれます。
// - Unity Input System の Gamepad.current を直接使います。
// 読むときの要点:
// - 新しい振動が来たら古いコルーチンを止め、最新の振動を優先します。
public class GamepadRumbleManager : MonoBehaviour
{
    private Coroutine rumbleCoroutine;

    // 指定した強さと時間でゲームパッド振動を開始します。
    public void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        // 現在のゲームパッドがない場合は何もしません。
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        if (rumbleCoroutine != null)
        {
            // 新しい振動を優先するため、前の振動を止めます。
            StopCoroutine(rumbleCoroutine);
            rumbleCoroutine = null;
        }

        duration = Mathf.Max(0f, duration);
        if (duration <= 0f)
        {
            StopRumble();
            return;
        }

        rumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, lowFrequency, highFrequency, duration));
    }

    // 実際にモーター速度を設定し、指定時間後に自動停止します。
    private IEnumerator RumbleCoroutine(Gamepad gamepad, float lowFrequency, float highFrequency, float duration)
    {
        // 入力された振動強度を 0〜1 に収めます。
        lowFrequency = Mathf.Clamp01(lowFrequency);
        highFrequency = Mathf.Clamp01(highFrequency);

        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        yield return new WaitForSeconds(duration);

        gamepad.SetMotorSpeeds(0f, 0f);
        rumbleCoroutine = null;
    }

    // 進行中の振動コルーチンと実機モーターを止めます。
    public void StopRumble()
    {
        // コルーチンと実機の振動を両方止めます。
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
            rumbleCoroutine = null;
        }

        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    // コンポーネント無効化時に振動を残さないようにします。
    private void OnDisable()
    {
        // オブジェクト無効化時に振動が残らないようにします。
        StopRumble();
    }

    // アプリ一時停止時は Input System のハプティクスも一時停止します。
    private void OnApplicationPause(bool pause)
    {
        // アプリ停止中はハプティクスを一時停止します。
        if (pause)
        {
            InputSystem.PauseHaptics();
        }
        else
        {
            InputSystem.ResumeHaptics();
        }
    }

    // アプリ終了時は全ハプティクス状態をリセットします。
    private void OnApplicationQuit()
    {
        // アプリ終了時は全ての振動状態をリセットします。
        InputSystem.ResetHaptics();
    }
}

