using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ScoreboardManager : MonoBehaviour
{
    [Serializable]
    private class TargetEntry
    {
        public ScoreTarget target;
        public int requiredCount;
    }

    [Header("References")]
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Text scoreboardText;
    [SerializeField] private SettlementArea settlementArea;

    [Header("Board Settings")]
    [SerializeField] private int targetCount = 3;
    [SerializeField] private int minRequiredItemCount = 3;
    [SerializeField] private int maxRequiredItemCount = 8;
    [SerializeField] private bool resetInventoryOnEnd = true;
    [SerializeField] private bool clearWhenTimerStops = false;

    [Header("Scoring")]
    [SerializeField] private int targetItemScoreMultiplier = 10;
    [SerializeField] private int settlementBonusScore = 50;

    [Header("Item Gain From Impact")]
    [SerializeField] private int minItemGain = 1;
    [SerializeField] private int maxItemGain = 5;

    [Header("Runtime UI")]
    [SerializeField] private bool createTextIfMissing = true;
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-32f, 32f);
    [SerializeField] private Vector2 size = new Vector2(360f, 150f);
    [SerializeField] private int fontSize = 28;

    [Header("Incomplete Warning")]
    [SerializeField] private Color incompleteFlashColor = Color.red;
    [SerializeField] private int incompleteFlashCount = 3;
    [SerializeField] private float incompleteFlashInterval = 0.12f;

    [Header("Runtime Debug")]
    [SerializeField] private float lastRawImpactSpeed = 0f;
    [SerializeField] private float lastImpactSpeed = 0f;
    [SerializeField] private float lastImpactRate = 0f;
    [SerializeField] private int lastItemGain = 0;
    [SerializeField] private int lastRequiredCount = 0;
    [SerializeField] private int completedSettlementCount = 0;
    [SerializeField] private int settledExcessTargetItemCount = 0;

    private readonly List<TargetEntry> activeEntries = new List<TargetEntry>();
    private readonly StringBuilder textBuilder = new StringBuilder();
    private Color normalTextColor = Color.white;
    private Coroutine incompleteFlashCoroutine;
    private bool isBoardActive = false;
    private bool canSettleCurrentTargets = true;
    private int pendingRequiredItemCountOffset = 0;
    private ScoreTarget[] targetPool = Array.Empty<ScoreTarget>();
    private SettlementArea subscribedSettlementArea;

    public event Action SettlementCompleted;

    public bool IsComplete
    {
        get
        {
            if (!isBoardActive || activeEntries.Count == 0)
            {
                return false;
            }

            foreach (TargetEntry entry in activeEntries)
            {
                if (entry == null || entry.target == null)
                {
                    return false;
                }

                if (GetCurrentCount(entry.target) < entry.requiredCount)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public bool IsActive => isBoardActive && activeEntries.Count > 0;
    public int CompletedSettlementCount => completedSettlementCount;
    public int SettledExcessTargetItemCount => settledExcessTargetItemCount;
    public int SettledTargetItemCount => settledExcessTargetItemCount;
    public int TargetItemScoreMultiplier => targetItemScoreMultiplier;
    public int SettlementBonusScorePerSettlement => settlementBonusScore;
    public int SettlementBonusScore => completedSettlementCount * settlementBonusScore;
    public int TargetItemScore => settledExcessTargetItemCount * targetItemScoreMultiplier;
    public int TotalScore => SettlementBonusScore + TargetItemScore;

    private void Awake()
    {
        ResolveReferences();

        if (scoreboardText == null && createTextIfMissing)
        {
            CreateRuntimeScoreboard();
        }

        if (scoreboardText != null)
        {
            normalTextColor = scoreboardText.color;
        }

        ResetScore();
        ClearScoreboard();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeInventory();
        SubscribeSettlementArea();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        }

        UnsubscribeSettlementArea();
    }

    public void StartScoreboard()
    {
        if (isBoardActive)
        {
            return;
        }

        ResolveReferences();
        canSettleCurrentTargets = settlementArea == null || !settlementArea.HasPlayerInside;
        EnsureTargetPool();
        StopIncompleteFlash();
        RestoreAllTargetHighlights();
        ResetTargetCollectionState();

        int requiredItemCountOffset = pendingRequiredItemCountOffset;
        pendingRequiredItemCountOffset = 0;
        activeEntries.Clear();
        isBoardActive = true;

        if (targetPool == null || targetPool.Length == 0)
        {
            RefreshText();
            return;
        }

        List<ScoreTarget> candidates = BuildUniqueCandidates();
        int count = Mathf.Min(targetCount, candidates.Count);

        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, candidates.Count);
            ScoreTarget selectedTarget = candidates[index];

            activeEntries.Add(new TargetEntry
            {
                target = selectedTarget,
                requiredCount = GetRandomRequiredCount(requiredItemCountOffset)
            });

            SetHighlightForItemId(selectedTarget.ItemId, true);
            candidates.RemoveAt(index);
        }

        RefreshText();
    }

    public bool TryCompleteScoreboard()
    {
        if (IsActive && !IsComplete)
        {
            FlashIncompleteWarning();
            return false;
        }

        CompleteScoreboard(resetInventoryOnEnd);
        return true;
    }

    public bool TrySettleCompletedTargets()
    {
        ResolveReferences();

        if (!IsActive)
        {
            return false;
        }

        if (!canSettleCurrentTargets)
        {
            return false;
        }

        if (!IsComplete)
        {
            FlashIncompleteWarning();
            return false;
        }

        RecordSettlementScore();
        CompleteScoreboard(true);

        if (SettlementCompleted != null)
        {
            SettlementCompleted.Invoke();
        }
        else
        {
            StartScoreboard();
        }

        return true;
    }

    public void AddNextRequiredItemCountOffset(int offset)
    {
        pendingRequiredItemCountOffset += offset;
    }

    public void MultiplyItemGainRange(float multiplier)
    {
        float clampedMultiplier = Mathf.Max(0f, multiplier);
        minItemGain = Mathf.Max(1, Mathf.CeilToInt(minItemGain * clampedMultiplier));
        maxItemGain = Mathf.Max(minItemGain, Mathf.CeilToInt(maxItemGain * clampedMultiplier));
    }

    public void ResetScore()
    {
        completedSettlementCount = 0;
        settledExcessTargetItemCount = 0;
    }

    public void ClearScoreboard()
    {
        RestoreAllTargetHighlights();
        StopIncompleteFlash();

        isBoardActive = false;
        activeEntries.Clear();
        RefreshText();
    }

    public void RegisterCollision(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        ScoreTarget target = FindScoreTarget(collision);

        if (target == null)
        {
            return;
        }

        ResolveReferences();

        if (inventory == null)
        {
            return;
        }

        if (!TryCalculateItemGain(collision.relativeVelocity.magnitude, out int itemGain))
        {
            return;
        }

        if (!inventory.TryAddItem(target, itemGain))
        {
            return;
        }

        if (!isBoardActive)
        {
            return;
        }

        UpdateTargetHighlight(target);
        RefreshText();
    }

    public void FlashIncompleteWarning()
    {
        if (scoreboardText == null)
        {
            return;
        }

        StopIncompleteFlash();
        incompleteFlashCoroutine = StartCoroutine(FlashIncompleteCoroutine());
    }

    private void RecordSettlementScore()
    {
        settledExcessTargetItemCount += CalculateSettledExcessTargetItemCount();
        completedSettlementCount++;
    }

    private int CalculateSettledExcessTargetItemCount()
    {
        if (inventory == null || activeEntries.Count == 0)
        {
            return 0;
        }

        int total = 0;
        HashSet<string> countedItemIds = new HashSet<string>();

        foreach (TargetEntry entry in activeEntries)
        {
            if (entry == null || entry.target == null)
            {
                continue;
            }

            string itemId = entry.target.ItemId;

            if (string.IsNullOrWhiteSpace(itemId) || !countedItemIds.Add(itemId))
            {
                continue;
            }

            int currentCount = inventory.GetCount(itemId);
            total += Mathf.Max(0, currentCount - entry.requiredCount);
        }

        return total;
    }

    private void CompleteScoreboard(bool clearInventory)
    {
        StopIncompleteFlash();
        RestoreAllTargetHighlights();
        isBoardActive = false;

        if (clearInventory && inventory != null)
        {
            inventory.ClearInventory();
        }

        if (clearWhenTimerStops)
        {
            ClearScoreboard();
        }
    }

    private void ResolveReferences()
    {
        if (impactSettings == null)
        {
            impactSettings = FindAnyObjectByType<ImpactSettings>();
        }

        if (impactSettings == null)
        {
            impactSettings = gameObject.AddComponent<ImpactSettings>();
        }

        if (inventory == null)
        {
            inventory = FindAnyObjectByType<PlayerInventory>();
        }

        if (inventory == null)
        {
            PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();
            GameObject owner = playerManager != null ? playerManager.gameObject : gameObject;
            inventory = owner.GetComponent<PlayerInventory>();

            if (inventory == null)
            {
                inventory = owner.AddComponent<PlayerInventory>();
            }
        }

        if (settlementArea == null)
        {
            settlementArea = FindAnyObjectByType<SettlementArea>();
        }

        SubscribeInventory();
        SubscribeSettlementArea();
    }

    private void HandleSettlementAreaEntered()
    {
        TrySettleCompletedTargets();
    }

    private void HandleSettlementAreaExited()
    {
        canSettleCurrentTargets = true;
    }

    private void SubscribeSettlementArea()
    {
        if (settlementArea == null)
        {
            return;
        }

        if (subscribedSettlementArea != null && subscribedSettlementArea != settlementArea)
        {
            subscribedSettlementArea.PlayerEntered -= HandleSettlementAreaEntered;
            subscribedSettlementArea.PlayerExited -= HandleSettlementAreaExited;
        }

        subscribedSettlementArea = settlementArea;
        subscribedSettlementArea.PlayerEntered -= HandleSettlementAreaEntered;
        subscribedSettlementArea.PlayerExited -= HandleSettlementAreaExited;
        subscribedSettlementArea.PlayerEntered += HandleSettlementAreaEntered;
        subscribedSettlementArea.PlayerExited += HandleSettlementAreaExited;
    }

    private void UnsubscribeSettlementArea()
    {
        if (subscribedSettlementArea == null)
        {
            return;
        }

        subscribedSettlementArea.PlayerEntered -= HandleSettlementAreaEntered;
        subscribedSettlementArea.PlayerExited -= HandleSettlementAreaExited;
        subscribedSettlementArea = null;
    }

    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        inventory.InventoryChanged += RefreshAfterInventoryChanged;
    }

    private List<ScoreTarget> BuildUniqueCandidates()
    {
        List<ScoreTarget> candidates = new List<ScoreTarget>();
        HashSet<string> usedItemIds = new HashSet<string>();

        foreach (ScoreTarget target in targetPool)
        {
            if (target == null || string.IsNullOrWhiteSpace(target.ItemId))
            {
                continue;
            }

            if (usedItemIds.Add(target.ItemId))
            {
                candidates.Add(target);
            }
        }

        return candidates;
    }

    private ScoreTarget FindScoreTarget(Collision collision)
    {
        ScoreTarget target = null;

        if (collision.collider != null)
        {
            target = collision.collider.GetComponentInParent<ScoreTarget>();

            if (target != null)
            {
                return target;
            }
        }

        if (collision.rigidbody != null)
        {
            target = collision.rigidbody.GetComponentInParent<ScoreTarget>();
        }

        return target;
    }

    private int GetRandomRequiredCount(int offset)
    {
        int min = Mathf.Min(minRequiredItemCount, maxRequiredItemCount);
        int max = Mathf.Max(minRequiredItemCount, maxRequiredItemCount);
        lastRequiredCount = Mathf.Max(1, UnityEngine.Random.Range(min, max + 1) + offset);
        return lastRequiredCount;
    }

    private bool TryCalculateItemGain(float impactSpeed, out int itemGain)
    {
        lastRawImpactSpeed = impactSpeed;
        lastImpactRate = impactSettings != null ? impactSettings.GetImpactRateFromRawSpeed(impactSpeed) : 0f;
        lastImpactSpeed = impactSettings != null ? impactSettings.LastAdjustedImpactSpeed : impactSpeed;

        if (impactSettings != null && !impactSettings.IsStrongEnough(lastImpactSpeed))
        {
            lastItemGain = 0;
            itemGain = 0;
            return false;
        }

        itemGain = Mathf.RoundToInt(Mathf.Lerp(minItemGain, maxItemGain, lastImpactRate));
        lastItemGain = itemGain;
        return true;
    }

    private void EnsureTargetPool()
    {
        targetPool = FindObjectsByType<ScoreTarget>();
    }

    private void ResetTargetCollectionState()
    {
        foreach (ScoreTarget target in targetPool)
        {
            if (target != null)
            {
                target.ResetCollected();
            }
        }
    }

    private void RefreshAfterInventoryChanged()
    {
        if (!isBoardActive)
        {
            return;
        }

        foreach (TargetEntry entry in activeEntries)
        {
            if (entry != null && entry.target != null)
            {
                UpdateTargetHighlight(entry.target);
            }
        }

        RefreshText();
    }

    private void UpdateTargetHighlight(ScoreTarget target)
    {
        TargetEntry entry = activeEntries.Find(item => item.target != null && item.target.ItemId == target.ItemId);

        if (entry == null || entry.target == null)
        {
            return;
        }

        bool stillNeeded = GetCurrentCount(entry.target) < entry.requiredCount;
        SetHighlightForItemId(entry.target.ItemId, stillNeeded);
    }

    private void SetHighlightForItemId(string itemId, bool highlighted)
    {
        if (targetPool == null || string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        foreach (ScoreTarget target in targetPool)
        {
            if (target == null || target.ItemId != itemId)
            {
                continue;
            }

            target.SetHighlighted(highlighted && target.CanCollect());
        }
    }

    private int GetCurrentCount(ScoreTarget target)
    {
        return inventory != null ? inventory.GetCount(target) : 0;
    }

    private void RefreshText()
    {
        if (scoreboardText == null)
        {
            return;
        }

        if (!isBoardActive || activeEntries.Count == 0)
        {
            scoreboardText.text = string.Empty;
            return;
        }

        textBuilder.Clear();

        foreach (TargetEntry entry in activeEntries)
        {
            if (entry == null || entry.target == null)
            {
                continue;
            }

            textBuilder
                .Append(entry.target.DisplayName)
                .Append(" (")
                .Append(GetCurrentCount(entry.target))
                .Append("/")
                .Append(entry.requiredCount)
                .AppendLine(")");
        }

        scoreboardText.text = textBuilder.ToString().TrimEnd();
    }

    private void RestoreAllTargetHighlights()
    {
        if (targetPool != null)
        {
            foreach (ScoreTarget target in targetPool)
            {
                if (target != null)
                {
                    target.SetHighlighted(false);
                }
            }
        }

        foreach (TargetEntry entry in activeEntries)
        {
            if (entry != null && entry.target != null)
            {
                entry.target.SetHighlighted(false);
            }
        }
    }

    private IEnumerator FlashIncompleteCoroutine()
    {
        int count = Mathf.Max(1, incompleteFlashCount);
        float interval = Mathf.Max(0.01f, incompleteFlashInterval);

        for (int i = 0; i < count; i++)
        {
            scoreboardText.color = incompleteFlashColor;
            yield return new WaitForSeconds(interval);

            scoreboardText.color = normalTextColor;
            yield return new WaitForSeconds(interval);
        }

        scoreboardText.color = normalTextColor;
        incompleteFlashCoroutine = null;
    }

    private void StopIncompleteFlash()
    {
        if (incompleteFlashCoroutine != null)
        {
            StopCoroutine(incompleteFlashCoroutine);
            incompleteFlashCoroutine = null;
        }

        if (scoreboardText != null)
        {
            scoreboardText.color = normalTextColor;
        }
    }

    private void CreateRuntimeScoreboard()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Scoreboard Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject panelObject = new GameObject("Scoreboard Panel");
        panelObject.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject textObject = new GameObject("Scoreboard Text");
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 12f);
        textRect.offsetMax = new Vector2(-16f, -12f);

        Text text = textObject.AddComponent<Text>();
        text.alignment = TextAnchor.LowerRight;
        text.font = JapaneseUIFont.Get(fontSize);
        text.fontSize = fontSize;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        scoreboardText = text;
    }

    private void OnValidate()
    {
        targetCount = Mathf.Max(1, targetCount);
        minRequiredItemCount = Mathf.Max(1, minRequiredItemCount);
        maxRequiredItemCount = Mathf.Max(minRequiredItemCount, maxRequiredItemCount);
        targetItemScoreMultiplier = Mathf.Max(0, targetItemScoreMultiplier);
        settlementBonusScore = Mathf.Max(0, settlementBonusScore);
        minItemGain = Mathf.Max(1, minItemGain);
        maxItemGain = Mathf.Max(minItemGain, maxItemGain);
        incompleteFlashCount = Mathf.Max(1, incompleteFlashCount);
        incompleteFlashInterval = Mathf.Max(0.01f, incompleteFlashInterval);
        size.x = Mathf.Max(120f, size.x);
        size.y = Mathf.Max(60f, size.y);
        fontSize = Mathf.Max(8, fontSize);
    }
}
