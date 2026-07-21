using UnityEngine;
using UnityEngine.InputSystem;

public class GameSetup : MonoBehaviour
{
    [Header("Derby")]
    public DerbyGameManager derbyManager;

    [Header("UI de vida")]
    public HealthBarUI healthBarP1;
    public HealthBarUI healthBarP2;

    [System.Serializable]
    public class PlayerSlotConfig
    {
        public Camera splitScreenCamera;
        public string controlScheme;
    }

    public CarRegistry registry;
    public PlayerSlotConfig[] playerSlotConfigs; // 2 elementos, siempre definidos

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

        GameSession session = GameSession.Instance;
        bool isMultiplayer = session != null && session.selectedMode == GameMode.MultiplayerSplitScreen;

        ConfigureCameraLayout(isMultiplayer);
        ConfigureHealthBars(isMultiplayer);

        SpawnPlayer(0, session != null ? session.player1Car : null);

        if (isMultiplayer)
            SpawnPlayer(1, session.player2Car);
        else
            playerSlotConfigs[1].splitScreenCamera.gameObject.SetActive(false);
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
        Transform spawnPoint = MapLoader.Instance.GetPlayerSpawn(slotIndex);
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
            carController.playerIndex = slotIndex + 1;
            carController.SetSpawnPoint(spawnPoint.position, spawnPoint.rotation);
        }

        PlayerInput playerInput = carInstance.GetComponent<PlayerInput>();
        playerInput.actions = Instantiate(playerInput.actions);
        playerInput.camera = config.splitScreenCamera;
        playerInput.SwitchCurrentControlScheme(config.controlScheme, Keyboard.current);

        AssignCameraChannel(carInstance, slotIndex);

        VehicleHealth health = carInstance.GetComponent<VehicleHealth>();
        if (health != null && derbyManager != null)
            derbyManager.RegisterPlayer($"Player {slotIndex + 1}", health, slotIndex);

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