using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
// スコアボードで要求される「取得対象アイテム」を表すコンポーネントです。
// 役割:
// - 表示名、アイテム ID、重量、取得済み状態を持ちます。
// - スコアボードからハイライト指示を受け、通常マテリアル色と壁越し表示用オーバーレイを切り替えます。
// 接続:
// - ScoreboardManager はシーン内の ScoreTarget を集めて目標候補にします。
// - PlayerInventory は TryAddItem 時に ScoreTarget の ItemId、DisplayName、ItemWeight を読みます。
// 読むときの要点:
// - itemId が空なら DisplayName、displayName が空なら GameObject 名が使われます。
// - flashVisibleThroughWalls が true の場合、Resources または Shader から専用マテリアルを作り、対象と同じメッシュを重ねます。
public class ScoreTarget : MonoBehaviour
{
    private const string VisibleOverlayResourceName = "ScoreTargetVisibleOverlay";

    private class MaterialColorState
    {
        public Material material;
        public string colorProperty;
        public Color originalColor;
    }

    private class VisibleOverlayState
    {
        public Renderer renderer;
        public GameObject gameObject;
    }

    [Header("Item Data")]
    [SerializeField] private string displayName;
    [SerializeField] private string itemId;
    [SerializeField] private float itemWeight = 1f;
    [SerializeField] private bool collectOnce = true;
    [SerializeField] private bool hideWhenCollected = false;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool flashVisibleThroughWalls = true;
    [SerializeField] private float flashFrequency = 1f;
    [SerializeField] private float visibleOverlayAlpha = 0.7f;

