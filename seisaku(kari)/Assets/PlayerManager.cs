using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private float acceleration = 20f;   // 加速度
    [SerializeField] private float deceleration = 30f;   // 減速度
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float maxSpeed = 30f;       // 最大速度

    private float currentSpeed = 0f;

    void Update()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null) return;

        // 回転
        float rotate =
            rotateSpeed *
            gamepad.leftStick.x.ReadValue() *
            Time.deltaTime;

        transform.Rotate(0, rotate, 0);

        // Bボタン(buttonEast)を押している間だけ加速
        if (gamepad.buttonEast.isPressed)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0, maxSpeed);
        }
        else
        {
            // 離したら減速
            currentSpeed -= deceleration * Time.deltaTime;

            // 0以下になったら完全停止＆リセット
            if (currentSpeed < 0f)
            {
                currentSpeed = 0f;
            }
        }

        // 前進
        transform.position +=
            transform.forward *
            currentSpeed *
            Time.deltaTime;
    }
}