using UnityEngine;

public class PlayerCollisionReporter : MonoBehaviour
{
    [Header("判定設定")]
    [SerializeField] private bool ignoreTrigger = false; // タイマー範囲を使うなら false にする

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("[PlayerCollisionReporter] OnCollisionEnter: " + collision.gameObject.name);

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PlayerCollisionReporter] GameManager.Instance が null です。");
            return;
        }

        GameManager.Instance.OnPlayerCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("[PlayerCollisionReporter] OnTriggerEnter: Triggerを検出しました。相手: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            Debug.Log("[PlayerCollisionReporter] ignoreTrigger が true のため、Trigger処理を無視します。");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PlayerCollisionReporter] GameManager.Instance が null です。");
            return;
        }

        GameManager.Instance.OnPlayerTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("[PlayerCollisionReporter] OnTriggerExit: " + other.gameObject.name);

        if (ignoreTrigger)
        {
            Debug.Log("[PlayerCollisionReporter] ignoreTrigger が true のため、Triggerを無視します。");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PlayerCollisionReporter] GameManager.Instance が null です。");
            return;
        }

        GameManager.Instance.OnPlayerTriggerExit(other);
        Debug.Log("1");
    }
}