using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>実行時生成する選択メニューの共通見た目です。</summary>
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

/// <summary>スライダー・選択肢・ボタンに共通する、フォーカス可能な 1 行です。</summary>
public abstract class RuntimeOptionItem
{
    // 表示行と背景への参照を保持します。
    protected RuntimeOptionItem(RectTransform row, Image background)
    {
        Row = row;
        Background = background;
    }

    // この行の配置に使用する RectTransform を公開します。
    internal RectTransform Row { get; }
    // 選択色や編集色を反映する背景画像を公開します。
    internal Image Background { get; }
    // この行が編集状態かを返します。
    public bool IsActive { get; private set; }

    // 編集中・選択中・通常の優先順で背景色を決めます。
    public void SetVisualState(bool isFocused, Color normalColor, Color focusedColor, Color activeColor)
    {
        if (Background == null)
        {
            return;
        }

        Background.color = IsActive ? activeColor : isFocused ? focusedColor : normalColor;
    }

    // 編集状態にして、行の種類に応じた決定処理を呼びます。
    public void Activate()
    {
        IsActive = true;
        OnActivated();
    }

    // 編集状態を解除し、行の種類に応じた解除処理を呼びます。
    public void Cancel()
    {
        IsActive = false;
        OnCanceled();
    }

    // 方向入力に応じた値変更を各行で実装します。ボタン行では何もしません。
    public abstract void Adjust(int direction);

    // 行の決定時に実行する処理です。基底実装は空で、ボタンは登録された処理を呼びます。
    protected virtual void OnActivated()
    {
    }

    // 編集終了時に派生クラスが追加処理を行うための入口です。基底実装は空です。
    protected virtual void OnCanceled()
    {
    }
}

/// <summary>左右入力で数値を変更するメニュー行です。</summary>
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

    // 表示参照と数値範囲・刻み幅・通知先を保持し、初期値を通知なしで設定します。
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

    // 補正と刻み幅の適用後のスライダー値を返します。
    public float Value => value;
    // バー表示用に、現在値をゼロから一へ変換します。
    public float NormalizedValue => Mathf.InverseLerp(minValue, maxValue, value);
    // ポインター入力で位置から値を求めるときに使う下限です。
    public float MinValue => minValue;
    // ポインター入力で位置から値を求めるときに使う上限です。
    public float MaxValue => maxValue;
    // 値をクリックで変更するときに基準にするバーの領域です。
    internal RectTransform TrackRect => fillRect == null ? null : fillRect.parent as RectTransform;

    // 方向入力に応じた値変更を各行で実装します。ボタン行では何もしません。
    public override void Adjust(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        SetValue(value + step * Math.Sign(direction), true);
    }

    // 値を範囲と刻み幅に合わせて表示し、指定された場合だけ変更を通知します。
    public void SetValue(float nextValue, bool notify)
    {
        // 範囲内に収めてから刻み幅へ丸め、丸めによる範囲超過も補正します。
        float clamped = Mathf.Clamp(nextValue, minValue, maxValue);
        float steppedValue = minValue + Mathf.Round((clamped - minValue) / step) * step;
        value = Mathf.Clamp(steppedValue, minValue, maxValue);

        RefreshDisplay();

        if (notify)
        {
            valueChanged?.Invoke(value);
        }
    }

    // 現在の数値または選択候補を文字へ反映し、スライダーではバーの長さも更新します。
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

    // スライダー値を四捨五入した整数文字列に変換します。
    private static string DefaultFormatValue(float sliderValue)
    {
        return Mathf.RoundToInt(sliderValue).ToString();
    }
}

/// <summary>左右入力で候補を切り替えるメニュー行です。</summary>
public sealed class RuntimeOptionChoice : RuntimeOptionItem
{
    private readonly Text valueText;
    private readonly string[] choices;
    private readonly Action<int, string> selectionChanged;
    private int selectedIndex;

    // 選択候補と通知先を保持し、候補が空なら代替項目を用意して初期選択を設定します。
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

    // 現在選択している候補の番号を返します。
    public int SelectedIndex => selectedIndex;
    // 現在選択している候補の表示文字を返します。
    public string SelectedValue => choices[selectedIndex];

    // 方向入力に応じた値変更を各行で実装します。ボタン行では何もしません。
    public override void Adjust(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        SetIndex(selectedIndex + Math.Sign(direction), true);
    }

    // 端で循環するよう選択番号を補正し、表示と必要に応じた通知を更新します。
    public void SetIndex(int index, bool notify)
    {
        // 候補は初期化時に必ず 1 件以上にします。端を越えたら反対側へ戻ります。
        selectedIndex = index % choices.Length;
        if (selectedIndex < 0)
        {
            selectedIndex += choices.Length;
        }
        RefreshDisplay();

        if (notify)
        {
            selectionChanged?.Invoke(selectedIndex, SelectedValue);
        }
    }

    // 現在の数値または選択候補を文字へ反映し、スライダーではバーの長さも更新します。
    private void RefreshDisplay()
    {
        if (valueText != null)
        {
            valueText.text = SelectedValue;
        }
    }
}

/// <summary>決定入力で登録済みの処理を呼ぶメニュー行です。</summary>
public sealed class RuntimeOptionButton : RuntimeOptionItem
{
    private readonly Action selected;

    // ボタン行の表示参照と、決定時に呼ぶ処理を保持します。
    internal RuntimeOptionButton(RectTransform row, Image background, Action selected) : base(row, background)
    {
        this.selected = selected;
    }

