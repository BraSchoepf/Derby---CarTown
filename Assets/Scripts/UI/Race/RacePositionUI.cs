using UnityEngine;
using TMPro;

public class RacePositionUI : MonoBehaviour
{
    public TextMeshProUGUI positionText;
    RaceManager.RacerProgress trackedRacer;

    public void SetTarget(RaceManager.RacerProgress racer)
    {
        trackedRacer = racer;
    }

    void Update()
    {
        if (trackedRacer == null || RaceManager.Instance == null) return;

        int position = RaceManager.Instance.GetLivePosition(trackedRacer);
        int total = RaceManager.Instance.Racers.Count;
        positionText.text = $"{position}°";
    }
}