using UnityEngine;

[DisallowMultipleComponent]
public class TimerStartOnFirstSettlementExit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SettlementArea settlementArea;
    [SerializeField] private TimerManager timerManager;

    private bool hasStartedTimer;

    private void OnEnable()
    {
        SubscribeSettlementArea();
    }

    private void OnDisable()
    {
        UnsubscribeSettlementArea();
    }

    private void SubscribeSettlementArea()
    {
        ResolveReferences();

        if (settlementArea == null)
        {
            return;
        }

        settlementArea.PlayerExited -= HandleSettlementAreaExited;
        settlementArea.PlayerExited += HandleSettlementAreaExited;
    }

    private void UnsubscribeSettlementArea()
    {
        if (settlementArea == null)
        {
            return;
        }

        settlementArea.PlayerExited -= HandleSettlementAreaExited;
    }

    private void HandleSettlementAreaExited()
    {
        if (hasStartedTimer)
        {
            return;
        }

        ResolveReferences();

        if (timerManager == null)
        {
            return;
        }

        hasStartedTimer = true;
        timerManager.StartTimer();
        UnsubscribeSettlementArea();
    }

    private void ResolveReferences()
    {
        if (settlementArea == null)
        {
            settlementArea = FindAnyObjectByType<SettlementArea>();
        }

        if (timerManager == null)
        {
            timerManager = FindAnyObjectByType<TimerManager>();
        }
    }
}
