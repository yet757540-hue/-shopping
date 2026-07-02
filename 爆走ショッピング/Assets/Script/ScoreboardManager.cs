using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// ランダムに選ばれた目標アイテムの必要数を表示し、取得状況を管理するスコアボードです。
// 役割:
// - ScoreTarget から目標候補を作り、開始時に複数種類の必要数をランダム決定します。
// - プレイヤーの衝突からアイテム獲得数を計算し、PlayerInventory へ追加します。
// - 目標達成状況を UI と ScoreTarget のハイライトへ反映します。
// 接続:
// - PlayerCollisionReporter.collisionEntered から RegisterCollision を呼ぶ想定です。
// - TimerZone はゴール時に TryCompleteScoreboard を呼び、未達成ならタイマー停止を止めます。
// - ImpactSettings は衝突速度から獲得数を決めるために使います。
// - PlayerInventory.InventoryChanged を購読して表示とハイライトを更新します。
// 読むときの要点:
// - isBoardActive は「現在の買い物リストが有効か」を表します。完了判定は IsComplete に集約されています。
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

    [Header("Board Settings")]
    [SerializeField] private int targetCount = 3;
    [SerializeField] private int minRequiredItemCount = 3;
    [SerializeField] private int maxRequiredItemCount = 8;
    [SerializeField] private bool resetInventoryOnEnd = true;
    [SerializeField] private bool clearWhenTimerStops = false;

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

    private readonly List<TargetEntry> activeEntries = new List<TargetEntry>();
    private readonly StringBuilder textBuilder = new StringBuilder();
    private Color normalTextColor = Color.white;
    private Coroutine incompleteFlashCoroutine;
    private bool isBoardActive = false;
    private ScoreTarget[] targetPool = Array.Empty<ScoreTarget>();

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

    // 必要な参照と UI をそろえ、初期状態ではスコアボードを空にします。
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

        ClearScoreboard();
    }

    // 有効化時に所持品変更イベントを購読します。
    private void OnEnable()
    {
        SubscribeInventory();
    }

    // 無効化時に所持品変更イベントの購読を解除します。
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        }
    }

    // 新しい買い物リストを開始し、目標アイテムと必要数をランダムに決めます。
    public void StartScoreboard()
    {
        if (isBoardActive)
        {
            return;
        }

        // 新しいスコアボード開始時は、前回のハイライトと取得済み状態を戻してから候補を選び直します。
        ResolveReferences();
        EnsureTargetPool();
        RestoreAllTargetHighlights();
        ResetTargetCollectionState();

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
                requiredCount = GetRandomRequiredCount()
            });

            SetHighlightForItemId(selectedTarget.ItemId, true);
            candidates.RemoveAt(index);
        }

        RefreshText();
    }

    // ゴール時に呼ばれ、未達成なら警告を出して false を返します。
    public bool TryCompleteScoreboard()
    {
        if (IsActive && !IsComplete)
        {
            FlashIncompleteWarning();
            return false;
        }

        CompleteScoreboard();
        return true;
    }

    // スコアボード状態とハイライトを消し、表示も空にします。
    public void ClearScoreboard()
    {
        RestoreAllTargetHighlights();
        StopIncompleteFlash();

        isBoardActive = false;
        activeEntries.Clear();
        RefreshText();
    }

    // プレイヤー衝突から ScoreTarget を見つけ、衝突強度に応じた個数を所持品へ追加します。
    public void RegisterCollision(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        // 衝突相手の親階層まで見て ScoreTarget を探します。
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

        // スコアボードが開始していない場合でも、所持品としては取得できます。
        if (!isBoardActive)
        {
            return;
        }

        UpdateTargetHighlight(target);
        RefreshText();
    }

    // 未達成のままゴールしようとしたとき、スコアボード表示を点滅させます。
    public void FlashIncompleteWarning()
    {
        if (scoreboardText == null)
        {
            return;
        }

        StopIncompleteFlash();
        incompleteFlashCoroutine = StartCoroutine(FlashIncompleteCoroutine());
    }

    // スコアボードを完了状態にし、必要なら所持品をリセットします。
    private void CompleteScoreboard()
    {
        RestoreAllTargetHighlights();
        isBoardActive = false;

        if (resetInventoryOnEnd && inventory != null)
        {
            inventory.ClearInventory();
        }

        if (clearWhenTimerStops)
        {
            ClearScoreboard();
        }
    }

    // ImpactSettings、PlayerInventory、UI 更新に必要な参照を探します。
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

        SubscribeInventory();
    }

    // 所持品変更時にスコアボード表示を更新できるように購読します。
    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        inventory.InventoryChanged += RefreshAfterInventoryChanged;
    }

    // targetPool から itemId が重複しない候補リストを作ります。
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

    // Collision の Collider または Rigidbody から親階層の ScoreTarget を探します。
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

    // Inspector で設定された最小最大範囲から必要数をランダムに選びます。
    private int GetRandomRequiredCount()
    {
        int min = Mathf.Min(minRequiredItemCount, maxRequiredItemCount);
        int max = Mathf.Max(minRequiredItemCount, maxRequiredItemCount);
        lastRequiredCount = UnityEngine.Random.Range(min, max + 1);
        return lastRequiredCount;
    }

    // 衝突速度から獲得数を計算します。弱すぎる衝突なら false を返します。
    private bool TryCalculateItemGain(float impactSpeed, out int itemGain)
    {
        lastRawImpactSpeed = impactSpeed;
        lastImpactRate = impactSettings != null ? impactSettings.GetImpactRateFromRawSpeed(impactSpeed) : 0f;
        lastImpactSpeed = impactSettings != null ? impactSettings.LastAdjustedImpactSpeed : impactSpeed;

        // 弱すぎる衝突ではアイテムを獲得しません。
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

    // シーン内の ScoreTarget を現在の候補プールとして取り直します。
    private void EnsureTargetPool()
    {
        targetPool = FindObjectsByType<ScoreTarget>();
    }

    // 新しいリスト開始前に、各 ScoreTarget の取得済み状態を戻します。
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

    // 所持品変更後、目標達成状況に合わせてハイライトと表示を更新します。
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

    // 指定 target と同じ itemId の目標がまだ必要かを見て、ハイライト状態を更新します。
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

    // 同じ itemId を持つ全 ScoreTarget のハイライトをまとめて切り替えます。
    private void SetHighlightForItemId(string itemId, bool highlighted)
    {
        if (targetPool == null || string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        // 同じ itemId のオブジェクトが複数ある場合、全て同じ目標アイテムとしてハイライトします。
        foreach (ScoreTarget target in targetPool)
        {
            if (target == null || target.ItemId != itemId)
            {
                continue;
            }

            target.SetHighlighted(highlighted && target.CanCollect());
        }
    }

    // 指定 target の現在所持数を PlayerInventory から取得します。
    private int GetCurrentCount(ScoreTarget target)
    {
        return inventory != null ? inventory.GetCount(target) : 0;
    }

    // activeEntries の現在数と必要数をスコアボード Text へ反映します。
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

    // 候補プールと現在リストの両方から、すべてのハイライトを解除します。
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

    // 未達成警告としてスコアボード文字色を一定回数点滅させます。
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

    // 点滅コルーチンを止め、文字色を通常色へ戻します。
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

    // Canvas、背景、Text を実行時に作り、スコアボード表示先を確保します。
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

    // スコアボード設定値を安全な範囲へ補正します。
    private void OnValidate()
    {
        targetCount = Mathf.Max(1, targetCount);
        minRequiredItemCount = Mathf.Max(1, minRequiredItemCount);
        maxRequiredItemCount = Mathf.Max(minRequiredItemCount, maxRequiredItemCount);
        minItemGain = Mathf.Max(1, minItemGain);
        maxItemGain = Mathf.Max(minItemGain, maxItemGain);
        incompleteFlashCount = Mathf.Max(1, incompleteFlashCount);
        incompleteFlashInterval = Mathf.Max(0.01f, incompleteFlashInterval);
        size.x = Mathf.Max(120f, size.x);
        size.y = Mathf.Max(60f, size.y);
        fontSize = Mathf.Max(8, fontSize);
    }
}
