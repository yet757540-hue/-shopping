using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CandyRewardWindowManager : MonoBehaviour
{
    private sealed class RewardChoice
    {
        public string Label;
        public string Description;
        public Action Selected;
    }

    [Header("References")]
    [SerializeField] private ScoreboardManager scoreboardManager;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerManager player;
    [SerializeField] private InventoryInfluenceSettings inventoryInfluenceSettings;
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private CandyEffectLibrary candyEffectLibrary;
    [SerializeField] private GameObject rewardRoot;
    [SerializeField] private RectTransform rewardContentRoot;
    [SerializeField] private Text descriptionText;

    [Header("Library")]
    [SerializeField] private int randomEffectOptionCount = 2;

    [Header("Input")]
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    [Header("Runtime UI")]
    [SerializeField] private bool createIfMissing = true;
    [SerializeField] private Vector2 popupSize = new Vector2(760f, 420f);
    [SerializeField] private Vector2 popupPosition = new Vector2(0f, -28f);
    [SerializeField] private Vector2 optionItemSize = new Vector2(360f, 62f);
    [SerializeField] private int titleFontSize = 38;
    [SerializeField] private int descriptionFontSize = 24;
    [SerializeField] private int optionItemFontSize = 30;
    [SerializeField] private float optionItemSpacing = 70f;
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color focusedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    [SerializeField] private Color activeBackgroundColor = new Color(0.52f, 0.72f, 1f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);

    private readonly List<RewardChoice> currentChoices = new List<RewardChoice>();
    private readonly List<CandyEffect> effectCandidates = new List<CandyEffect>();
    private readonly string pauseSourceId = "CandyRewardWindow:" + Guid.NewGuid().ToString("N");
    private RuntimeOptionMenu rewardMenu;
    private bool isOpen;
    private bool isNavigationHeld;

    public bool IsOpen => isOpen;

    public void Initialize(GameSessionServices services, InventoryInfluenceSettings configuredInfluenceSettings)
    {
        scoreboardManager = services.Scoreboard;
        timerManager = services.Timer;
        inventory = services.Inventory;
        player = services.Player;
        pauseManager = services.Pause;
        candyEffectLibrary = services.CandyEffectLibrary;
        inventoryInfluenceSettings = configuredInfluenceSettings;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureRewardView();
        SetRewardVisible(false);
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
        if (!isOpen)
        {
            return;
        }

        HandleRewardInput(Gamepad.current, Keyboard.current);
    }

    public void ShowRewardWindow()
    {
        ResolveReferences();
        EnsureRewardView();

        if (rewardRoot == null)
        {
            StartNextScoreboard();
            return;
        }

        BuildChoices();
        RebuildRewardMenu();
        ResetInputState();
        SetRewardVisible(true);
        RequestPause();
    }

    private void BuildChoices()
    {
        currentChoices.Clear();

        currentChoices.Add(new RewardChoice
        {
            Label = "スキップ",
            Description = "効果を選ばずに次の目標へ進みます。",
            Selected = CloseWithoutEffect
        });

        CandyEffectContext context = CreateEffectContext();
        List<CandyEffect> randomEffects = PickRandomEffects(context);

        foreach (CandyEffect effect in randomEffects)
        {
            CandyEffect capturedEffect = effect;
            currentChoices.Add(new RewardChoice
            {
                Label = capturedEffect.EffectName,
                Description = capturedEffect.Description,
                Selected = () => SelectEffect(capturedEffect)
            });
        }
    }

    private List<CandyEffect> PickRandomEffects(CandyEffectContext context)
    {
        effectCandidates.Clear();

        if (candyEffectLibrary == null)
        {
            return effectCandidates;
        }

        IReadOnlyList<CandyEffect> effects = candyEffectLibrary.Effects;

        if (effects == null)
        {
            return effectCandidates;
        }

        foreach (CandyEffect effect in effects)
        {
            if (effect != null && effect.CanApply(context))
            {
                effectCandidates.Add(effect);
            }
        }

        int optionCount = Mathf.Min(Mathf.Max(0, randomEffectOptionCount), effectCandidates.Count);
        List<CandyEffect> selectedEffects = new List<CandyEffect>(optionCount);

        for (int i = 0; i < optionCount; i++)
        {
            int index = UnityEngine.Random.Range(0, effectCandidates.Count);
            selectedEffects.Add(effectCandidates[index]);
            effectCandidates.RemoveAt(index);
        }

        return selectedEffects;
    }

    private void RebuildRewardMenu()
    {
        if (rewardContentRoot == null)
        {
            return;
        }

        for (int i = rewardContentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = rewardContentRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        rewardMenu = new RuntimeOptionMenu(rewardContentRoot, CreateRewardMenuStyle());

        foreach (RewardChoice choice in currentChoices)
        {
            rewardMenu.AddButton(choice.Label, choice.Selected);
        }

        rewardMenu.SelectIndex(0);
        RefreshDescription();
    }

    private void SelectEffect(CandyEffect effect)
    {
        if (effect != null)
        {
            effect.Apply(CreateEffectContext());
        }

        CloseRewardWindow();
    }

    private void CloseWithoutEffect()
    {
        CloseRewardWindow();
    }

    private void CloseRewardWindow()
    {
        SetRewardVisible(false);
        ReleasePauseRequest();
        StartNextScoreboard();
    }

    private void StartNextScoreboard()
    {
        if (scoreboardManager != null)
        {
            scoreboardManager.StartScoreboard();
        }
    }

    private void HandleRewardInput(Gamepad gamepad, Keyboard keyboard)
    {
        int movement = ReadNavigationMovement(gamepad, keyboard);

        if (movement != 0)
        {
            rewardMenu?.MoveFocus(movement);
            RefreshDescription();
        }

        if (IsConfirmPressed(gamepad, keyboard))
        {
            rewardMenu?.ActivateFocused();
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

    private void ResetInputState()
    {
        isNavigationHeld = false;
    }

    private void RefreshDescription()
    {
        if (descriptionText == null)
        {
            return;
        }

        int index = rewardMenu != null ? rewardMenu.FocusedIndex : -1;

        if (index < 0 || index >= currentChoices.Count)
        {
            descriptionText.gameObject.SetActive(false);
            descriptionText.text = string.Empty;
            return;
        }

        string description = currentChoices[index].Description;
        descriptionText.gameObject.SetActive(!string.IsNullOrWhiteSpace(description));
        descriptionText.text = description;
    }

    private CandyEffectContext CreateEffectContext()
    {
        ResolveReferences();

        return new CandyEffectContext
        {
            TimerManager = timerManager,
            ScoreboardManager = scoreboardManager,
            Inventory = inventory,
            Player = player,
            InventoryInfluenceSettings = inventoryInfluenceSettings
        };
    }

    private void ResolveReferences()
    {
    }

    private void RequestPause()
    {
        if (pauseManager != null)
        {
            pauseManager.RequestPause(pauseSourceId);
        }
    }

    private void ReleasePauseRequest()
    {
        if (pauseManager != null)
        {
            pauseManager.ReleasePause(pauseSourceId);
        }
    }

    private void SetRewardVisible(bool visible)
    {
        isOpen = visible;

        if (rewardRoot != null)
        {
            rewardRoot.SetActive(visible);
        }
    }

    private void EnsureRewardView()
    {
        if (rewardRoot != null || !createIfMissing)
        {
            return;
        }

        Canvas canvas = CreateCanvas();

        RectTransform rootRect = CreateRect("Candy Reward Root", canvas.transform);
        Stretch(rootRect, Vector2.zero, Vector2.zero);
        rewardRoot = rootRect.gameObject;

        Image overlay = rootRect.gameObject.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = false;

        CreateRewardPopup(rootRect);
    }

    private void CreateRewardPopup(Transform parent)
    {
        RectTransform popupRect = CreateRect("Candy Reward Popup", parent);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = popupPosition;
        popupRect.sizeDelta = popupSize;

        Image popupBackground = popupRect.gameObject.AddComponent<Image>();
        popupBackground.color = backgroundColor;
        popupBackground.raycastTarget = false;

        CreateBorder(popupRect);

        Text title = CreateText("Candy Reward Title", popupRect, "キャンディ効果", titleFontSize, TextAnchor.UpperLeft, 52f);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(34f, -22f);
        titleRect.sizeDelta = new Vector2(-68f, 58f);

        rewardContentRoot = CreateRect("Candy Reward Content Root", popupRect);
        rewardContentRoot.anchorMin = new Vector2(0f, 0f);
        rewardContentRoot.anchorMax = new Vector2(0.56f, 1f);
        rewardContentRoot.offsetMin = new Vector2(44f, 38f);
        rewardContentRoot.offsetMax = new Vector2(-18f, -112f);

        descriptionText = CreateText("Candy Reward Description", popupRect, string.Empty, descriptionFontSize, TextAnchor.UpperLeft, 220f);
        RectTransform descriptionRect = descriptionText.GetComponent<RectTransform>();
        descriptionRect.anchorMin = new Vector2(0.58f, 0f);
        descriptionRect.anchorMax = new Vector2(1f, 1f);
        descriptionRect.pivot = new Vector2(0f, 1f);
        descriptionRect.offsetMin = new Vector2(12f, 38f);
        descriptionRect.offsetMax = new Vector2(-44f, -112f);
        descriptionText.gameObject.SetActive(false);
    }

    private RuntimeOptionMenuStyle CreateRewardMenuStyle()
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
        GameObject canvasObject = new GameObject("Candy Reward Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateBorder(RectTransform parent)
    {
        CreateBorderSegment("Candy Reward Border Top", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
        CreateBorderSegment("Candy Reward Border Bottom", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateBorderSegment("Candy Reward Border Left", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateBorderSegment("Candy Reward Border Right", parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
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

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment, float preferredHeight)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = JapaneseUIFont.Get(fontSize);
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
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
        randomEffectOptionCount = Mathf.Max(0, randomEffectOptionCount);
        navigationDeadZone = Mathf.Clamp(navigationDeadZone, 0.1f, 1f);
        navigationReleaseThreshold = Mathf.Clamp(navigationReleaseThreshold, 0.05f, navigationDeadZone);
        popupSize.x = Mathf.Max(420f, popupSize.x);
        popupSize.y = Mathf.Max(260f, popupSize.y);
        optionItemSize.x = Mathf.Max(240f, optionItemSize.x);
        optionItemSize.y = Mathf.Max(48f, optionItemSize.y);
        titleFontSize = Mathf.Max(12, titleFontSize);
        descriptionFontSize = Mathf.Max(10, descriptionFontSize);
        optionItemFontSize = Mathf.Max(12, optionItemFontSize);
        optionItemSpacing = Mathf.Max(optionItemSize.y, optionItemSpacing);
    }
}
