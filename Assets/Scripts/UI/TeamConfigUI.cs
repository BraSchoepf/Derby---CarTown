using UnityEngine;

public class TeamConfigUI : MonoBehaviour
{
    [Header("Sub-paneles")]
    public GameObject teamSizePanel;
    public GameObject teamPickPanel;

    public GameModeSO currentMode;

    public TeamId player1Team = TeamId.TeamA;
    public TeamId player2Team = TeamId.TeamB;
    public int selectedTeamSize = 2;

    void OnEnable()
    {
        // Cada vez que se abre este panel completo, arranca siempre desde la elección de tamaño
        teamSizePanel.SetActive(true);
        teamPickPanel.SetActive(false);
    }

    // --- Paso 1: tamaño ---
    public void SelectTeamSize2() => SelectTeamSize(2);
    public void SelectTeamSize3() => SelectTeamSize(3);

    void SelectTeamSize(int size)
    {
        selectedTeamSize = size;
        teamSizePanel.SetActive(false);
        teamPickPanel.SetActive(true);
    }

    // --- Paso 2: equipos ---
    public void SetPlayer1TeamA() => player1Team = TeamId.TeamA;
    public void SetPlayer1TeamB() => player1Team = TeamId.TeamB;
    public void SetPlayer2TeamA() => player2Team = TeamId.TeamA;
    public void SetPlayer2TeamB() => player2Team = TeamId.TeamB;

    // Volver de la selección de equipos al tamaño, por si se equivocó
    public void BackToTeamSize()
    {
        teamPickPanel.SetActive(false);
        teamSizePanel.SetActive(true);
    }
}