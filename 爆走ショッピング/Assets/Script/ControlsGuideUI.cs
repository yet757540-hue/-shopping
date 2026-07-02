using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 画面左側に操作方法を表示する軽量 UI です。
// 役割:
// - Canvas や Text が未配置でも、実行時に操作ガイドを自動生成します。
// - entries のキー名と説明文を 1 つの本文テキストへ組み立てます。
// 接続:
// - GameManager がシーン起動時に未配置なら自動で追加します。
// - 日本語表示には JapaneseUIFont.Get を使い、環境にある日本語フォントを選びます。
// 読むときの要点:
// - CreateRuntimeGuide が UI の生成、RefreshText が表示文の再構築を担当します。
public class ControlsGuideUI : MonoBehaviour
{
    [Serializable]
    private class ControlEntry
    {
        public string key;
        public string action;
    }

    [Header("Display")]
    [SerializeField] private bool createIfMissing = true;
    [SerializeField] private ControlEntry[] entries =
    {
        new ControlEntry { key = "\u5de6\u30b9\u30c6\u30a3\u30c3\u30af", action = "\u65b9\u5411\u8ee2\u63db" },
        new ControlEntry { key = "RT / R2", action = "\u52a0\u901f" },
        new ControlEntry { key = "LT / L2", action = "\u30d6\u30ec\u30fc\u30ad / \u30d0\u30c3\u30af" },
        new ControlEntry { key = "L1", action = "\u62bc\u3057\u3066\u3044\u308b\u9593\u3001\u4fef\u77b0\u8868\u793a" },
        new ControlEntry { key = "\u53f3\u30b9\u30c6\u30a3\u30c3\u30af", action = "\u4fef\u77b0\u4e2d\u306b\u30ab\u30e1\u30e9\u79fb\u52d5" },
        new ControlEntry { key = "\u30bf\u30fc\u30b2\u30c3\u30c8\u306b\u885d\u7a81", action = "\u7269\u54c1\u53d6\u5f97" },
        new ControlEntry { key = "\u76ee\u6a19\u9054\u6210\u5f8c\u306b\u30b4\u30fc\u30eb", action = "\u30bf\u30a4\u30de\u30fc\u505c\u6b62" }
    };

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(24f, 48f);
    [SerializeField] private Vector2 size = new Vector2(360f, 260f);
    [SerializeField] private int titleFontSize = 28;
    [SerializeField] private int bodyFontSize = 22;

    [Header("Style")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(1f, 1f, 1f, 0.92f);

    private RectTransform panelRect;
    private Text titleText;
    private Text bodyText;
    private readonly StringBuilder textBuilder = new StringBuilder();

    // 必要なら UI を自動生成し、初期表示文を作ります。
    private void Awake()
    {
        if (createIfMissing && panelRect == null)
        {
            CreateRuntimeGuide();
        }

        RefreshText();
    }

    // Canvas、背景パネル、タイトル、本文テキストを実行時に組み立てます。
    private void CreateRuntimeGuide()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("\u64cd\u4f5c\u30ac\u30a4\u30c9\u30ad\u30e3\u30f3\u30d0\u30b9");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("\u64cd\u4f5c\u30ac\u30a4\u30c9\u30d1\u30cd\u30eb");
        panelObject.transform.SetParent(canvas.transform, false);

        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot = new Vector2(0f, 0.5f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.AddComponent<Image>();
        background.color = backgroundColor;

        titleText = CreateText("\u64cd\u4f5c\u30ac\u30a4\u30c9\u30bf\u30a4\u30c8\u30eb", panelObject.transform);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(18f, -54f);
        titleRect.offsetMax = new Vector2(-18f, -14f);

        titleText.text = "\u64cd\u4f5c\u65b9\u6cd5";
        titleText.font = GetRuntimeFont(titleFontSize);
        titleText.fontSize = titleFontSize;
        titleText.color = titleColor;
        titleText.fontStyle = FontStyle.Bold;

        bodyText = CreateText("\u64cd\u4f5c\u30ac\u30a4\u30c9\u672c\u6587", panelObject.transform);
        RectTransform bodyRect = bodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = Vector2.zero;
        bodyRect.anchorMax = Vector2.one;
        bodyRect.offsetMin = new Vector2(18f, 16f);
        bodyRect.offsetMax = new Vector2(-18f, -62f);

        bodyText.fontSize = bodyFontSize;
        bodyText.font = GetRuntimeFont(bodyFontSize);
        bodyText.color = bodyColor;
        bodyText.lineSpacing = 1.25f;
    }

    // 共通設定済みの Text オブジェクトを作ります。
    private Text CreateText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    // 実行時 UI 用の日本語対応フォントを取得します。
    private Font GetRuntimeFont(int fontSize)
    {
        return JapaneseUIFont.Get(fontSize);
    }

    // entries の内容を 1 つの本文文字列へ整形します。
    private void RefreshText()
    {
        if (bodyText == null)
        {
            return;
        }

        textBuilder.Clear();

        foreach (ControlEntry entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key))
            {
                continue;
            }

            textBuilder
                .Append(entry.key)
                .Append("  -  ")
                .AppendLine(entry.action);
        }

        bodyText.text = textBuilder.ToString().TrimEnd();
    }

    // UI サイズとフォントサイズの最低値を保証します。
    private void OnValidate()
    {
        size.x = Mathf.Max(180f, size.x);
        size.y = Mathf.Max(100f, size.y);
        titleFontSize = Mathf.Max(8, titleFontSize);
        bodyFontSize = Mathf.Max(8, bodyFontSize);
    }
}
