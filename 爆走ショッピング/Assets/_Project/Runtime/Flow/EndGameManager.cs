using UnityEngine;
using UnityEngine.InputSystem;

public class EndGameManager : MonoBehaviour
{
    public static EndGameManager Instance { get; private set; }

    private void Awake()
    {
        // インスタンスが存在しない場合
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        // すでにインスタンスが存在する場合
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    void Update()
    {
        if (Keyboard.current != null &&
       Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            {
                Debug.Log("endgame");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                  Application.Quit();
#endif
            }
        }
    }
}
