using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;
public class RaceSetup : MonoBehaviour
{
    [Header("Race")]
    public RaceManager raceManager;

    [Header("Bots (relleno de parrilla)")]
    public GameObject[] aiCarPrefabs;
    public int botsToFillGrid = 4;

    [Header("Drift Score UI (opcional, solo si el modo lo requiere)")]
    public DriftScoreUI driftScoreUIP1;
    public DriftScoreUI driftScoreUIP2;

    [Header("Speedometer UI")]
    public SpeedometerUI speedometerP1;
    public SpeedometerUI speedometerP2;

    [System.Serializable]
    public class PlayerSlotConfig
    {
        public Camera splitScreenCamera;
        public string controlScheme;
    }

    public CarRegistry registry;
    public PlayerSlotConfig[] playerSlotConfigs;

    GameSession session;
    bool isMultiplayer;

    void Start()
    {
        if (MapLoader.Instance.IsMapReady)
            OnMapReady();
        else
            MapLoader.Instance.OnMapReady += OnMapReady;
    }

    void OnMapReady()
    {
        MapLoader.Instance.OnMapReady -= OnMapReady;

        session = GameSession.Instance;
        isMultiplayer = session != null && session.selectedMode == GameMode.MultiplayerSplitScreen;

        if (!session.chosenGameMode.isDriftScoringMode)
        {
            if (driftScoreUIP1 != null) driftScoreUIP1.gameObject.SetActive(false);
            if (driftScoreUIP2 != null) driftScoreUIP2.gameObject.SetActive(false);
        }

        ConfigureSpeedometers(isMultiplayer);

        RaceCourseSet courseSet = MapLoader.Instance.GetRaceCourseSet();
        if (courseSet == null)
        {
            Debug.LogError("[RaceSetup] El mapa actual no tiene RaceCourseSet.", this);
            return;
        }

        raceManager.activeCourse = courseSet.GetCourseFor(session.chosenGameMode);
        if (raceManager.activeCourse == null)
        {
            Debug.LogError($"[RaceSetup] El mapa no tiene un curso configurado para '{session.chosenGameMode.modeName}'.", this);
            return;
        }

        raceManager.InitializeCheckpoints();

        ConfigureCameraLayout(isMultiplayer);

        SpawnPlayer(0, session.player1Car, session.player1Color);
        if (isMultiplayer)
            SpawnPlayer(1, session.player2Car, session.player2Color);
        else
            playerSlotConfigs[1].splitScreenCamera.gameObject.SetActive(false);

        SpawnGridBots();

        raceManager.BeginRace();
    }

    void ConfigureCameraLayout(bool multiplayer)
    {
        Camera cam1 = playerSlotConfigs[0].splitScreenCamera;
        Camera cam2 = playerSlotConfigs[1].splitScreenCamera;

        if (multiplayer)
        {
            cam1.rect = new Rect(0f, 0f, 0.5f, 1f);
            cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            cam2.gameObject.SetActive(true);
        }
        else
        {
            cam1.rect = new Rect(0, 0, 1, 1);
            cam2.gameObject.SetActive(false);
        }
    }

