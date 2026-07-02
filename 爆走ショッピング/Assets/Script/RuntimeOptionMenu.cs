using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// RuntimeOptionMenu 内で使う見た目設定です。
// StartMenuManager が生成し、各行のフォント、色、サイズ、間隔へ反映します。
public sealed class RuntimeOptionMenuStyle
{
    public Font Font;
    public int FontSize = 34;
    public Color TextColor = Color.black;
    public Color NormalBackgroundColor = new Color(1f, 1f, 1f, 0f);
    public Color FocusedBackgroundColor = new Color(0.86f, 0.92f, 1f, 1f);
    public Color ActiveBackgroundColor = new Color(0.52f, 0.72f, 1f, 1f);
    public Color BarBackgroundColor = new Color(0f, 0f, 0f, 0.18f);
    public Color BarFillColor = Color.black;
    public Vector2 RowSize = new Vector2(640f, 78f);
    public float RowSpacing = 92f;
}

// オプションメニューの 1 行分を表す基底クラスです。
// 役割:
// - フォーカス中、編集中、通常時の背景色を切り替えます。
// - Slider や Choice が共通して持つ Activate / Cancel / Adjust の流れを定義します。
public abstract class RuntimeOptionItem
{
    // 行の RectTransform と背景 Image を保持し、派生クラス共通の表示制御に使います。
    protected RuntimeOptionItem(RectTransform row, Image background)
    {
        Row = row;
        Background = background;
    }

    internal RectTransform Row { get; }
    internal Image Background { get; }
    public bool IsActive { get; private set; }

    // フォーカス中、編集中、通常時の背景色を現在状態に合わせて反映します。
    public void SetVisualState(bool isFocused, Color normalColor, Color focusedColor, Color activeColor)
    {
        if (Background == null)
        {
            return;
        }

        Background.color = IsActive ? activeColor : isFocused ? focusedColor : normalColor;
    }

    // この行を編集中状態にし、派生クラス側の開始処理を呼びます。
    public void Activate()
    {
        IsActive = true;
        OnActivated();
    }

    // この行の編集中状態を解除し、派生クラス側の終了処理を呼びます。
    public void Cancel()
    {
        IsActive = false;
        OnCanceled();
    }

    public abstract void Adjust(int direction);

    // 派生クラスが必要な場合だけ、編集開始時の処理を追加します。
    protected virtual void OnActivated()
    {
    }

    // 派生クラスが必要な場合だけ、編集終了時の処理を追加します。
    protected virtual void OnCanceled()
    {
    }
}

// 左右入力で数値を増減するオプション行です。
// 役割:
// - 音量のような連続値を min/max/step で丸めて保持します。
// - 値テキストとバーの塗り幅を同時に更新します。
public sealed class RuntimeOptionSlider : RuntimeOptionItem
{
    private readonly Text valueText;
    private readonly RectTransform fillRect;
    private readonly float minValue;
    private readonly float maxValue;
    private readonly float step;
    private readonly Action<float> valueChanged;
    private readonly Func<float, string> formatValue;
    private float value;

    // Slider 行に必要な UI と値範囲を受け取り、初期表示まで設定します。
    internal RuntimeOptionSlider(
        RectTransform row,
        Image background,
        Text valueText,
        RectTransform fillRect,
        float minValue,
        float maxValue,
        float step,
        float initialValue,
        Action<float> valueChanged,
        Func<float, string> formatValue
    ) : base(row, background)
    {
        this.valueText = valueText;
        this.fillRect = fillRect;
        this.minValue = Mathf.Min(minValue, maxValue);
        this.maxValue = Mathf.Max(minValue, maxValue);
        this.step = Mathf.Max(0.0001f, Mathf.Abs(step));
        this.valueChanged = valueChanged;
        this.formatValue = formatValue ?? DefaultFormatValue;

        SetValue(initialValue, false);
    }

    public float Value => value;
    public float NormalizedValue => Mathf.InverseLerp(minValue, maxValue, value);

    // 左右入力の方向に応じて step 単位で値を変更します。
    public override void Adjust(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        SetValue(value + step * Math.Sign(direction), true);
    }

    // 値を範囲内かつ step 単位に丸め、必要なら変更通知を送ります。
    public void SetValue(float nextValue, bool notify)
    {
        float clamped = Mathf.Clamp(nextValue, minValue, maxValue);
        // step 単位に丸めることで、ゲームパッド操作でも値が細かく揺れすぎないようにします。
        float steppedValue = minValue + Mathf.Round((clamped - minValue) / step) * step;
        value = Mathf.Clamp(steppedValue, minValue, maxValue);

        RefreshDisplay();

        if (notify)
        {
            valueChanged?.Invoke(value);
        }
    }

