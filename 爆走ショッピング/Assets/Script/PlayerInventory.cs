using System;
using System.Collections.Generic;
using UnityEngine;

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

        if (!itemCounts.ContainsKey(itemId))
        {
            itemCounts[itemId] = 0;
        }

        itemCounts[itemId] += amount;
        InventoryChanged?.Invoke();
        return true;
    }

    public int GetCount(ScoreTarget target)
    {
        if (target == null)
        {
            return 0;
        }

        return GetCount(target.ItemId);
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
