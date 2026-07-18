using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject modeSelectionPanel;
    public GameObject carSelectionPanel;

    [Header("Car Selection UI")]
    public CarSelectionSlotUI player1SelectionUI;
    public CarSelectionSlotUI player2SelectionUI; // se activa solo en multiplayer

    [Header("Escenas")]
    public string gameSceneName = "GameScene";

    GameMode chosenMode;

    // --- Llamado por los botones "Single Player" / "Multiplayer" ---
    public void OnSelectSinglePlayer()
    {
        chosenMode = GameMode.SinglePlayer;
        ShowCarSelection();
    }

    public void OnSelectMultiplayer()
    {
        chosenMode = GameMode.MultiplayerSplitScreen;
        ShowCarSelection();
    }

    void ShowCarSelection()
    {
        modeSelectionPanel.SetActive(false);
        carSelectionPanel.SetActive(true);

        player1SelectionUI.gameObject.SetActive(true);
        player2SelectionUI.gameObject.SetActive(chosenMode == GameMode.MultiplayerSplitScreen);
    }

    // --- Llamado por el botón "Confirmar" / "Empezar" ---
    public void OnConfirmSelection()
    {
        GameObject sessionObj = new GameObject("GameSession");
        GameSession session = sessionObj.AddComponent<GameSession>();

        session.selectedMode = chosenMode;
        session.player1Car = player1SelectionUI.SelectedCar;
        session.player2Car = chosenMode == GameMode.MultiplayerSplitScreen
            ? player2SelectionUI.SelectedCar
            : null;

        SceneManager.LoadScene(gameSceneName);
    }
}