using UnityEngine;
using System.Collections.Generic;

public class AISpawner : MonoBehaviour
{
    [Header("Autos de IA disponibles")]
    public GameObject[] aiCarPrefabs;

    [Header("Categoría que maneja este spawner")]
    public GameModeCategory category = GameModeCategory.Demolition;

    [Header("Spawn")]
    public bool allowRepeatedCars = true;
    public DerbyGameManager derbyManager;

    GameSession session;

    void Start()
    {
        GameSession session = GameSession.Instance;

        // Salir si el modo elegido no corresponde a la categoría de este spawner
        if (session != null && session.chosenGameMode != null
            && session.chosenGameMode.category != category)
        {
            Debug.Log($"[AISpawner] Modo actual es {session.chosenGameMode.category}, este spawner maneja {category} — no actúa.");
            return;
        }

        bool teamsActive = session != null && session.chosenGameMode != null
                            && session.chosenGameMode.supportsTeams && session.teamSize > 0;

        if (teamsActive)
        {
            Debug.Log("[AISpawner] Modo con equipos activo — los spawn points los completa GameSetup.SpawnTeamFillBots(), este spawner no actúa.");
            return;
        }

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

        // Ya no hardcodeado: usa la categoría propia de este spawner
        Transform[] aiSpawnPoints = MapLoader.Instance.GetAISpawnPoints(category);
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
                CarStatsSO baseCarStats = carController.stats;
                DrivingProfileSO profile = GameSession.Instance != null && GameSession.Instance.chosenGameMode != null
                ? GameSession.Instance.chosenGameMode.drivingProfile
                : null;
                CarStatsSO effectiveStats = CarStatsFactory.BuildEffectiveStats(baseCarStats, profile);

                carController.Initialize(effectiveStats);
                carController.playerIndex = -1;
                carController.SetSpawnPoint(aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);
            }

            CarAIController aiController = instance.GetComponent<CarAIController>();
            if (aiController == null) aiController = instance.AddComponent<CarAIController>();

            VehicleHealth health = instance.GetComponent<VehicleHealth>();
            if (health != null)
            {
                health.damageEnabled = GameSession.Instance == null || GameSession.Instance.chosenGameMode == null
                    || GameSession.Instance.chosenGameMode.enableDamage;
                derbyManager.RegisterPlayer($"Bot {i + 1}", health);
            }

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