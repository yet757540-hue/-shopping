using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GamepadRumbleManager : MonoBehaviour
{
    private Coroutine rumbleCoroutine;

    public void Rumble(float lowFrequency, float highFrequency, float duration)
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
        {
            return;
        }

        if (rumbleCoroutine != null)
        {
            StopCoroutine(rumbleCoroutine);
        }

        rumbleCoroutine = StartCoroutine(RumbleCoroutine(gamepad, lowFrequency, highFrequency, duration));
    }

    private IEnumerator RumbleCoroutine(Gamepad gamepad, float lowFrequency, float highFrequency, float duration)
    {
        // 数値を0～1に制限
        lowFrequency = Mathf.Clamp01(lowFrequency);
        highFrequency = Mathf.Clamp01(highFrequency);

        // 振動開始
        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        yield return new WaitForSeconds(duration);

        // 振動停止
        gamepad.SetMotorSpeeds(0f, 0f);

        rumbleCoroutine = null;
    }

    private void OnDisable()
    {
        // オブジェクトが無効になったとき、振動を止める
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    private void OnApplicationPause(bool pause)
    {
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
        // ゲーム終了時に振動をリセット
        InputSystem.ResetHaptics();
    }
}