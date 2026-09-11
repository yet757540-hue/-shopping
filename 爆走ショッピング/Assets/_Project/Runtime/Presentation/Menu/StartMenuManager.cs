using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>タイトル画面の選択、オプション設定、ゲーム開始を管理します。</summary>
public class StartMenuManager : MonoBehaviour
{
    // メインメニューから実行できる三つの操作を表します。
    private enum MenuOption
    {
        Start,
        Option,
        Exit
    }

    // メニューの操作種別と、選択色を反映する背景を保持します。
    private sealed class MenuItem
    {
        public MenuOption option;
        public Image background;
    }

    // 開始時に読み込むゲームシーンです。
    [Header("シーン")]
    [SerializeField] private string gameSceneName = "idou";

    // 既存シーンのビューと、生成に使う任意の Prefab を指定します。
    [Header("事前作成ビュー")]
    [SerializeField] private StartMenuView sceneMenuView;
    [SerializeField] private StartMenuView menuViewPrefab;

    // 入力を受け付ける条件と、スティックの反応・解除しきい値です。
    [Header("入力")]
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    // UI の色・文字サイズ・配置寸法をまとめた設定です。
    [Header("見た目")]
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

    // 選択可能な走行パラメータ一式と、現在の選択番号です。
    [Header("移動プリセット")]
    [SerializeField] private PlayerMovementPreset[] movementPresets =
    {
        PlayerMovementPreset.CreateClassic(),
        PlayerMovementPreset.CreateHard()
    };
    [SerializeField] private int selectedMovementPresetIndex = 0;

    // 選択可能なボタン配置と、現在の選択番号です。
    [Header("操作プリセット")]
    [SerializeField] private PlayerMovementControlPreset[] movementControlPresets =
    {
        PlayerMovementControlPreset.CreateTriggers(),
        PlayerMovementControlPreset.CreateFaceButtons()
    };
    [SerializeField] private int selectedMovementControlPresetIndex = 0;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
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

    // オプション項目を配置する領域を公開します。
    public RectTransform OptionContentRoot => optionContentRoot;
    // タイトル画面のオプションが開いているかを返します。
    public bool IsOptionOpen => isOptionOpen;
    // 現在選択しているメインメニューの番号を返します。
    public int SelectedMenuIndex => selectedMenuIndex;
    // 接続中の事前作成ビューを返します。自動生成 UI の場合は null です。
    public StartMenuView ActiveMenuView => activeMenuView;

    // プリセットと UI を準備し、先頭項目を選んでオプションを閉じた状態にします。
    private void Awake()
    {
        EnsureMovementPresets();
        EnsureMovementControlPresets();
        CreateRuntimeUI();
        SelectMenuIndex(0);
        SetOptionVisible(false);
    }

    // オプション表示中はオプション入力、それ以外はメインメニュー入力だけを処理します。
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

    // 方向入力で選択行を移動し、決定入力で選択中の処理を実行します。
    private void HandleMainMenuInput(Gamepad gamepad, Keyboard keyboard)
    {
        int movement = ReadNavigationMovement(gamepad, keyboard);

        if (movement != 0)
        {
            MoveSelection(movement);
        }

        if (RuntimeMenuInput.IsConfirmPressed(gamepad, keyboard))
        {
            ActivateSelectedOption();
        }
    }

