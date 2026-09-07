using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class InGameOptionMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private PlayerManager playerManager;

    [Header("Input")]
    [SerializeField] private bool listenForInput = true;
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    [Header("Style")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color focusedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    [SerializeField] private Color activeBackgroundColor = new Color(0.52f, 0.72f, 1f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Vector2 popupSize = new Vector2(820f, 620f);
    [SerializeField] private Vector2 optionItemSize = new Vector2(680f, 72f);
    [SerializeField] private int titleFontSize = 44;
    [SerializeField] private int hintFontSize = 26;
    [SerializeField] private int optionItemFontSize = 32;
    [SerializeField] private float optionItemSpacing = 78f;
    [SerializeField] private float volumeStep = 0.05f;

    [Header("Movement Control Presets")]
    [SerializeField] private PlayerMovementControlPreset[] movementControlPresets =
    {
        PlayerMovementControlPreset.CreateTriggers(),
        PlayerMovementControlPreset.CreateFaceButtons()
    };
    [SerializeField] private int selectedMovementControlPresetIndex = 0;

    [Header("Events")]
    [SerializeField] private UnityEvent optionOpened = new UnityEvent();
    [SerializeField] private UnityEvent optionClosed = new UnityEvent();

    private readonly string pauseSourceId = "InGameOptionMenu:" + Guid.NewGuid().ToString("N");
    private Canvas canvas;
    private GameObject optionRoot;
    private RectTransform optionContentRoot;
    private Text optionBackHint;
    private RuntimeOptionMenu optionMenu;
    private RuntimeOptionChoice movementControlChoice;
    private bool isOpen;
    private bool isOptionNavigationHeld;
    private bool isOptionAdjustmentHeld;

    public bool IsOpen => isOpen;

    public void Initialize(GameTimePauseManager configuredPauseManager, GameRestartManager configuredRestartManager, PlayerManager configuredPlayerManager)
    {
        pauseManager = configuredPauseManager;
        restartManager = configuredRestartManager;
        playerManager = configuredPlayerManager;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureMovementControlPresets();
        SyncSelectionIndexes();
        CreateRuntimeUI();
        SetOptionVisible(false);
    }

    private void OnDisable()
    {
        ReleasePauseRequest();
    }

    private void Update()
    {
        if (!listenForInput)
        {
            return;
        }

        Gamepad gamepad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;

        if (gamepad == null && keyboard == null)
        {
            return;
        }

        if (IsMenuPressed(gamepad, keyboard))
        {
            ToggleOptions();
            return;
        }

        if (!isOpen)
        {
            return;
        }

        HandleOptionInput(gamepad, keyboard);
    }

    public void ToggleOptions()
    {
        if (isOpen)
        {
            CloseOptions();
            return;
        }

        OpenOptions();
    }

    public void OpenOptions()
    {
        if (isOpen)
        {
            return;
        }

        ResolveReferences();
        ResetOptionInputState();
        optionMenu?.CancelActiveItem();
        optionMenu?.SelectIndex(0);
        RefreshOptionBackHint();
        SetOptionVisible(true);
        RequestPause();
        optionOpened.Invoke();
    }

    public void CloseOptions()
    {
        CloseOptions(true);
    }

    private void CloseOptions(bool invokeEvent)
    {
        if (!isOpen)
        {
            return;
        }

        ResetOptionInputState();
        optionMenu?.CancelActiveItem();
        SetOptionVisible(false);
        ReleasePauseRequest();

        if (invokeEvent)
        {
            optionClosed.Invoke();
        }
    }

    private void HandleOptionInput(Gamepad gamepad, Keyboard keyboard)
    {
        if (optionMenu != null && optionMenu.HasActiveItem)
        {
            if (IsBackPressed(gamepad, keyboard))
            {
                optionMenu.CancelActiveItem();
                RefreshOptionBackHint();
                return;
            }

            int adjustment = ReadOptionAdjustment(gamepad, keyboard);

            if (adjustment != 0)
            {
                optionMenu.AdjustActive(adjustment);
            }

            return;
        }

        if (IsBackPressed(gamepad, keyboard))
        {
            CloseOptions();
            return;
        }

        int movement = ReadOptionNavigationMovement(gamepad, keyboard);

        if (movement != 0)
        {
            optionMenu?.MoveFocus(movement);
        }

        if (IsConfirmPressed(gamepad, keyboard))
        {
            optionMenu?.ActivateFocused();
            RefreshOptionBackHint();
        }
    }

    private bool IsMenuPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.startButton.wasPressedThisFrame) ||
               (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
    }

    private bool IsConfirmPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
               (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    private bool IsBackPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
               (keyboard != null && keyboard.backspaceKey.wasPressedThisFrame);
    }

    private int ReadOptionNavigationMovement(Gamepad gamepad, Keyboard keyboard)
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

        return ReadAnalogAxisStep(gamepad.leftStick.y.ReadValue(), ref isOptionNavigationHeld, false);
    }

    private int ReadOptionAdjustment(Gamepad gamepad, Keyboard keyboard)
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.left.wasPressedThisFrame)
            {
                return -1;
            }

            if (gamepad.dpad.right.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (gamepad == null)
        {
            return 0;
        }

        return ReadAnalogAxisStep(gamepad.leftStick.x.ReadValue(), ref isOptionAdjustmentHeld, true);
    }

    private int ReadAnalogAxisStep(float axisValue, ref bool isHeld, bool positiveMovesNext)
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

        if (axisValue > 0f)
        {
            return positiveMovesNext ? 1 : -1;
        }

        return positiveMovesNext ? -1 : 1;
    }

    private void CreateRuntimeUI()
    {
        canvas = CreateCanvas();

        RectTransform rootRect = CreateRect("In Game Option Root", canvas.transform);
        Stretch(rootRect, Vector2.zero, Vector2.zero);
        optionRoot = rootRect.gameObject;

        Image overlay = rootRect.gameObject.AddComponent<Image>();
        overlay.color = overlayColor;
        overlay.raycastTarget = false;

        RectTransform popupRect = CreateRect("In Game Option Popup", rootRect);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = popupSize;

        Image popupBackground = popupRect.gameObject.AddComponent<Image>();
        popupBackground.color = backgroundColor;
        popupBackground.raycastTarget = false;

        CreateBorder(popupRect);

        Text title = CreateText("Option Title", popupRect, "OPTION", titleFontSize, TextAnchor.UpperLeft);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(34f, -22f);
        titleRect.sizeDelta = new Vector2(-68f, 64f);

        optionBackHint = CreateText("Option Back Hint", popupRect, string.Empty, hintFontSize, TextAnchor.UpperRight);
        RectTransform hintRect = optionBackHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-34f, -26f);
        hintRect.sizeDelta = new Vector2(260f, 48f);

        optionContentRoot = CreateRect("Option Content Root", popupRect);
        optionContentRoot.anchorMin = Vector2.zero;
        optionContentRoot.anchorMax = Vector2.one;
        optionContentRoot.offsetMin = new Vector2(44f, 54f);
        optionContentRoot.offsetMax = new Vector2(-44f, -118f);

        optionMenu = new RuntimeOptionMenu(optionContentRoot, CreateOptionMenuStyle());
        RegisterOptionItems();
        RefreshOptionBackHint();
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("In Game Option Canvas");
        Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.sortingOrder = 1200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return createdCanvas;
    }

    private RuntimeOptionMenuStyle CreateOptionMenuStyle()
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

    private void RegisterOptionItems()
    {
        optionMenu.AddButton("CONTINUE", CloseOptions);

        optionMenu.AddSlider(
            "VOLUME",
            0f,
            1f,
            volumeStep,
            AudioListener.volume,
            SetMasterVolume,
            FormatVolumeValue
        );

        movementControlChoice = optionMenu.AddChoice(
            "MOVE CONTROL",
            GetMovementControlPresetLabels(),
            selectedMovementControlPresetIndex,
            HandleMovementControlPresetChanged
        );

        optionMenu.AddButton("RESTART", RestartGame);
        optionMenu.AddButton("START MENU", ReturnToStartMenu);
        optionMenu.SelectIndex(0);
    }

    private void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private string FormatVolumeValue(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private void HandleMovementControlPresetChanged(int index, string _)
    {
        selectedMovementControlPresetIndex = Mathf.Clamp(index, 0, movementControlPresets.Length - 1);
        PlayerMovementControlScheme controlScheme = movementControlPresets[selectedMovementControlPresetIndex].ControlScheme;
        PlayerMovementPresetApplier.SetPendingControlScheme(controlScheme, selectedMovementControlPresetIndex);

        if (playerManager != null)
        {
            playerManager.ApplyControlScheme(controlScheme);
        }
    }

    private void RestartGame()
    {
        CloseOptions(false);

        if (restartManager == null)
        {
            Debug.LogWarning("[InGameOptionMenu] Restart manager is not ready.", this);
            return;
        }

        restartManager.RestartGame();
    }

    private void ReturnToStartMenu()
    {
        CloseOptions(false);

        if (restartManager == null)
        {
            Debug.LogWarning("[InGameOptionMenu] Restart manager is not ready.", this);
            return;
        }

        restartManager.ReturnToStartMenu();
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

    private void RefreshOptionBackHint()
    {
        if (optionBackHint == null)
        {
            return;
        }

        optionBackHint.text = optionMenu != null && optionMenu.HasActiveItem ? "B CANCEL" : "MENU/B BACK";
    }

    private void SetOptionVisible(bool visible)
    {
        isOpen = visible;

        if (optionRoot != null)
        {
            optionRoot.SetActive(visible);
        }
    }

    private void ResetOptionInputState()
    {
        isOptionNavigationHeld = false;
        isOptionAdjustmentHeld = false;
    }

    private void ResolveReferences()
    {
    }

    private void SyncSelectionIndexes()
    {
        if (PlayerMovementPresetApplier.TryGetRetainedControlSchemeIndex(out int retainedControlIndex))
        {
            selectedMovementControlPresetIndex = Mathf.Clamp(retainedControlIndex, 0, movementControlPresets.Length - 1);
            return;
        }

        if (playerManager == null)
        {
            return;
        }

        for (int i = 0; i < movementControlPresets.Length; i++)
        {
            if (movementControlPresets[i] != null && movementControlPresets[i].ControlScheme == playerManager.ControlScheme)
            {
                selectedMovementControlPresetIndex = i;
                return;
            }
        }
    }

    private string[] GetMovementControlPresetLabels()
    {
        EnsureMovementControlPresets();

        string[] labels = new string[movementControlPresets.Length];

        for (int i = 0; i < movementControlPresets.Length; i++)
        {
            labels[i] = movementControlPresets[i].DisplayName;
        }

        return labels;
    }

    private void EnsureMovementControlPresets()
    {
        if (movementControlPresets == null || movementControlPresets.Length == 0)
        {
            movementControlPresets = new[]
            {
                PlayerMovementControlPreset.CreateTriggers(),
                PlayerMovementControlPreset.CreateFaceButtons()
            };
        }
        else if (movementControlPresets.Length == 1)
        {
            movementControlPresets = new[]
            {
                movementControlPresets[0] ?? PlayerMovementControlPreset.CreateTriggers(),
                PlayerMovementControlPreset.CreateFaceButtons()
            };
        }

        for (int i = 0; i < movementControlPresets.Length; i++)
        {
            if (movementControlPresets[i] == null)
            {
                movementControlPresets[i] = i == 1
                    ? PlayerMovementControlPreset.CreateFaceButtons()
                    : PlayerMovementControlPreset.CreateTriggers();
            }

            movementControlPresets[i].Validate();
        }

        selectedMovementControlPresetIndex = Mathf.Clamp(selectedMovementControlPresetIndex, 0, movementControlPresets.Length - 1);

        if (movementControlChoice != null)
        {
            movementControlChoice.SetIndex(selectedMovementControlPresetIndex, false);
        }
    }

    private void CreateBorder(RectTransform parent)
    {
        CreateBorderSegment("Option Border Top", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
        CreateBorderSegment("Option Border Bottom", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateBorderSegment("Option Border Left", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateBorderSegment("Option Border Right", parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
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

    private Text CreateText(string objectName, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = JapaneseUIFont.Get(fontSize);
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
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
        popupSize.x = Mathf.Max(360f, popupSize.x);
        popupSize.y = Mathf.Max(360f, popupSize.y);
        optionItemSize.x = Mathf.Max(240f, optionItemSize.x);
        optionItemSize.y = Mathf.Max(48f, optionItemSize.y);
        titleFontSize = Mathf.Max(12, titleFontSize);
        hintFontSize = Mathf.Max(12, hintFontSize);
        optionItemFontSize = Mathf.Max(12, optionItemFontSize);
        optionItemSpacing = Mathf.Max(optionItemSize.y, optionItemSpacing);
        volumeStep = Mathf.Clamp(volumeStep, 0.001f, 1f);
        EnsureMovementControlPresets();
    }
}
