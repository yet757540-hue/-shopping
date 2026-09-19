using System.Collections.Generic;
using UnityEngine;

/// <summary>衝突で取得できるアイテム 1 種と、そのハイライト表示を持ちます。</summary>
[DisallowMultipleComponent]
public class ScoreTarget : MonoBehaviour
{
    // 材質と色プロパティ、復元用の元の色を一組として保持します。
    private class MaterialColorState
    {
        public Material material;
        public string colorProperty;
        public Color originalColor;
    }

    // 生成した透視表示のレンダラーと、破棄対象のオブジェクトを保持します。
    private class VisibleOverlayState
    {
        public Renderer renderer;
        public GameObject gameObject;
    }

    // 集計に使う ID・表示名・重量と、取得後の表示方針です。
    [Header("アイテム情報")]
    [SerializeField] private string displayName;
    [SerializeField] private string itemId;
    [SerializeField] private float itemWeight = 1f;
    [SerializeField] private bool collectOnce = true;
    [SerializeField] private bool hideWhenCollected = false;

    // 目標の強調色と、壁越し表示に使う対象・点滅・材質の設定です。
    [Header("ハイライト")]
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private bool flashVisibleThroughWalls = true;
    [SerializeField] private float flashFrequency = 1f;
    [SerializeField] private float visibleOverlayAlpha = 0.7f;
    [SerializeField] private Material visibleOverlayMaterialTemplate;

    private readonly List<MaterialColorState> originalColors = new List<MaterialColorState>();
    private readonly List<VisibleOverlayState> visibleOverlays = new List<VisibleOverlayState>();
    private Material visibleOverlayMaterial;
    private bool isHighlighted = false;
    private bool isCollected = false;

    // 未設定の場合は表示名、最後に GameObject 名を識別に使います。
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    // 集計用 ID を返し、未設定なら表示名を使用します。
    public string ItemId => string.IsNullOrWhiteSpace(itemId) ? DisplayName : itemId;

    // アイテム一個の重量を、ゼロ以上に補正して返します。
    public float ItemWeight => Mathf.Max(0f, itemWeight);
    // この目標が取得済みとして記録されているかを返します。
    public bool IsCollected => isCollected;

    // 透視表示に使用する材質テンプレートを受け取ります。ここでは表示オブジェクトを生成しません。
    public void InitializeVisibleOverlayMaterial(Material template)
    {
        visibleOverlayMaterialTemplate = template;
    }

    // 対象レンダラーと元の色を保存し、壁越し表示のオブジェクトを準備します。
    private void Awake()
    {
        CacheRenderers();
        CacheOriginalColors();
        CreateVisibleOverlays();
    }

    // ハイライト中だけ、時間に応じた透視表示の点滅を更新します。
    private void Update()
    {
        if (!isHighlighted || !flashVisibleThroughWalls || visibleOverlays.Count == 0)
        {
            return;
        }

        // 往復する値を滑らかに補間し、壁越し表示の透明度として使います。
        float fade = Mathf.PingPong(Time.time * flashFrequency, 1f);
        fade = fade * fade * (3f - 2f * fade);
        SetOverlayAlpha(visibleOverlayAlpha * fade);
    }

    // 一度だけ取得する設定と取得済み状態から、取得可能かを返します。
    public bool CanCollect()
    {
        return !collectOnce || !isCollected;
    }

    // 取得済みにし、ハイライトを解除して、設定に応じて通常表示を隠します。
    public void MarkCollected()
    {
        isCollected = true;
        SetHighlighted(false);

        if (hideWhenCollected)
        {
            SetRenderersVisible(false);
        }
    }

    // 取得済み状態を解除し、通常レンダラーを再表示します。
    public void ResetCollected()
    {
        isCollected = false;
        SetRenderersVisible(true);
    }

    // 状態が変わった場合だけ材質色を切り替え、透視表示の有効・無効を更新します。
    public void SetHighlighted(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;

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
            SetOverlayAlpha(visibleOverlayAlpha);
            ApplyOverlayVisible(flashVisibleThroughWalls);
            return;
        }

