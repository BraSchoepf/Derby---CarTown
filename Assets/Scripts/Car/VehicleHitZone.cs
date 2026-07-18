using UnityEngine;

public class VehicleHitZone : MonoBehaviour
{
    public enum Zone { Front, Rear, Left, Right }
    public Zone zone;
    [Range(0.5f, 3f)] public float damageMultiplier = 1f; // por zona, no por par de zonas

    // Cacheado una vez, no hay diccionario que mantener sincronizado
    void Reset()
    {
        if (GetComponent<Collider>() == null)
            Debug.LogWarning($"{name}: VehicleHitZone sin Collider en el mismo GameObject");
    }
}