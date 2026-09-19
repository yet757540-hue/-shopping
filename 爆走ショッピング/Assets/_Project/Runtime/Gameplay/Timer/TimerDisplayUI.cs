using UnityEngine;
using UnityEngine.UI;

/// <summary>TimerManager の残り時間を HUD に反映します。</summary>
public class TimerDisplayUI : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform timerRect;

    // 既存のタイマー文字に適用する書式です。size は現在、配置変更には使用していません。
    [Header("実行時 UI")]
    [SerializeField] private Vector2 size = new Vector2(260f, 64f);
    [SerializeField] private int fontSize = 42;
    [SerializeField] private Color textColor = Color.white;

    // 画面内でタイマー文字を配置する位置です。
    [Header("表示位置")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 268f);

    // 停止・時間切れ時に、文字を隠すか破棄するかを指定します。
    [Header("終了時の状態")]
    [SerializeField] private bool hideAfterStop = false;
    [SerializeField] private bool destroyWhenCompleted = true;

    private int lastDisplayedCentiseconds = -1;

    // タイマーを受け取り、イベント購読と現在値の表示を行います。
    public void Initialize(TimerManager configuredTimerManager)
    {
        timerManager = configuredTimerManager;
        SubscribeTimer();
        RefreshFromTimer();
    }

    // 参照と文字設定を整え、タイマー表示を初期状態へ戻します。
    private void Awake()
    {
        ResolveReferences();
        EnsureTimerText();
        ConfigureTimerText();
        ResetTimerView();
    }

    // タイマー通知を購読し、再表示時の残り時間を同期します。
    private void OnEnable()
    {
        ResolveReferences();
        SubscribeTimer();
        RefreshFromTimer();
    }

    // 無効化中に通知が届かないよう、タイマーのイベント購読を解除します。
    private void OnDisable()
    {
        UnsubscribeTimer();
    }

    // タイマーが未指定なら、同じオブジェクトから取得します。
    private void ResolveReferences()
    {
        if (timerManager == null)
        {
            timerManager = GetComponent<TimerManager>();
        }
    }

    // 古い購読を外してから登録し、同じ通知が重複しないようにします。
    private void SubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.Completed -= HandleTimerCompleted;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;

        timerManager.Started += HandleTimerStarted;
        timerManager.Stopped += HandleTimerStopped;
        timerManager.Completed += HandleTimerCompleted;
        timerManager.ResetCompleted += HandleTimerReset;
        timerManager.TimeChanged += HandleTimeChanged;
    }

    // この表示が登録したタイマー通知を全て解除します。
    private void UnsubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.Completed -= HandleTimerCompleted;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;
    }

    // 現在の残り時間を強制表示し、参照がない場合はゼロを表示します。
    private void RefreshFromTimer()
    {
        if (timerManager == null)
        {
            UpdateTimerText(0f, true);
            return;
        }

        UpdateTimerText(timerManager.RemainingTime, true);
    }

    // 開始時に、表示位置・表示状態・時間の文字を初期化します。
    private void HandleTimerStarted()
    {
        ResetTimerView();
    }

    // 停止時の時間を表示し、設定に応じて文字を非表示にします。
    private void HandleTimerStopped()
    {
        RefreshFromTimer();

        if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    // 終了時の時間を表示し、設定に応じて UI を破棄または非表示にします。
    private void HandleTimerCompleted()
    {
        RefreshFromTimer();

        if (destroyWhenCompleted)
        {
            DestroyTimerView();
        }
        else if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }
    }

    // リセット時にタイマー表示を初期状態へ戻します。
    private void HandleTimerReset()
    {
        ResetTimerView();
    }

    // イベントで受け取った残り時間を表示へ反映します。引数名は elapsedTime ですが値は残り時間です。
    private void HandleTimeChanged(float elapsedTime)
    {
        UpdateTimerText(elapsedTime, false);
    }

    // 設定位置へ戻して表示を有効にし、現在の時間を同期します。
    private void ResetTimerView()
    {
        if (timerRect != null)
        {
            timerRect.anchoredPosition = topPosition;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
        }

        RefreshFromTimer();
    }

    // 既存の子 Text とその RectTransform を取得します。新しい UI は生成しません。
    private void EnsureTimerText()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<Text>();
        }

        if (timerRect == null && timerText != null)
        {
            timerRect = timerText.GetComponent<RectTransform>();
        }
    }

    // 日本語フォント、文字サイズ、色、中央揃えを設定します。
    private void ConfigureTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        timerText.font = JapaneseUIFont.Get(fontSize);
        timerText.fontSize = fontSize;
        timerText.color = textColor;
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        timerText.verticalOverflow = VerticalWrapMode.Overflow;
        timerText.raycastTarget = false;
    }

    // 残り時間を分・秒・百分の一秒へ変換し、表示値が変わったときに更新します。
    private void UpdateTimerText(float elapsedTime, bool force)
    {
        if (timerText == null)
        {
            return;
        }

        int centiseconds = Mathf.FloorToInt(elapsedTime * 100f);

        if (!force && centiseconds == lastDisplayedCentiseconds)
        {
            return;
        }

        lastDisplayedCentiseconds = centiseconds;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = centiseconds % 100;
        timerText.text = $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }

    // 保持している UI 参照を解除してから、タイマー文字のオブジェクトを破棄します。
    private void DestroyTimerView()
    {
        if (timerText == null)
        {
            return;
        }

        GameObject timerObject = timerText.gameObject;
        timerText = null;
        timerRect = null;

        Destroy(timerObject);
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        size.x = Mathf.Max(80f, size.x);
        size.y = Mathf.Max(24f, size.y);
        fontSize = Mathf.Max(8, fontSize);
    }
}