    // 方向入力に応じた値変更を各行で実装します。ボタン行では何もしません。
    public override void Adjust(int direction)
    {
    }

    // 行の決定時に実行する処理です。基底実装は空で、ボタンは登録された処理を呼びます。
    protected override void OnActivated()
    {
        selected?.Invoke();
        Cancel();
    }
}

/// <summary>複数のメニュー行のフォーカス、決定、キャンセルをまとめます。</summary>
public sealed class RuntimeOptionMenu
{
    private readonly RectTransform root;
    private readonly RuntimeOptionMenuStyle style;
    private readonly List<RuntimeOptionItem> items = new List<RuntimeOptionItem>();
    private int focusedIndex;

    // 配置先と共通スタイルを受け取り、行を管理できる状態にします。
    public RuntimeOptionMenu(RectTransform root, RuntimeOptionMenuStyle style)
    {
        this.root = root;
        this.style = style;
    }

    // 登録済みのメニュー行数を返します。
    public int Count => items.Count;
    // フォーカス中の行番号を返します。行がなければマイナス一です。
    public int FocusedIndex => items.Count == 0 ? -1 : focusedIndex;
    // 編集中の行が存在するかを返します。
    public bool HasActiveItem => ActiveItem != null;
    // 編集中の行を探して返します。なければ null です。
    public RuntimeOptionItem ActiveItem => items.Find(item => item.IsActive);

    // ラベル・数値・バーを持つ調整行を作り、一覧へ追加して配置を更新します。
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
        Image background = row.GetComponent<Image>();

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

        // ラベルと数値の間にバーを置き、子の塗りつぶし幅で値を表します。
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

        // 追加した行を選択し、一覧全体の位置を更新します。
        items.Add(slider);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return slider;
    }

    // ラベルと候補名を持つ選択行を作り、一覧へ追加して配置を更新します。
    public RuntimeOptionChoice AddChoice(
        string label,
        string[] choices,
        int initialIndex,
        Action<int, string> selectionChanged
    )
    {
        RectTransform row = CreateRow(label + " Row");
        Image background = row.GetComponent<Image>();

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

        // 追加した候補行を選択し、一覧全体の位置を更新します。
        items.Add(choice);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return choice;
    }

    // 中央揃えのボタン行を作り、決定時の処理を登録します。
    public RuntimeOptionButton AddButton(string label, Action selected)
    {
        RectTransform row = CreateRow(label + " Row");
        Image background = row.GetComponent<Image>();

        Text labelText = CreateText(label + " Label", row, label, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-20f, -10f);

        RuntimeOptionButton button = new RuntimeOptionButton(row, background, selected);

        // 追加したボタンを選択し、一覧全体の位置を更新します。
        items.Add(button);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return button;
    }

    // 選択番号を一覧の有効範囲に収め、各行の見た目を更新します。
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

    // 編集中でなければ選択を移動し、一覧の端を越えたら反対側へ戻します。
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

    // 画面上の位置にある行の番号を返します。どの行にも当たらない場合は -1 です。
    public int FindIndexAtScreenPoint(Vector2 screenPoint)
    {
        for (int i = 0; i < items.Count; i++)
        {
            RectTransform row = items[i].Row;

            if (row != null && RectTransformUtility.RectangleContainsScreenPoint(row, screenPoint, null))
            {
                return i;
            }
        }

        return -1;
    }

    // クリック位置を値へ反映します。
    // 数値行はクリックした位置の値へ移動し、候補行は次の候補へ進みます。
    public bool TrySetValueAtScreenPoint(int index, Vector2 screenPoint)
    {
        if (index < 0 || index >= items.Count)
        {
            return false;
        }

        if (items[index] is RuntimeOptionSlider slider)
        {
            RectTransform track = slider.TrackRect;

            if (track == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(track, screenPoint, null, out Vector2 localPoint))
            {
                return false;
            }

            float normalized = Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, localPoint.x);
            slider.SetValue(Mathf.Lerp(slider.MinValue, slider.MaxValue, Mathf.Clamp01(normalized)), true);
            return true;
        }

        if (items[index] is RuntimeOptionChoice choice)
        {
            choice.Adjust(1);
            return true;
        }

        return false;
    }

    // 前の編集を解除してから、選択中の行を決定状態にします。
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

    // 編集中の行があれば解除して表示を更新し、解除できたかを返します。
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

    // 編集中の行がある場合だけ、左右方向の値変更を渡します。
    public void AdjustActive(int direction)
    {
        RuntimeOptionItem activeItem = ActiveItem;

        if (activeItem == null)
        {
            return;
        }

        activeItem.Adjust(direction);
    }

    // 全ての行に、選択位置と編集状態に対応する色を適用します。
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

    // 全種類の行に共通する位置・サイズ・背景を一か所で設定します。
    private RectTransform CreateRow(string objectName)
    {
        RectTransform row = CreateRect(objectName, root);
        row.anchorMin = new Vector2(0.5f, 1f);
        row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = style.RowSize;
        Image background = row.gameObject.AddComponent<Image>();
        background.color = style.NormalBackgroundColor;
        background.raycastTarget = false;
        return row;
    }

    // 共通スタイルのフォント・色・揃えを使い、入力を遮らない文字を作ります。
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

    // 指定した親の下に RectTransform を持つ UI オブジェクトを作成します。
    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    // 登録順に各行を縦方向へ並べ、設定した間隔を適用します。
    private void RefreshLayout()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Row.anchoredPosition = new Vector2(0f, -style.RowSpacing * i);
        }
    }
}
