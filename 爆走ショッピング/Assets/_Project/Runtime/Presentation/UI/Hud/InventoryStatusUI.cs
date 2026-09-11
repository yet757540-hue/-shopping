using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>所持品の個数・総重量・性能補正を HUD に表示します。</summary>
public class InventoryStatusUI : MonoBehaviour
{
    // 同じアイテム ID の表示名と合計個数をまとめます。
    private class ItemSummary
    {
        public string displayName;
        public int count;
    }

    // 他の機能や表示部品への参照です。設定方法は Initialize または初期化処理を参照してください。
    [Header("参照")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private InventoryInfluenceSettings influenceSettings;

    // HUD の位置・サイズ・文字サイズと、種類別一覧の表示上限です。
    [Header("レイアウト")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(24f, 24f);
    [SerializeField] private Vector2 size = new Vector2(390f, 250f);
    [SerializeField] private int titleFontSize = 24;
    [SerializeField] private int bodyFontSize = 18;
    [SerializeField] private int influenceFontSize = 18;
    [SerializeField] private int maxVisibleItems = 3;
    [SerializeField] private float influenceHeight = 82f;

    // UI の色・文字サイズ・配置寸法をまとめた設定です。
    [Header("見た目")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField] private Color titleColor = Color.white;
    [SerializeField] private Color bodyColor = new Color(1f, 1f, 1f, 0.92f);

    private RectTransform panelRect;
    private Text titleText;
    private Text inventoryText;
    private Text influenceText;
    private readonly StringBuilder inventoryBuilder = new StringBuilder();
    private readonly StringBuilder influenceBuilder = new StringBuilder();
    private readonly Dictionary<string, ItemSummary> itemSummaries = new Dictionary<string, ItemSummary>();

    // 所持品と重量補正の参照を受け取り、通知の購読と表示更新を行います。
    public void Initialize(PlayerInventory configuredInventory, InventoryInfluenceSettings configuredInfluenceSettings)
    {
        inventory = configuredInventory;
        influenceSettings = configuredInfluenceSettings;
        SubscribeInventory();
        RefreshText();
    }

    // 参照確認の入口を呼び、所持情報パネルを作って初期表示します。
    private void Awake()
    {
        ResolveReferences();
        CreateRuntimePanel();
        RefreshText();
    }

    // 所持品通知を購読し、再有効化時の表示を同期します。
    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
        RefreshText();
    }

    // この HUD が登録した所持品通知を解除します。
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshText;
        }
    }

    // 参照が不足していれば購読を試み、現在の所持品と重量補正を表示します。
    private void Update()
    {
        if (inventory == null || influenceSettings == null)
        {
            ResolveReferences();
            SubscribeInventory();
        }

        RefreshText();
    }

    // 現在は処理を行いません。参照は Initialize から受け取ります。
    private void ResolveReferences()
    {
    }

    // 重複登録を防いで所持品変更イベントを購読します。
    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= RefreshText;
        inventory.InventoryChanged += RefreshText;
    }

    // 既存 Canvas を利用し、所持数・一覧・重量補正を表示するパネルを一度だけ作ります。
    private void CreateRuntimePanel()
    {
        if (panelRect != null)
        {
            return;
        }

        // 既存 Canvas を再利用し、存在しない場合だけ描画先を作ります。
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Inventory Status Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("Inventory Status Panel");
        panelObject.transform.SetParent(canvas.transform, false);

        panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.zero;
        panelRect.pivot = Vector2.zero;
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.AddComponent<Image>();
        background.color = backgroundColor;

        // 上部に所持情報の見出しを配置します。
        titleText = CreateText("Inventory Status Title", panelObject.transform);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(16f, -42f);
        titleRect.offsetMax = new Vector2(-16f, -10f);

        titleText.text = "\u6240\u6301\u60c5\u5831";
        titleText.font = JapaneseUIFont.Get(titleFontSize);
        titleText.fontSize = titleFontSize;
        titleText.color = titleColor;
        titleText.fontStyle = FontStyle.Bold;

        // 中央に種類別の所持数を表示する領域を確保します。
        inventoryText = CreateText("Inventory Item List", panelObject.transform);
        RectTransform inventoryRect = inventoryText.GetComponent<RectTransform>();
        inventoryRect.anchorMin = Vector2.zero;
        inventoryRect.anchorMax = Vector2.one;
        inventoryRect.offsetMin = new Vector2(16f, influenceHeight + 16f);
        inventoryRect.offsetMax = new Vector2(-16f, -50f);

        inventoryText.font = JapaneseUIFont.Get(bodyFontSize);
        inventoryText.fontSize = bodyFontSize;
        inventoryText.color = bodyColor;
        inventoryText.lineSpacing = 1.1f;

        // 下部に重量による性能倍率を表示します。
        influenceText = CreateText("Inventory Influence", panelObject.transform);
        RectTransform influenceRect = influenceText.GetComponent<RectTransform>();
        influenceRect.anchorMin = Vector2.zero;
        influenceRect.anchorMax = new Vector2(1f, 0f);
        influenceRect.pivot = Vector2.zero;
        influenceRect.offsetMin = new Vector2(16f, 12f);
        influenceRect.offsetMax = new Vector2(-16f, influenceHeight);

        influenceText.font = JapaneseUIFont.Get(influenceFontSize);
        influenceText.fontSize = influenceFontSize;
        influenceText.color = bodyColor;
        influenceText.lineSpacing = 1.05f;
    }

    // 親の下に左上揃えの文字を作り、表示範囲を超えた縦方向の文字を切り詰めます。
    private Text CreateText(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    // 表示先がそろっていれば、所持品と重量補正の文字列を作って反映します。
    private void RefreshText()
    {
        if (inventoryText == null || influenceText == null)
        {
            return;
        }

        BuildInventoryText();
        BuildInfluenceText();
        inventoryText.text = inventoryBuilder.ToString().TrimEnd();
        influenceText.text = influenceBuilder.ToString().TrimEnd();
    }

    // 総数と総重量を表示し、種類別一覧は表示上限まで並べ、残り種類数を末尾に付けます。
    private void BuildInventoryText()
    {
        inventoryBuilder.Clear();

        float totalWeight = inventory != null ? inventory.TotalWeight : 0f;
        int totalCount = inventory != null ? inventory.CarriedItems.Count : 0;

        inventoryBuilder
            .Append("\u6240\u6301\u6570: ")
            .Append(totalCount)
            .Append(" / \u7dcf\u91cd\u91cf: ")
            .Append(totalWeight.ToString("0.0"))
            .AppendLine();

        if (inventory == null || inventory.CarriedItems.Count == 0)
        {
            inventoryBuilder.AppendLine("\u6240\u6301\u54c1: \u306a\u3057");
            return;
        }

        BuildItemSummaries();

        int shown = 0;
        foreach (ItemSummary summary in itemSummaries.Values)
        {
            if (shown >= maxVisibleItems)
            {
                break;
            }

            inventoryBuilder
                .Append(summary.displayName)
                .Append(" x")
                .Append(summary.count)
                .AppendLine();

            shown++;
        }

        // 表示上限を超えた種類は、詳細の代わりに残りの種類数を表示します。
        int hiddenCount = itemSummaries.Count - shown;
        if (hiddenCount > 0)
        {
            inventoryBuilder
                .Append("\u307b\u304b ")
                .Append(hiddenCount)
                .AppendLine(" \u7a2e\u985e");
        }
    }

    // 加速・減速・旋回・衝突の重量補正倍率を表示用の文章にまとめます。
    private void BuildInfluenceText()
    {
        influenceBuilder.Clear();
        influenceBuilder.AppendLine("\u91cd\u91cf\u5f71\u97ff");

        float acceleration = influenceSettings != null ? influenceSettings.CurrentAccelerationMultiplier : 1f;
        float deceleration = influenceSettings != null ? influenceSettings.CurrentDecelerationMultiplier : 1f;
        float turnAcceleration = influenceSettings != null ? influenceSettings.CurrentTurnAccelerationMultiplier : 1f;
        float turnDeceleration = influenceSettings != null ? influenceSettings.CurrentTurnDecelerationMultiplier : 1f;
        float collision = influenceSettings != null ? influenceSettings.CurrentCollisionMultiplier : 1f;

        influenceBuilder
            .Append("\u52a0\u901f ")
            .Append(FormatMultiplier(acceleration))
            .Append(" / \u6e1b\u901f ")
            .Append(FormatMultiplier(deceleration))
            .AppendLine();

        influenceBuilder
            .Append("\u65cb\u56de\u52a0\u901f ")
            .Append(FormatMultiplier(turnAcceleration))
            .Append(" / \u65cb\u56de\u6e1b\u901f ")
            .Append(FormatMultiplier(turnDeceleration))
            .AppendLine();

        influenceBuilder
            .Append("\u885d\u7a81 ")
            .Append(FormatMultiplier(collision))
            .AppendLine();
    }

    // 同じアイテム ID をまとめ、HUD には短い一覧だけを表示します。
    private void BuildItemSummaries()
    {
        itemSummaries.Clear();

        foreach (PlayerInventory.CarriedItem item in inventory.CarriedItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
            {
                continue;
            }

            if (!itemSummaries.TryGetValue(item.itemId, out ItemSummary summary))
            {
                summary = new ItemSummary
                {
                    displayName = string.IsNullOrWhiteSpace(item.displayName) ? item.itemId : item.displayName,
                    count = 0
                };

                itemSummaries.Add(item.itemId, summary);
            }

            summary.count++;
        }
    }

    // 性能倍率を小数二桁の文字列に整えます。
    private string FormatMultiplier(float value)
    {
        return "x" + value.ToString("0.00");
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        size.x = Mathf.Max(220f, size.x);
        size.y = Mathf.Max(180f, size.y);
        titleFontSize = Mathf.Max(8, titleFontSize);
        bodyFontSize = Mathf.Max(8, bodyFontSize);
        influenceFontSize = Mathf.Max(8, influenceFontSize);
        maxVisibleItems = Mathf.Clamp(maxVisibleItems, 1, 4);
        influenceHeight = Mathf.Clamp(influenceHeight, 60f, size.y - 90f);
    }
}
