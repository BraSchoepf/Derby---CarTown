using UnityEngine;
using UnityEngine.SceneManagement;

public class CarSelectionManager : MonoBehaviour
{
    public PlayerCarCursor player1Cursor;
    public PlayerCarCursor player2Cursor;
    public GameObject startPromptUI;
    public string gameSceneName = "GameScene";

    bool player2Joined;

    void Update()
    {
        if (!player2Joined && AnyP2InputPressed())
            player2Joined = true;

        bool readyToStart = player1Cursor.IsLocked && (!player2Joined || player2Cursor.IsLocked);
        startPromptUI.SetActive(readyToStart);

        if (readyToStart && Input.GetKeyDown(KeyCode.Return))
            StartGame();
    }

    bool AnyP2InputPressed() =>
        Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
        Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);

    void StartGame()
    {
        var session = new GameObject("GameSession").AddComponent<GameSession>();
        session.selectedMode = player2Joined ? GameMode.MultiplayerSplitScreen : GameMode.SinglePlayer;
        session.player1Car = player1Cursor.SelectedCar;
        session.player2Car = player2Joined ? player2Cursor.SelectedCar : null;
        SceneManager.LoadScene(gameSceneName);
    }
}