    // 数値表示とバーの塗り幅を現在値に合わせます。
    private void RefreshDisplay()
    {
        if (valueText != null)
        {
            valueText.text = formatValue(value);
        }

        if (fillRect != null)
        {
            fillRect.anchorMax = new Vector2(NormalizedValue, 1f);
            fillRect.offsetMax = Vector2.zero;
        }
    }

    // formatValue が指定されていない場合の標準表示です。
    private static string DefaultFormatValue(float sliderValue)
    {
        return Mathf.RoundToInt(sliderValue).ToString();
    }
}

// 左右入力で候補を切り替えるオプション行です。
// 役割:
// - 移動プリセットや操作方式のような離散的な選択肢を扱います。
// - 端まで移動したら反対側へ回り込む循環選択です。
public sealed class RuntimeOptionChoice : RuntimeOptionItem
{
    private readonly Text valueText;
    private readonly string[] choices;
    private readonly Action<int, string> selectionChanged;
    private int selectedIndex;

    // Choice 行に必要な UI と候補リストを受け取り、初期表示まで設定します。
    internal RuntimeOptionChoice(
        RectTransform row,
        Image background,
        Text valueText,
        string[] choices,
        int initialIndex,
        Action<int, string> selectionChanged
    ) : base(row, background)
    {
        this.valueText = valueText;
        this.choices = choices == null || choices.Length == 0 ? new[] { "None" } : choices;
        this.selectionChanged = selectionChanged;

        SetIndex(initialIndex, false);
    }

    public int SelectedIndex => selectedIndex;
    public string SelectedValue => choices[selectedIndex];

    // 左右入力の方向に応じて選択肢を 1 つ進める、または戻します。
    public override void Adjust(int direction)
    {
        if (direction == 0 || choices.Length == 0)
        {
            return;
        }

        SetIndex(selectedIndex + Math.Sign(direction), true);
    }

    // 指定 index を循環範囲に丸め、必要なら選択変更通知を送ります。
    public void SetIndex(int index, bool notify)
    {
        if (choices.Length == 0)
        {
            selectedIndex = 0;
            RefreshDisplay();
            return;
        }

        int nextIndex = index;

        while (nextIndex < 0)
        {
            nextIndex += choices.Length;
        }

        selectedIndex = nextIndex % choices.Length;
        RefreshDisplay();

        if (notify)
        {
            selectionChanged?.Invoke(selectedIndex, SelectedValue);
        }
    }

    // 現在選択中の候補名を表示テキストへ反映します。
    private void RefreshDisplay()
    {
        if (valueText != null)
        {
            valueText.text = SelectedValue;
        }
    }
}

// 実行時生成 UI 用のオプションメニュー本体です。
// 役割:
// - Slider / Choice の行を作成し、フォーカス移動、決定、キャンセル、値調整をまとめて処理します。
// 接続:
// - StartMenuManager が RuntimeOptionMenu を所有し、入力結果を MoveFocus / ActivateFocused / AdjustActive へ渡します。
// 読むときの要点:
// - ActiveItem がある間は、その行を編集中として扱い、上下移動ではなく左右調整を受け付けます。
public sealed class RuntimeOptionMenu
{
    private readonly RectTransform root;
    private readonly RuntimeOptionMenuStyle style;
    private readonly List<RuntimeOptionItem> items = new List<RuntimeOptionItem>();
    private int focusedIndex;

    // 行を追加する親 RectTransform と見た目設定を受け取ります。
    public RuntimeOptionMenu(RectTransform root, RuntimeOptionMenuStyle style)
    {
        this.root = root;
        this.style = style;
    }

    public int Count => items.Count;
    public bool HasActiveItem => ActiveItem != null;
    public RuntimeOptionItem ActiveItem => items.Find(item => item.IsActive);

    // 数値変更用の Slider 行を生成し、メニュー項目として登録します。
    public RuntimeOptionSlider AddSlider(
        string label,
        float minValue,
        float maxValue,
        float step,
        float initialValue,
        Action<float> valueChanged,
        Func<float, string> formatValue = null
    )
    {
        RectTransform row = CreateRow(label + " Row");
        Image background = row.gameObject.AddComponent<Image>();
        background.color = style.NormalBackgroundColor;
        background.raycastTarget = false;

        Text labelText = CreateText(label + " Label", row, label, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.46f, 1f);
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-8f, -10f);

        Text valueText = CreateText(label + " Value", row, string.Empty, TextAnchor.MiddleRight);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.74f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(8f, 10f);
        valueRect.offsetMax = new Vector2(-20f, -10f);