    // 編集中は左右で値を変更し、未編集中は上下選択・決定・メニューを閉じる操作を扱います。
    private void HandleOptionInput(Gamepad gamepad, Keyboard keyboard)
    {
        // 編集中の戻る操作は項目編集だけを終了し、オプション画面は開いたままにします。
        if (optionMenu != null && optionMenu.HasActiveItem)
        {
            if (IsBackPressed(gamepad, keyboard))
            {
                optionMenu.CancelActiveItem();
                RefreshOptionBackHint();
                return;
            }

            int adjustment = RuntimeMenuInput.ReadHorizontalAdjustment(
                gamepad, keyboard, ref isOptionAdjustmentHeld, navigationDeadZone, navigationReleaseThreshold);

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

        int movement = RuntimeMenuInput.ReadVerticalMovement(
            gamepad, keyboard, ref isOptionNavigationHeld, navigationDeadZone, navigationReleaseThreshold);

        if (movement != 0)
        {
            optionMenu?.MoveFocus(movement);
        }

        if (RuntimeMenuInput.IsConfirmPressed(gamepad, keyboard))
        {
            optionMenu?.ActivateFocused();
            RefreshOptionBackHint();
        }
    }

    // 十字キー・矢印キー・左スティックから移動方向を返し、スティックの連続反応を抑えます。
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

        // スティックは中央へ戻るまで一度だけ反応させ、意図しない連続選択を防ぎます。
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

    // ゲームパッドの右フェイスボタン、または Escape の押下を戻る操作として読みます。
    private bool IsBackPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
               (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
    }

    // 次のメインメニュー項目へ移動します。
    public void SelectNext()
    {
        MoveSelection(1);
    }

    // 前のメインメニュー項目へ移動します。
    public void SelectPrevious()
    {
        MoveSelection(-1);
    }

    // 選択中の項目に応じて、開始・オプション・終了を実行します。
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

    // 選択番号を増減し、一覧の端を越えたら反対側へ戻します。
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

    // 選択番号を有効範囲に補正し、選択色を更新します。
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

    // 選択中の行だけを強調色にし、他の行を通常色に戻します。
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

    // 遷移先を検証して設定を保持し、開始通知の後にゲームシーンを読み込みます。
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

        // 新シーンで PlayerManager が見つかったときに適用できるよう、選択内容を先に保存します。
        QueueSelectedMovementPreset();
        startSelected.Invoke();
        StartSelected?.Invoke();
        SceneManager.LoadScene(gameSceneName);
    }

    // 入力状態と選択行を初期化してオプションを表示し、開いたことを通知します。
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

    // 項目の編集状態を解除してオプションを隠し、閉じたことを通知します。
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

    // 終了通知の後、エディターでは Play Mode を終了し、ビルドではアプリを終了します。
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

    // 利用できる事前作成ビューを探し、見つからない場合に基本メニュー UI を生成します。
    private void CreateRuntimeUI()
    {
        menuItems.Clear();

        if (!TryCreatePrefabUI())
        {
            CreateDefaultRuntimeUI();
        }

        RegisterDefaultOptionItems();
    }

    // シーン参照・指定 Prefab・既存ビューの順に探し、必須参照がそろうビューを接続します。
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

        // 必須参照が欠けたビューは使用せず、自分で生成した Prefab だけを破棄します。
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

    // 事前作成ビューの参照を保持し、三つの行と共通オプションメニューを接続します。
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

    // 既存行のラベルと背景色を設定し、実行する項目を登録します。
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

    // Canvas、背景、メインメニュー、オプションを順に生成します。
    private void CreateDefaultRuntimeUI()
    {
        activeMenuView = null;
        canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        CreateMainMenu(canvas.transform);
        CreateOptionPopup(canvas.transform);
    }

    // タイトル画面専用の Canvas を生成し、解像度に応じた拡縮と描画順を設定します。
    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Start Menu Canvas");
        Canvas createdCanvas = canvasObject.AddComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.sortingOrder = 1000;

        // 基準解像度に対して拡縮し、画面サイズが変わっても UI の比率を保ちます。
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    // 画面全体を覆う背景を作り、設定色を適用します。
    private void CreateBackground(Transform parent)
    {
        RectTransform backgroundRect = CreateRect("White Background", parent);
        Stretch(backgroundRect, Vector2.zero, Vector2.zero);

        Image background = backgroundRect.gameObject.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;
    }

    // メインメニューの領域を用意し、開始・オプション・終了の行を配置します。
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

