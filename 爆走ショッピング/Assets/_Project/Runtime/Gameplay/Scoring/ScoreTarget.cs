
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテム情報と選択時の画像ターゲット表示を管理します。
/// </summary>
[DisallowMultipleComponent]
public class ScoreTarget : MonoBehaviour
{
    // 棚の商品情報
    [Serializable]
    public class ShelfProduct
    {
        public string genre;
        public string itemId;
        public string displayName;
        public float itemWeight = 1f;
    }

    [Header("アイテム情報")]
    [SerializeField] private string displayName;
    [SerializeField] private string itemId;
    [SerializeField] private float itemWeight = 1f;
    [SerializeField] private bool collectOnce = true;
    [SerializeField] private bool hideWhenCollected = false;

    [Header("棚にある商品")]
    [SerializeField]
    private List<ShelfProduct> shelfProducts =
        new List<ShelfProduct>();

    public IReadOnlyList<ShelfProduct> ShelfProducts => shelfProducts;

    [Header("ターゲット画像")]
    [SerializeField] private SpriteRenderer iconPrefab;
    [SerializeField] private float iconHeightOffset = 0.5f;

    [Header("表示対象")]
    [SerializeField] private Renderer[] targetRenderers;

    private SpriteRenderer iconInstance;
    private bool isHighlighted = false;
    private bool isCollected = false;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(displayName)
        ? gameObject.name : displayName;

    public string ItemId =>
        string.IsNullOrWhiteSpace(itemId)
        ? DisplayName : itemId;

    public float ItemWeight => Mathf.Max(0f, itemWeight);

    public bool IsCollected => isCollected;

    private void Awake()
    {
        CacheRenderers();
    }

    private void LateUpdate()
    {
        // 画像をカメラの方向に向ける
        if (iconInstance != null && Camera.main != null)
        {
            iconInstance.transform.rotation =
                Camera.main.transform.rotation;
        }
    }

    // 取得可能か確認
    public bool CanCollect()
    {
        return !collectOnce || !isCollected;
    }

    // アイテム取得
    public void MarkCollected()
    {
        isCollected = true;

        SetHighlighted(false);

        if (hideWhenCollected)
        {
            SetRenderersVisible(false);
        }
    }

    // 取得状態をリセット
    public void ResetCollected()
    {
        isCollected = false;
        SetRenderersVisible(true);
    }

    // ターゲット画像の表示・非表示
    public void SetHighlighted(bool highlighted)
    {
        if (highlighted)
        {
            // すでに画像が存在する場合は何もしない
            if (iconInstance != null)
            {
                return;
            }

            // Prefabが設定されていない場合
            if (iconPrefab == null)
            {
                Debug.LogWarning(
                    "[ScoreTarget] Icon Prefabが設定されていません。",
                    this
                );
                return;
            }

            // 画像を生成
            iconInstance = Instantiate(iconPrefab, transform);

            // アイテムの真上に配置
            iconInstance.transform.localPosition =
                GetIconLocalPosition()
                + Vector3.up * iconHeightOffset;

            isHighlighted = true;

            Debug.Log("ターゲット表示: " + DisplayName, this);
        }
        else
        {
            // 画像を削除
            if (iconInstance != null)
            {
                Destroy(iconInstance.gameObject);
                iconInstance = null;
            }

            isHighlighted = false;
        }
    }

    // Renderer取得
    private void CacheRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    // アイテムの上端の位置を取得
    private Vector3 GetIconLocalPosition()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            return Vector3.zero;
        }

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(targetRenderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return Vector3.zero;
        }

        Vector3 worldTop =
            combinedBounds.center
            + Vector3.up * combinedBounds.extents.y;

        return transform.InverseTransformPoint(worldTop);
    }

    // アイテム本体の表示・非表示
    private void SetRenderersVisible(bool visible)
    {
        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = visible;
            }
        }
    }

    // 無効化時に画像を削除
    private void OnDisable()
    {
        SetHighlighted(false);
    }

    // 破棄時に画像を削除
    private void OnDestroy()
    {
        if (iconInstance != null)
        {
            Destroy(iconInstance.gameObject);
            iconInstance = null;
        }
    }

    private void OnValidate()
    {
        itemWeight = Mathf.Max(0f, itemWeight);
        iconHeightOffset = Mathf.Max(0f, iconHeightOffset);
    }
}
