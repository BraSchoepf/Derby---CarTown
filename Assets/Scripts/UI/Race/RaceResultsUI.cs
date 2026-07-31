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
            panelP1.ShowRaceResult(p1Result.finishPlacement, finalResults.Count, p1Result.finishTime, 0f);
            sharedBackToMenuButton.Show(); // ← mostrar acá, apenas P1 tiene resultado, sin esperar a P2
        }

        var p2Result = finalResults.Find(r => r.humanSlotIndex == 1);
        if (p2Result != null)
            panelP2.ShowRaceResult(p2Result.finishPlacement, finalResults.Count, p2Result.finishTime, 0f);
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