        ApplyOverlayVisible(false);
    }

    // 対象が未設定なら子階層のレンダラーを集めます。
    private void CacheRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

    // 各材質の色プロパティと元の色を記録し、ハイライト解除時に復元できるようにします。
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

    // 材質が対応する色プロパティ名を調べ、対応していなければ null を返します。
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

    // 前の透視表示を破棄し、設定が有効な場合だけ各レンダラーの表示用コピーを作ります。
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

    // 通常メッシュまたはスキンメッシュに合わせて、透視表示用のレンダラーを複製します。
    private void CreateVisibleOverlay(Renderer targetRenderer, Material overlayMaterial)
    {
        // 通常メッシュは形状を共有し、スキンメッシュはボーン参照も引き継ぎます。
        MeshRenderer meshRenderer = targetRenderer as MeshRenderer;

        if (meshRenderer != null)
        {
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();

            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            GameObject overlayObject = CreateOverlayObject(targetRenderer.transform);
            MeshFilter overlayMeshFilter = overlayObject.AddComponent<MeshFilter>();
            overlayMeshFilter.sharedMesh = meshFilter.sharedMesh;

            MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            AddVisibleOverlay(overlayRenderer, overlayMaterial);
            return;
        }

        SkinnedMeshRenderer skinnedRenderer = targetRenderer as SkinnedMeshRenderer;

        if (skinnedRenderer == null || skinnedRenderer.sharedMesh == null)
        {
            return;
        }

        GameObject skinnedOverlayObject = CreateOverlayObject(targetRenderer.transform);
        SkinnedMeshRenderer skinnedOverlayRenderer = skinnedOverlayObject.AddComponent<SkinnedMeshRenderer>();
        skinnedOverlayRenderer.sharedMesh = skinnedRenderer.sharedMesh;
        skinnedOverlayRenderer.bones = skinnedRenderer.bones;
        skinnedOverlayRenderer.rootBone = skinnedRenderer.rootBone;
        AddVisibleOverlay(skinnedOverlayRenderer, overlayMaterial);
    }

    // 対象の子として保存対象外の表示オブジェクトを作り、ローカル変換をそろえます。
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

    // メッシュの種類に関係なく、材質と影の設定を共通化します。
    private void AddVisibleOverlay(Renderer overlayRenderer, Material overlayMaterial)
    {
        overlayRenderer.sharedMaterial = overlayMaterial;
        overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
        visibleOverlays.Add(new VisibleOverlayState
        {
            gameObject = overlayRenderer.gameObject,
            renderer = overlayRenderer
        });
    }

    // 生成済み材質を再利用し、初回だけテンプレートから専用材質を作ります。
    private Material GetVisibleOverlayMaterial()
    {
        if (visibleOverlayMaterial != null)
        {
            return visibleOverlayMaterial;
        }

        // 共有テンプレートを直接変更せず、この目標専用の材質を生成します。
        if (visibleOverlayMaterialTemplate != null)
        {
            visibleOverlayMaterial = new Material(visibleOverlayMaterialTemplate);
            visibleOverlayMaterial.hideFlags = HideFlags.DontSave;
            ApplyOverlayColor(visibleOverlayMaterial, visibleOverlayAlpha);
            return visibleOverlayMaterial;
        }

        Debug.LogWarning("[ScoreTarget] Visible overlay material is not assigned.", this);
        return null;
    }

    // 材質の取得と色の書き込みを分け、初期化の相互呼び出しを避けます。
    private void SetOverlayAlpha(float alpha)
    {
        Material overlayMaterial = GetVisibleOverlayMaterial();

        if (overlayMaterial == null)
        {
            return;
        }

        ApplyOverlayColor(overlayMaterial, alpha);
    }

    // ハイライト色に指定アルファを設定し、透視表示の材質へ書き込みます。
    private void ApplyOverlayColor(Material overlayMaterial, float alpha)
    {
        Color overlayColor = highlightColor;
        overlayColor.a = Mathf.Clamp01(alpha);
        overlayMaterial.SetColor("_BaseColor", overlayColor);
    }

    // 透視表示用の全レンダラーをまとめて表示・非表示にします。
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

    // 通常表示用の全レンダラーをまとめて表示・非表示にします。
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

    // 生成した透視表示オブジェクトを破棄し、管理一覧を空にします。
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

    // 無効化時に元の材質色へ戻し、透視表示を停止します。
    private void OnDisable()
    {
        SetHighlighted(false);
    }

    // 生成した透視表示と専用材質を解放します。共有テンプレートは破棄しません。
    private void OnDestroy()
    {
        DestroyVisibleOverlays();

        if (visibleOverlayMaterial != null)
        {
            Destroy(visibleOverlayMaterial);
            visibleOverlayMaterial = null;
        }
    }

    // Inspector の変更時に、設定値を有効な範囲へ補正します。
    private void OnValidate()
    {
        itemWeight = Mathf.Max(0f, itemWeight);
        flashFrequency = Mathf.Max(0.01f, flashFrequency);
        visibleOverlayAlpha = Mathf.Clamp01(visibleOverlayAlpha);

        if (Application.isPlaying && isHighlighted)
        {
            SetOverlayAlpha(visibleOverlayAlpha);
        }
    }
}
