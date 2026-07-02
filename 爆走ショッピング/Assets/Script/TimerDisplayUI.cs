using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// TimerManager の経過時間を画面に表示し、停止時に中央へ移動させる UI です。
// 役割:
// - TimerManager の Started / Stopped / ResetCompleted / TimeChanged を購読して表示を更新します。
// - Text が未配置でも実行時にタイマー表示を生成します。
// 接続:
// - TimerManager と同じ GameObject、またはシーン内の TimerManager を自動探索します。
// - 日本語フォントヘルパー JapaneseUIFont を使いますが、表示文字自体は mm:ss.cc 形式です。
// 読むときの要点:
// - TimeChanged は 1/100 秒単位で通知されるため、表示更新もセンチ秒単位で抑えています。
public class TimerDisplayUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private Text timerText;
    [SerializeField] private RectTransform timerRect;

    [Header("Runtime UI")]
    [SerializeField] private bool createTextIfMissing = true;
    [SerializeField] private Vector2 size = new Vector2(260f, 64f);
    [SerializeField] private int fontSize = 42;
    [SerializeField] private Color textColor = Color.white;

    [Header("Display Positions")]
    [SerializeField] private Vector2 topPosition = new Vector2(0f, 268f);
    [SerializeField] private Vector2 centerPosition = new Vector2(0f, 0f);

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float centerHoldTime = 1.0f;
    [SerializeField] private bool hideAfterStop = false;

    private int lastDisplayedCentiseconds = -1;
    private Coroutine moveCoroutine;

    // 参照解決、Text 確保、見た目設定、初期表示を行います。
    private void Awake()
    {
        ResolveReferences();
        EnsureTimerText();
        ConfigureTimerText();
        ResetTimerView();
    }

    // 有効化時に TimerManager へイベント購読し、現在時間で表示を更新します。
    private void OnEnable()
    {
        ResolveReferences();
        SubscribeTimer();
        RefreshFromTimer();
    }

    // 無効化時に TimerManager のイベント購読を解除します。
    private void OnDisable()
    {
        UnsubscribeTimer();
    }

    // 同じ GameObject、またはシーン内から TimerManager を探します。
    private void ResolveReferences()
    {
        if (timerManager == null)
        {
            timerManager = GetComponent<TimerManager>();
        }

        if (timerManager == null)
        {
            timerManager = FindAnyObjectByType<TimerManager>();
        }
    }

    // TimerManager の状態イベントと時間変更イベントを購読します。
    private void SubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;

        timerManager.Started += HandleTimerStarted;
        timerManager.Stopped += HandleTimerStopped;
        timerManager.ResetCompleted += HandleTimerReset;
        timerManager.TimeChanged += HandleTimeChanged;
    }

    // TimerManager から登録したイベントハンドラを外します。
    private void UnsubscribeTimer()
    {
        if (timerManager == null)
        {
            return;
        }

        timerManager.Started -= HandleTimerStarted;
        timerManager.Stopped -= HandleTimerStopped;
        timerManager.ResetCompleted -= HandleTimerReset;
        timerManager.TimeChanged -= HandleTimeChanged;
    }

    // TimerManager の現在時間を使って表示を同期します。
    private void RefreshFromTimer()
    {
        if (timerManager == null)
        {
            UpdateTimerText(0f, true);
            return;
        }

        UpdateTimerText(timerManager.ElapsedTime, true);
    }

    // タイマー開始時は表示位置と表示状態を初期状態へ戻します。
    private void HandleTimerStarted()
    {
        ResetTimerView();
    }

    // タイマー停止時は最終時間を表示し、中央移動演出を開始します。
    private void HandleTimerStopped()
    {
        RefreshFromTimer();
        // ゴール時に結果が目立つよう、タイマー表示を中央へ移動します。
        StartMoveTimerToCenter();
    }

    // タイマーリセット時は表示も初期状態へ戻します。
    private void HandleTimerReset()
    {
        ResetTimerView();
    }

    // TimerManager から通知された経過時間を表示に反映します。
    private void HandleTimeChanged(float elapsedTime)
    {
        UpdateTimerText(elapsedTime, false);
    }

    // タイマー表示を上部に戻し、非表示状態なら再表示します。
    private void ResetTimerView()
    {
        StopMoveCoroutine();

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

    // 既存 Text を探し、なければ実行時に作ります。
    private void EnsureTimerText()
    {
        if (timerText == null)
        {
            timerText = GetComponentInChildren<Text>();
        }

        if (timerText == null && createTextIfMissing)
        {
            CreateRuntimeTimerText();
        }

        if (timerRect == null && timerText != null)
        {
            timerRect = timerText.GetComponent<RectTransform>();
        }
    }

    // Canvas と Timer Text を実行時に作ります。
    private void CreateRuntimeTimerText()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Timer Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject textObject = new GameObject("Timer Text");
        textObject.transform.SetParent(canvas.transform, false);

        timerRect = textObject.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 0.5f);
        timerRect.anchorMax = new Vector2(0.5f, 0.5f);
        timerRect.pivot = new Vector2(0.5f, 0.5f);
        timerRect.anchoredPosition = topPosition;
        timerRect.sizeDelta = size;

        timerText = textObject.AddComponent<Text>();
    }

    // Timer Text のフォント、サイズ、色、配置を設定します。
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

    // 経過時間を mm:ss.cc 形式へ変換し、必要なときだけ Text を更新します。
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

    // 中央移動演出を開始します。既存演出があれば止めてから始めます。
    private void StartMoveTimerToCenter()
    {
        StopMoveCoroutine();
        moveCoroutine = StartCoroutine(MoveTimerToCenter());
    }

    // タイマー表示を現在位置から中央位置へ滑らかに移動します。
    private IEnumerator MoveTimerToCenter()
    {
        if (timerRect == null)
        {
            yield break;
        }

        Vector2 startPosition = timerRect.anchoredPosition;
        float timer = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / duration);
            timerRect.anchoredPosition = Vector2.Lerp(startPosition, centerPosition, t);
            yield return null;
        }

        timerRect.anchoredPosition = centerPosition;
        yield return new WaitForSeconds(Mathf.Max(0f, centerHoldTime));

        if (hideAfterStop && timerText != null)
        {
            timerText.gameObject.SetActive(false);
        }

        moveCoroutine = null;
    }

    // 中央移動コルーチンが動いていれば停止します。
    private void StopMoveCoroutine()
    {
        if (moveCoroutine == null)
        {
            return;
        }

        StopCoroutine(moveCoroutine);
        moveCoroutine = null;
    }

    // UI サイズ、フォント、演出時間を安全な範囲へ補正します。
    private void OnValidate()
    {
        size.x = Mathf.Max(80f, size.x);
        size.y = Mathf.Max(24f, size.y);
        fontSize = Mathf.Max(8, fontSize);
        moveDuration = Mathf.Max(0.01f, moveDuration);
        centerHoldTime = Mathf.Max(0f, centerHoldTime);
    }
}
