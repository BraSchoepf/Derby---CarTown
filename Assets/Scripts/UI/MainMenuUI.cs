using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject modeSelectionPanel;
    public GameObject carSelectionPanel;

    [Header("Car Selection UI")]
    public CarSelectionGridUI grid;
    public PlayerCarCursor player1Cursor;
    public PlayerCarCursor player2Cursor;

    [Header("UI extra")]
    public GameObject startPromptUI;
    public Button startButton;

    [Header("Escenas")]
    public string gameSceneName = "GameScene";

    GameMode chosenMode;
    bool multiplayer;

    public void OnSelectSinglePlayer()
    {
        chosenMode = GameMode.SinglePlayer;
        multiplayer = false;
        ShowCarSelection();
    }

    public void OnSelectMultiplayer()
    {
        chosenMode = GameMode.MultiplayerSplitScreen;
        multiplayer = true;
        ShowCarSelection();
    }

    void ShowCarSelection()
    {
        modeSelectionPanel.SetActive(false);
        carSelectionPanel.SetActive(true);

        player1Cursor.gameObject.SetActive(true);

        // Reset explícito de P2: si venía de una ronda anterior en multiplayer,
        // hay que forzar que se apague ANTES de decidir si se vuelve a prender,
        // así su Awake()/OnEnable() corre limpio si vuelve a activarse
        player2Cursor.gameObject.SetActive(false);
        player2Cursor.ForceUnlock(); // limpia cualquier selección/ícono que haya quedado

        if (multiplayer)
            player2Cursor.gameObject.SetActive(true);
    }

    void Update()
    {
        if (!carSelectionPanel.activeSelf) return;

        // Escape global: deselecciona a AMBOS jugadores, sin importar quién lo presionó
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            player1Cursor.ForceUnlock();
            if (multiplayer) player2Cursor.ForceUnlock();
            return;
        }

        bool readyToStart = player1Cursor.IsLocked && (!multiplayer || player2Cursor.IsLocked);

        if (startPromptUI != null)
            startPromptUI.SetActive(readyToStart);

        if (startButton != null)
            startButton.interactable = readyToStart;
    }

    // Llamado por el nuevo botón "Volver a elegir modo"
    public void OnBackToModeSelection()
    {
        player1Cursor.ForceUnlock();
        if (multiplayer) player2Cursor.ForceUnlock();

        player1Cursor.gameObject.SetActive(false);
        player2Cursor.gameObject.SetActive(false);

        carSelectionPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    // Enganchar al OnClick() del botón en el Inspector
    public void OnConfirmSelection()
    {
        GameSession session = GameSession.Instance;
        if (session == null)
            session = new GameObject("GameSession").AddComponent<GameSession>();

        session.selectedMode = chosenMode;
        session.player1Car = player1Cursor.SelectedCar;
        session.player2Car = multiplayer ? player2Cursor.SelectedCar : null;
        session.player1Color = player1Cursor.SelectedColor;
        session.player2Color = multiplayer ? player2Cursor.SelectedColor : Color.white;

        SceneManager.LoadScene(gameSceneName);
    }
}