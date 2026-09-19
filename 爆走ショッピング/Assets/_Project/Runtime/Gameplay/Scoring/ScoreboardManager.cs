using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>現在の目標、獲得数、結算得点を管理し、HUD 用の文字列を提供します。</summary>
public class ScoreboardManager : MonoBehaviour
{
    // 抽選された目標と、そのラウンドで必要な個数を保持します。
    [Serializable]
    private class TargetEntry
    {
        public ScoreTarget target;
        public int requiredCount;
    }

    // 他のゲーム機能への参照は、GameSessionRoot から初期化時に受け取ります。
    [Header("参照")]
    [SerializeField] private ImpactSettings impactSettings;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Text scoreboardText;
    [SerializeField] private SettlementArea settlementArea;
    [SerializeField] private ScoreboardView scoreboardView;

    // 目標の種類数・必要個数の範囲と、終了時の所持品・表示の扱いです。
    [Header("目標ボード設定")]
    [SerializeField] private int targetCount = 3;
    [SerializeField] private int minRequiredItemCount = 3;
    [SerializeField] private int maxRequiredItemCount = 8;
    [SerializeField] private bool resetInventoryOnEnd = true;
    [SerializeField] private bool clearWhenTimerStops = false;

    // 結算一回の固定点と、必要数を超えたアイテム一個あたりの得点です。
    [Header("得点")]
    [SerializeField] private int targetItemScoreMultiplier = 10;
    [SerializeField] private int settlementBonusScore = 50;

    // 衝突強度に応じて補間する、獲得個数の最小値と最大値です。
    [Header("衝突時のアイテム獲得")]
    [SerializeField] private int minItemGain = 1;
    [SerializeField] private int maxItemGain = 5;

    // 以前の UI 配置設定です。現在の表示位置と文字設定には使用していません。
    [Header("実行時 UI")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(-32f, 32f);
    [SerializeField] private Vector2 size = new Vector2(360f, 150f);
    [SerializeField] private int fontSize = 28;

    // 結算条件が足りないときの点滅色・回数・間隔です。
    [Header("未達成時の警告")]
    [SerializeField] private Color incompleteFlashColor = Color.red;
    [SerializeField] private int incompleteFlashCount = 3;
    [SerializeField] private float incompleteFlashInterval = 0.12f;

    // 直前の計算値や集計値を、Inspector で確認するためのフィールドです。
    [Header("実行時デバッグ")]
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
    private ScoreTarget[] targetPool = Array.Empty<ScoreTarget>();
    private SettlementArea subscribedSettlementArea;

    public event Action StateChanged;

    // 目標が有効で一件以上あり、全ての必要数を満たしているかを返します。
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

    // 目標ボードが有効で、目標が一件以上あるかを返します。
    public bool IsActive => isBoardActive && activeEntries.Count > 0;
    // 成功した結算の累計回数を返します。
    public int CompletedSettlementCount => completedSettlementCount;
    // 結算時に必要数を超えていたアイテムの累計個数を返します。
    public int SettledExcessTargetItemCount => settledExcessTargetItemCount;
    // 超過アイテム一個あたりの得点を返します。
    public int TargetItemScoreMultiplier => targetItemScoreMultiplier;
    // 結算成功一回あたりの固定得点を返します。
    public int SettlementBonusScorePerSettlement => settlementBonusScore;
    // 結算回数に固定得点を掛けた累計点を返します。
    public int SettlementBonusScore => completedSettlementCount * settlementBonusScore;
    // 超過アイテムの累計個数に一個あたりの得点を掛けて返します。
    public int TargetItemScore => settledExcessTargetItemCount * targetItemScoreMultiplier;
    // 結算回数による得点と超過アイテムによる得点の合計を返します。
    public int TotalScore => SettlementBonusScore + TargetItemScore;

    // 通常の文字色を記録し、得点と目標表示を初期化します。
    private void Awake()
    {
        if (scoreboardText != null)
        {
            normalTextColor = scoreboardText.color;
        }

        ResetScore();
        ClearScoreboard();
    }

    // 所持品の変化と結算エリアの入退場イベントを購読します。
    private void OnEnable()
    {
        SubscribeInventory();
        SubscribeSettlementArea();
    }

    // 所持品と結算エリアのイベント購読を解除します。
    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        }

