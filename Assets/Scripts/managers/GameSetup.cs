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

    [Header("Speedometer UI")]
    public SpeedometerUI speedometerP1;
    public SpeedometerUI speedometerP2;

    [Header("Nitro UI")]
    public NitroSliderUI nitroSliderP1;
    public NitroSliderUI nitroSliderP2;

    [Header("Respawn Hold UI")]
    public RespawnHoldUI respawnHoldP1;
    public RespawnHoldUI respawnHoldP2;

    [Header("Bots de equipo (Demolición con teams)")]
    public GameObject[] teamFillBotPrefabs;

    [Header("Colores de equipo (modos con Teams)")]
    public Color teamAColor = Color.blue;
    public Color teamBColor = Color.red;

    [Header("Bomb Direction Indicators (Papá Caliente)")]
    public BombDirectionIndicator bombIndicatorP1;
    public BombDirectionIndicator bombIndicatorP2;
    [Header("Bomb Timer UI")]
    public BombTimerUI bombTimerUI;

    [Header("Sumo")]
    public LayerMask groundLayer;

    [Header("Reglas generales")]
    public bool enableDamage = true;
    public bool requiresCheckpoints = false;
    public int lapsDefault = 1;
    public bool usesBombMechanic = false;

    [Header("Shaders para bots (opcional)")]
    public CarShaderVariantSO[] defaultBotShaders;

    [Header("Ruedas para bots (opcional)")]
    public WheelVisualSO[] availableWheelsForBots;

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
        ConfigureSpeedometers(isMultiplayer);
        ConfigureNitro(isMultiplayer);
        ConfigureRespawnUI(isMultiplayer);

        SpawnPlayer(0, session != null ? session.player1Car : null);

        if (isMultiplayer)
            SpawnPlayer(1, session.player2Car);
        else
            playerSlotConfigs[1].splitScreenCamera.gameObject.SetActive(false);

        if (teamsActive)
            SpawnTeamFillBots();

        if (session.chosenGameMode.usesBombMechanic)
            StartCoroutine(StartBombRoundNextFrame());

        if (bombTimerUI != null)
            bombTimerUI.gameObject.SetActive(session.chosenGameMode.usesBombMechanic);
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

        carController.SetupInputActions();

        AssignCameraChannel(carInstance, slotIndex);

        VehicleHealth health = carInstance.GetComponent<VehicleHealth>();
        if (health != null)
        {
            health.damageEnabled = session.chosenGameMode.enableDamage;
            health.teamsActive = teamsActive;
            health.friendlyFireEnabled = false;

            TeamId team = teamsActive ? (slotIndex == 0 ? session.player1Team : session.player2Team) : default;
            health.team = team;

            derbyManager.RegisterPlayer($"Player {slotIndex + 1}", health, slotIndex, team);
        }

        SpeedometerUI speedo = slotIndex == 0 ? speedometerP1 : speedometerP2;
        if (speedo != null && carController != null)
            speedo.SetTarget(carController);

        NitroSliderUI nitroUI = slotIndex == 0 ? nitroSliderP1 : nitroSliderP2;
        if (nitroUI != null && carController != null)
            nitroUI.SetTarget(carController);

        RespawnHoldUI respawnUI = slotIndex == 0 ? respawnHoldP1 : respawnHoldP2;
        if (respawnUI != null && carController != null)
            respawnUI.SetTarget(carController);

        EdgeAvoidance edgeAvoidance = carInstance.GetComponent<EdgeAvoidance>();
        if (edgeAvoidance == null) edgeAvoidance = carInstance.AddComponent<EdgeAvoidance>();

        bool isSumo = session.chosenGameMode != null && session.chosenGameMode.enableEdgeDetection;
        edgeAvoidance.enabled = isSumo;

        if (isSumo)
            edgeAvoidance.edgeCheckDistance = groundLayer;

        HealthBarUI bar = slotIndex == 0 ? healthBarP1 : healthBarP2;
        if (bar != null && health != null)
            bar.SetTarget(health);

        MinimapIcon minimapIcon = carInstance.GetComponent<MinimapIcon>();
        if (minimapIcon != null)
            minimapIcon.SetOwner(slotIndex == 0 ? MinimapOwnerType.Player1 : MinimapOwnerType.Player2);
        else
            Debug.LogWarning($"[GameSetup] {carInstance.name} no tiene MinimapIcon.", this);

        WheelCustomizer wheelCustomizer = carInstance.GetComponentInChildren<WheelCustomizer>();
        if (wheelCustomizer != null)
        {
            WheelVisualSO chosenWheel = slotIndex == 0 ? session.player1WheelVisual : session.player2WheelVisual;
            if (chosenWheel != null)
                wheelCustomizer.ApplyWheel(chosenWheel);
        }

        CarShaderApplier shaderApplier = carInstance.GetComponentInChildren<CarShaderApplier>();
        if (shaderApplier != null)
        {
            CarShaderVariantSO chosenVariant = slotIndex == 0 ? session.player1ShaderVariant : session.player2ShaderVariant;
            if (chosenVariant != null) shaderApplier.ApplyShaderVariant(chosenVariant);
        }

        CarColorApplier colorApplier = carInstance.GetComponentInChildren<CarColorApplier>();
        if (colorApplier != null)
        {
            Color chosenColor;
            if (teamsActive)
            {
                TeamId team = slotIndex == 0 ? session.player1Team : session.player2Team;
                chosenColor = team == TeamId.TeamA ? teamAColor : teamBColor;
            }
            else
            {
                chosenColor = slotIndex == 0 ? session.player1Color : session.player2Color;
            }
            colorApplier.SetColor(chosenColor);
        }

        BombDirectionIndicator indicator = slotIndex == 0 ? bombIndicatorP1 : bombIndicatorP2;
        if (indicator != null && session.chosenGameMode.usesBombMechanic)
        {
            indicator.ownCarTransform = carInstance.transform;
            indicator.playerCamera = config.splitScreenCamera;
            indicator.gameObject.SetActive(true);
        }
        else if (indicator != null)
        {
            indicator.gameObject.SetActive(false); // en otros modos, el indicador queda oculto
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

            if (session.chosenGameMode.usesBombMechanic)
            {
                BombAIController bombAI = instance.GetComponent<BombAIController>();
                if (bombAI == null) bombAI = instance.AddComponent<BombAIController>();

                BombCarrier bombCarrier = instance.GetComponent<BombCarrier>();
                if (bombCarrier == null) bombCarrier = instance.AddComponent<BombCarrier>();
            }
            else
            {
                CarAIController aiController = instance.GetComponent<CarAIController>();
                if (aiController == null) aiController = instance.AddComponent<CarAIController>();
            }


            VehicleHealth health = instance.GetComponent<VehicleHealth>();
            if (health != null)
            {
                // ESTO faltaba: sin esto, el propio bot no sabe su team ni que está en un modo con equipos,
                // así que su FindTarget() nunca filtra a los compañeros correctamente
                health.team = botSlots[i].team;
                health.teamsActive = true;
                health.friendlyFireEnabled = false;
                health.damageEnabled = session.chosenGameMode.enableDamage;

                derbyManager.RegisterPlayer($"Bot ({botSlots[i].team})", health, -1, botSlots[i].team);
            }

            EdgeAvoidance edgeAvoidance = instance.GetComponent<EdgeAvoidance>();
            if (edgeAvoidance == null) edgeAvoidance = instance.AddComponent<EdgeAvoidance>();

            bool isSumo = session.chosenGameMode != null && session.chosenGameMode.enableEdgeDetection;
            edgeAvoidance.enabled = isSumo;

            if (isSumo)
                edgeAvoidance.edgeCheckDistance = groundLayer;

            MinimapIcon minimapIcon = instance.GetComponent<MinimapIcon>();
            if (minimapIcon != null)
                minimapIcon.SetOwner(MinimapOwnerType.Bot);

            WheelCustomizer wheelCustomizer = instance.GetComponentInChildren<WheelCustomizer>();
            if (wheelCustomizer != null && availableWheelsForBots != null && availableWheelsForBots.Length > 0)
            {
                WheelVisualSO randomWheel = availableWheelsForBots[Random.Range(0, availableWheelsForBots.Length)];
                wheelCustomizer.ApplyWheel(randomWheel);
            }

            CarShaderApplier shaderApplier = instance.GetComponentInChildren<CarShaderApplier>();
            if (shaderApplier != null && defaultBotShaders != null && defaultBotShaders.Length > 0)
            {
                CarShaderVariantSO randomShader = defaultBotShaders[Random.Range(0, defaultBotShaders.Length)];
                shaderApplier.ApplyShaderVariant(randomShader); // ← el método se llama ApplyShaderVariant, no shaderApply
            }

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
    void ConfigureSpeedometers(bool multiplayer)
    {
        if (speedometerP1 != null) speedometerP1.gameObject.SetActive(true); // P1 siempre visible

        if (speedometerP2 != null)
            speedometerP2.gameObject.SetActive(multiplayer); // P2 solo si hay multiplayer
    }
    void ConfigureNitro(bool multiplayer)
    {
        if (nitroSliderP1 != null) nitroSliderP1.gameObject.SetActive(true); // P1 siempre visible

        if (nitroSliderP2 != null)
            nitroSliderP2.gameObject.SetActive(multiplayer); // P2 solo si hay multiplayer
    }
    void ConfigureRespawnUI(bool multiplayer)
    {
        if (respawnHoldP1 != null) respawnHoldP1.gameObject.SetActive(true); // P1 siempre visible

        if (respawnHoldP2 != null)
            respawnHoldP2.gameObject.SetActive(multiplayer); // P2 solo si hay multiplayer
    }
    System.Collections.IEnumerator StartBombRoundNextFrame()
    {
        yield return null; // espera un frame, para que otros listeners de OnMapReady (como AISpawner) ya hayan corrido
        BombCarrierManager.Instance.StartBombRound();
    }
}