using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject modeSelectionPanel;
    public GameObject categorySelectionPanel;  // Racing / Demolition (nuevo)
    public GameObject modeTypeSelectionPanel;
    public GameObject teamConfigPanel;
    public GameObject carSelectionPanel;
    public GameObject mapSelectionPanel;

    [Header("Datos de modos")]
    public GameModeSO[] allGameModes;

    [Header("Team Config UI")]
    public TeamConfigUI teamConfig;

    [Header("Mode Type UI")]
    public GameModeTypeListUI modeTypeList;

    [Header("Car Selection UI")]
    public CarSelectionGridUI grid;
    public PlayerCarCursor player1Cursor;
    public PlayerCarCursor player2Cursor;

    [Header("Car Preview Layout")]
    public CarPreviewLayoutUI previewLayout;

    [Header("Map Selection UI")]
    public MapCarouselUI mapCarousel;
    public TMPro.TextMeshProUGUI mapNameText;

    [Header("UI extra")]
    public GameObject startPromptUI;
    public Button startButton; // confirma selección de auto, pasa a selección de mapa

    [Header("Escenas")]
    public string gameplayCoreSceneName = "GameplayCore";
    public string raceCoreSceneName = "RaceCore";

    GameMode chosenMode;
    bool multiplayer;
    GameModeSO chosenGameMode;

    public void OnSelectSinglePlayer()
    {
        chosenMode = GameMode.SinglePlayer;
        multiplayer = false;
        ShowCategorySelection(); // antes decía ShowCarSelection()
    }

    public void OnSelectMultiplayer()
    {
        chosenMode = GameMode.MultiplayerSplitScreen;
        multiplayer = true;
        ShowCategorySelection();
    }

    public void OnSelectCategory(int categoryIndex) // 0 = Racing, 1 = Demolition, conectado desde 2 botones
    {
        GameModeCategory category = (GameModeCategory)categoryIndex;
        var filteredModes = System.Array.FindAll(allGameModes, m => m.category == category);

        modeTypeList.PopulateModes(filteredModes, OnSelectGameModeType);

        categorySelectionPanel.SetActive(false);
        modeTypeSelectionPanel.SetActive(true);
    }

    void ShowCategorySelection()
    {
        modeSelectionPanel.SetActive(false);
        categorySelectionPanel.SetActive(true);
    }

    void OnSelectGameModeType(GameModeSO mode)
    {
        Debug.Log($"[MainMenuUI] Modo elegido: {mode.modeName}, supportsTeams: {mode.supportsTeams}, multiplayer: {multiplayer}");
        chosenGameMode = mode;
        modeTypeSelectionPanel.SetActive(false);

        if (mode.supportsTeams && multiplayer)
        {
            teamConfig.currentMode = mode;
            teamConfigPanel.SetActive(true);
        }
        else
        {
            ShowCarSelection();
        }
    }

    public void OnConfirmTeamConfig()
    {
        teamConfigPanel.SetActive(false);
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

        previewLayout.ConfigureLayout(multiplayer);
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

        mapCarousel.SetAvailableMaps(chosenGameMode);

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
        GameSession session = GameSession.Instance ?? new GameObject("GameSession").AddComponent<GameSession>();

        session.selectedMode = chosenMode;
        session.chosenGameMode = chosenGameMode;
        session.player1Car = player1Cursor.SelectedCar;
        session.player2Car = multiplayer ? player2Cursor.SelectedCar : null;
        session.player1Color = player1Cursor.SelectedColor;
        session.player2Color = multiplayer ? player2Cursor.SelectedColor : Color.white;
        session.selectedMapSceneName = mapCarousel.CurrentMap.sceneName;

        if (chosenGameMode.supportsTeams)
        {
            session.player1Team = teamConfig.player1Team;
            session.player2Team = teamConfig.player2Team;
            session.teamSize = teamConfig.selectedTeamSize;
        }

        string targetScene = chosenGameMode.category == GameModeCategory.Racing
            ? raceCoreSceneName
            : gameplayCoreSceneName;
        SceneManager.LoadScene(targetScene);
    }
}