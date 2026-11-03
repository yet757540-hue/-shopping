using UnityEngine;

public class CandyBuffScript : MonoBehaviour
{
    //SerializeFieldで数値をいじれるようにする
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private TimerManager timerManager;

    private bool lotteryCandy_acc = false;
    private bool lotteryCandy_break = false;
    private bool lotteryCandy_Time = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //Candyは加速度アップ、障害物破壊、時間遅く
    //Candyは20秒持続、重複可

    //加速度
    private void acc_Candy()
    {
        if(lotteryCandy_acc==true)
        {
            playerManager.SetLoadInfluence(2f, 1f, 1f, 1f);
        }
        else
        {
            playerManager.SetLoadInfluence(1f, 1f, 1f, 1f);
        }
    }
    //破壊
    private void break_Candy()
    {
        if (lotteryCandy_break == true)
        {
            playerManager.SetLoadInfluence(2f, 1f, 1f, 1f);
        }
        else
        {
            playerManager.SetLoadInfluence(1f, 1f, 1f, 1f);
        }
    }
    //時間遅延
    private void Time_Candy()
    {
        if (lotteryCandy_Time == true)
        {
            playerManager.SetLoadInfluence(2f, 1f, 1f, 1f);
        }
        else
        {
            playerManager.SetLoadInfluence(1f, 1f, 1f, 1f);
        }
    }
}
