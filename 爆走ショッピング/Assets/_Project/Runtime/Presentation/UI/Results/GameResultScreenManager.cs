using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>時間切れ後の得点表示と、再開・タイトル復帰の選択を管理します。</summary>
[DisallowMultipleComponent]
public class GameResultScreenManager : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ScoreboardManager scoreboardManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private RectTransform resultContentRoot;
    [SerializeField] private Text totalScoreText;
    [SerializeField] private Text detailText;

    // 入力を受け付ける条件と、スティックの反応・解除しきい値です。
    [Header("入力")]
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    // 結果 UI の自動生成可否と、得点・メニューの配置や書式です。
    [Header("実行時 UI")]
    [SerializeField] private bool createIfMissing = true;
    [SerializeField] private Vector2 scoreBlockSize = new Vector2(900f, 260f);
    [SerializeField] private Vector2 scoreBlockPosition = new Vector2(0f, 160f);
    [SerializeField] private Vector2 popupSize = new Vector2(620f, 230f);
    [SerializeField] private Vector2 popupPosition = new Vector2(0f, -170f);
    [SerializeField] private Vector2 optionItemSize = new Vector2(500f, 62f);
    [SerializeField] private int titleFontSize = 48;
    [SerializeField] private int scoreFontSize = 42;
    [SerializeField] private int detailFontSize = 26;
    [SerializeField] private int optionItemFontSize = 30;
    [SerializeField] private float optionItemSpacing = 70f;
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color scoreTextColor = Color.white;
    [SerializeField] private Color focusedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    [SerializeField] private Color activeBackgroundColor = new Color(0.52f, 0.72f, 1f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);

    private const string PauseSourceId = "GameResultScreen";
    private RuntimeOptionMenu resultMenu;
    private bool isShown;
    private bool isNavigationHeld;

    // 結果画面が表示されているかを返します。
    public bool IsShown => isShown;

    // 時間・得点・シーン遷移・停止の参照を受け取ります。
    public void Initialize(TimerManager configuredTimer, ScoreboardManager configuredScoreboard, GameRestartManager configuredRestart, GameTimePauseManager configuredPause)
    {
        timerManager = configuredTimer;
        scoreboardManager = configuredScoreboard;
        restartManager = configuredRestart;
        pauseManager = configuredPause;
    }

    // 必要に応じて結果 UI を準備し、開始時は非表示にします。
    private void Awake()
    {
        EnsureResultView();
        SetResultVisible(false);
    }

    // 結果画面が登録した停止依頼を解除します。
    private void OnDisable()
    {
        ReleasePauseRequest();
    }

    // 結果画面が表示されている間だけ、選択と決定の入力を処理します。
    private void Update()
    {
        if (!isShown)
        {
            return;
        }

        HandleResultInput(Gamepad.current, Keyboard.current);
    }

    // 得点を表示して目標 HUD を消し、メニューを初期選択にしてゲームを停止します。
    public void ShowResultScreen()
    {
        EnsureResultView();

        if (resultRoot == null)
        {
            return;
        }

        // 目標 HUD を消す前に得点を文字へ反映し、結果画面に残します。
        RefreshScoreText();

        if (scoreboardManager != null)
        {
            scoreboardManager.ClearScoreboard();
        }

        ResetResultInputState();
        resultMenu?.CancelActiveItem();
        resultMenu?.SelectIndex(0);
        SetResultVisible(true);
        RequestPause();
    }

    // 結果画面の停止依頼を解除し、再開管理へゲーム再読み込みを依頼します。
    public void RestartGame()
    {
        ReleasePauseRequest();

        if (restartManager == null)
        {
            Debug.LogWarning("[GameResultScreenManager] Restart manager is not ready.", this);
            return;
        }

        restartManager.RestartGame();
    }

    // 結果画面の停止依頼を解除し、再開管理へタイトル復帰を依頼します。
    public void ReturnToStartMenu()
    {
        ReleasePauseRequest();

        if (restartManager == null)
        {
            Debug.LogWarning("[GameResultScreenManager] Restart manager is not ready.", this);
            return;
        }

        restartManager.ReturnToStartMenu();
    }

    // 上下入力で項目を移動し、決定入力で選択中のボタンを実行します。
    private void HandleResultInput(Gamepad gamepad, Keyboard keyboard)
    {
        int movement = RuntimeMenuInput.ReadVerticalMovement(
            gamepad, keyboard, ref isNavigationHeld, navigationDeadZone, navigationReleaseThreshold);

        if (movement != 0)
        {
            resultMenu?.MoveFocus(movement);
        }

        if (RuntimeMenuInput.IsConfirmPressed(gamepad, keyboard))
        {
            resultMenu?.ActivateFocused();
        }
    }

    // 結果画面を開き直したときに、方向入力を再び受け付ける状態にします。
    private void ResetResultInputState()
    {
        isNavigationHeld = false;
    }

    // ScoreboardManager の集計値を、結果画面用の文言へ変換します。
    private void RefreshScoreText()
    {
        int totalScore = scoreboardManager != null ? scoreboardManager.TotalScore : 0;
        int passScore = scoreboardManager != null ? scoreboardManager.SettlementBonusScore : 0;
        int itemScore = scoreboardManager != null ? scoreboardManager.TargetItemScore : 0;
        int passCount = scoreboardManager != null ? scoreboardManager.CompletedSettlementCount : 0;
        int itemCount = scoreboardManager != null ? scoreboardManager.SettledExcessTargetItemCount : 0;
        int passScorePerSettlement = scoreboardManager != null ? scoreboardManager.SettlementBonusScorePerSettlement : 0;
        int itemScoreMultiplier = scoreboardManager != null ? scoreboardManager.TargetItemScoreMultiplier : 0;

        if (totalScoreText != null)
        {
            totalScoreText.text = "総得点 " + totalScore;
        }

        if (detailText != null)
        {
            detailText.text =
                "得点内訳\n" +
                "達成回数による得点: " + passScore + " (" + passCount + "回 × " + passScorePerSettlement + ")\n" +
                "超過品数による得点: " + itemScore + " (" + itemCount + "個 × " + itemScoreMultiplier + ")";
        }
    }

    // 結果画面を依頼元として、ゲーム時間の停止を要求します。
    private void RequestPause()
    {
        if (pauseManager != null)
        {
            pauseManager.RequestPause(PauseSourceId);
        }
    }

    // 結果画面の停止依頼だけを解除します。
    private void ReleasePauseRequest()
    {
        if (pauseManager != null)
        {
            pauseManager.ReleasePause(PauseSourceId);
        }
    }

    // 結果表示の状態を保存し、ルートオブジェクトの表示を切り替えます。
    private void SetResultVisible(bool visible)
    {
        isShown = visible;

        if (resultRoot != null)
        {
            resultRoot.SetActive(visible);
        }
    }

    // 既存 UI を優先し、不足時の生成が許可されていれば背景・得点・メニューを作ります。
    private void EnsureResultView()
    {
        // 作成済みルートや生成禁止設定を尊重し、必要なら選択メニューだけを補います。
        if (resultRoot != null || !createIfMissing)
        {
            if (resultMenu == null && resultContentRoot != null)
            {
                CreateResultMenu();
            }

            return;
        }

        Canvas canvas = CreateCanvas();

        RectTransform rootRect = CreateRect("Result Screen Root", canvas.transform);
        Stretch(rootRect, Vector2.zero, Vector2.zero);
        resultRoot = rootRect.gameObject;

        Image overlay = rootRect.gameObject.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = false;

        CreateScoreBlock(rootRect);
        CreateResultPopup(rootRect);
        CreateResultMenu();
    }

    // 結果タイトル、合計得点、得点内訳を縦に並べる領域を作ります。
    private void CreateScoreBlock(Transform parent)
    {
        RectTransform scoreRect = CreateRect("Result Score Block", parent);
        scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRect.pivot = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = scoreBlockPosition;
        scoreRect.sizeDelta = scoreBlockSize;

        // タイトル・合計・内訳を、個別の高さを保ちながら縦に並べます。
        VerticalLayoutGroup layout = scoreRect.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText("Result Title", scoreRect, "結果", titleFontSize, TextAnchor.MiddleCenter, scoreTextColor, 68f);
        totalScoreText = CreateText("Total Score Text", scoreRect, string.Empty, scoreFontSize, TextAnchor.MiddleCenter, scoreTextColor, 58f);
        detailText = CreateText("Score Detail Text", scoreRect, string.Empty, detailFontSize, TextAnchor.MiddleCenter, scoreTextColor, 116f);
    }

    // 結果メニューの背景と枠を作り、項目を並べる領域を確保します。
    private void CreateResultPopup(Transform parent)
    {
        RectTransform popupRect = CreateRect("Result Option Popup", parent);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = popupPosition;
        popupRect.sizeDelta = popupSize;

        Image popupBackground = popupRect.gameObject.AddComponent<Image>();
        popupBackground.color = backgroundColor;
        popupBackground.raycastTarget = false;

        CreateBorder(popupRect);

        Text title = CreateText("Result Menu Title", popupRect, "メニュー", optionItemFontSize, TextAnchor.UpperLeft, textColor, 46f);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(28f, -16f);
        titleRect.sizeDelta = new Vector2(-56f, 46f);

        // メニュー見出しの下にボタン配置用の余白を確保します。
        resultContentRoot = CreateRect("Result Option Content Root", popupRect);
        resultContentRoot.anchorMin = Vector2.zero;
        resultContentRoot.anchorMax = Vector2.one;
        resultContentRoot.offsetMin = new Vector2(44f, 28f);
        resultContentRoot.offsetMax = new Vector2(-44f, -66f);
    }

    // やり直しとタイトル復帰のボタンを登録し、先頭を選択します。
    private void CreateResultMenu()
    {
        resultMenu = new RuntimeOptionMenu(resultContentRoot, CreateResultMenuStyle());
        resultMenu.AddButton("やり直す", RestartGame);
        resultMenu.AddButton("開始画面へ", ReturnToStartMenu);
        resultMenu.SelectIndex(0);
    }

    // 結果メニュー用の色・フォント・行サイズを共通スタイルへまとめます。
    private RuntimeOptionMenuStyle CreateResultMenuStyle()
    {
        return new RuntimeOptionMenuStyle
        {
            Font = JapaneseUIFont.Get(optionItemFontSize),
            FontSize = optionItemFontSize,
            TextColor = textColor,
            NormalBackgroundColor = normalBackgroundColor,
            FocusedBackgroundColor = focusedBackgroundColor,
            ActiveBackgroundColor = activeBackgroundColor,
            BarFillColor = textColor,
            RowSize = optionItemSize,
            RowSpacing = optionItemSpacing
        };
    }

    // 結果画面専用の Canvas を生成し、描画順と解像度に応じた拡縮を設定します。
    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Result Screen Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // パネルの上下左右に枠線を配置します。
    private void CreateBorder(RectTransform parent)
    {
        CreateBorderSegment("Result Border Top", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
        CreateBorderSegment("Result Border Bottom", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateBorderSegment("Result Border Left", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateBorderSegment("Result Border Right", parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
    }

    // 指定した辺の位置と太さで、入力を遮らない枠線を作成します。
    private void CreateBorderSegment(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        RectTransform borderRect = CreateRect(objectName, parent);
        borderRect.anchorMin = anchorMin;
        borderRect.anchorMax = anchorMax;
        borderRect.pivot = pivot;
        borderRect.anchoredPosition = Vector2.zero;
        borderRect.sizeDelta = sizeDelta;

        Image border = borderRect.gameObject.AddComponent<Image>();
        border.color = textColor;
        border.raycastTarget = false;
    }

    // 日本語フォントと指定書式の文字を作り、レイアウト用の高さを設定します。
    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment, Color color, float preferredHeight)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = JapaneseUIFont.Get(fontSize);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;

        return text;
    }

    // 指定した親の下に RectTransform を持つ UI オブジェクトを作成します。
    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    // 親全体へアンカーを広げ、指定した余白を設定します。
    private void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        navigationDeadZone = Mathf.Clamp(navigationDeadZone, 0.1f, 1f);
        navigationReleaseThreshold = Mathf.Clamp(navigationReleaseThreshold, 0.05f, navigationDeadZone);
        scoreBlockSize.x = Mathf.Max(360f, scoreBlockSize.x);
        scoreBlockSize.y = Mathf.Max(160f, scoreBlockSize.y);
        popupSize.x = Mathf.Max(360f, popupSize.x);
        popupSize.y = Mathf.Max(160f, popupSize.y);
        optionItemSize.x = Mathf.Max(240f, optionItemSize.x);
        optionItemSize.y = Mathf.Max(48f, optionItemSize.y);
        titleFontSize = Mathf.Max(12, titleFontSize);
        scoreFontSize = Mathf.Max(12, scoreFontSize);
        detailFontSize = Mathf.Max(10, detailFontSize);
        optionItemFontSize = Mathf.Max(12, optionItemFontSize);
        optionItemSpacing = Mathf.Max(optionItemSize.y, optionItemSpacing);
    }
}
