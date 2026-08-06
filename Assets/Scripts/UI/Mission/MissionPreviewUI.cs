using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MissionPreviewUI : MonoBehaviour
{
    public GameObject container;
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI missionDescriptionText;
    public Image rewardIcon;
    public TextMeshProUGUI rewardNameText;
    public GameObject noMissionMessage; // "Sin misión disponible" o similar

    public void ShowMissionFor(GameModeSO mode, MapDataSO map)
    {
        MissionSO mission = MissionManager.Instance?.GetActiveMissionFor(mode, map);

        if (mission == null)
        {
            container.SetActive(false);
            if (noMissionMessage != null) noMissionMessage.SetActive(true);
            return;
        }

        container.SetActive(true);
        if (noMissionMessage != null) noMissionMessage.SetActive(false);

        missionNameText.text = mission.missionName;
        missionDescriptionText.text = mission.description;

        if (mission.rewards.Length > 0)
        {
            var firstReward = mission.rewards[0];
            rewardIcon.sprite = firstReward.icon;
            rewardNameText.text = firstReward.displayName;
        }
    }
}