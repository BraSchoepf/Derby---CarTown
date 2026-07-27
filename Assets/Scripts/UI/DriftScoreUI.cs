using UnityEngine;
using TMPro;

public class DriftScoreUI : MonoBehaviour
{
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI multiplierText;

    DriftScoreTracker tracker;

    public void SetTracker(DriftScoreTracker newTracker)
    {
        if (tracker != null)
        {
            tracker.OnScoreChanged -= UpdateTotalScore;
        }

        tracker = newTracker;
        tracker.OnScoreChanged += UpdateTotalScore;
    }

    void Update()
    {
        if (tracker == null) return;
        if (multiplierText != null)
            multiplierText.text = $"x{tracker.CurrentMultiplier:F1}";
    }

    void UpdateTotalScore(float newTotal)
    {
        if (totalScoreText != null)
            totalScoreText.text = Mathf.FloorToInt(newTotal).ToString();
    }

    void OnDestroy()
    {
        if (tracker != null) tracker.OnScoreChanged -= UpdateTotalScore;
    }
}