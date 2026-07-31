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

    public System.Action OnConfirmed;
    public System.Action OnCancelled;

    void OnEnable()
    {
        teamSizePanel.SetActive(true);
        teamPickPanel.SetActive(false);
    }

    public void SelectTeamSize2() => SelectTeamSize(2);
    public void SelectTeamSize3() => SelectTeamSize(3);

    void SelectTeamSize(int size)
    {
        selectedTeamSize = size;
        teamSizePanel.SetActive(false);
        teamPickPanel.SetActive(true);
    }

    public void SetPlayer1TeamA() => player1Team = TeamId.TeamA;
    public void SetPlayer1TeamB() => player1Team = TeamId.TeamB;
    public void SetPlayer2TeamA() => player2Team = TeamId.TeamA;
    public void SetPlayer2TeamB() => player2Team = TeamId.TeamB;

    public void BackToTeamSize()
    {
        teamPickPanel.SetActive(false);
        teamSizePanel.SetActive(true);
    }

    public void OnConfirmClicked()
    {
        OnConfirmed?.Invoke();
    }

    public void OnCancelClicked()
    {
        OnCancelled?.Invoke();
    }
}