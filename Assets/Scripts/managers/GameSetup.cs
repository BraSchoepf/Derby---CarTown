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
        public Transform spawnPoint;
        public Camera splitScreenCamera;
        public string controlScheme;
    }

    public CarRegistry registry;
    public PlayerSlotConfig[] playerSlotConfigs; // 2 elementos, siempre definidos

    void Start()
    {
        GameSession session = GameSession.Instance;
        bool isMultiplayer = session != null && session.selectedMode == GameMode.MultiplayerSplitScreen;

        ConfigureCameraLayout(isMultiplayer);

        SpawnPlayer(playerSlotConfigs[0], session != null ? session.player1Car : null, 0);

        if (isMultiplayer)
            SpawnPlayer(playerSlotConfigs[1], session.player2Car, 1);
        else
            playerSlotConfigs[1].splitScreenCamera.gameObject.SetActive(false);
    }

    void ConfigureCameraLayout(bool isMultiplayer)
    {
        Camera cam1 = playerSlotConfigs[0].splitScreenCamera;
        Camera cam2 = playerSlotConfigs[1].splitScreenCamera;

        if (isMultiplayer)
        {
            cam1.rect = new Rect(0f, 0f, 0.5f, 1f);   // mitad IZQUIERDA
            cam2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            cam2.gameObject.SetActive(true);
        }
        else
        {
            cam1.rect = new Rect(0, 0, 1, 1);
            cam2.gameObject.SetActive(false);
        }
    }

    void SpawnPlayer(PlayerSlotConfig config, CarStatsSO carStats, int slotIndex)
    {
        GameObject prefabToSpawn = carStats != null
            ? registry.GetPrefabForStats(carStats)
            : registry.cars[0].prefab;

        GameObject carInstance = Instantiate(prefabToSpawn, config.spawnPoint.position, config.spawnPoint.rotation);

        PlayerInput playerInput = carInstance.GetComponent<PlayerInput>();
        playerInput.actions = Instantiate(playerInput.actions);
        playerInput.camera = config.splitScreenCamera;
        playerInput.SwitchCurrentControlScheme(config.controlScheme, Keyboard.current);

        // Asignar el Output Channel dinámicamente según el slot, no el prefab
        AssignCameraChannel(carInstance, slotIndex);

        VehicleHealth health = carInstance.GetComponent<VehicleHealth>();
        if (health != null && derbyManager != null)
            derbyManager.RegisterPlayer($"Player {slotIndex + 1}", health);

        HealthBarUI bar = slotIndex == 0 ? healthBarP1 : healthBarP2;
        if (bar != null && health != null)
            bar.SetTarget(health);

        MinimapIcon minimapIcon = carInstance.GetComponent<MinimapIcon>();
        if (minimapIcon != null)
            minimapIcon.SetOwner(slotIndex == 0 ? MinimapOwnerType.Player1 : MinimapOwnerType.Player2);
        else
            Debug.LogWarning($"[GameSetup] {carInstance.name} no tiene MinimapIcon — no va a aparecer en el minimapa.", this);
    }

    void AssignCameraChannel(GameObject carInstance, int slotIndex)
    {
        var vcam = carInstance.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>();
        if (vcam == null)
        {
            Debug.LogWarning($"No se encontró CinemachineCamera en {carInstance.name}");
            return;
        }

        // slotIndex + 1 para saltar el bit de "Default" y empezar en Channel01
        vcam.OutputChannel = (Unity.Cinemachine.OutputChannels)(1 << (slotIndex + 1));
        Debug.Log($"{carInstance.name} (slot {slotIndex}) → vcam '{vcam.name}' canal asignado: {vcam.OutputChannel}");
    }
}