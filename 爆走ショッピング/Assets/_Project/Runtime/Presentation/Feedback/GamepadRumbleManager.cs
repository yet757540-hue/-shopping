using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ゲームパッドの振動を一定時間だけ再生・停止します。
public class GamepadRumbleManager : MonoBehaviour
{
    private Coroutine rumbleCoroutine;

    // 接続中の全ゲームパッドのモーター出力をゼロにします。
    public static void StopAllGamepadRumble()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }

    // 入力システム全体の振動状態を初期化します。
    public static void ResetAllHaptics()
    {
        StopAllGamepadRumble();
        InputSystem.ResetHaptics();
    }

    // 前の振動を取り消し、現在のゲームパッドへ指定強度と時間の振動を開始します。
    public void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        // 現在のゲームパッドがない場合は何もしません。
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        if (Time.timeScale == 0f)
        {
            StopRumble();
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

    // 強度を有効範囲へ補正してモーターを動かし、待機後に停止します。
    private IEnumerator RumbleCoroutine(Gamepad gamepad, float lowFrequency, float highFrequency, float duration)
    {
        // 入力された振動強度を 0〜1 に収めます。
        lowFrequency = Mathf.Clamp01(lowFrequency);
        highFrequency = Mathf.Clamp01(highFrequency);

        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        yield return new WaitForSecondsRealtime(duration);

        gamepad.SetMotorSpeeds(0f, 0f);
        rumbleCoroutine = null;
    }

    // 実行中のコルーチンと接続ゲームパッドの振動を停止します。
    public void StopRumble()
    {
        // コルーチンと実機の振動を両方止めます。
        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
            rumbleCoroutine = null;
        }

        StopAllGamepadRumble();
    }

    // 無効化後も振動が残らないよう、停止処理を実行します。
    private void OnDisable()
    {
        // オブジェクト無効化時に振動が残らないようにします。
        StopRumble();
    }

    // アプリの停止状態に合わせて、入力システムの振動を停止・再開します。
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

    // 終了時に、入力システムの振動状態をリセットします。
    private void OnApplicationQuit()
    {
        // アプリ終了時は全ての振動状態をリセットします。
        ResetAllHaptics();
    }
}

