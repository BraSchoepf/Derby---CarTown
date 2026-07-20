using UnityEngine;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    [Header("Autos de IA disponibles")]
    [Tooltip("Todos los prefabs de auto IA que pueden aparecer (CON CarAIController, SIN PlayerInput)")]
    public GameObject[] aiCarPrefabs;

    [Header("Spawn")]
    public Transform[] aiSpawnPoints;
    public bool allowRepeatedCars = true; // false = cada auto aparece como máximo una vez por partida

    public DerbyGameManager derbyManager;

    void Start()
    {
        if (aiCarPrefabs == null || aiCarPrefabs.Length == 0)
        {
            Debug.LogError("[AISpawner] No hay prefabs de auto IA asignados.", this);
            return;
        }

        List<GameObject> pool = allowRepeatedCars
            ? null
            : new List<GameObject>(aiCarPrefabs);

        for (int i = 0; i < aiSpawnPoints.Length; i++)
        {
            GameObject prefabToSpawn = allowRepeatedCars
                ? aiCarPrefabs[Random.Range(0, aiCarPrefabs.Length)]
                : PickAndRemoveRandom(pool);

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[AISpawner] No quedan autos disponibles para el spawn point {i} (sin repetición activado y se agotó el pool).", this);
                continue;
            }

            GameObject instance = Instantiate(prefabToSpawn, aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);

            CarColorApplier colorApplier = instance.GetComponentInChildren<CarColorApplier>();
            if (colorApplier != null)
                colorApplier.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f)); // evita colores muy oscuros o desaturados/feos

            VehicleHealth health = instance.GetComponent<VehicleHealth>();
            if (health != null) derbyManager.RegisterPlayer($"Bot {i + 1}", health);

            MinimapIcon minimapIcon = instance.GetComponent<MinimapIcon>();
            if (minimapIcon != null)
                minimapIcon.SetOwner(MinimapOwnerType.Bot);
        }
    }

    GameObject PickAndRemoveRandom(List<GameObject> pool)
    {
        if (pool == null || pool.Count == 0) return null;
        int index = Random.Range(0, pool.Count);
        GameObject picked = pool[index];
        pool.RemoveAt(index);
        return picked;
    }
}