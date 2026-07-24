using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System.Collections.Generic;

public class GameSetup : MonoBehaviour
{
    [Header("Derby")]
    public DerbyGameManager derbyManager;

    [Header("UI de vida")]
    public HealthBarUI healthBarP1;
    public HealthBarUI healthBarP2;

    [Header("Bots de equipo (Demolición con teams)")]
    public GameObject[] teamFillBotPrefabs;

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
    bool teamsActive;

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
        teamsActive = session != null && session.chosenGameMode != null
                      && session.chosenGameMode.supportsTeams && session.teamSize > 0;

        derbyManager.SetTeamsEnabled(teamsActive);

        ConfigureCameraLayout(isMultiplayer);
        ConfigureHealthBars(isMultiplayer);

        SpawnPlayer(0, session != null ? session.player1Car : null);

        if (isMultiplayer)
            SpawnPlayer(1, session.player2Car);
        else
            playerSlotConfigs[1].splitScreenCamera.gameObject.SetActive(false);

        if (teamsActive)
            SpawnTeamFillBots();
    }

    void ConfigureCameraLayout(bool isMultiplayer)
    {
        Camera cam1 = playerSlotConfigs[0].splitScreenCamera;
        Camera cam2 = playerSlotConfigs[1].splitScreenCamera;

        if (isMultiplayer)
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

    void ConfigureHealthBars(bool isMultiplayer)
    {
        if (healthBarP2 != null)
            healthBarP2.gameObject.SetActive(isMultiplayer);
    }

    void SpawnPlayer(int slotIndex, CarStatsSO carStats)
    {
        Transform spawnPoint = MapLoader.Instance.GetPlayerSpawn(slotIndex, GameModeCategory.Demolition);
        if (spawnPoint == null)
        {
            Debug.LogError($"[GameSetup] No hay spawn point de mapa para el slot {slotIndex}.", this);
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

            carController.Initialize(effectiveStats);
            carController.playerIndex = slotIndex + 1;
            carController.SetSpawnPoint(spawnPoint.position, spawnPoint.rotation);
        }

        PlayerInput playerInput = carInstance.GetComponent<PlayerInput>();
        playerInput.actions = Instantiate(playerInput.actions);
        playerInput.camera = config.splitScreenCamera;
        playerInput.SwitchCurrentControlScheme(config.controlScheme, Keyboard.current);

        AssignCameraChannel(carInstance, slotIndex);

        TeamId team = teamsActive
            ? (slotIndex == 0 ? session.player1Team : session.player2Team)
            : default;

        VehicleHealth health = carInstance.GetComponent<VehicleHealth>();
        if (health != null && derbyManager != null)
            derbyManager.RegisterPlayer($"Player {slotIndex + 1}", health, slotIndex, team);

        if (health != null)
            health.damageEnabled = GameSession.Instance.chosenGameMode == null || GameSession.Instance.chosenGameMode.enableDamage;

        HealthBarUI bar = slotIndex == 0 ? healthBarP1 : healthBarP2;
        if (bar != null && health != null)
            bar.SetTarget(health);

        MinimapIcon minimapIcon = carInstance.GetComponent<MinimapIcon>();
        if (minimapIcon != null)
            minimapIcon.SetOwner(slotIndex == 0 ? MinimapOwnerType.Player1 : MinimapOwnerType.Player2);
        else
            Debug.LogWarning($"[GameSetup] {carInstance.name} no tiene MinimapIcon.", this);

        CarColorApplier colorApplier = carInstance.GetComponentInChildren<CarColorApplier>();
        if (colorApplier != null)
        {
            Color chosenColor = slotIndex == 0 ? GameSession.Instance.player1Color : GameSession.Instance.player2Color;
            colorApplier.SetColor(chosenColor);
        }
    }

    void SpawnTeamFillBots()
    {
        var roster = BuildTeamRoster(session.teamSize, isMultiplayer);
        var botSlots = roster.Where(r => !r.isHuman).ToList();

        Transform[] aiSpawnPoints = MapLoader.Instance.GetAISpawnPoints(GameModeCategory.Demolition);
        if (aiSpawnPoints.Length < botSlots.Count)
            Debug.LogWarning($"[GameSetup] Se necesitan {botSlots.Count} spawn points de bot para completar equipos, el mapa tiene {aiSpawnPoints.Length}.", this);

        for (int i = 0; i < botSlots.Count && i < aiSpawnPoints.Length; i++)
        {
            GameObject prefab = teamFillBotPrefabs[Random.Range(0, teamFillBotPrefabs.Length)];
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
            if (health != null)
                derbyManager.RegisterPlayer($"Bot ({botSlots[i].team})", health, -1, botSlots[i].team);

            MinimapIcon minimapIcon = instance.GetComponent<MinimapIcon>();
            if (minimapIcon != null)
                minimapIcon.SetOwner(MinimapOwnerType.Bot);

            CarColorApplier colorApplier = instance.GetComponentInChildren<CarColorApplier>();
            if (colorApplier != null)
                colorApplier.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.6f, 1f));
        }
    }

    List<TeamAssignment> BuildTeamRoster(int teamSize, bool multiplayer)
    {
        var roster = new List<TeamAssignment>();

        roster.Add(new TeamAssignment { team = session.player1Team, isHuman = true, humanSlotIndex = 0 });

        if (multiplayer)
            roster.Add(new TeamAssignment { team = session.player2Team, isHuman = true, humanSlotIndex = 1 });

        int botsNeededA = teamSize - roster.Count(r => r.team == TeamId.TeamA);
        int botsNeededB = teamSize - roster.Count(r => r.team == TeamId.TeamB);

        for (int i = 0; i < botsNeededA; i++)
            roster.Add(new TeamAssignment { team = TeamId.TeamA, isHuman = false, humanSlotIndex = -1 });
        for (int i = 0; i < botsNeededB; i++)
            roster.Add(new TeamAssignment { team = TeamId.TeamB, isHuman = false, humanSlotIndex = -1 });

        return roster;
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

    public void ExpandToFullscreen(int survivingSlotIndex)
    {
        int eliminatedSlotIndex = survivingSlotIndex == 0 ? 1 : 0;

        Camera survivingCam = playerSlotConfigs[survivingSlotIndex].splitScreenCamera;
        survivingCam.rect = new Rect(0f, 0f, 1f, 1f);

        playerSlotConfigs[eliminatedSlotIndex].splitScreenCamera.gameObject.SetActive(false);
    }
}