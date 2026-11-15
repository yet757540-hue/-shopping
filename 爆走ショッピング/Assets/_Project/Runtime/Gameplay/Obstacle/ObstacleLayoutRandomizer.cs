using System.Collections.Generic;
using UnityEngine;

/// <summary>レジに入るたび、事前配置した障害物レイアウトを切り替えます。</summary>
[DisallowMultipleComponent]
public class ObstacleLayoutRandomizer : MonoBehaviour
{
    [Header("レジ")]
    [SerializeField] private SettlementArea settlementArea;

    [Header("事前に作った配置パターン")]
    [SerializeField] private GameObject[] layouts;

    private int currentLayoutIndex = -1;

    private void OnEnable()
    {
        if (settlementArea != null)
        {
            settlementArea.PlayerEntered -= HandlePlayerEntered;
            settlementArea.PlayerEntered += HandlePlayerEntered;
        }
    }

    private void Start()
    {
        // ゲーム開始時にも配置を一つ選びます。
        ChooseNextLayout();
    }

    private void OnDisable()
    {
        if (settlementArea != null)
        {
            settlementArea.PlayerEntered -= HandlePlayerEntered;
        }
    }

    private void HandlePlayerEntered()
    {
        ChooseNextLayout();
    }

    private void ChooseNextLayout()
    {
        List<int> candidates = new List<int>();

        if (layouts == null)
        {
            return;
        }

        for (int i = 0; i < layouts.Length; i++)
        {
            if (layouts[i] == null || i == currentLayoutIndex)
            {
                continue;
            }

            candidates.Add(i);
        }

        // 有効なパターンが一つだけなら、それを使います。
        if (candidates.Count == 0)
        {
            if (currentLayoutIndex >= 0 && currentLayoutIndex < layouts.Length && layouts[currentLayoutIndex] != null)
            {
                return;
            }

            for (int i = 0; i < layouts.Length; i++)
            {
                if (layouts[i] != null)
                {
                    candidates.Add(i);
                }
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[ObstacleLayoutRandomizer] 有効な配置パターンがありません。", this);
            return;
        }

        int selectedIndex = candidates[Random.Range(0, candidates.Count)];

        for (int i = 0; i < layouts.Length; i++)
        {
            if (layouts[i] != null)
            {
                layouts[i].SetActive(i == selectedIndex);
            }
        }

        currentLayoutIndex = selectedIndex;
    }
}