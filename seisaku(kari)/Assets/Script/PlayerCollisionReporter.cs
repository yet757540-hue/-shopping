using UnityEngine;

public class PlayerCollisionReporter : MonoBehaviour
{
    [Header("判定設定")]
    [SerializeField] private bool ignoreTrigger = true; // Triggerを無視するか

    private void Awake()
    {
        Debug.Log("[PlayerCollisionReporter] Awake: スクリプトが読み込まれました。");
    }

    private void OnEnable()
    {
        Debug.Log("[PlayerCollisionReporter] OnEnable: スクリプトが有効になりました。");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("[PlayerCollisionReporter] OnCollisionEnter: 衝突を検出しました。");

        Debug.Log(
            "[PlayerCollisionReporter] 衝突相手: " + collision.gameObject.name +
            " / Tag: " + collision.gameObject.tag +
            " / Layer: " + LayerMask.LayerToName(collision.gameObject.layer)
        );

        Debug.Log(
            "[PlayerCollisionReporter] 相対速度: " +
            collision.relativeVelocity.magnitude
        );

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PlayerCollisionReporter] GameManager.Instance が null です。GameManager がシーンに存在しない可能性があります。");
            return;
        }

        Debug.Log("[PlayerCollisionReporter] GameManager.Instance を確認しました。OnPlayerCollision を呼び出します。");

        GameManager.Instance.OnPlayerCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        Debug.Log(
            "[PlayerCollisionReporter] OnCollisionStay: 接触中 - " +
            collision.gameObject.name
        );
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log(
            "[PlayerCollisionReporter] OnCollisionExit: 接触終了 - " +
            collision.gameObject.name
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PlayerCollisionReporter] OnTriggerEnter: Triggerを検出しました。相手: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            Debug.Log("[PlayerCollisionReporter] ignoreTrigger が true のため、Trigger処理を無視します。");
            return;
        }

        Debug.Log("[PlayerCollisionReporter] Trigger処理を実行できます。");
    }
}