using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

public abstract class RuntimeOptionItem
{
    protected RuntimeOptionItem(RectTransform row, Image background)
    {
        Row = row;
        Background = background;
    }

    internal RectTransform Row { get; }
    internal Image Background { get; }
    public bool IsActive { get; private set; }

    public void SetVisualState(bool isFocused, Color normalColor, Color focusedColor, Color activeColor)
    {
        if (Background == null)
        {
            return;
        }

        Background.color = IsActive ? activeColor : isFocused ? focusedColor : normalColor;
    }

    public void Activate()
    {
        IsActive = true;
        OnActivated();
    }

    public void Cancel()
    {
        IsActive = false;
        OnCanceled();
    }

    public abstract void Adjust(int direction);

    protected virtual void OnActivated()
    {
    }

    protected virtual void OnCanceled()
    {
    }
}

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

    public override void Adjust(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        SetValue(value + step * Math.Sign(direction), true);
    }

    public void SetValue(float nextValue, bool notify)
    {
        float clamped = Mathf.Clamp(nextValue, minValue, maxValue);
        float steppedValue = minValue + Mathf.Round((clamped - minValue) / step) * step;
        value = Mathf.Clamp(steppedValue, minValue, maxValue);

        RefreshDisplay();

        if (notify)
        {
            valueChanged?.Invoke(value);
        }
    }

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

    private static string DefaultFormatValue(float sliderValue)
    {
        return Mathf.RoundToInt(sliderValue).ToString();
    }
}

public sealed class RuntimeOptionChoice : RuntimeOptionItem
{
    private readonly Text valueText;
    private readonly string[] choices;
    private readonly Action<int, string> selectionChanged;
    private int selectedIndex;

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

    public override void Adjust(int direction)
    {
        if (direction == 0 || choices.Length == 0)
        {
            return;
        }

        SetIndex(selectedIndex + Math.Sign(direction), true);
    }

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

    private void RefreshDisplay()
    {
        if (valueText != null)
        {
            valueText.text = SelectedValue;
        }
    }
}

public sealed class RuntimeOptionButton : RuntimeOptionItem
{
    private readonly Action selected;

    internal RuntimeOptionButton(RectTransform row, Image background, Action selected) : base(row, background)
    {
        this.selected = selected;
    }

    public override void Adjust(int direction)
    {
    }

    protected override void OnActivated()
    {
        selected?.Invoke();
        Cancel();
    }
}

public sealed class RuntimeOptionMenu
{
    private readonly RectTransform root;
    private readonly RuntimeOptionMenuStyle style;
    private readonly List<RuntimeOptionItem> items = new List<RuntimeOptionItem>();
    private int focusedIndex;

    public RuntimeOptionMenu(RectTransform root, RuntimeOptionMenuStyle style)
    {
        this.root = root;
        this.style = style;
    }

    public int Count => items.Count;
    public int FocusedIndex => items.Count == 0 ? -1 : focusedIndex;
    public bool HasActiveItem => ActiveItem != null;
    public RuntimeOptionItem ActiveItem => items.Find(item => item.IsActive);

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

    public RuntimeOptionButton AddButton(string label, Action selected)
    {
        RectTransform row = CreateRow(label + " Row");
        Image background = row.gameObject.AddComponent<Image>();
        background.color = style.NormalBackgroundColor;
        background.raycastTarget = false;

        Text labelText = CreateText(label + " Label", row, label, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 10f);
        labelRect.offsetMax = new Vector2(-20f, -10f);

        RuntimeOptionButton button = new RuntimeOptionButton(row, background, selected);

        items.Add(button);
        SelectIndex(items.Count - 1);
        RefreshLayout();
        return button;
    }

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

    public void AdjustActive(int direction)
    {
        RuntimeOptionItem activeItem = ActiveItem;

        if (activeItem == null)
        {
            return;
        }

        activeItem.Adjust(direction);
    }

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

    private RectTransform CreateRow(string objectName)
    {
        RectTransform row = CreateRect(objectName, root);
        row.anchorMin = new Vector2(0.5f, 1f);
        row.anchorMax = new Vector2(0.5f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = style.RowSize;
        return row;
    }

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

    private RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private void RefreshLayout()
    {
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Row.anchoredPosition = new Vector2(0f, -style.RowSpacing * i);
        }
    }
}