        UnsubscribeSettlementArea();
    }

    // 新しい目標を作る前に、前回のハイライトと取得状態を必ず初期化します。
    public void StartScoreboard()
    {
        if (isBoardActive)
        {
            return;
        }

        // エリア内で次の目標が始まった場合は、一度退出するまで再結算を許可しません。
        canSettleCurrentTargets = settlementArea == null || !settlementArea.HasPlayerInside;
        StopIncompleteFlash();
        RestoreAllTargetHighlights();
        ResetTargetCollectionState();

        activeEntries.Clear();
        isBoardActive = true;

        if (targetPool == null || targetPool.Length == 0)
        {
            RefreshText();
            return;
        }

        // 同じ種類を重複抽選しない候補一覧を作り、選んだ候補は一覧から取り除きます。
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

    // 未達成の目標があれば警告し、それ以外は設定に応じて所持品を消して目標を終了します。
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

    // 目標達成と再入場条件を確認し、得点を加算して次の目標へ進みます。
    public bool TrySettleCompletedTargets()
    {
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

        // 所持品を消す前に超過分の得点を集計します。
        RecordSettlementScore();
        CompleteScoreboard(true);

        // 報酬選択を挟まず、達成後はすぐ次の目標を作成します。
        StartScoreboard();

        return true;
    }

    // 達成回数と超過取得個数をゼロに戻します。
    public void ResetScore()
    {
        completedSettlementCount = 0;
        settledExcessTargetItemCount = 0;
    }

    // ハイライトと警告を解除し、目標一覧を空にして表示側へ通知します。
    public void ClearScoreboard()
    {
        RestoreAllTargetHighlights();
        StopIncompleteFlash();

        isBoardActive = false;
        activeEntries.Clear();
        RefreshText();
    }

    // 衝突相手が目標で、かつ十分な速度ならアイテムを入手します。
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

    // 表示先があれば、前の点滅を止めて未達成警告の点滅を開始します。
    public void FlashIncompleteWarning()
    {
        if (scoreboardText == null && scoreboardView == null)
        {
            return;
        }

        StopIncompleteFlash();
        incompleteFlashCoroutine = StartCoroutine(FlashIncompleteCoroutine());
    }

    // 現在の超過取得個数を累計へ加え、達成回数を増やします。
    private void RecordSettlementScore()
    {
        settledExcessTargetItemCount += CalculateSettledExcessTargetItemCount();
        completedSettlementCount++;
    }

    // 目標の必要数を超えた所持個数だけを、アイテム ID ごとに重複なく合計します。
    private int CalculateSettledExcessTargetItemCount()
    {
        if (inventory == null || activeEntries.Count == 0)
        {
            return 0;
        }

        int total = 0;
        // 同じ ID の得点を二重計上しないための記録です。
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

    // 目標を終了状態にし、引数と設定に応じて所持品や目標表示を消します。
    private void CompleteScoreboard(bool clearInventory)
    {
        StopIncompleteFlash();
        RestoreAllTargetHighlights();
        isBoardActive = false;

        // ボードを先に無効化しているため、所持品変更通知で目標を再更新しません。
        if (clearInventory && inventory != null)
        {
            inventory.ClearInventory();
        }

        if (clearWhenTimerStops)
        {
            ClearScoreboard();
        }
    }

    /// <summary>ゲーム開始前に GameSessionRoot から参照を受け取り、表示と通知を初期化します。</summary>
    // GameSessionRoot から依存先を受け取り、目標候補・表示・イベント購読を設定します。
    public void Initialize(
        ImpactSettings configuredImpactSettings,
        PlayerInventory configuredInventory,
        SettlementArea configuredSettlementArea,
        ScoreTarget[] configuredTargets,
        ScoreboardView configuredView)
    {
        impactSettings = configuredImpactSettings;
        inventory = configuredInventory;
        settlementArea = configuredSettlementArea;
        targetPool = configuredTargets ?? Array.Empty<ScoreTarget>();
        scoreboardView = configuredView;

        if (scoreboardView != null)
        {
            scoreboardText = scoreboardView.Text;
            if (scoreboardText != null)
            {
                normalTextColor = scoreboardText.color;
            }

            scoreboardView.Initialize(this);
        }

        SubscribeInventory();
        SubscribeSettlementArea();
        RefreshText();
    }

    // 結算エリアへの入場時に、現在の目標を結算できるか確認します。
    private void HandleSettlementAreaEntered()
    {
        TrySettleCompletedTargets();
    }

    // 一度エリアから出たことで、次回入場時の結算を許可します。
    private void HandleSettlementAreaExited()
    {
        canSettleCurrentTargets = true;
    }

    // 以前のエリアの購読を解除し、現在のエリアへ重複なく登録します。
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

    // 実際に購読したエリアから入退場通知を解除し、購読先をクリアします。
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

    // 所持品の変化を重複なく購読し、目標表示の更新につなげます。
    private void SubscribeInventory()
    {
        if (inventory == null)
        {
            return;
        }

        inventory.InventoryChanged -= RefreshAfterInventoryChanged;
        inventory.InventoryChanged += RefreshAfterInventoryChanged;
    }

    // 同じアイテム ID を一つにまとめ、今回の抽選候補を作ります。
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

    // 衝突した Collider の親を優先し、見つからなければ Rigidbody の親から目標を探します。
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

    // 必要数は Inspector の範囲から抽選します。
    private int GetRandomRequiredCount()
    {
        int min = Mathf.Min(minRequiredItemCount, maxRequiredItemCount);
        int max = Mathf.Max(minRequiredItemCount, maxRequiredItemCount);
        lastRequiredCount = Mathf.Max(1, UnityEngine.Random.Range(min, max + 1));
        return lastRequiredCount;
    }

    // 補正済みの衝突強度から獲得数を計算し、しきい値未満なら取得を拒否します。
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

    // 全ての目標候補を未取得・表示状態へ戻します。
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

    // 目標が有効な間だけ、必要個数に応じたハイライトと文字を更新します。
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

    // 同じアイテム ID の目標を探し、必要数がまだ足りないかを表示へ反映します。
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

    // 指定 ID の候補全てに、取得可能状態を考慮したハイライトを適用します。
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

    // 目標と同じアイテム ID の所持数を返します。所持品参照がなければゼロです。
    private int GetCurrentCount(ScoreTarget target)
    {
        return inventory != null ? inventory.GetCount(target) : 0;
    }

    // 目標文字を更新し、購読している表示コンポーネントへ変更を通知します。
    private void RefreshText()
    {
        if (scoreboardText != null)
        {
            scoreboardText.text = BuildDisplayText();
        }

        StateChanged?.Invoke();
    }

    // 表示側が使用する、現在の目標と所持数の文字列を返します。
    public string GetDisplayText()
    {
        return BuildDisplayText();
    }

    // 直接の Text 更新と ScoreboardView が同じ目標文を使うため、文字列の組み立てを一か所に置きます。
    private string BuildDisplayText()
    {
        if (!isBoardActive || activeEntries.Count == 0)
        {
            return string.Empty;
        }

        textBuilder.Clear();
        foreach (TargetEntry entry in activeEntries)
        {
            if (entry == null || entry.target == null)
            {
                continue;
            }

            textBuilder.Append(entry.target.DisplayName)
                .Append(" (")
                .Append(GetCurrentCount(entry.target))
                .Append("/")
                .Append(entry.requiredCount)
                .AppendLine(")");
        }

        return textBuilder.ToString().TrimEnd();
    }

    // 候補一覧と現在の目標に残っているハイライトを解除します。
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

    // 警告色と通常色を交互に表示し、指定回数後に通常色へ戻します。
    private IEnumerator FlashIncompleteCoroutine()
    {
        int count = Mathf.Max(1, incompleteFlashCount);
        float interval = Mathf.Max(0.01f, incompleteFlashInterval);

        for (int i = 0; i < count; i++)
        {
            SetScoreboardColor(incompleteFlashColor, true);
            yield return new WaitForSeconds(interval);

            SetScoreboardColor(normalTextColor, false);
            yield return new WaitForSeconds(interval);
        }

        SetScoreboardColor(normalTextColor, false);
        incompleteFlashCoroutine = null;
    }

    // 進行中の点滅を停止し、文字色とビューの警告状態を元に戻します。
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

        if (scoreboardView != null)
        {
            scoreboardView.SetWarning(false);
        }
    }

    // 直接の Text と専用ビューの両方へ、警告表示の状態を反映します。
    private void SetScoreboardColor(Color color, bool warning)
    {
        if (scoreboardText != null)
        {
            scoreboardText.color = color;
        }

        if (scoreboardView != null)
        {
            scoreboardView.SetWarning(warning);
        }
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
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
