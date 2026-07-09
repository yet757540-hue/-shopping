using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        EnsureBasicGameInterfaces();

        if (FindAnyObjectByType<ControlsGuideUI>() == null)
        {
            gameObject.AddComponent<ControlsGuideUI>();
        }

        EnsureInventorySystems();

        if (FindAnyObjectByType<InventoryStatusUI>() == null)
        {
            gameObject.AddComponent<InventoryStatusUI>();
        }
    }

    private void EnsureBasicGameInterfaces()
    {
        if (FindAnyObjectByType<GameTimePauseManager>() == null)
        {
            gameObject.AddComponent<GameTimePauseManager>();
        }

        if (FindAnyObjectByType<InGameWindowManager>() == null)
        {
            gameObject.AddComponent<InGameWindowManager>();
        }

        if (FindAnyObjectByType<GameRestartManager>() == null)
        {
            gameObject.AddComponent<GameRestartManager>();
        }

        if (FindAnyObjectByType<InGameOptionMenu>() == null)
        {
            gameObject.AddComponent<InGameOptionMenu>();
        }

        if (FindAnyObjectByType<GameResultScreenManager>() == null)
        {
            gameObject.AddComponent<GameResultScreenManager>();
        }

        if (FindAnyObjectByType<CandyRewardWindowManager>() == null)
        {
            gameObject.AddComponent<CandyRewardWindowManager>();
        }

        if (FindAnyObjectByType<TimerStartOnFirstSettlementExit>() == null)
        {
            gameObject.AddComponent<TimerStartOnFirstSettlementExit>();
        }
    }

    private void EnsureInventorySystems()
    {
        PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();

        if (playerManager != null && playerManager.GetComponent<PlayerInventory>() == null)
        {
            playerManager.gameObject.AddComponent<PlayerInventory>();
        }

        if (FindAnyObjectByType<InventoryInfluenceSettings>() == null)
        {
            gameObject.AddComponent<InventoryInfluenceSettings>();
        }
    }
}
