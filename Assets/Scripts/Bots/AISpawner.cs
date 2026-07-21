using UnityEngine;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    [Header("Autos de IA disponibles")]
    public GameObject[] aiCarPrefabs;

    [Header("Spawn")]
    public bool allowRepeatedCars = true;

    public DerbyGameManager derbyManager;

    void Start()
    {
        if (MapLoader.Instance.IsMapReady)
            SpawnBots();
        else
            MapLoader.Instance.OnMapReady += SpawnBots;
    }

    void SpawnBots()
    {
        MapLoader.Instance.OnMapReady -= SpawnBots;

        if (aiCarPrefabs == null || aiCarPrefabs.Length == 0)
        {
            Debug.LogError("[AISpawner] No hay prefabs de auto IA asignados.", this);
            return;
        }

        Transform[] aiSpawnPoints = MapLoader.Instance.GetAISpawnPoints();
        if (aiSpawnPoints.Length == 0)
        {
            Debug.LogError("[AISpawner] El mapa actual no tiene puntos de spawn de IA.", this);
            return;
        }

        List<GameObject> pool = allowRepeatedCars ? null : new List<GameObject>(aiCarPrefabs);

        for (int i = 0; i < aiSpawnPoints.Length; i++)
        {
            GameObject prefabToSpawn = allowRepeatedCars
                ? aiCarPrefabs[Random.Range(0, aiCarPrefabs.Length)]
                : PickAndRemoveRandom(pool);

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[AISpawner] No quedan autos disponibles para el spawn point {i}.", this);
                continue;
            }

            GameObject instance = Instantiate(prefabToSpawn, aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);

            CarController carController = instance.GetComponent<CarController>();
            if (carController != null)
            {
                carController.playerIndex = -1;
                carController.SetSpawnPoint(aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);
            }

            VehicleHealth health = instance.GetComponent<VehicleHealth>();
            if (health != null) derbyManager.RegisterPlayer($"Bot {i + 1}", health);

            MinimapIcon minimapIcon = instance.GetComponent<MinimapIcon>();
            if (minimapIcon != null)
                minimapIcon.SetOwner(MinimapOwnerType.Bot);

            CarColorApplier colorApplier = instance.GetComponentInChildren<CarColorApplier>();
            if (colorApplier != null)
                colorApplier.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f));
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