using StarterAssets;
using UnityEngine;

// モバイル UI からの入力を StarterAssetsInputs へ中継します。
public class VirtualInput : MonoBehaviour
{
    [Header("Output")]
    public StarterAssetsInputs StarterAssetsInputs;

    public void VirtualMoveInput(Vector2 virtualMoveDirection)
    {
        // 仮想移動入力を通常の移動入力として渡します。
        StarterAssetsInputs.MoveInput(virtualMoveDirection);
    }

    public void VirtualLookInput(Vector2 virtualLookDirection)
    {
        // 仮想視点入力を通常の視点入力として渡します。
        StarterAssetsInputs.LookInput(virtualLookDirection);
    }

    public void VirtualJumpInput(bool virtualJumpState)
    {
        // 仮想ジャンプボタンの状態を渡します。
        StarterAssetsInputs.JumpInput(virtualJumpState);
    }

    public void VirtualSprintInput(bool virtualSprintState)
    {
        // 仮想スプリントボタンの状態を渡します。
        StarterAssetsInputs.SprintInput(virtualSprintState);
    }
}
