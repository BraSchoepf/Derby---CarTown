using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class GameSetup : MonoBehaviour
{
    [System.Serializable]
    public class PlayerSlot
    {
        public GameObject carPrefab;
        public Transform spawnPoint;
        public Camera splitScreenCamera;
        public string controlScheme;
    }

    public PlayerSlot[] playerSlots;

    void Start()
    {
        foreach (var slot in playerSlots)
        {
            GameObject carInstance = Instantiate(slot.carPrefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
            PlayerInput playerInput = carInstance.GetComponent<PlayerInput>();

            // Clonar el asset de acciones para que esta instancia tenga su propia copia,
            // no comparta el mismo objeto en memoria con los demás jugadores
            playerInput.actions = Instantiate(playerInput.actions);

            playerInput.camera = slot.splitScreenCamera;
            playerInput.SwitchCurrentControlScheme(slot.controlScheme, Keyboard.current);
        }
    }
}