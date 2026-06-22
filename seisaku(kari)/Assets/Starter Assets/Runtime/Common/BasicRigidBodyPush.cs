using UnityEngine;

// CharacterController がぶつかった Rigidbody を押すための Starter Assets 補助スクリプトです。
public class BasicRigidBodyPush : MonoBehaviour
{
	public LayerMask pushLayers;
	public bool canPush;
	[Range(0.5f, 5f)] public float strength = 1.1f;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		// 押し出しが有効な時だけ Rigidbody へ力を加えます。
		if (canPush) PushRigidBodies(hit);
	}

	private void PushRigidBodies(ControllerColliderHit hit)
	{
		// Unity 公式サンプルの押し出し処理を基にしています。
		// https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

		// 物理で動かせない Rigidbody は押しません。
		// make sure we hit a non kinematic rigidbody
		Rigidbody body = hit.collider.attachedRigidbody;
		if (body == null || body.isKinematic) return;

		// 指定された Layer だけを押し出し対象にします。
		// make sure we only push desired layer(s)
		var bodyLayerMask = 1 << body.gameObject.layer;
		if ((bodyLayerMask & pushLayers.value) == 0) return;

		// 足元方向の接触では押し出しません。
		// We dont want to push objects below us
		if (hit.moveDirection.y < -0.3f) return;

		// 水平方向だけを押し出し方向として使います。
		// Calculate push direction from move direction, horizontal motion only
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

		// 設定された強さを掛けて力を加えます。
		// Apply the push and take strength into account
		body.AddForce(pushDir * strength, ForceMode.Impulse);
	}
}
