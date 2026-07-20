using UnityEngine;

[System.Serializable]
public class PaintableRenderer
{
    public Renderer renderer;
    [Tooltip("Índice del material pintable dentro de este Renderer. Varía según cómo esté armado el mesh de cada auto.")]
    public int materialIndex = 0;
}

public class CarColorApplier : MonoBehaviour
{
    [Tooltip("Configurá acá, por auto, qué Renderer y qué índice de material corresponde a la carrocería pintable")]
    public PaintableRenderer[] paintableRenderers;
    public string colorPropertyName = "_BaseColor";

    MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    public void SetColor(Color color)
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        foreach (var entry in paintableRenderers)
        {
            if (entry.renderer == null) continue;

            entry.renderer.GetPropertyBlock(propBlock, entry.materialIndex);
            propBlock.SetColor(colorPropertyName, color);
            entry.renderer.SetPropertyBlock(propBlock, entry.materialIndex);
        }
    }
}