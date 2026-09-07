using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        GameSessionRoot root = GetComponent<GameSessionRoot>();

        if (root == null)
        {
            Debug.LogError("[GameManager] GameSessionRoot is required. Runtime component auto-creation has been removed.", this);
        }
    }
}
