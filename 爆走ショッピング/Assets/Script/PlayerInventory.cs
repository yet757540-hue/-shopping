using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// プレイヤーが取得した ScoreTarget 由来のアイテムを保持する所持品データです。
// 役割:
// - 取得アイテムを個別リスト CarriedItems と、itemId ごとの個数 itemCounts で管理します。
// - 総重量 TotalWeight を保持し、重量影響や UI 表示に使えるようにします。
// 接続:
// - ScoreboardManager が衝突時に TryAddItem を呼びます。
// - InventoryInfluenceSettings と InventoryStatusUI は InventoryChanged を購読します。
// 読むときの要点:
// - CarriedItem.source に元の ScoreTarget を残しているため、取得元情報が後から参照できます。
public class PlayerInventory : MonoBehaviour
{
    [Serializable]
    public class CarriedItem
    {
        public string itemId;
        public string displayName;
        public float weight;
        public ScoreTarget source;
    }

    private readonly List<CarriedItem> carriedItems = new List<CarriedItem>();
    private readonly Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    private float totalWeight = 0f;

    public event Action InventoryChanged;

    public IReadOnlyList<CarriedItem> CarriedItems => carriedItems;
    public float TotalWeight => totalWeight;

    // 1 個だけ取得する場合の簡易入口です。
    public bool TryAddItem(ScoreTarget target)
    {
        return TryAddItem(target, 1);
    }

    // ScoreTarget の情報を所持品として指定個数ぶん追加します。
    public bool TryAddItem(ScoreTarget target, int amount)
    {
        if (target == null)
        {
            return false;
        }

        amount = Mathf.Max(1, amount);
        string itemId = target.ItemId;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        // UI の一覧表示や重量計算に使うため、取得数ぶん CarriedItem を個別に追加します。
        for (int i = 0; i < amount; i++)
        {
            CarriedItem item = new CarriedItem
            {
                itemId = itemId,
                displayName = target.DisplayName,
                weight = target.ItemWeight,
                source = target
            };

            carriedItems.Add(item);
            totalWeight += item.weight;
        }

        if (!itemCounts.ContainsKey(itemId))
        {
            itemCounts[itemId] = 0;
        }

        itemCounts[itemId] += amount;
        // 所持品の変更を起点に、重量影響と表示 UI が更新されます。
        InventoryChanged?.Invoke();
        return true;
    }

    // ScoreTarget から itemId を取り出し、その所持数を返します。
    public int GetCount(ScoreTarget target)
    {
        if (target == null)
        {
            return 0;
        }

        return GetCount(target.ItemId);
    }

    // itemId ごとの現在所持数を返します。
    public int GetCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return itemCounts.TryGetValue(itemId, out int count) ? count : 0;
    }

    // 所持品、個数集計、総重量をすべて初期化します。
    public void ClearInventory()
    {
        carriedItems.Clear();
        itemCounts.Clear();
        totalWeight = 0f;
        InventoryChanged?.Invoke();
    }
}
