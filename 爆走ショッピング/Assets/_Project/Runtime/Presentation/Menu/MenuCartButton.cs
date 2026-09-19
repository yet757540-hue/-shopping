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

        float offsetX = pressed ? style.pressedOffsetX : selected ? style.selectedOffsetX : 0f;
        float scale = pressed ? style.pressedScale : selected ? style.selectedScale : 1f;

        visual.anchoredPosition = new Vector2(offsetX, 0f);
        visual.localScale = new Vector3(scale, scale, 1f);

        if (frameImage != null)
        {
            frameImage.sprite = pressed ? pressedSprite : selected ? selectedSprite : normalSprite;
        }

        if (labelText != null)
        {
            labelText.fontStyle = selected || pressed ? FontStyle.BoldAndItalic : FontStyle.Bold;
        }
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
