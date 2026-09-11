using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 実行時メニューに共通する決定・戻る・方向入力を読み取ります。
/// UI や選択状態を持たないため、各画面は必要な状態だけを自分で保持できます。
/// </summary>
public static class RuntimeMenuInput
{
    // 下側フェイスボタン、Enter、Space の押下を決定操作として読みます。
    public static bool IsConfirmPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
               (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    // 右側フェイスボタンまたは Backspace の押下を戻る操作として読みます。
    public static bool IsBackPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
               (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame);
    }

    // 十字キー・矢印キーを優先し、未入力なら左スティックの縦軸から移動方向を読みます。
    public static int ReadVerticalMovement(
        Gamepad gamepad, Keyboard keyboard, ref bool isHeld, float deadZone, float releaseThreshold)
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame)
            {
                return -1;
            }

            if (gamepad.dpad.down.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        return gamepad == null
            ? 0
            : ReadAxisStep(gamepad.leftStick.y.ReadValue(), ref isHeld, deadZone, releaseThreshold, false);
    }

    // 十字キー・矢印キーを優先し、未入力なら左スティックの横軸から調整方向を読みます。
    public static int ReadHorizontalAdjustment(
        Gamepad gamepad, Keyboard keyboard, ref bool isHeld, float deadZone, float releaseThreshold)
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.left.wasPressedThisFrame)
            {
                return -1;
            }

            if (gamepad.dpad.right.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        return gamepad == null
            ? 0
            : ReadAxisStep(gamepad.leftStick.x.ReadValue(), ref isHeld, deadZone, releaseThreshold, true);
    }

    // 倒し始めに一回だけ反応し、解除しきい値まで戻るまでは次の入力を抑えます。
    private static int ReadAxisStep(
        float axisValue, ref bool isHeld, float deadZone, float releaseThreshold, bool positiveMovesNext)
    {
        deadZone = Mathf.Clamp(deadZone, 0.1f, 1f);
        releaseThreshold = Mathf.Clamp(releaseThreshold, 0.05f, deadZone);

        if (Mathf.Abs(axisValue) <= releaseThreshold)
        {
            isHeld = false;
            return 0;
        }

        if (isHeld || Mathf.Abs(axisValue) <= deadZone)
        {
            return 0;
        }

        isHeld = true;
        // 縦軸では上が前の項目、横軸では右が次の値に対応します。
        bool isPositive = axisValue > 0f;
        return isPositive == positiveMovesNext ? 1 : -1;
    }
}
