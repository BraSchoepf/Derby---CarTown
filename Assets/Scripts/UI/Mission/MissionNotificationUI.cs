using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MissionNotificationUI : MonoBehaviour
{
    public GameObject notificationPanel;
    public Image rewardIcon;
    public TextMeshProUGUI rewardNameText;
    public TextMeshProUGUI missionNameText;
    public float displayDuration = 4f;

    void Start()
    {
        notificationPanel.SetActive(false);
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted += ShowNotification;
    }

    void ShowNotification(MissionSO mission)
    {
        StartCoroutine(ShowRoutine(mission));
    }

    IEnumerator ShowRoutine(MissionSO mission)
    {
        foreach (var reward in mission.rewards)
        {
            notificationPanel.SetActive(true);
            missionNameText.text = $"¡Misión completada: {mission.missionName}!";
            rewardNameText.text = reward.displayName;
            if (rewardIcon != null) rewardIcon.sprite = reward.icon;

            yield return new WaitForSeconds(displayDuration);
            notificationPanel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnMissionCompleted -= ShowNotification;
    }
}