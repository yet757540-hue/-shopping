using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>取得済みアイテムの明細・個数・総重量を一貫して保持します。</summary>
[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    // 取得した一個分の ID・表示名・重量・取得元を保持します。
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

    // 取得したアイテム一個ごとの明細を、読み取り専用の一覧として公開します。
    public IReadOnlyList<CarriedItem> CarriedItems => carriedItems;
    // 所持している全アイテムの合計重量を返します。
    public float TotalWeight => totalWeight;

    // 目標の明細・重量・個数を所持品へ追加し、変更を通知します。個数省略時は一個です。
    public bool TryAddItem(ScoreTarget target)
    {
        return TryAddItem(target, 1);
    }

    // 同一アイテムの個数は Dictionary、個別の重量明細は List に保存します。
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

        itemCounts.TryGetValue(itemId, out int currentCount);
        itemCounts[itemId] = currentCount + amount;
        InventoryChanged?.Invoke();
        return true;
    }

    // 目標またはアイテム ID に対応する所持数を返し、未登録・無効な指定ならゼロを返します。
    public int GetCount(ScoreTarget target)
    {
        if (target == null)
        {
            return 0;
        }

        return GetCount(target.ItemId);
    }

    // 目標またはアイテム ID に対応する所持数を返し、未登録・無効な指定ならゼロを返します。
    public int GetCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return itemCounts.TryGetValue(itemId, out int count) ? count : 0;
    }

    // 次のラウンド用に、表示の元になるすべての所持データを同時に初期化します。
    public void ClearInventory()
    {
        carriedItems.Clear();
        itemCounts.Clear();
        totalWeight = 0f;
        InventoryChanged?.Invoke();
    }
}
