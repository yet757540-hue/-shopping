using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	// Starter Assets の入力値を保持し、Input System のイベントから更新します。
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			// 移動入力を Vector2 として保存します。
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			// カーソル操作が許可されている時だけ視点入力を受け取ります。
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			// ジャンプボタンの押下状態を保存します。
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			// スプリントボタンの押下状態を保存します。
			SprintInput(value.isPressed);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			// 外部 UI からも移動入力を上書きできるようにします。
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			// 外部 UI からも視点入力を上書きできるようにします。
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			// フォーカス復帰時にカーソルロック状態を戻します。
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			// ゲーム中はカーソルをロックし、必要に応じて解除します。
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}
