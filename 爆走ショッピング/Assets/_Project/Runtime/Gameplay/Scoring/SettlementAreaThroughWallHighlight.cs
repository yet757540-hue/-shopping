using UnityEngine;

/// <summary>レジエリアのRendererを複製し、遮蔽物の上に透過表示します。</summary>
[DisallowMultipleComponent]
public class SettlementAreaThroughWallHighlight : MonoBehaviour
{
    [Header("表示対象")]
    [SerializeField] private Renderer sourceRenderer;
    [SerializeField] private Material overlayMaterialTemplate;
    [SerializeField] private Color overlayColor = new Color(0.35f, 1f, 0.25f, 0.45f);

    private GameObject overlayObject;
    private Material runtimeMaterial;

    private void Awake()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<Renderer>();
        }

        if (sourceRenderer == null)
        {
            Debug.LogWarning("[SettlementAreaThroughWallHighlight] Renderer が見つかりません。", this);
            return;
        }

        if (overlayMaterialTemplate == null)
        {
            Debug.LogWarning("[SettlementAreaThroughWallHighlight] 透視表示用マテリアルを設定してください。", this);
            return;
        }

        CreateOverlay();
    }

    private void CreateOverlay()
    {
        MeshRenderer meshRenderer = sourceRenderer as MeshRenderer;
        SkinnedMeshRenderer skinnedRenderer = sourceRenderer as SkinnedMeshRenderer;

        if (meshRenderer == null && skinnedRenderer == null)
        {
            Debug.LogWarning("[SettlementAreaThroughWallHighlight] MeshRenderer または SkinnedMeshRenderer が必要です。", this);
            return;
        }

        overlayObject = new GameObject("SettlementArea Through Wall Overlay");
        overlayObject.transform.SetParent(sourceRenderer.transform, false);
        overlayObject.transform.localPosition = Vector3.zero;
        overlayObject.transform.localRotation = Quaternion.identity;
        overlayObject.transform.localScale = Vector3.one;
        overlayObject.hideFlags = HideFlags.DontSave;

        if (meshRenderer != null)
        {
            MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                Destroy(overlayObject);
                overlayObject = null;
                Debug.LogWarning("[SettlementAreaThroughWallHighlight] MeshFilter または Mesh がありません。", this);
                return;
            }

            MeshFilter overlayFilter = overlayObject.AddComponent<MeshFilter>();
            overlayFilter.sharedMesh = sourceFilter.sharedMesh;
            MeshRenderer overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            ConfigureOverlayRenderer(overlayRenderer);
        }
        else
        {
            if (skinnedRenderer.sharedMesh == null)
            {
                Destroy(overlayObject);
                overlayObject = null;
                Debug.LogWarning("[SettlementAreaThroughWallHighlight] Skinned Mesh がありません。", this);
                return;
            }

            SkinnedMeshRenderer overlayRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
            overlayRenderer.sharedMesh = skinnedRenderer.sharedMesh;
            overlayRenderer.bones = skinnedRenderer.bones;
            overlayRenderer.rootBone = skinnedRenderer.rootBone;
            ConfigureOverlayRenderer(overlayRenderer);
        }
    }

    private void ConfigureOverlayRenderer(Renderer overlayRenderer)
    {
        runtimeMaterial = new Material(overlayMaterialTemplate);
        runtimeMaterial.hideFlags = HideFlags.DontSave;

        if (runtimeMaterial.HasProperty("_BaseColor"))
            runtimeMaterial.SetColor("_BaseColor", overlayColor);
        else if (runtimeMaterial.HasProperty("_Color"))
            runtimeMaterial.SetColor("_Color", overlayColor);

        overlayRenderer.sharedMaterial = runtimeMaterial;
        overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;
    }

    private void OnDestroy()
    {
        if (overlayObject != null)
            Destroy(overlayObject);

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}