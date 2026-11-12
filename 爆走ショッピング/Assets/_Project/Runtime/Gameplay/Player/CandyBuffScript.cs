using UnityEngine;

public class CandyBuffScript : MonoBehaviour
{
    //SerializeFieldで数値をいじれるようにする

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private TimerManager timerManager;

    
    [SerializeField]private float BuffTime = 20;

    private float randomValue;
    public bool lotteryCandy_acc = false;
    public bool lotteryCandy_break = false;
    public bool lotteryCandy_Time = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //Candyは加速度アップ、障害物破壊、時間遅く
    //Candyは20秒持続、重複可

    //加速度

    //アメはノーマルは30%ずつ、スペシャル(全部盛り)は10%
    public void lotteryCandy()
    {
        if (lotteryCandy_acc == true && lotteryCandy_Time == true)
        {
            return;
        }

        randomValue = Random.Range(1f, 100f);

        if(lotteryCandy_acc==true&&lotteryCandy_Time==false)
        {
            randomValue += 50;
            if (randomValue>100)
            {
                randomValue = 100;
            }
        }
        if (lotteryCandy_Time == true && lotteryCandy_acc == true)
        {
            randomValue -= 50;
            if (randomValue < 0)
            {
                randomValue = 0;
            }
        }

        if (randomValue<=50)
        {
            lotteryCandy_acc = true;
            acc_Candy();
        }
        else if(randomValue>=51)
        {
            lotteryCandy_Time = true;
            Time_Candy();
        }

    }
    private void acc_Candy()
    {
        StartCoroutine(playerManager.acc_BuffRoutine(BuffTime));
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
        StartCoroutine(playerManager.Time_BuffRoutine(BuffTime));
    }
}
