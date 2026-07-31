using UnityEngine;

[CreateAssetMenu(fileName = "NewCarVFX", menuName = "Cars/Car VFX Data")]
public class CarVFXDataSO : ScriptableObject
{
    [Header("Trail de neumáticos (drift/deslizamiento)")]
    public float lateralSlipThreshold = 0.3f;
    public float brakeSlipThreshold = 0.35f;

    [Header("Nitro")]
    public GameObject nitroVFXPrefab;
    public float nitroVFXScale = 1f;

    // A futuro: acá se suman más entradas — humo de daño, chispas de choque, etc.
    // cada una con sus propios parámetros, sin tocar CarController ni CarStatsSO
}