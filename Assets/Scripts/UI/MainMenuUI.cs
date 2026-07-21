using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject modeSelectionPanel;
    public GameObject carSelectionPanel;
    public GameObject mapSelectionPanel;

    [Header("Car Selection UI")]
    public CarSelectionGridUI grid;
    public PlayerCarCursor player1Cursor;
    public PlayerCarCursor player2Cursor;

    [Header("Map Selection UI")]
    public MapCarouselUI mapCarousel;
    public TMPro.TextMeshProUGUI mapNameText;

    [Header("UI extra")]
    public GameObject startPromptUI;
    public Button startButton; // confirma selección de auto, pasa a selección de mapa

    [Header("Escenas")]
    public string gameplayCoreSceneName = "GameplayCore";

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

        player2Cursor.gameObject.SetActive(false);
        player2Cursor.ForceUnlock();

        if (multiplayer)
            player2Cursor.gameObject.SetActive(true);
    }

    void Update()
    {
        if (carSelectionPanel.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                player1Cursor.ForceUnlock();
                if (multiplayer) player2Cursor.ForceUnlock();
                return;
            }

            bool readyToStart = player1Cursor.IsLocked && (!multiplayer || player2Cursor.IsLocked);

            if (startPromptUI != null) startPromptUI.SetActive(readyToStart);
            if (startButton != null) startButton.interactable = readyToStart;
        }

        if (mapSelectionPanel.activeSelf && Keyboard.current != null)
        {
            bool moved = false;
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                mapCarousel.Move(-1);
                moved = true;
            }
            if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                mapCarousel.Move(1);
                moved = true;
            }

            if (moved && mapNameText != null)
                mapNameText.text = mapCarousel.CurrentMap.mapName;
        }
    }

    // Llamado por el botón de confirmar auto — pasa a selección de mapa
    public void OnConfirmCarSelection()
    {
        carSelectionPanel.SetActive(false);
        mapSelectionPanel.SetActive(true);

        if (mapNameText != null)
            mapNameText.text = mapCarousel.CurrentMap.mapName;
    }

    // Llamado por el botón "Volver a elegir modo" dentro de selección de auto
    public void OnBackToModeSelection()
    {
        player1Cursor.ForceUnlock();
        if (multiplayer) player2Cursor.ForceUnlock();

        player1Cursor.gameObject.SetActive(false);
        player2Cursor.gameObject.SetActive(false);

        carSelectionPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    // Llamado por el botón "Volver a elegir auto" dentro de selección de mapa
    public void OnBackToCarSelection()
    {
        mapSelectionPanel.SetActive(false);
        carSelectionPanel.SetActive(true);
    }

    // Llamado por el botón final "Empezar partida" dentro de selección de mapa
    public void OnConfirmMapSelection()
    {
        GameSession session = GameSession.Instance;
        if (session == null)
            session = new GameObject("GameSession").AddComponent<GameSession>();

        session.selectedMode = chosenMode;
        session.player1Car = player1Cursor.SelectedCar;
        session.player2Car = multiplayer ? player2Cursor.SelectedCar : null;
        session.player1Color = player1Cursor.SelectedColor;
        session.player2Color = multiplayer ? player2Cursor.SelectedColor : Color.white;
        session.selectedMapSceneName = mapCarousel.CurrentMap.sceneName;

        SceneManager.LoadScene(gameplayCoreSceneName);
    }
}