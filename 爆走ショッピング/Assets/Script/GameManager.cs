using UnityEngine;

// シーン起動時に最低限必要な補助コンポーネントをそろえる初期化役です。
// 役割:
// - 操作ガイド、所持品 UI、所持品システム、重量影響設定が不足していれば追加します。
// 接続:
// - PlayerManager が見つかる場合、その同じ GameObject に PlayerInventory を付けます。
// - InventoryInfluenceSettings はこの GameObject に追加され、PlayerInventory と PlayerManager を自動探索します。
// 読むときの要点:
// - ここはゲームルールそのものではなく、シーン配置漏れを補うための起動時セットアップです。
public class GameManager : MonoBehaviour
{
    // シーンに不足している補助 UI と所持品関連コンポーネントを起動時に補います。
    private void Awake()
    {
        if (FindAnyObjectByType<ControlsGuideUI>() == null)
        {
            gameObject.AddComponent<ControlsGuideUI>();
        }

        EnsureInventorySystems();

        if (FindAnyObjectByType<InventoryStatusUI>() == null)
        {
            gameObject.AddComponent<InventoryStatusUI>();
        }
    }

    // PlayerInventory と InventoryInfluenceSettings が存在する状態を作ります。
    private void EnsureInventorySystems()
    {
        PlayerManager playerManager = FindAnyObjectByType<PlayerManager>();

        if (playerManager != null && playerManager.GetComponent<PlayerInventory>() == null)
        {
            playerManager.gameObject.AddComponent<PlayerInventory>();
        }

        if (FindAnyObjectByType<InventoryInfluenceSettings>() == null)
        {
            gameObject.AddComponent<InventoryInfluenceSettings>();
        }
    }
}
