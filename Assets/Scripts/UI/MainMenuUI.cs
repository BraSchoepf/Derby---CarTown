using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
        player2Cursor.gameObject.SetActive(multiplayer);
    }

    void Update()
    {
        if (!carSelectionPanel.activeSelf) return;

        bool readyToStart = player1Cursor.IsLocked && (!multiplayer || player2Cursor.IsLocked);

        if (startPromptUI != null)
            startPromptUI.SetActive(readyToStart);

        if (startButton != null)
            startButton.interactable = readyToStart;
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

        SceneManager.LoadScene(gameSceneName);
    }
}