using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuManager : MonoBehaviour
{
    private enum MenuOption
    {
        Start,
        Option,
        Exit
    }

    private sealed class MenuItem
    {
        public MenuOption option;
        public Image background;
    }

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "idou";

    [Header("Prefab View")]
    [SerializeField] private StartMenuView sceneMenuView;
    [SerializeField] private StartMenuView menuViewPrefab;

    [Header("Input")]
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    [Header("Style")]
    [SerializeField] private int menuFontSize = 52;
    [SerializeField] private int optionHintFontSize = 30;
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color selectedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    [SerializeField] private Color normalBackgroundColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color optionFocusedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    [SerializeField] private Color optionActiveBackgroundColor = new Color(0.52f, 0.72f, 1f, 1f);
    [SerializeField] private Vector2 menuSize = new Vector2(520f, 260f);
    [SerializeField] private Vector2 optionPopupSize = new Vector2(760f, 420f);
    [SerializeField] private Vector2 optionItemSize = new Vector2(640f, 78f);
    [SerializeField] private int optionItemFontSize = 34;
    [SerializeField] private float optionItemSpacing = 92f;
    [SerializeField] private float volumeStep = 0.05f;

    [Header("Movement Presets")]
    [SerializeField] private PlayerMovementPreset[] movementPresets =
    {
        PlayerMovementPreset.CreateClassic(),
        PlayerMovementPreset.CreateHard()
    };
    [SerializeField] private int selectedMovementPresetIndex = 0;

    [Header("Movement Control Presets")]
    [SerializeField] private PlayerMovementControlPreset[] movementControlPresets =
    {
        PlayerMovementControlPreset.CreateTriggers(),
        PlayerMovementControlPreset.CreateFaceButtons()
    };
    [SerializeField] private int selectedMovementControlPresetIndex = 0;

    [Header("Events")]
    [SerializeField] private UnityEvent startSelected = new UnityEvent();
    [SerializeField] private UnityEvent optionOpened = new UnityEvent();
    [SerializeField] private UnityEvent optionClosed = new UnityEvent();
    [SerializeField] private UnityEvent exitSelected = new UnityEvent();

    private Canvas canvas;
    private RectTransform mainMenuRoot;
    private GameObject optionPopupRoot;
    private RectTransform optionContentRoot;
    private Text optionBackHint;
    private RuntimeOptionMenu optionMenu;
    private RuntimeOptionChoice movementPresetChoice;
    private RuntimeOptionChoice movementControlChoice;
    private StartMenuView activeMenuView;
    private readonly List<MenuItem> menuItems = new List<MenuItem>();
    private int selectedMenuIndex;
    private bool isOptionOpen;
    private bool isStickNavigationHeld;
    private bool isOptionNavigationHeld;
    private bool isOptionAdjustmentHeld;

    public event Action StartSelected;
    public event Action OptionOpened;
    public event Action OptionClosed;
    public event Action ExitSelected;

    public RectTransform OptionContentRoot => optionContentRoot;
    public bool IsOptionOpen => isOptionOpen;
    public int SelectedMenuIndex => selectedMenuIndex;
    public StartMenuView ActiveMenuView => activeMenuView;

    private void Awake()
    {
        EnsureMovementPresets();
        EnsureMovementControlPresets();
        CreateRuntimeUI();
        SelectMenuIndex(0);
        SetOptionVisible(false);
    }

    private void Update()
    {
        Gamepad gamepad = Gamepad.current;
        Keyboard keyboard = Keyboard.current;

        if (gamepad == null && keyboard == null)
        {
            return;
        }

        if (isOptionOpen)
        {
            HandleOptionInput(gamepad, keyboard);
            return;
        }

        HandleMainMenuInput(gamepad, keyboard);
    }

    private void HandleMainMenuInput(Gamepad gamepad, Keyboard keyboard)
    {
        int movement = ReadNavigationMovement(gamepad, keyboard);

        if (movement != 0)
        {
            MoveSelection(movement);
        }

        if (IsConfirmPressed(gamepad, keyboard))
        {
            ActivateSelectedOption();
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

    private int ReadNavigationMovement(Gamepad gamepad, Keyboard keyboard)
    {
        if (gamepad != null)
        {
            if (gamepad.dpad.up.wasPressedThisFrame || gamepad.dpad.left.wasPressedThisFrame)
            {
                return -1;
            }

            if (gamepad.dpad.down.wasPressedThisFrame || gamepad.dpad.right.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (keyboard != null)
        {
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                return -1;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                return 1;
            }
        }

        if (gamepad == null)
        {
            return 0;
        }

        Vector2 stick = gamepad.leftStick.ReadValue();
        float deadZone = Mathf.Clamp(navigationDeadZone, 0.1f, 1f);
        float releaseThreshold = Mathf.Clamp(navigationReleaseThreshold, 0.05f, deadZone);

        if (stick.magnitude <= releaseThreshold)
        {
            isStickNavigationHeld = false;
            return 0;
        }

        if (isStickNavigationHeld)
        {
            return 0;
        }

        if (stick.y > deadZone || stick.x < -deadZone)
        {
            isStickNavigationHeld = true;
            return -1;
        }

        if (stick.y < -deadZone || stick.x > deadZone)
        {
            isStickNavigationHeld = true;
            return 1;
        }

        return 0;
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

    private bool IsConfirmPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
               (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame));
    }

    private bool IsBackPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
               (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
    }

    public void SelectNext()
    {
        MoveSelection(1);
    }

    public void SelectPrevious()
    {
        MoveSelection(-1);
    }

    public void ActivateSelectedOption()
    {
        if (menuItems.Count == 0)
        {
            return;
        }

        switch (menuItems[selectedMenuIndex].option)
        {
            case MenuOption.Start:
                StartGame();
                break;
            case MenuOption.Option:
                OpenOptions();
                break;
            case MenuOption.Exit:
                ExitGame();
                break;
        }
    }

    private void MoveSelection(int delta)
    {
        if (menuItems.Count == 0)
        {
            return;
        }

        int nextIndex = selectedMenuIndex + delta;

        while (nextIndex < 0)
        {
            nextIndex += menuItems.Count;
        }

        SelectMenuIndex(nextIndex % menuItems.Count);
    }

    private void SelectMenuIndex(int index)
    {
        if (menuItems.Count == 0)
        {
            selectedMenuIndex = 0;
            return;
        }

        selectedMenuIndex = Mathf.Clamp(index, 0, menuItems.Count - 1);
        RefreshSelectionVisuals();
    }

    private void RefreshSelectionVisuals()
    {
        for (int i = 0; i < menuItems.Count; i++)
        {
            MenuItem item = menuItems[i];
            bool isSelected = i == selectedMenuIndex;

            if (item.background != null)
            {
                item.background.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
            }
        }
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[StartMenuManager] Game scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError("[StartMenuManager] Scene is not available in Build Settings: " + gameSceneName);
            return;
        }

        QueueSelectedMovementPreset();
        startSelected.Invoke();
        StartSelected?.Invoke();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenOptions()
    {
        if (isOptionOpen)
        {
            return;
        }

        ResetOptionInputState();
        optionMenu?.CancelActiveItem();
        optionMenu?.SelectIndex(0);
        RefreshOptionBackHint();
        SetOptionVisible(true);
        optionOpened.Invoke();
        OptionOpened?.Invoke();
    }

    public void CloseOptions()
    {
        if (!isOptionOpen)
        {
            return;
        }

        ResetOptionInputState();
        optionMenu?.CancelActiveItem();
        RefreshOptionBackHint();
        SetOptionVisible(false);
        optionClosed.Invoke();
        OptionClosed?.Invoke();
    }

    public void ExitGame()
    {
        exitSelected.Invoke();
        ExitSelected?.Invoke();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CreateRuntimeUI()
    {
        menuItems.Clear();

        if (!TryCreatePrefabUI())
        {
            CreateDefaultRuntimeUI();
        }

        RegisterDefaultOptionItems();
    }

    private bool TryCreatePrefabUI()
    {
        StartMenuView view = sceneMenuView;
        bool instantiatedPrefab = false;

        if (view == null && menuViewPrefab != null)
        {
            view = Instantiate(menuViewPrefab);
            view.name = menuViewPrefab.name;
            instantiatedPrefab = true;
        }

        if (view == null)
        {
            view = FindAnyObjectByType<StartMenuView>();
        }

        if (view == null)
        {
            return false;
        }

        if (!view.HasRequiredReferences)
        {
            Debug.LogWarning("[StartMenuManager] Start menu prefab/view is missing required references. Falling back to default UI.");

            if (instantiatedPrefab)
            {
                Destroy(view.gameObject);
            }

            return false;
        }

        BindPrefabView(view);
        return true;
    }

    private void BindPrefabView(StartMenuView view)
    {
        activeMenuView = view;
        canvas = view.Canvas;
        mainMenuRoot = view.MainMenuRoot;
        optionPopupRoot = view.OptionPopupRoot;
        optionContentRoot = view.OptionContentRoot;
        optionBackHint = view.OptionBackHint;

        BindPrefabMenuRow(view.StartRow, "START", MenuOption.Start);
        BindPrefabMenuRow(view.OptionRow, "OPTION", MenuOption.Option);
        BindPrefabMenuRow(view.ExitRow, "EXIT", MenuOption.Exit);

        optionMenu = new RuntimeOptionMenu(optionContentRoot, CreateOptionMenuStyle());
    }

    private void BindPrefabMenuRow(StartMenuView.MenuRowReference row, string label, MenuOption option)
    {
        row.SetLabel(label);
        row.Background.color = normalBackgroundColor;

        menuItems.Add(new MenuItem
        {
            option = option,
            background = row.Background
        });
    }

    private void CreateDefaultRuntimeUI()
    {
        activeMenuView = null;
        canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        CreateMainMenu(canvas.transform);
        CreateOptionPopup(canvas.transform);
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Start Menu Canvas");
        Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    private void CreateBackground(Transform parent)
    {
        RectTransform backgroundRect = CreateRect("White Background", parent);
        Stretch(backgroundRect, Vector2.zero, Vector2.zero);

        Image background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
    }

    private void CreateMainMenu(Transform parent)
    {
        menuItems.Clear();

        mainMenuRoot = CreateRect("Main Menu Root", parent);
        mainMenuRoot.anchorMin = new Vector2(0.5f, 0.5f);
        mainMenuRoot.anchorMax = new Vector2(0.5f, 0.5f);
        mainMenuRoot.pivot = new Vector2(0.5f, 0.5f);
        mainMenuRoot.anchoredPosition = Vector2.zero;
        mainMenuRoot.sizeDelta = menuSize;

        float rowSpacing = Mathf.Max(72f, menuSize.y / 3f);
        CreateMenuRow("Start Row", "START", MenuOption.Start, rowSpacing);
        CreateMenuRow("Option Row", "OPTION", MenuOption.Option, 0f);
        CreateMenuRow("Exit Row", "EXIT", MenuOption.Exit, -rowSpacing);
    }

    private void CreateMenuRow(string objectName, string label, MenuOption option, float yPosition)
    {
        RectTransform row = CreateRect(objectName, mainMenuRoot);
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, yPosition);
        row.sizeDelta = new Vector2(menuSize.x, 68f);

        Image selectionBackground = row.gameObject.AddComponent<Image>();
        selectionBackground.color = normalBackgroundColor;
        selectionBackground.raycastTarget = false;

        Text labelText = CreateText(label + " Label", row, label, menuFontSize, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(18f, 0f);
        labelRect.offsetMax = new Vector2(-18f, 0f);

        menuItems.Add(new MenuItem
        {
            option = option,
            background = selectionBackground
        });
    }

    private void CreateOptionPopup(Transform parent)
    {
        RectTransform popupRect = CreateRect("Option Popup", parent);
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = optionPopupSize;
        optionPopupRoot = popupRect.gameObject;

        Image popupBackground = popupRect.gameObject.AddComponent<Image>();
        popupBackground.color = backgroundColor;
        popupBackground.raycastTarget = false;

        CreateBorder(popupRect);

        optionBackHint = CreateText("Option Back Hint", popupRect, "B BACK", optionHintFontSize, TextAnchor.UpperRight);
        RectTransform hintRect = optionBackHint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(1f, 1f);
        hintRect.anchoredPosition = new Vector2(-24f, -18f);
        hintRect.sizeDelta = new Vector2(180f, 48f);

        optionContentRoot = CreateRect("OptionContentRoot", popupRect);
        optionContentRoot.anchorMin = Vector2.zero;
        optionContentRoot.anchorMax = Vector2.one;
        optionContentRoot.offsetMin = new Vector2(32f, 32f);
        optionContentRoot.offsetMax = new Vector2(-32f, -84f);

        optionMenu = new RuntimeOptionMenu(optionContentRoot, CreateOptionMenuStyle());
    }

    public RuntimeOptionSlider AddSliderOption(
        string label,
        float minValue,
        float maxValue,
        float step,
        float initialValue,
        Action<float> valueChanged,
        Func<float, string> formatValue = null
    )
    {
        if (optionMenu == null)
        {
            Debug.LogWarning("[StartMenuManager] Option menu is not ready.");
            return null;
        }

        RuntimeOptionSlider slider = optionMenu.AddSlider(
            label,
            minValue,
            maxValue,
            step,
            initialValue,
            valueChanged,
            formatValue
        );

        RefreshOptionBackHint();
        return slider;
    }

    private RuntimeOptionMenuStyle CreateOptionMenuStyle()
    {
        return new RuntimeOptionMenuStyle
        {
            Font = JapaneseUIFont.Get(optionItemFontSize),
            FontSize = optionItemFontSize,
            TextColor = textColor,
            NormalBackgroundColor = normalBackgroundColor,
            FocusedBackgroundColor = optionFocusedBackgroundColor,
            ActiveBackgroundColor = optionActiveBackgroundColor,
            BarFillColor = textColor,
            RowSize = optionItemSize,
            RowSpacing = optionItemSpacing
        };
    }

    private void RegisterDefaultOptionItems()
    {
        AddSliderOption(
            "VOLUME",
            0f,
            1f,
            volumeStep,
            AudioListener.volume,
            SetMasterVolume,
            FormatVolumeValue
        );

        movementPresetChoice = AddChoiceOption(
            "MOVE PRESET",
            GetMovementPresetLabels(),
            selectedMovementPresetIndex,
            HandleMovementPresetChanged
        );

        movementControlChoice = AddChoiceOption(
            "MOVE CONTROL",
            GetMovementControlPresetLabels(),
            selectedMovementControlPresetIndex,
            HandleMovementControlPresetChanged
        );
    }

    public RuntimeOptionChoice AddChoiceOption(
        string label,
        string[] choices,
        int initialIndex,
        Action<int, string> selectionChanged
    )
    {
        if (optionMenu == null)
        {
            Debug.LogWarning("[StartMenuManager] Option menu is not ready.");
            return null;
        }

        RuntimeOptionChoice choice = optionMenu.AddChoice(label, choices, initialIndex, selectionChanged);
        RefreshOptionBackHint();
        return choice;
    }

    private void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private string FormatVolumeValue(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    private void HandleMovementPresetChanged(int index, string _)
    {
        selectedMovementPresetIndex = Mathf.Clamp(index, 0, movementPresets.Length - 1);
    }

    private void HandleMovementControlPresetChanged(int index, string _)
    {
        selectedMovementControlPresetIndex = Mathf.Clamp(index, 0, movementControlPresets.Length - 1);
    }

    private string[] GetMovementPresetLabels()
    {
        EnsureMovementPresets();

        string[] labels = new string[movementPresets.Length];

        for (int i = 0; i < movementPresets.Length; i++)
        {
            labels[i] = movementPresets[i].DisplayName;
        }

        return labels;
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

    private void QueueSelectedMovementPreset()
    {
        EnsureMovementPresets();
        EnsureMovementControlPresets();

        PlayerMovementPreset preset = movementPresets[selectedMovementPresetIndex];
        PlayerMovementPresetApplier.SetPendingSettings(preset.CreateSettingsCopy(), selectedMovementPresetIndex);

        PlayerMovementControlPreset controlPreset = movementControlPresets[selectedMovementControlPresetIndex];
        PlayerMovementPresetApplier.SetPendingControlScheme(controlPreset.ControlScheme, selectedMovementControlPresetIndex);
    }

    private void EnsureMovementPresets()
    {
        if (movementPresets == null || movementPresets.Length == 0)
        {
            movementPresets = new[]
            {
                PlayerMovementPreset.CreateClassic(),
                PlayerMovementPreset.CreateHard()
            };
        }
        else if (movementPresets.Length == 1)
        {
            movementPresets = new[]
            {
                movementPresets[0] ?? PlayerMovementPreset.CreateClassic(),
                PlayerMovementPreset.CreateHard()
            };
        }

        for (int i = 0; i < movementPresets.Length; i++)
        {
            if (movementPresets[i] == null)
            {
                movementPresets[i] = i == 1 ? PlayerMovementPreset.CreateHard() : PlayerMovementPreset.CreateClassic();
            }

            movementPresets[i].Validate();
        }

        selectedMovementPresetIndex = Mathf.Clamp(selectedMovementPresetIndex, 0, movementPresets.Length - 1);

        if (movementPresetChoice != null)
        {
            movementPresetChoice.SetIndex(selectedMovementPresetIndex, false);
        }
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

    private void RefreshOptionBackHint()
    {
        if (optionBackHint == null)
        {
            return;
        }

        optionBackHint.text = optionMenu != null && optionMenu.HasActiveItem ? "B CANCEL" : "B BACK";
    }

    private void ResetOptionInputState()
    {
        isOptionNavigationHeld = false;
        isOptionAdjustmentHeld = false;
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

    private void SetOptionVisible(bool visible)
    {
        isOptionOpen = visible;

        if (mainMenuRoot != null)
        {
            mainMenuRoot.gameObject.SetActive(!visible);
        }

        if (optionPopupRoot != null)
        {
            optionPopupRoot.SetActive(visible);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            gameSceneName = "idou";
        }

        menuFontSize = Mathf.Max(12, menuFontSize);
        optionHintFontSize = Mathf.Max(12, optionHintFontSize);
        optionItemFontSize = Mathf.Max(12, optionItemFontSize);
        navigationDeadZone = Mathf.Clamp(navigationDeadZone, 0.1f, 1f);
        navigationReleaseThreshold = Mathf.Clamp(navigationReleaseThreshold, 0.05f, navigationDeadZone);
        menuSize.x = Mathf.Max(240f, menuSize.x);
        menuSize.y = Mathf.Max(180f, menuSize.y);
        optionPopupSize.x = Mathf.Max(320f, optionPopupSize.x);
        optionPopupSize.y = Mathf.Max(220f, optionPopupSize.y);
        optionItemSize.x = Mathf.Max(240f, optionItemSize.x);
        optionItemSize.y = Mathf.Max(48f, optionItemSize.y);
        optionItemSpacing = Mathf.Max(optionItemSize.y, optionItemSpacing);
        volumeStep = Mathf.Clamp(volumeStep, 0.001f, 1f);
        EnsureMovementPresets();
        EnsureMovementControlPresets();
    }
}
