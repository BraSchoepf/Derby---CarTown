using UnityEngine;
using TMPro;

public class ResultPanelUI : MonoBehaviour
{
    [Header("Encabezados (mutuamente excluyentes)")]
    public GameObject victoryHeader;
    public GameObject defeatHeader;

    [Header("Resumen - Demolición")]
    public TextMeshProUGUI placementText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI survivalTimeText;
    public TextMeshProUGUI killedByText;

    [Header("Resumen - Race")]
    public GameObject raceResultHeader;
    public TextMeshProUGUI racePlacementText;
    public TextMeshProUGUI raceTimeText;
    public GameObject driftScoreContainer;
    public TextMeshProUGUI driftScoreText;

    [Header("Navegación")]
    public SharedBackToMenuButton sharedButton;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ShowRaceResult(int placement, int totalRacers, float raceTime, float driftScore = -1f)
    {
        gameObject.SetActive(true);
        victoryHeader.SetActive(placement == 1);
        defeatHeader.SetActive(false);
        if (killedByText != null) killedByText.gameObject.SetActive(false);
        if (raceResultHeader != null) raceResultHeader.SetActive(true);

        if (racePlacementText != null)
            racePlacementText.text = $"{placement}° de {totalRacers}";

        if (raceTimeText != null)
        {
            int minutes = Mathf.FloorToInt(raceTime / 60f);
            int seconds = Mathf.FloorToInt(raceTime % 60f);
            int millis = Mathf.FloorToInt((raceTime * 1000f) % 1000f);
            raceTimeText.text = $"Tiempo: {minutes:00}:{seconds:00}.{millis:000}";
        }

        bool showDrift = driftScore >= 0f;
        if (driftScoreContainer != null) driftScoreContainer.SetActive(showDrift);
        if (showDrift && driftScoreText != null)
            driftScoreText.text = $"Puntaje Drift: {Mathf.RoundToInt(driftScore)}";

        sharedButton?.Show();
    }

    public void ShowVictory(DerbyGameManager.PlayerEntry entry)
    {
        gameObject.SetActive(true);
        victoryHeader.SetActive(true);
        defeatHeader.SetActive(false);
        killedByText.gameObject.SetActive(false);
        FillSummary(entry);
        sharedButton?.Show();
    }

    public void ShowDefeat(DerbyGameManager.PlayerEntry entry)
    {
        gameObject.SetActive(true);
        victoryHeader.SetActive(false);
        defeatHeader.SetActive(true);
        killedByText.gameObject.SetActive(true);
        killedByText.text = $"Eliminado por: {entry.killedByName}";
        FillSummary(entry);
        sharedButton?.Show();
    }

    void FillSummary(DerbyGameManager.PlayerEntry entry)
    {
        placementText.text = $"Puesto {entry.placement}°";
        killsText.text = $"Eliminaciones: {entry.killCount}";
        float survived = Time.time - entry.survivalStartTime;
        int minutes = Mathf.FloorToInt(survived / 60f);
        int seconds = Mathf.FloorToInt(survived % 60f);
        survivalTimeText.text = $"Tiempo en pie: {minutes:00}:{seconds:00}";
    }
}