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
        lowFrequency = Mathf.Clamp01(lowFrequency);
        highFrequency = Mathf.Clamp01(highFrequency);

        gamepad.SetMotorSpeeds(lowFrequency, highFrequency);

        yield return new WaitForSeconds(duration);

        gamepad.SetMotorSpeeds(0f, 0f);
        rumbleCoroutine = null;
    }

    public void StopRumble()
    {
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

    private void OnDisable()
    {
        StopRumble();
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
        InputSystem.ResetHaptics();
    }
}

