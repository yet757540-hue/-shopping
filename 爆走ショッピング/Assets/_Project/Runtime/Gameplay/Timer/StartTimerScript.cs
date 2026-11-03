using UnityEngine;

public class StartTimerScript: MonoBehaviour
{
    [SerializeField] private TimerManager timerManager;
    [SerializeField]private ScoreboardManager scoreboardManager;
    private void Start()
    {
        if (timerManager == null)
        {
            Debug.LogError("TimerManagerが設定されていません");
            return;
        }
       
        timerManager.StartTimer();
    }
}