    private readonly List<MaterialColorState> originalColors = new List<MaterialColorState>();
    private readonly List<VisibleOverlayState> visibleOverlays = new List<VisibleOverlayState>();
    private Material visibleOverlayMaterial;
    private bool isHighlighted = false;
    private bool isCollected = false;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            return gameObject.name;
        }
    }

    public string ItemId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                return itemId;
            }

            return DisplayName;
        }
    }

    public float ItemWeight => Mathf.Max(0f, itemWeight);
    public bool IsCollected => isCollected;

    // 表示対象 Renderer、元色、壁越し表示用オーバーレイを準備します。
    private void Awake()
    {
        CacheRenderers();
        CacheOriginalColors();
        CreateVisibleOverlays();
    }

    // ハイライト中だけ、壁越し表示オーバーレイの透明度を点滅させます。
    private void Update()
    {
        if (!isHighlighted || !flashVisibleThroughWalls || visibleOverlays.Count == 0)
        {
            return;
        }

        float fade = Mathf.PingPong(Time.time * flashFrequency, 1f);
        fade = fade * fade * (3f - 2f * fade);
        SetOverlayAlpha(visibleOverlayAlpha * fade);
    }

    // collectOnce 設定と取得済み状態から、今取得できるかを返します。
    public bool CanCollect()
    {
        return !collectOnce || !isCollected;
    }

    // 取得済みにし、必要なら Renderer を非表示にします。
    public void MarkCollected()
    {
        isCollected = true;
        SetHighlighted(false);

        if (hideWhenCollected)
        {
            SetRenderersVisible(false);
        }
    }

    // 取得済み状態を戻し、Renderer を再表示します。
    public void ResetCollected()
    {
        isCollected = false;
        SetRenderersVisible(true);
    }

    // 通常マテリアル色と壁越しオーバーレイのハイライト状態を切り替えます。
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

        // 元の色を Awake で保存しておき、ハイライト解除時に確実に戻します。
        foreach (MaterialColorState state in originalColors)
        {
            if (state.material == null || string.IsNullOrEmpty(state.colorProperty))
            {
                continue;
            }

            state.material.SetColor(
                state.colorProperty,
                highlighted ? highlightColor : state.originalColor
            );
        }

        if (highlighted)
        {
            UpdateVisibleOverlayMaterial();
            ApplyOverlayVisible(flashVisibleThroughWalls);
            return;
        }

        ApplyOverlayVisible(false);
    }

    // targetRenderers が未設定なら子階層の Renderer を自動取得します。
    private void CacheRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    // ハイライト解除時に戻すため、各 Material の元色を保存します。
    private void CacheOriginalColors()
    {
        originalColors.Clear();

        if (targetRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            foreach (Material material in targetRenderer.materials)
            {
                string colorProperty = GetColorProperty(material);

                if (string.IsNullOrEmpty(colorProperty))
                {
                    continue;
                }

                originalColors.Add(new MaterialColorState
                {
                    material = material,
                    colorProperty = colorProperty,
                    originalColor = material.GetColor(colorProperty)
                });
            }
        }
    }

    // URP と Built-in の両方に対応するため、使える色プロパティ名を探します。
    private string GetColorProperty(Material material)
    {
        if (material == null)
        {
            return null;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return "_BaseColor";
        }

        if (material.HasProperty("_Color"))
        {
            return "_Color";
        }

        return null;
    }

    // Renderer ごとに壁越し表示用のオーバーレイ Renderer を作ります。
    private void CreateVisibleOverlays()
    {
        DestroyVisibleOverlays();

        if (!flashVisibleThroughWalls || targetRenderers == null)
        {
            return;
        }

        Material overlayMaterial = GetVisibleOverlayMaterial();

        if (overlayMaterial == null)
        {
            return;
        }

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            CreateVisibleOverlay(targetRenderer, overlayMaterial);
        }

        ApplyOverlayVisible(false);
    }

    // MeshRenderer または SkinnedMeshRenderer に合わせてオーバーレイを作ります。
    private void CreateVisibleOverlay(Renderer targetRenderer, Material overlayMaterial)
    {
        MeshRenderer meshRenderer = targetRenderer as MeshRenderer;

        if (meshRenderer != null)
        {
            // 通常 MeshRenderer は同じ MeshFilter を持つ透明オーバーレイを子に作ります。
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            GameObject overlayObject = CreateOverlayObject(targetRenderer.transform);
            MeshFilter overlayMeshFilter = overlayObject.AddComponent<MeshFilter>();
            overlayMeshFilter.sharedMesh = meshFilter.sharedMesh;

            MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            overlayRenderer.sharedMaterial = overlayMaterial;
            overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            AddVisibleOverlay(overlayObject, overlayRenderer);
            return;
        }

        SkinnedMeshRenderer skinnedRenderer = targetRenderer as SkinnedMeshRenderer;

        if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
        {
            return;
        }

        // SkinnedMeshRenderer は骨情報もコピーし、アニメーションに追従するオーバーレイにします。
        GameObject skinnedOverlayObject = CreateOverlayObject(targetRenderer.transform);
        SkinnedMeshRenderer skinnedOverlayRenderer = skinnedOverlayObject.AddComponent<SkinnedMeshRenderer>();
        skinnedOverlayRenderer.sharedMesh = skinnedRenderer.sharedMesh;
        skinnedOverlayRenderer.bones = skinnedRenderer.bones;
        skinnedOverlayRenderer.rootBone = skinnedRenderer.rootBone;
        skinnedOverlayRenderer.sharedMaterial = overlayMaterial;
        skinnedOverlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        skinnedOverlayRenderer.receiveShadows = false;
        AddVisibleOverlay(skinnedOverlayObject, skinnedOverlayRenderer);
    }

    // 対象 Renderer の子として、同じ位置・回転・スケールのオーバーレイ用 GameObject を作ります。
    private GameObject CreateOverlayObject(Transform parent)
    {
        GameObject overlayObject = new GameObject("ScoreTarget Visible Overlay");
        overlayObject.transform.SetParent(parent, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;
        overlayObject.hideFlags = HideFlags.DontSave;
        return overlayObject;
    }

    // 作成したオーバーレイを後で表示切替・破棄できるように記録します。
    private void AddVisibleOverlay(GameObject overlayObject, Renderer overlayRenderer)
    {
        visibleOverlays.Add(new VisibleOverlayState
        {
            gameObject = overlayObject,
            renderer = overlayRenderer
        });
    }

    // Resources または Shader から壁越し表示用 Material を取得・生成します。
    private Material GetVisibleOverlayMaterial()
    {
        if (visibleOverlayMaterial != null)
        {
            return visibleOverlayMaterial;
        }

        Material resourceMaterial = Resources.Load<Material>(VisibleOverlayResourceName);

        if (resourceMaterial != null)
        {
            visibleOverlayMaterial = new Material(resourceMaterial);
            visibleOverlayMaterial.hideFlags = HideFlags.DontSave;
            UpdateVisibleOverlayMaterial();
            return visibleOverlayMaterial;
        }

        Shader shader = Shader.Find("Custom/ScoreTargetVisibleOverlay");

        if (shader == null)
        {
            Debug.LogWarning("[ScoreTarget] Visible overlay shader/material was not found.", this);
            return null;
        }

        visibleOverlayMaterial = new Material(shader);
        visibleOverlayMaterial.hideFlags = HideFlags.DontSave;
        UpdateVisibleOverlayMaterial();
        return visibleOverlayMaterial;
    }

    // ハイライト色と設定透明度をオーバーレイ Material へ反映します。
    private void UpdateVisibleOverlayMaterial()
    {
        Material overlayMaterial = GetVisibleOverlayMaterial();

        if (overlayMaterial == null)
        {
            return;
        }

        Color overlayColor = highlightColor;
        overlayColor.a = Mathf.Clamp01(visibleOverlayAlpha);
        overlayMaterial.SetColor("_BaseColor", overlayColor);
    }

    // 点滅用にオーバーレイ Material の透明度だけを更新します。
    private void SetOverlayAlpha(float alpha)
    {
        Material overlayMaterial = GetVisibleOverlayMaterial();

        if (overlayMaterial == null)
        {
            return;
        }

        Color overlayColor = highlightColor;
        overlayColor.a = Mathf.Clamp01(alpha);
        overlayMaterial.SetColor("_BaseColor", overlayColor);
    }

    // 作成済みオーバーレイ Renderer の enabled をまとめて切り替えます。
    private void ApplyOverlayVisible(bool visible)
    {
        foreach (VisibleOverlayState overlay in visibleOverlays)
        {
            if (overlay != null && overlay.renderer != null)
            {
                overlay.renderer.enabled = visible;
            }
        }
    }

    // 取得済み非表示などで、本体 Renderer の表示状態をまとめて切り替えます。
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

    // 生成済みオーバーレイ GameObject を破棄し、記録リストを空にします。
    private void DestroyVisibleOverlays()
    {
        foreach (VisibleOverlayState overlay in visibleOverlays)
        {
            if (overlay == null || overlay.gameObject == null)
            {
                continue;
            }

            Destroy(overlay.gameObject);
        }

        visibleOverlays.Clear();
    }

    // 無効化時はハイライトを解除し、表示状態を残さないようにします。
    private void OnDisable()
    {
        SetHighlighted(false);
    }

    // 破棄時に実行時生成したオーバーレイと Material を片付けます。
    private void OnDestroy()
    {
        DestroyVisibleOverlays();

        if (visibleOverlayMaterial != null)
        {
            Destroy(visibleOverlayMaterial);
            visibleOverlayMaterial = null;
        }
    }

    // Inspector 値を安全範囲に補正し、プレイ中のハイライト色変更も反映します。
    private void OnValidate()
    {
        itemWeight = Mathf.Max(0f, itemWeight);
        flashFrequency = Mathf.Max(0.01f, flashFrequency);
        visibleOverlayAlpha = Mathf.Clamp01(visibleOverlayAlpha);

        if (Application.isPlaying && isHighlighted)
        {
            UpdateVisibleOverlayMaterial();
        }
    }
}
