using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ゲームパッドの振動を一定時間だけ再生・停止します。
public class GamepadRumbleManager : MonoBehaviour
{
    private Coroutine rumbleCoroutine;

    public static void StopAllGamepadRumble()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            gamepad.SetMotorSpeeds(0f, 0f);
        }
    }

    public static void ResetAllHaptics()
    {
        StopAllGamepadRumble();
        InputSystem.ResetHaptics();
    }

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

    private void OnDisable()
    {
        // オブジェクト無効化時に振動が残らないようにします。
        StopRumble();
    }

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

    private void OnApplicationQuit()
    {
        // アプリ終了時は全ての振動状態をリセットします。
        ResetAllHaptics();
    }
}

