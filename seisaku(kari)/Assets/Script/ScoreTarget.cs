using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ScoreTarget : MonoBehaviour
{
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

    private void Awake()
    {
        CacheRenderers();
        CacheOriginalColors();
        CreateVisibleOverlays();
    }

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

    public bool CanCollect()
    {
        return !collectOnce || !isCollected;
    }

    public void MarkCollected()
    {
        isCollected = true;
        SetHighlighted(false);

        if (hideWhenCollected)
        {
            SetRenderersVisible(false);
        }
    }

    public void ResetCollected()
    {
        isCollected = false;
        SetRenderersVisible(true);
    }

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
            UpdateVisibleOverlayMaterial();
            ApplyOverlayVisible(flashVisibleThroughWalls);
            return;
        }

        ApplyOverlayVisible(false);
    }

    private void CacheRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
        {
            return;
        }

        targetRenderers = GetComponentsInChildren<Renderer>();
    }

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

    private void CreateVisibleOverlay(Renderer targetRenderer, Material overlayMaterial)
    {
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

    private void AddVisibleOverlay(GameObject overlayObject, Renderer overlayRenderer)
    {
        visibleOverlays.Add(new VisibleOverlayState
        {
            gameObject = overlayObject,
            renderer = overlayRenderer
        });
    }

    private Material GetVisibleOverlayMaterial()
    {
        if (visibleOverlayMaterial != null)
        {
            return visibleOverlayMaterial;
        }

        Shader shader = Shader.Find("Custom/ScoreTargetVisibleOverlay");

        if (shader == null)
        {
            return null;
        }

        visibleOverlayMaterial = new Material(shader);
        visibleOverlayMaterial.hideFlags = HideFlags.DontSave;
        UpdateVisibleOverlayMaterial();
        return visibleOverlayMaterial;
    }

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

    private void OnDisable()
    {
        SetHighlighted(false);
    }

    private void OnDestroy()
    {
        DestroyVisibleOverlays();

        if (visibleOverlayMaterial != null)
        {
            Destroy(visibleOverlayMaterial);
            visibleOverlayMaterial = null;
        }
    }

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
