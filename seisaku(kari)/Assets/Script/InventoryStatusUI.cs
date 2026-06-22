using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class InventoryStatusUI : MonoBehaviour
{
    private class ItemSummary
    {
        public string displayName;
        public int count;
    }

    [Header("References")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private InventoryInfluenceSettings influenceSettings;

    [Header("Layout")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(24f, 24f);
    [SerializeField] private Vector2 size = new Vector2(390f, 250f);
    [SerializeField] private int titleFontSize = 24;
    [SerializeField] private int bodyFontSize = 18;
    [SerializeField] private int influenceFontSize = 18;
    [SerializeField] private int maxVisibleItems = 3;
    [SerializeField] private float influenceHeight = 82f;

    [Header("Style")]
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

    private void Awake()
    {
        ResolveReferences();
        CreateRuntimePanel();
        RefreshText();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
        RefreshText();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshText;
        }
    }

    private void Update()
    {
        if (inventory == null || influenceSettings == null)
        {
            ResolveReferences();
            SubscribeInventory();
        }

        RefreshText();
    }

    private void ResolveReferences()
    {
        if (inventory == null)
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (influenceSettings == null)
        {
            influenceSettings = FindAnyObjectByType<InventoryInfluenceSettings>();
        }
    }

    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= RefreshText;
        inventory.InventoryChanged += RefreshText;
    }

    private void CreateRuntimePanel()
    {
        if (panelRect != null)
        {
            return;
        }

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

        int hiddenCount = itemSummaries.Count - shown;
        if (hiddenCount > 0)
        {
            inventoryBuilder
                .Append("\u307b\u304b ")
                .Append(hiddenCount)
                .AppendLine(" \u7a2e\u985e");
        }
    }

    private void BuildInfluenceText()
    {
        influenceBuilder.Clear();
        influenceBuilder.AppendLine("\u91cd\u91cf\u5f71\u97ff");

        float acceleration = influenceSettings != null ? influenceSettings.CurrentAccelerationMultiplier : 1f;
        float deceleration = influenceSettings != null ? influenceSettings.CurrentDecelerationMultiplier : 1f;
        float turn = influenceSettings != null ? influenceSettings.CurrentTurnMultiplier : 1f;
        float collision = influenceSettings != null ? influenceSettings.CurrentCollisionMultiplier : 1f;

        influenceBuilder
            .Append("\u52a0\u901f ")
            .Append(FormatMultiplier(acceleration))
            .Append(" / \u6e1b\u901f ")
            .Append(FormatMultiplier(deceleration))
            .AppendLine();

        influenceBuilder
            .Append("\u65cb\u56de ")
            .Append(FormatMultiplier(turn))
            .Append(" / \u885d\u7a81 ")
            .Append(FormatMultiplier(collision))
            .AppendLine();
    }

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

    private string FormatMultiplier(float value)
    {
        return "x" + value.ToString("0.00");
    }

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
