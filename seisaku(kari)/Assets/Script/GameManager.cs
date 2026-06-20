using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CollisionFeedbackManager collisionFeedbackManager;
    [SerializeField] private TimerManager timerManager;

    private void Awake()
    {
        if (collisionFeedbackManager == null)
        {
            collisionFeedbackManager = FindAnyObjectByType<CollisionFeedbackManager>();
        }

        if (timerManager == null)
        {
            timerManager = FindAnyObjectByType<TimerManager>();
        }
    }

    public void OnPlayerCollision(Collision collision)
    {
        if (collisionFeedbackManager == null)
        {
            return;
        }

        collisionFeedbackManager.PlayFeedback(collision);
    }

    public void OnPlayerTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out TimerZone timerZone))
        {
            timerZone.HandleEnter(timerManager);
        }
    }

    public void OnPlayerTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out TimerZone timerZone))
        {
            timerZone.HandleExit(timerManager);
        }
    }
}
