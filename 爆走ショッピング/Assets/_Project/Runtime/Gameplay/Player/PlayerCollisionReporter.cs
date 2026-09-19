using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>プレイヤーの衝突・トリガーをイベントとして他の機能へ通知します。</summary>
public class PlayerCollisionReporter : MonoBehaviour
{
    // Collision を Inspector のイベントへ渡すための型です。
    [Serializable]
    public class CollisionEvent : UnityEvent<Collision>
    {
    }

    // Collider を Inspector のイベントへ渡すための型です。
    [Serializable]
    public class ColliderEvent : UnityEvent<Collider>
    {
    }

    // トリガー通知の有効・無効と、デバッグログの表示を切り替えます。
    [Header("衝突設定")]
    [SerializeField] private bool ignoreTrigger = false;
    [SerializeField] private bool showDebugLog = false;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
    [SerializeField] private CollisionEvent collisionEntered = new CollisionEvent();
    [SerializeField] private ColliderEvent triggerEntered = new ColliderEvent();
    [SerializeField] private ColliderEvent triggerExited = new ColliderEvent();

    public event Action<Collision> CollisionEntered;
    public event Action<Collider> TriggerEntered;
    public event Action<Collider> TriggerExited;

    // 衝突の相対速度を購読先が利用できるよう、Collision を C# イベントで通知します。
    private void OnCollisionEnter(Collision collision)
    {
        Log("[PlayerCollisionReporter] OnCollisionEnter: " + collision.gameObject.name);
        CollisionEntered?.Invoke(collision);
    }

    // トリガーへの進入を、設定された通知先へ渡します。
    private void OnTriggerEnter(Collider other)
    {
        Log("[PlayerCollisionReporter] OnTriggerEnter: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            return;
        }

        triggerEntered.Invoke(other);
        TriggerEntered?.Invoke(other);
    }

    // トリガーからの退出を、設定された通知先へ渡します。
    private void OnTriggerExit(Collider other)
    {
        Log("[PlayerCollisionReporter] OnTriggerExit: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            return;
        }

        triggerExited.Invoke(other);
        TriggerExited?.Invoke(other);
    }

    // デバッグ表示が有効な場合だけ、受け取ったメッセージを出力します。
    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
    }
}
