using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>取得済みアイテムの明細・個数・総重量を一貫して保持します。</summary>
[DisallowMultipleComponent]
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

    // 既存コードとの互換性を保ち、棚の ScoreTarget 情報で追加します。
    public bool TryAddItem(ScoreTarget target)
    {
        return TryAddItem(target, 1);
    }

    public bool TryAddItem(ScoreTarget target, int amount)
    {
        if (target == null)
        {
            return false;
        }

        return TryAddItem(
            target,
            target.ItemId,
            target.DisplayName,
            target.ItemWeight,
            amount);
    }

    // タスクで選ばれた商品の ID・表示名で、衝突した棚からの取得を記録します。
    public bool TryAddItem(
        ScoreTarget source,
        string itemId,
        string displayName,
        float weight,
        int amount)
    {
        if (source == null || string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        amount = Mathf.Max(1, amount);
        weight = Mathf.Max(0f, weight);

        for (int i = 0; i < amount; i++)
        {
            carriedItems.Add(new CarriedItem
            {
                itemId = itemId,
                displayName = string.IsNullOrWhiteSpace(displayName) ? itemId : displayName,
                weight = weight,
                source = source
            });

            totalWeight += weight;
        }

        itemCounts.TryGetValue(itemId, out int currentCount);
        itemCounts[itemId] = currentCount + amount;
        InventoryChanged?.Invoke();
        return true;
    }

    public int GetCount(ScoreTarget target)
    {
        return target == null ? 0 : GetCount(target.ItemId);
    }

    public int GetCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return itemCounts.TryGetValue(itemId, out int count) ? count : 0;
    }

    public void ClearInventory()
    {
        carriedItems.Clear();
        itemCounts.Clear();
        totalWeight = 0f;
        InventoryChanged?.Invoke();
    }
}
