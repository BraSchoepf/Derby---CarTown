using UnityEngine;

public class CarShaderApplier : MonoBehaviour
{
    public PaintableRenderer[] paintableRenderers;

    public void ApplyShaderVariant(CarShaderVariantSO variant)
    {
        if (variant == null)
        {
            Debug.LogWarning("[CarShaderApplier] Variant es NULL.", this);
            return;
        }

        foreach (var entry in paintableRenderers)
        {
            if (entry.renderer == null)
            {
                Debug.LogWarning("[CarShaderApplier] Hay un PaintableRenderer sin Renderer asignado.", this);
                continue;
            }

            Material[] mats = entry.renderer.materials;
            if (entry.materialIndex < 0 || entry.materialIndex >= mats.Length)
            {
                Debug.LogWarning(
                    $"[CarShaderApplier] Índice inválido: {entry.materialIndex} en {entry.renderer.name}. Tiene {mats.Length} materiales.",
                    this
                );
                continue;
            }

            Material mat = mats[entry.materialIndex];

            // Cambiar el shader solo si el variant define uno distinto
            if (variant.shader != null)
                mat.shader = variant.shader;

            ApplyParameters(mat, variant);

            mats[entry.materialIndex] = mat;
            entry.renderer.materials = mats;
        }
    }

    void ApplyParameters(Material mat, CarShaderVariantSO variant)
    {
        SetFloatIfExists(mat, "_Smoothness", variant.smoothness);
        SetFloatIfExists(mat, "_Metallic", variant.metallic);

        SetFloatIfExists(mat, "_Shades", variant.shades);
        SetFloatIfExists(mat, "_CellDensity", variant.cellDensity);
        SetFloatIfExists(mat, "_Min", variant.minValue);
        SetFloatIfExists(mat, "_Max", variant.maxValue);

        SetColorIfExists(mat, "_FresnelColor", variant.fresnelColor);
        SetFloatIfExists(mat, "_FresnelPower", variant.fresnelPower);
        SetFloatIfExists(mat, "_FresnelStrength", variant.fresnelStrength);
        SetFloatIfExists(mat, "_FresnelStart", variant.fresnelStart);
        SetFloatIfExists(mat, "_FresnelEnd", variant.fresnelEnd);

        SetFloatIfExists(mat, "_EmissionIntensity", variant.emissionIntensity);
    }

    void SetFloatIfExists(Material mat, string propertyName, float value)
    {
        if (mat.HasProperty(propertyName))
            mat.SetFloat(propertyName, value);
    }

    void SetColorIfExists(Material mat, string propertyName, Color value)
    {
        if (mat.HasProperty(propertyName))
            mat.SetColor(propertyName, value);
    }

    // Para cuando armes la UI de Fresnel en vivo — actualiza solo esa propiedad,
    // sin necesitar un CarShaderVariantSO completo
    public void SetFresnelColor(Color color)
    {
        foreach (var entry in paintableRenderers)
        {
            if (entry.renderer == null) continue;
            Material[] mats = entry.renderer.materials;
            if (entry.materialIndex < 0 || entry.materialIndex >= mats.Length) continue;

            Material mat = mats[entry.materialIndex];
            if (mat.HasProperty("_FresnelColor"))
                mat.SetColor("_FresnelColor", color);
        }
    }
}