    void SpawnPlayer(int slotIndex, CarStatsSO carStats, Color color)
    {
        Transform spawnPoint = MapLoader.Instance.GetPlayerSpawn(slotIndex, GameModeCategory.Racing);
        if (spawnPoint == null)
        {
            Debug.LogError($"[RaceSetup] No hay spawn point para el slot {slotIndex}.", this);
            return;
        }

        PlayerSlotConfig config = playerSlotConfigs[slotIndex];

        GameObject prefabToSpawn = carStats != null
            ? registry.GetPrefabForStats(carStats)
            : registry.cars[0].prefab;

        GameObject carInstance = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

        CarController carController = carInstance.GetComponent<CarController>();
        if (carController != null)
        {
            CarStatsSO baseCarStats = carStats != null ? carStats : carController.stats;
            DrivingProfileSO profile = session.chosenGameMode != null ? session.chosenGameMode.drivingProfile : null;
            CarStatsSO effectiveStats = CarStatsFactory.BuildEffectiveStats(baseCarStats, profile);

            if (effectiveStats != null)
                carController.Initialize(effectiveStats);
            carController.playerIndex = slotIndex + 1;
            carController.SetSpawnPoint(spawnPoint.position, spawnPoint.rotation);
        }

        VehicleHealth health = carInstance.GetComponent<VehicleHealth>();
        if (health != null)
            health.damageEnabled = session.chosenGameMode.enableDamage;

        PlayerInput playerInput = carInstance.GetComponent<PlayerInput>();
        playerInput.actions = Instantiate(playerInput.actions);
        playerInput.camera = config.splitScreenCamera;
        playerInput.SwitchCurrentControlScheme(config.controlScheme, Keyboard.current);

        carController.SetupInputActions();

        AssignCameraChannel(carInstance, slotIndex);

        var progress = new RaceManager.RacerProgress
        {
            racerName = $"Player {slotIndex + 1}",
            humanSlotIndex = slotIndex
        };
        raceManager.RegisterRacer(progress);

        SpeedometerUI speedo = slotIndex == 0 ? speedometerP1 : speedometerP2;
        if (speedo != null && carController != null)
            speedo.SetTarget(carController);


        RaceCarIdentity identity = carInstance.GetComponent<RaceCarIdentity>();
        if (identity == null) identity = carInstance.AddComponent<RaceCarIdentity>();
        identity.Initialize(progress);

        if (session.chosenGameMode.isDriftScoringMode)
        {
            DriftScoreTracker scoreTracker = carInstance.GetComponent<DriftScoreTracker>();
            if (scoreTracker == null) scoreTracker = carInstance.AddComponent<DriftScoreTracker>();

            // Guardamos la referencia para que el HUD del jugador pueda leer el puntaje en vivo
            StorePlayerDriftTracker(slotIndex, scoreTracker);
        }

        CarColorApplier colorApplier = carInstance.GetComponentInChildren<CarColorApplier>();
        if (colorApplier != null) colorApplier.SetColor(color);

        PlayerRaceRespawn playerRespawn = carInstance.GetComponent<PlayerRaceRespawn>();
        if (playerRespawn == null) playerRespawn = carInstance.AddComponent<PlayerRaceRespawn>();
        playerRespawn.waypointPath = raceManager.activeCourse.aiPath;

        carController.autoRespawnWhenStuck = false;

    }
    void AssignCameraChannel(GameObject carInstance, int slotIndex)
    {
        var vcam = carInstance.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (vcam == null)
        {
            Debug.LogWarning($"No se encontró CinemachineCamera en {carInstance.name}");
            return;
        }
        vcam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << (slotIndex + 1));
    }

    void SpawnGridBots()
    {
        if (!session.chosenGameMode.allowBots)
        {
            Debug.Log("[RaceSetup] El modo actual no permite bots — no se spawnean rivales de IA.");
            return;
        }

        Transform[] aiSpawnPoints = MapLoader.Instance.GetAISpawnPoints(GameModeCategory.Racing);
        int count = Mathf.Min(botsToFillGrid, aiSpawnPoints.Length);

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = aiCarPrefabs[Random.Range(0, aiCarPrefabs.Length)];
            GameObject instance = Instantiate(prefab, aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);

            CarController carController = instance.GetComponent<CarController>();
            if (carController != null)
            {
                CarStatsSO baseCarStats = carController.stats;
                DrivingProfileSO profile = session.chosenGameMode != null ? session.chosenGameMode.drivingProfile : null;
                CarStatsSO effectiveStats = CarStatsFactory.BuildEffectiveStats(baseCarStats, profile);

                carController.Initialize(effectiveStats);
                carController.playerIndex = -1;
                carController.SetSpawnPoint(aiSpawnPoints[i].position, aiSpawnPoints[i].rotation);
            }

            VehicleHealth health = instance.GetComponent<VehicleHealth>();
            if (health != null) health.damageEnabled = session.chosenGameMode.enableDamage;

            var progress = new RaceManager.RacerProgress
            {
                racerName = $"Bot {i + 1}",
                humanSlotIndex = -1
            };
            raceManager.RegisterRacer(progress);

            RaceCarIdentity identity = instance.GetComponent<RaceCarIdentity>();
            if (identity == null) identity = instance.AddComponent<RaceCarIdentity>();
            identity.Initialize(progress);

            // Nuevo: le damos el "cerebro" de carrera en vez de dejarlo sin control
            if (session.chosenGameMode.isDriftScoringMode)
            {
                DriftAIController driftAI = instance.GetComponent<DriftAIController>();
                if (driftAI == null) driftAI = instance.AddComponent<DriftAIController>();
                driftAI.progress = progress;
                driftAI.raceManager = raceManager;
                driftAI.waypointPath = raceManager.activeCourse.aiPath;

                DriftScoreTracker scoreTracker = instance.GetComponent<DriftScoreTracker>();
                if (scoreTracker == null) scoreTracker = instance.AddComponent<DriftScoreTracker>();
            }
            else
            {
                RaceAIController raceAI = instance.GetComponent<RaceAIController>();
                if (raceAI == null) raceAI = instance.AddComponent<RaceAIController>();
                raceAI.progress = progress;
                raceAI.raceManager = raceManager;
                raceAI.waypointPath = raceManager.activeCourse.aiPath;
            }

            CarColorApplier colorApplier = instance.GetComponentInChildren<CarColorApplier>();
            if (colorApplier != null)
                colorApplier.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f));
        }
    }
    void StorePlayerDriftTracker(int slotIndex, DriftScoreTracker tracker)
    {
        DriftScoreUI ui = slotIndex == 0 ? driftScoreUIP1 : driftScoreUIP2;
        if (ui != null)
        {
            ui.gameObject.SetActive(true);
            ui.SetTracker(tracker);
        }
    }
    void ConfigureSpeedometers(bool multiplayer)
    {
        if (speedometerP1 != null) speedometerP1.gameObject.SetActive(true); // P1 siempre visible

        if (speedometerP2 != null)
            speedometerP2.gameObject.SetActive(multiplayer); // P2 solo si hay multiplayer
    }
}