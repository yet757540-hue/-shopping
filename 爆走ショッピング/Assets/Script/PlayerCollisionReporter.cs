using System;
using UnityEngine;
using UnityEngine.Events;

// プレイヤーの衝突・トリガー接触を UnityEvent として外へ流す中継コンポーネントです。
// 役割:
// - OnCollisionEnter、OnTriggerEnter、OnTriggerExit を検知し、Inspector で接続した処理を呼びます。
// - 衝突検知そのものと、スコア加算・演出再生などの具体処理を分離します。
// 接続:
// - collisionEntered は ScoreboardManager.RegisterCollision や CollisionFeedbackManager.PlayFeedback へつなぐ想定です。
// - triggerEntered / triggerExited は TimerZone.HandleEnter / HandleExit などへつなげます。
// 読むときの要点:
// - ignoreTrigger を true にするとトリガーイベントだけ無視できます。物理衝突イベントは常に通知します。
public class PlayerCollisionReporter : MonoBehaviour
{
    [Serializable]
    public class CollisionEvent : UnityEvent<Collision>
    {
    }

    [Serializable]
    public class ColliderEvent : UnityEvent<Collider>
    {
    }

    [Header("Collision Settings")]
    [SerializeField] private bool ignoreTrigger = false;
    [SerializeField] private bool showDebugLog = false;

    [Header("Events")]
    [SerializeField] private CollisionEvent collisionEntered = new CollisionEvent();
    [SerializeField] private ColliderEvent triggerEntered = new ColliderEvent();
    [SerializeField] private ColliderEvent triggerExited = new ColliderEvent();

    // 物理衝突を検知し、登録された UnityEvent へ Collision を渡します。
    private void OnCollisionEnter(Collision collision)
    {
        Log("[PlayerCollisionReporter] OnCollisionEnter: " + collision.gameObject.name);
        collisionEntered.Invoke(collision);
    }

    // Trigger 進入を検知し、必要なら登録先へ Collider を渡します。
    private void OnTriggerEnter(Collider other)
    {
        Log("[PlayerCollisionReporter] OnTriggerEnter: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            return;
        }

        triggerEntered.Invoke(other);
    }

    // Trigger 離脱を検知し、必要なら登録先へ Collider を渡します。
    private void OnTriggerExit(Collider other)
    {
        Log("[PlayerCollisionReporter] OnTriggerExit: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            return;
        }

        triggerExited.Invoke(other);
    }

    // showDebugLog が有効なときだけ接触ログを出します。
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
    }
}
