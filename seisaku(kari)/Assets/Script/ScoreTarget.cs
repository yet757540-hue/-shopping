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

    [Header("Display")]
    [SerializeField] private string displayName;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = Color.red;
    [SerializeField] private Renderer[] targetRenderers;

    private readonly List<MaterialColorState> originalColors = new List<MaterialColorState>();
    private bool isHighlighted = false;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(displayName) && displayName.Trim().Length > 0)
            {
                return displayName;
            }

            return gameObject.name;
        }
    }

    private void Awake()
    {
        CacheRenderers();
        CacheOriginalColors();
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

    private void OnDisable()
    {
        SetHighlighted(false);
    }
}
