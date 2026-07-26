using UnityEngine;
using TMPro;

public class BombTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    void Start()
    {
        BombCarrierManager.Instance.OnTimerTick += UpdateTimerDisplay;
    }

    void UpdateTimerDisplay(float secondsRemaining)
    {
        timerText.text = Mathf.CeilToInt(secondsRemaining).ToString();
    }

    void OnDestroy()
    {
        if (BombCarrierManager.Instance != null)
            BombCarrierManager.Instance.OnTimerTick -= UpdateTimerDisplay;
    }
}