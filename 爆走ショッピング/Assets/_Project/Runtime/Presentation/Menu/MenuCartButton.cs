using UnityEngine;
using UnityEngine.UI;

// カート型のタイトル画面ボタンです。枠は実行時に描き起こし、
// 選択中は右へずらして「今にも走り出す」状態を表します。
public sealed class MenuCartButton : MonoBehaviour
{
    private RectTransform root;
    private RectTransform visual;
    private Image frameImage;
    private Text labelText;
    private MenuRushInAnimator rush;
    private CartButtonStyle style;
    private Sprite normalSprite;
    private Sprite selectedSprite;
    private Sprite pressedSprite;
    private bool selected;
    private bool pressed;
    private float motionTime;
    private float currentOffset;
    private float currentScale = 1f;

    public RectTransform Root => root;
    public RectTransform Visual => visual;
    public MenuRushInAnimator Rush => rush;
    public bool IsSelected => selected;

    public void Initialize(
        CartButtonStyle cartStyle,
        string label,
        Font font,
        Color labelColor,
        Sprite idleSprite,
        Sprite highlightSprite,
        Sprite pressSprite,
        RushInStyle rushStyle
    )
    {
        root = (RectTransform)transform;
        style = cartStyle != null ? cartStyle : new CartButtonStyle();
        normalSprite = idleSprite;
        selectedSprite = highlightSprite != null ? highlightSprite : idleSprite;
        pressedSprite = pressSprite != null ? pressSprite : selectedSprite;

        visual = CreateRect("Visual", root);
        visual.anchorMin = new Vector2(0.5f, 0.5f);
        visual.anchorMax = new Vector2(0.5f, 0.5f);
        visual.pivot = new Vector2(0.5f, 0.5f);
        visual.anchoredPosition = Vector2.zero;
        visual.sizeDelta = style.size;

        RectTransform frameRect = CreateRect("Cart Frame", visual);
        Stretch(frameRect);
        frameImage = frameRect.gameObject.AddComponent<Image>();
        frameImage.sprite = normalSprite;
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;
        frameImage.preserveAspect = false;

        RectTransform labelRect = CreateRect("Label", visual);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = style.labelOffset;
        labelRect.sizeDelta = new Vector2(style.size.x * 0.7f, style.labelFontSize * 1.6f);

        labelText = labelRect.gameObject.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font != null ? font : JapaneseUIFont.Get(style.labelFontSize);
        labelText.fontSize = style.labelFontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = labelColor;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        labelText.raycastTarget = false;

        rush = root.gameObject.AddComponent<MenuRushInAnimator>();
        rush.Initialize(rushStyle);

        RefreshVisual();
    }

    public void SetSelected(bool value)
    {
        if (selected == value)
        {
            return;
        }

        selected = value;
        RefreshVisual();
    }

    public void SetPressed(bool value)
    {
        if (pressed == value)
        {
            return;
        }

        pressed = value;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (visual == null || style == null)
        {
            return;
        }

        if (frameImage != null)
        {
            frameImage.sprite = pressed ? pressedSprite : selected ? selectedSprite : normalSprite;
        }

        if (labelText != null)
        {
            labelText.fontStyle = selected || pressed ? FontStyle.BoldAndItalic : FontStyle.Bold;
        }
    }

    private void Update()
    {
        if (visual == null || style == null) return;

        float blend = 1f - Mathf.Exp(-Mathf.Max(0.1f, style.responseSpeed) * Time.unscaledDeltaTime);
        currentOffset = Mathf.Lerp(currentOffset,
            pressed ? style.pressedOffsetX : selected ? style.selectedOffsetX : 0f, blend);
        currentScale = Mathf.Lerp(currentScale,
            pressed ? style.pressedScale : selected ? style.selectedScale : 1f, blend);

        bool idle = style.idleMotion && !pressed && (rush == null || !rush.IsPlaying);
        if (idle) motionTime += Time.unscaledDeltaTime;
        float phase = motionTime * Mathf.PI * 2f / Mathf.Max(0.1f, style.idlePeriod);
        float wave = idle ? Mathf.Sin(phase) : 0f;
        float pulse = idle && selected ? style.selectedPulse * (1f - Mathf.Cos(phase)) * 0.5f : 0f;
        visual.anchoredPosition = new Vector2(currentOffset, wave * style.idleBob);
        visual.localRotation = Quaternion.Euler(0f, 0f, wave * style.idleTilt);
        visual.localScale = Vector3.one * (currentScale + pulse);
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
