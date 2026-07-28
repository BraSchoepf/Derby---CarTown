using UnityEngine;

public class RaceResultsUI : MonoBehaviour
{
    public ResultPanelUI panelP1;
    public ResultPanelUI panelP2;

    void Start()
    {
        RaceManager.Instance.OnRaceEnded += HandleRaceEnded;
    }

    void HandleRaceEnded(System.Collections.Generic.List<RaceManager.RacerProgress> finalResults)
    {
        var p1Result = finalResults.Find(r => r.humanSlotIndex == 0);
        if (p1Result != null) panelP1.ShowRaceResult(p1Result.finishPlacement, finalResults.Count);

        var p2Result = finalResults.Find(r => r.humanSlotIndex == 1);
        if (p2Result != null) panelP2.ShowRaceResult(p2Result.finishPlacement, finalResults.Count);
    }
}