    // 指定位置にメニュー行の背景とラベルを作り、選択管理へ登録します。
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

    // オプションの背景・枠・案内・項目用の領域を配置します。
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

    // 共通メニューへ数値調整行を追加し、値変更時の処理と表示書式を渡します。
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

    // Inspector の見た目設定を、共通メニュー用のスタイルにまとめます。
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

    // 音量・移動設定・操作方式の標準項目を登録します。
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

    // 共通メニューへ候補選択行を追加し、選択変更時の処理を渡します。
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

    // 音量設定を AudioListener に反映します。
    private void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    // 音量をメニュー表示用の文字列に変換します。
    private string FormatVolumeValue(float value)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }

    // 選択した移動プリセット番号を有効範囲に収めて保持します。実際の設定保存はゲーム開始時です。
    private void HandleMovementPresetChanged(int index, string _)
    {
        selectedMovementPresetIndex = Mathf.Clamp(index, 0, movementPresets.Length - 1);
    }

    // 選択した操作プリセット番号を有効範囲に収めて保持します。実際の設定保存はゲーム開始時です。
    private void HandleMovementControlPresetChanged(int index, string _)
    {
        selectedMovementControlPresetIndex = Mathf.Clamp(index, 0, movementControlPresets.Length - 1);
    }

    // 補完済みの移動プリセットから、表示する名前の一覧を作ります。
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

    // 補完済みの操作プリセットから、メニューに表示する名前を取り出します。
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

    // 選択中の移動設定と操作方式を、シーンをまたいで保持するクラスへ渡します。
    private void QueueSelectedMovementPreset()
    {
        EnsureMovementPresets();
        EnsureMovementControlPresets();

        PlayerMovementPreset preset = movementPresets[selectedMovementPresetIndex];
        PlayerMovementPresetApplier.SetPendingSettings(preset.CreateSettingsCopy(), selectedMovementPresetIndex);

        PlayerMovementControlPreset controlPreset = movementControlPresets[selectedMovementControlPresetIndex];
        PlayerMovementPresetApplier.SetPendingControlScheme(controlPreset.ControlScheme, selectedMovementControlPresetIndex);
    }

    // 移動プリセットの不足と未設定要素を補い、選択番号と表示を同期します。
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

    // 共通の補完ルールを適用してから、この画面の選択位置を更新します。
    private void EnsureMovementControlPresets()
    {
        movementControlPresets = PlayerMovementControlPreset.EnsureDefaults(movementControlPresets);
        selectedMovementControlPresetIndex = Mathf.Clamp(selectedMovementControlPresetIndex, 0, movementControlPresets.Length - 1);

        if (movementControlChoice != null)
        {
            movementControlChoice.SetIndex(selectedMovementControlPresetIndex, false);
        }
    }

    // 項目の編集中かどうかに応じて、キャンセルと戻るの案内を切り替えます。
    private void RefreshOptionBackHint()
    {
        if (optionBackHint == null)
        {
            return;
        }

        optionBackHint.text = optionMenu != null && optionMenu.HasActiveItem ? "B CANCEL" : "B BACK";
    }

    // スティックの押しっぱなし判定を解除し、次回の入力を受け付けます。
    private void ResetOptionInputState()
    {
        isOptionNavigationHeld = false;
        isOptionAdjustmentHeld = false;
    }

    // パネルの上下左右に枠線を配置します。
    private void CreateBorder(RectTransform parent)
    {
        CreateBorderSegment("Option Border Top", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
        CreateBorderSegment("Option Border Bottom", parent, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f));
        CreateBorderSegment("Option Border Left", parent, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
        CreateBorderSegment("Option Border Right", parent, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(2f, 0f));
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

    // 日本語フォントとメニューの色・揃えを設定した文字オブジェクトを作ります。
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

    // オプションの開閉状態を保存し、メインメニューとオプションの表示を切り替えます。
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

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
