using System;
using UnityEngine;
using UnityEngine.Events;

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

    public event Action<Collision> CollisionEntered;
    public event Action<Collider> TriggerEntered;
    public event Action<Collider> TriggerExited;

    private void OnCollisionEnter(Collision collision)
    {
        Log("[PlayerCollisionReporter] OnCollisionEnter: " + collision.gameObject.name);
        CollisionEntered?.Invoke(collision);
    }

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

    private void Log(string message)
    {
        if (showDebugLog)
        {
            Debug.Log(message);
        }
    }
}
