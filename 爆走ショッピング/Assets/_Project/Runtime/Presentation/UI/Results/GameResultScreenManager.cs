using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameResultScreenManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private ScoreboardManager scoreboardManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private RectTransform resultContentRoot;
    [SerializeField] private Text totalScoreText;
    [SerializeField] private Text detailText;

    [Header("Input")]
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    [Header("Runtime UI")]
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

    public bool IsShown => isShown;

    public void Initialize(TimerManager configuredTimer, ScoreboardManager configuredScoreboard, GameRestartManager configuredRestart, GameTimePauseManager configuredPause)
    {
        timerManager = configuredTimer;
        scoreboardManager = configuredScoreboard;
        restartManager = configuredRestart;
        pauseManager = configuredPause;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureResultView();
        SetResultVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        ReleasePauseRequest();
    }

    private void Update()
    {
        if (!isShown)
        {
            return;
        }

        HandleResultInput(Gamepad.current, Keyboard.current);
    }

    public void ShowResultScreen()
    {
        ResolveReferences();
        EnsureResultView();

        if (resultRoot == null)
        {
            return;
        }

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

    private void HandleResultInput(Gamepad gamepad, Keyboard keyboard)
    {
        int movement = ReadNavigationMovement(gamepad, keyboard);

        if (movement != 0)
        {
            resultMenu?.MoveFocus(movement);
        }

        if (IsConfirmPressed(gamepad, keyboard))
        {
            resultMenu?.ActivateFocused();
        }
    }

    private bool IsConfirmPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
               (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    private int ReadNavigationMovement(Gamepad gamepad, Keyboard keyboard)
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame)
            {
                return -1;
            }

            if (gamepad.dpad.down.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (gamepad == null)
        {
            return 0;
        }

        return ReadAnalogAxisStep(gamepad.leftStick.y.ReadValue(), ref isNavigationHeld);
    }

    private int ReadAnalogAxisStep(float axisValue, ref bool isHeld)
    {
        float deadZone = Mathf.Clamp(navigationDeadZone, 0.1f, 1f);
        float releaseThreshold = Mathf.Clamp(navigationReleaseThreshold, 0.05f, deadZone);

        if (Mathf.Abs(axisValue) <= releaseThreshold)
        {
            isHeld = false;
            return 0;
        }

        if (isHeld || Mathf.Abs(axisValue) <= deadZone)
        {
            return 0;
        }

        isHeld = true;
        return axisValue > 0f ? -1 : 1;
    }

    private void ResetResultInputState()
    {
        isNavigationHeld = false;
    }

    private void ResolveReferences()
    {
    }

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

    private void RequestPause()
    {
        if (pauseManager != null)
        {
            pauseManager.RequestPause(PauseSourceId);
        }
    }

    private void ReleasePauseRequest()
    {
        if (pauseManager != null)
        {
            pauseManager.ReleasePause(PauseSourceId);
        }
    }

    private void SetResultVisible(bool visible)
    {
        isShown = visible;

        if (resultRoot != null)
        {
            resultRoot.SetActive(visible);
        }
    }

    private void EnsureResultView()
    {
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

    private void CreateScoreBlock(Transform parent)
    {
        RectTransform scoreRect = CreateRect("Result Score Block", parent);
        scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRect.pivot = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = scoreBlockPosition;
        scoreRect.sizeDelta = scoreBlockSize;

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

        resultContentRoot = CreateRect("Result Option Content Root", popupRect);
        resultContentRoot.anchorMin = Vector2.zero;
        resultContentRoot.anchorMax = Vector2.one;
        resultContentRoot.offsetMin = new Vector2(44f, 28f);
        resultContentRoot.offsetMax = new Vector2(-44f, -66f);
    }

    private void CreateResultMenu()
    {
        resultMenu = new RuntimeOptionMenu(resultContentRoot, CreateResultMenuStyle());
        resultMenu.AddButton("やり直す", RestartGame);
        resultMenu.AddButton("開始画面へ", ReturnToStartMenu);
        resultMenu.SelectIndex(0);
    }

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

    private void CreateBorder(RectTransform parent)
    {
        CreateBorderSegment("Result Border Top", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
        CreateBorderSegment("Result Border Bottom", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateBorderSegment("Result Border Left", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateBorderSegment("Result Border Right", parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
    }

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

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

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
