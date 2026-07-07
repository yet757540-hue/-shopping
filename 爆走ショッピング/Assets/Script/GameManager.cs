using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
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
