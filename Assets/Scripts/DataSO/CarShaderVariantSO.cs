using UnityEngine;

[CreateAssetMenu(fileName = "NewShaderVariant", menuName = "Cars/Shader Variant")]
public class CarShaderVariantSO : ScriptableObject
{
    [Header("Identidad / Desbloqueo")]
    public string variantName;
    public Sprite previewIcon;
    public string unlockRewardId = ""; // vacío = siempre disponible

    [Header("Shader base")]
    [Tooltip("El shader a aplicar. Si querés mantener el shader actual del material y solo cambiar parámetros, dejalo vacío.")]
    public Shader shader;

    [Header("Texturas")]
    public Texture2D texture2D;

    [Header("Superficie (Metallic/Smoothness)")]
    [Range(0f, 1f)] public float smoothness = 0.5f;
    [Range(0f, 1f)] public float metallic = 0.5f;

    [Header("Toon Shading / Cel Shading")]
    [Tooltip("Cantidad de bandas de sombra (cel shading)")]
    public float shades = 3f;
    [Tooltip("Densidad de la celda/patrón, según tu implementación de shader")]
    public float cellDensity = 1f;
    public float minValue = 0f;
    public float maxValue = 1f;

    [Header("Fresnel (rim light)")]
    public Color fresnelColor = Color.white;
    public float fresnelPower = 2f;
    [Range(0f, 5f)] public float fresnelStrength = 1f;
    [Range(0f, 1f)] public float fresnelStart = 0f;
    [Range(0f, 1f)] public float fresnelEnd = 1f;

    [Header("Wave")]
    public Vector2 direction;

    [Header("Emisión")]
    public float emissionIntensity = 1f;
}