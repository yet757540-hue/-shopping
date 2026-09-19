using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>ゲーム中に開く一時停止付きオプションメニューです。</summary>
[DisallowMultipleComponent]
public class InGameOptionMenu : MonoBehaviour
{
    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private GameTimePauseManager pauseManager;
    [SerializeField] private GameRestartManager restartManager;
    [SerializeField] private PlayerManager playerManager;

    // 入力を受け付ける条件と、スティックの反応・解除しきい値です。
    [Header("入力")]
    [SerializeField] private bool listenForInput = true;
    [SerializeField] private float navigationDeadZone = 0.55f;
    [SerializeField] private float navigationReleaseThreshold = 0.3f;

    // UI の色・文字サイズ・配置寸法をまとめた設定です。
    [Header("見た目")]
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

    // 選択可能なボタン配置と、現在の選択番号です。
    [Header("操作プリセット")]
    [SerializeField] private PlayerMovementControlPreset[] movementControlPresets =
    {
        PlayerMovementControlPreset.CreateTriggers(),
        PlayerMovementControlPreset.CreateFaceButtons()
    };
    [SerializeField] private int selectedMovementControlPresetIndex = PlayerMovementControlPreset.DefaultIndex;

    // Inspector から接続する通知用フィールドです。実際の発火条件は各処理で決まります。
    [Header("イベント")]
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

    // ゲーム中オプションが開いているかを返します。
    public bool IsOpen => isOpen;

    // 停止・再開・操作設定の変更に使う参照を、GameSessionRoot から受け取ります。
    public void Initialize(GameTimePauseManager configuredPauseManager, GameRestartManager configuredRestartManager, PlayerManager configuredPlayerManager)
    {
        pauseManager = configuredPauseManager;
        restartManager = configuredRestartManager;
        playerManager = configuredPlayerManager;
    }

    // 操作プリセットと現在の選択を同期し、UI を作って非表示にします。
    private void Awake()
    {
        EnsureMovementControlPresets();
        SyncSelectionIndexes();
        CreateRuntimeUI();
        SetOptionVisible(false);
    }

    // このメニューが登録した停止依頼を解除します。
    private void OnDisable()
    {
        ReleasePauseRequest();
    }

    // 入力が有効なら開閉操作を読み、開いている間だけ項目操作を処理します。
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

    // 現在の開閉状態に応じて、オプションを開くか閉じます。
    public void ToggleOptions()
    {
        if (isOpen)
        {
            CloseOptions();
            return;
        }

        OpenOptions();
    }

    // 選択状態を初期化して表示し、このメニューの停止依頼を追加して開いたことを通知します。
    public void OpenOptions()
    {
        if (isOpen)
        {
            return;
        }

        ResetOptionInputState();
        optionMenu?.CancelActiveItem();
        optionMenu?.SelectIndex(0);
        RefreshOptionBackHint();
        SetOptionVisible(true);
        // このメニューの依頼元 ID を使い、他の停止画面と独立して管理します。
        RequestPause();
        optionOpened.Invoke();
    }

    // 表示と編集状態を解除して停止依頼を取り除き、必要に応じて閉じたことを通知します。
    public void CloseOptions()
    {
        CloseOptions(true);
    }

    // 表示と編集状態を解除して停止依頼を取り除き、必要に応じて閉じたことを通知します。
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

    // 編集中の項目は左右入力で変更し、それ以外は上下入力で項目を選びます。
    private void HandleOptionInput(Gamepad gamepad, Keyboard keyboard)
    {
        // 値の編集中は戻る入力を編集解除に使い、画面全体を閉じません。
        if (optionMenu != null && optionMenu.HasActiveItem)
        {
            if (RuntimeMenuInput.IsBackPressed(gamepad, keyboard))
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

        if (RuntimeMenuInput.IsBackPressed(gamepad, keyboard))
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

    // Start ボタンまたは Escape の押下を、メニュー開閉操作として読みます。
    private bool IsMenuPressed(Gamepad gamepad, Keyboard keyboard)
    {
        return (gamepad != null && gamepad.startButton.wasPressedThisFrame) ||
               (keyboard != null && keyboard.escapeKey.wasPressedThisFrame);
    }

    // シーンに専用 UI を置かないため、表示ツリーはここで一度だけ生成します。
    private void CreateRuntimeUI()
    {
        canvas = CreateCanvas();

        RectTransform rootRect = CreateRect("In Game Option Root", canvas.transform);
        Stretch(rootRect, Vector2.zero, Vector2.zero);
        optionRoot = rootRect.gameObject;

        // 背景を暗くする全画面表示を、中央の設定パネルより後ろに置きます。
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

        // タイトルと案内の下に、選択項目を並べる領域を確保します。
        optionContentRoot = CreateRect("Option Content Root", popupRect);
        optionContentRoot.anchorMin = Vector2.zero;
        optionContentRoot.anchorMax = Vector2.one;
        optionContentRoot.offsetMin = new Vector2(44f, 54f);
        optionContentRoot.offsetMax = new Vector2(-44f, -118f);

        optionMenu = new RuntimeOptionMenu(optionContentRoot, CreateOptionMenuStyle());
        RegisterOptionItems();
        RefreshOptionBackHint();
    }

    // オプション専用の Canvas を生成し、描画順と解像度に応じた拡縮を設定します。
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

    // Inspector の見た目設定を、共通メニュー用のスタイルにまとめます。
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

    // 音量・操作方式・再開・タイトル復帰の項目と、それぞれの処理を登録します。
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

    // 選択した操作方式を現在のプレイヤーへ適用し、次のシーン用にも保持します。
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

    // メニューを閉じてから、再開管理へゲームの再読み込みを依頼します。
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

    // メニューを閉じてから、再開管理へタイトルシーンへの移動を依頼します。
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

    // このメニュー固有の識別名で停止を要求します。
    private void RequestPause()
    {
        if (pauseManager != null)
        {
            pauseManager.RequestPause(pauseSourceId);
        }
    }

    // このメニューの停止依頼だけを解除し、他の画面の依頼は保持します。
    private void ReleasePauseRequest()
    {
        if (pauseManager != null)
        {
            pauseManager.ReleasePause(pauseSourceId);
        }
    }

    // 項目の編集中かどうかに応じて、キャンセルと戻るの案内を切り替えます。
    private void RefreshOptionBackHint()
    {
        if (optionBackHint == null)
        {
            return;
        }

        optionBackHint.text = optionMenu != null && optionMenu.HasActiveItem ? "B CANCEL" : "MENU/B BACK";
    }

    // 開閉状態を保存し、メニュー全体の表示を切り替えます。
    private void SetOptionVisible(bool visible)
    {
        isOpen = visible;

        if (optionRoot != null)
        {
            optionRoot.SetActive(visible);
        }
    }

    // スティックの押しっぱなし判定を解除し、次回の入力を受け付けます。
    private void ResetOptionInputState()
    {
        isOptionNavigationHeld = false;
        isOptionAdjustmentHeld = false;
    }

    // 保持済みの選択番号を優先し、なければプレイヤーの操作方式に合う候補を探します。
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

    // 日本語フォント・文字色・揃えを設定したメニュー文字を作ります。
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

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
