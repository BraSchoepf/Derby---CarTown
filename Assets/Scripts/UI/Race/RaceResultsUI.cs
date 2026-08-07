using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class RaceResultsUI : MonoBehaviour
{
    public ResultPanelUI panelP1;
    public ResultPanelUI panelP2;
    public SharedBackToMenuButton sharedBackToMenuButton;

    void Start()
    {
        RaceManager.Instance.OnRaceEnded += HandleRaceEnded;
    }

    void HandleRaceEnded(List<RaceManager.RacerProgress> finalResults)
    {
        var p1Result = finalResults.Find(r => r.humanSlotIndex == 0);
        if (p1Result != null)
        {
            ShowResultFor(p1Result, finalResults.Count, panelP1);
            sharedBackToMenuButton.Show();
        }

        var p2Result = finalResults.Find(r => r.humanSlotIndex == 1);
        if (p2Result != null)
        {
            ShowResultFor(p2Result, finalResults.Count, panelP2);
        }
    }

    void ShowResultFor(RaceManager.RacerProgress racer, int totalRacers, ResultPanelUI panel)
    {
        float raceTime = RaceManager.Instance.GetRaceTime(racer);

        float driftScore = -1f;
        GameSession session = GameSession.Instance;
        if (session != null && session.chosenGameMode != null && session.chosenGameMode.isDriftScoringMode)
        {
            DriftScoreTracker tracker = FindTrackerForSlot(racer.humanSlotIndex);
            if (tracker != null) driftScore = tracker.TotalScore;

            if (session.chosenGameMode.isDriftScoringMode && driftScore >= 0)
                MissionManager.Instance?.ReportResult(session.chosenGameMode, session.chosenMap, MissionObjectiveType.DriftScoreThreshold, driftScore, racer.humanSlotIndex);
            else
                MissionManager.Instance?.ReportResult(session.chosenGameMode, session.chosenMap, MissionObjectiveType.RaceTimeUnder, raceTime, racer.humanSlotIndex);

            MissionManager.Instance?.ReportResult(session.chosenGameMode, session.chosenMap, MissionObjectiveType.FinishPlacement, racer.finishPlacement, racer.humanSlotIndex);
        }

        panel.ShowRaceResult(racer.finishPlacement, totalRacers, raceTime, driftScore);

    }

    DriftScoreTracker FindTrackerForSlot(int humanSlotIndex)
    {
        // Busca entre todos los RaceCarIdentity de la escena el que corresponda a este jugador
        foreach (var identity in FindObjectsByType<RaceCarIdentity>(FindObjectsSortMode.None))
        {
            if (identity.Progress != null && identity.Progress.humanSlotIndex == humanSlotIndex)
                return identity.GetComponent<DriftScoreTracker>();
        }
        return null;
    }

    void OnDestroy()
    {
        if (RaceManager.Instance != null)
            RaceManager.Instance.OnRaceEnded -= HandleRaceEnded;
    }
}