        RectTransform trackRect = CreateRect(label + " Track", row);
        trackRect.anchorMin = new Vector2(0.46f, 0.5f);
        trackRect.anchorMax = new Vector2(0.72f, 0.5f);
        trackRect.pivot = new Vector2(0f, 0.5f);
        trackRect.anchoredPosition = Vector2.zero;
        trackRect.sizeDelta = new Vector2(0f, 10f);

        Image trackImage = trackRect.gameObject.AddComponent<Image>();
        trackImage.color = style.BarBackgroundColor;
        trackImage.raycastTarget = false;

        RectTransform fillRect = CreateRect(label + " Fill", trackRect);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.color = style.BarFillColor;
        fillImage.raycastTarget = false;

        RuntimeOptionSlider slider = new RuntimeOptionSlider(
            row,
            background,
            valueText,
            fillRect,
            minValue,
            maxValue,
            step,
            initialValue,
            valueChanged,
            formatValue
        );

        items.Add(slider);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return slider;
    }

    // 候補選択用の Choice 行を生成し、メニュー項目として登録します。
    public RuntimeOptionChoice AddChoice(
        string label,
        string[] choices,
        int initialIndex,
        Action<int, string> selectionChanged
    )
    {
        RectTransform row = CreateRow(label + " Row");
        Image background = row.gameObject.AddComponent<Image>();
        background.color = style.NormalBackgroundColor;
        background.raycastTarget = false;

        Text labelText = CreateText(label + " Label", row, label, TextAnchor.MiddleLeft);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.58f, 1f);
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-8f, -10f);

        Text valueText = CreateText(label + " Value", row, string.Empty, TextAnchor.MiddleRight);
        RectTransform valueRect = valueText.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.58f, 0f);
        valueRect.anchorMax = new Vector2(1f, 1f);
        valueRect.offsetMin = new Vector2(8f, 10f);
        valueRect.offsetMax = new Vector2(-20f, -10f);

        RuntimeOptionChoice choice = new RuntimeOptionChoice(
            row,
            background,
            valueText,
            choices,
            initialIndex,
            selectionChanged
        );

        items.Add(choice);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return choice;
    }

    // 指定 index の行へフォーカスを移し、表示状態を更新します。
    public void SelectIndex(int index)
    {
        if (items.Count == 0)
        {
            focusedIndex = 0;
            return;
        }

        focusedIndex = Mathf.Clamp(index, 0, items.Count - 1);
        RefreshVisuals();
    }

    // 編集中の項目がないときだけ、上下フォーカスを循環移動します。
    public void MoveFocus(int delta)
    {
        if (items.Count == 0 || HasActiveItem)
        {
            return;
        }

        int nextIndex = focusedIndex + delta;

        while (nextIndex < 0)
        {
            nextIndex += items.Count;
        }

        SelectIndex(nextIndex % items.Count);
    }

    // 現在フォーカス中の行を編集状態にします。
    public void ActivateFocused()
    {
        if (items.Count == 0)
        {
            return;
        }

        CancelActiveItem();
        items[focusedIndex].Activate();
        RefreshVisuals();
    }

    // 編集中の行があればキャンセルし、キャンセルできたかを返します。
    public bool CancelActiveItem()
    {
        RuntimeOptionItem activeItem = ActiveItem;

        if (activeItem == null)
        {
            return false;
        }

        activeItem.Cancel();
        RefreshVisuals();
        return true;
    }

    // 編集中の行へ左右調整入力を渡します。
    public void AdjustActive(int direction)
    {
        RuntimeOptionItem activeItem = ActiveItem;

        if (activeItem == null)
        {
            return;
        }

        activeItem.Adjust(direction);
    }

    // 全行の背景色をフォーカス状態と編集状態に合わせて更新します。
    public void RefreshVisuals()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetVisualState(
                i == focusedIndex,
                style.NormalBackgroundColor,
                style.FocusedBackgroundColor,
                style.ActiveBackgroundColor
            );
        }
    }

    // optionContentRoot 内に 1 行ぶんの RectTransform を作ります。
    private RectTransform CreateRow(string objectName)
    {
        RectTransform row = CreateRect(objectName, root);
        row.anchorMin = new Vector2(0.5f, 1f);
        row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = style.RowSize;
        return row;
    }

    // RuntimeOptionMenu 用の Text を共通設定つきで作ります。
    private Text CreateText(string objectName, Transform parent, string value, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = value;
        text.font = style.Font;
        text.fontSize = style.FontSize;
        text.color = style.TextColor;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // RectTransform 付き GameObject を作り、指定親へ接続します。
    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    // 追加済み行の縦位置を RowSpacing に合わせて並べ直します。
    private void RefreshLayout()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Row.anchoredPosition = new Vector2(0f, -style.RowSpacing * i);
        }
    }
}
